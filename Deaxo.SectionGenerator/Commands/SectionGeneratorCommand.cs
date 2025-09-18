using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows;
using Autodesk.Revit.Attributes;
using Autodesk.Revit.DB;
using Autodesk.Revit.UI;
using Autodesk.Revit.UI.Selection;
using Deaxo.SectionGenerator.UI;
using Deaxo.SectionGenerator.Commands;

namespace Deaxo.SectionGenerator.Commands
{
    [Transaction(TransactionMode.Manual)]
    public class SectionGeneratorCommand : IExternalCommand
    {
        public Result Execute(ExternalCommandData commandData, ref string message, ElementSet elements)
        {
            UIApplication uiapp = commandData.Application;
            UIDocument uidoc = uiapp.ActiveUIDocument;
            Document doc = uidoc.Document;

            try
            {
                // 1) Category choices (same as auto elevation)
                var selectOpts = new Dictionary<string, object>()
                {
                    {"Walls"                 , BuiltInCategory.OST_Walls},
                    {"Windows"               , BuiltInCategory.OST_Windows},
                    {"Doors"                 , BuiltInCategory.OST_Doors},
                    {"Columns"               , new BuiltInCategory[] { BuiltInCategory.OST_Columns, BuiltInCategory.OST_StructuralColumns } },
                    {"Beams/Framing"         , BuiltInCategory.OST_StructuralFraming},
                    {"Furniture"             , new BuiltInCategory[] { BuiltInCategory.OST_Furniture, BuiltInCategory.OST_FurnitureSystems } },
                    {"Plumbing Fixtures"     , new BuiltInCategory[] { BuiltInCategory.OST_Furniture, BuiltInCategory.OST_PlumbingFixtures } },
                    {"Generic Models"        , BuiltInCategory.OST_GenericModel},
                    {"Casework"              , BuiltInCategory.OST_Casework},
                    {"Curtain Walls"         , BuiltInCategory.OST_Walls},
                    {"Lighting Fixtures"     , BuiltInCategory.OST_LightingFixtures},
                    {"Mass"                  , BuiltInCategory.OST_Mass},
                    {"Parking"               , BuiltInCategory.OST_Parking},
                    {"All Loadable Families" , typeof(FamilyInstance)},
                    {"Electrical Fixtures, Equipment, Circuits",
                        new BuiltInCategory[] {BuiltInCategory.OST_ElectricalFixtures, BuiltInCategory.OST_ElectricalEquipment }}
                };

                // 2) UI: select categories
                var selectWindow = new SelectFromDictWindow(selectOpts.Keys.ToList(),
                    "DEAXO - Select Categories", allowMultiple: true);
                bool? res = selectWindow.ShowDialog();
                if (res != true || selectWindow.SelectedItems.Count == 0)
                {
                    TaskDialog.Show("DEAXO", "No Category was selected. Cancelled.");
                    return Result.Cancelled;
                }

                // convert selected keys to list of allowed types/categories
                var allowedTypesOrCats = new List<object>();
                foreach (var key in selectWindow.SelectedItems)
                {
                    var val = selectOpts[key];
                    if (val is BuiltInCategory bic) allowedTypesOrCats.Add(bic);
                    else if (val is BuiltInCategory[] bicArr)
                        allowedTypesOrCats.AddRange(bicArr.Cast<object>());
                    else allowedTypesOrCats.Add(val);
                }

                // 3) Selection: prompt user to select elements with filter
                var selFilter = new SectionSelectionFilter(allowedTypesOrCats);
                IList<Reference> refs = null;
                try
                {
                    refs = uidoc.Selection.PickObjects(ObjectType.Element, selFilter, "Select elements and click Finish");
                }
                catch (OperationCanceledException)
                {
                    TaskDialog.Show("DEAXO", "Selection cancelled.");
                    return Result.Cancelled;
                }

                if (refs == null || refs.Count == 0)
                {
                    TaskDialog.Show("DEAXO", "No elements selected.");
                    return Result.Cancelled;
                }

                // 4) Get view templates for optional selection
                var allViews = new FilteredElementCollector(doc).OfClass(typeof(View)).Cast<View>().ToList();
                var viewTemplates = allViews.Where(v => v.IsTemplate).ToList();

                // ask template (single-select)
                View chosenTemplate = null;
                if (viewTemplates.Count > 0)
                {
                    var templateNames = viewTemplates.Select(v => v.Name).ToList();
                    templateNames.Insert(0, "None"); // Add "None" option

                    var templateWindow = new SelectFromDictWindow(templateNames,
                        "Select ViewTemplate for Sections", allowMultiple: false);
                    bool? tr = templateWindow.ShowDialog();
                    if (tr == true && templateWindow.SelectedItems.Count > 0)
                    {
                        var name = templateWindow.SelectedItems[0];
                        if (name != "None")
                        {
                            chosenTemplate = viewTemplates.FirstOrDefault(v => v.Name == name);
                        }
                    }
                }

                // 5) Transaction: create cross-sections only (no sheets)
                var results = new List<SectionResult>();
                using (Transaction t = new Transaction(doc, "DEAXO - Generate Cross-Sections"))
                {
                    t.Start();

                    foreach (var r in refs)
                    {
                        try
                        {
                            Element el = doc.GetElement(r);
                            var props = new SectionElementProperties(doc, el);

                            if (!props.IsValid) continue;

                            // create only the cross-section (no elevation, no plan)
                            var sectionGen = new SectionViewGenerator(doc, props.Origin, props.Vector,
                                props.Width, props.Height, props.offset(), props.Depth, props.offset());

                            var crossSection = sectionGen.CreateCrossSectionOnly(props.TypeName, el.Id.IntegerValue);
                            if (crossSection == null) continue;

                            // apply view template if selected
                            if (chosenTemplate != null)
                                crossSection.ViewTemplateId = chosenTemplate.Id;

                            results.Add(new SectionResult
                            {
                                Category = el.Category?.Name ?? "Unknown",
                                TypeName = props.TypeName ?? el.Category?.Name ?? "Element",
                                ElementId = el.Id,
                                SheetId = ElementId.InvalidElementId, // No sheet
                                SectionId = crossSection.Id
                            });
                        }
                        catch (Exception)
                        {
                            // swallow per-element errors but could log if needed
                        }
                    }

                    t.Commit();
                }

                // show results table
                if (results.Count > 0)
                {
                    var resultsWindow = new SectionResultsWindow(results);
                    resultsWindow.ShowDialog();
                }
                else
                {
                    TaskDialog.Show("DEAXO - Section Generator", "No cross-sections were created.");
                }

                return Result.Succeeded;
            }
            catch (Exception ex)
            {
                message = ex.Message;
                return Result.Failed;
            }
        }
    }

    public class SectionResult
    {
        public string Category { get; set; }
        public string TypeName { get; set; }
        public ElementId ElementId { get; set; }
        public ElementId SheetId { get; set; }
        public ElementId SectionId { get; set; }
    }
}