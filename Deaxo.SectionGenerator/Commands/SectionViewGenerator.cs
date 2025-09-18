using System;
using Autodesk.Revit.DB;

namespace Deaxo.SectionGenerator.Commands
{
    public class SectionViews
    {
        public ViewSection elevation;
        public ViewSection cross;
        public ViewSection plan;
    }

    /// <summary>
    /// Section generator that mirrors the Python SectionGenerator class functionality.
    /// Creates elevation, cross-section, and plan views for elements.
    /// </summary>
    public class SectionViewGenerator
    {
        private readonly Document _doc;
        private readonly XYZ _origin;
        private readonly XYZ _vector;
        private readonly double _width;
        private readonly double _height;
        private readonly double _offset;
        private readonly double _depth;
        private readonly double _depthOffset;

        public SectionViewGenerator(Document doc, XYZ origin, XYZ vector, double width = 1, double height = 1,
            double offset = 1, double depth = 1, double depthOffset = 1)
        {
            _doc = doc;
            _origin = origin;
            _vector = vector;
            _width = width;
            _height = height;
            _offset = offset;
            _depth = depth;
            _depthOffset = depthOffset;
        }

        /// <summary>
        /// Create transform for section view orientation based on mode.
        /// Mirrors the Python create_transform method.
        /// </summary>
        private Transform CreateTransform(string mode = "elevation")
        {
            var trans = Transform.Identity;
            trans.Origin = _origin;

            var vector = _vector.Normalize();

            switch (mode.ToLower())
            {
                case "elevation":
                    trans.BasisX = vector;
                    trans.BasisY = XYZ.BasisZ;
                    trans.BasisZ = vector.CrossProduct(XYZ.BasisZ);
                    break;

                case "cross":
                    var vectorCross = vector.CrossProduct(XYZ.BasisZ);
                    trans.BasisX = vectorCross;
                    trans.BasisY = XYZ.BasisZ;
                    trans.BasisZ = vectorCross.CrossProduct(XYZ.BasisZ);
                    break;

                case "plan":
                    trans.BasisX = -vector;
                    trans.BasisY = -(XYZ.BasisZ.CrossProduct(-vector)).Normalize();
                    trans.BasisZ = -XYZ.BasisZ;
                    break;
            }

            return trans;
        }

        /// <summary>
        /// Create section bounding box based on mode.
        /// Mirrors the Python create_section_box method.
        /// </summary>
        private BoundingBoxXYZ CreateSectionBox(string mode = "elevation")
        {
            var sectionBox = new BoundingBoxXYZ();
            var trans = CreateTransform(mode);

            double wHalf = _width / 2.0;
            double hHalf = _height / 2.0;
            double dHalf = _depth / 2.0;

            switch (mode.ToLower())
            {
                case "elevation":
                    double half = _width / 2.0;
                    sectionBox.Min = new XYZ(-half - _offset, -hHalf - _offset, 0);
                    sectionBox.Max = new XYZ(half + _offset, hHalf + _offset, dHalf + _offset);
                    break;

                case "cross":
                    sectionBox.Min = new XYZ(-dHalf - _offset, -hHalf - _offset, 0);
                    sectionBox.Max = new XYZ(dHalf + _offset, hHalf + _offset, wHalf + _offset);
                    break;

                case "plan":
                    sectionBox.Min = new XYZ(-wHalf - _offset, -dHalf - _offset, 0);
                    sectionBox.Max = new XYZ(wHalf + _offset, dHalf + _offset, hHalf + _offset);
                    break;
            }

            sectionBox.Transform = trans;
            return sectionBox;
        }

        /// <summary>
        /// Rename view with fallback for duplicate names.
        /// Mirrors the Python rename_view method.
        /// </summary>
        private void RenameView(View view, string newName)
        {
            if (view == null) return;
            for (int i = 0; i < 10; ++i)
            {
                try
                {
                    view.Name = newName;
                    break;
                }
                catch
                {
                    newName += "*";
                }
            }
        }

        /// <summary>
        /// Create only cross-section view for the element.
        /// Does not create elevation or plan views.
        /// </summary>
        /// <param name="viewNameBase">Base name for the view</param>
        /// <param name="elementId">Element ID for unique naming</param>
        /// <returns>ViewSection for cross-section only</returns>
        public ViewSection CreateCrossSectionOnly(string viewNameBase, int elementId)
        {
            // Create only cross-section box
            var sectionBoxCross = CreateSectionBox("cross");

            // Get section type
            ElementId sectionTypeId = _doc.GetDefaultElementTypeId(ElementTypeGroup.ViewTypeSection);
            if (sectionTypeId == null || sectionTypeId == ElementId.InvalidElementId)
                throw new Exception("No section view type available in document.");

            // Create only cross-section
            var sectionCross = ViewSection.CreateSection(_doc, sectionTypeId, sectionBoxCross);

            // Create view name
            string newNameCross = $"{viewNameBase}_{elementId}_Cross";

            // Rename view
            RenameView(sectionCross, newNameCross);

            return sectionCross;
        }

        /// <summary>
        /// Create elevation, cross-section, and plan views for the element.
        /// Mirrors the Python create_sections method.
        /// </summary>
        /// <param name="viewNameBase">Base name for the views</param>
        /// <param name="elementId">Element ID for unique naming</param>
        /// <returns>SectionViews containing all three view types</returns>
        public SectionViews CreateSections(string viewNameBase, int elementId)
        {
            // Create section boxes
            var sectionBoxElev = CreateSectionBox("elevation");
            var sectionBoxCross = CreateSectionBox("cross");
            var sectionBoxPlan = CreateSectionBox("plan");

            // Get section type
            ElementId sectionTypeId = _doc.GetDefaultElementTypeId(ElementTypeGroup.ViewTypeSection);
            if (sectionTypeId == null || sectionTypeId == ElementId.InvalidElementId)
                throw new Exception("No section view type available in document.");

            // Create sections
            var sectionElev = ViewSection.CreateSection(_doc, sectionTypeId, sectionBoxElev);
            var sectionCross = ViewSection.CreateSection(_doc, sectionTypeId, sectionBoxCross);
            var sectionPlan = ViewSection.CreateSection(_doc, sectionTypeId, sectionBoxPlan);

            // Create view names
            string newNameElev = $"{viewNameBase}_{elementId}_Elevation";
            string newNameCross = $"{viewNameBase}_{elementId}_Cross";
            string newNamePlan = $"{viewNameBase}_{elementId}_Plan";

            // Rename views
            RenameView(sectionElev, newNameElev);
            RenameView(sectionCross, newNameCross);
            RenameView(sectionPlan, newNamePlan);

            return new SectionViews
            {
                elevation = sectionElev,
                cross = sectionCross,
                plan = sectionPlan
            };
        }
    }
}