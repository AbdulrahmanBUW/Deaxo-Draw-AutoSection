using System.Collections.Generic;
using System.Linq;
using System.Windows;
using Autodesk.Revit.DB;
using Deaxo.SectionGenerator.Commands;

namespace Deaxo.SectionGenerator.UI
{
    public partial class SectionResultsWindow : Window
    {
        public SectionResultsWindow(List<SectionResult> results)
        {
            InitializeComponent();

            // Transform results for DataGrid display
            var displayResults = results.Select(r => new SectionResultDisplay
            {
                Category = r.Category,
                TypeName = r.TypeName,
                ElementIdString = r.ElementId.IntegerValue.ToString(),
                SheetIdString = r.SheetId.IntegerValue.ToString(),
                SectionIdString = r.SectionId.IntegerValue.ToString()
            }).ToList();

            ResultsDataGrid.ItemsSource = displayResults;
        }

        private void Close_Click(object sender, RoutedEventArgs e)
        {
            this.Close();
        }
    }

    /// <summary>
    /// Display model for the results table with string representations of ElementIds
    /// </summary>
    public class SectionResultDisplay
    {
        public string Category { get; set; }
        public string TypeName { get; set; }
        public string ElementIdString { get; set; }
        public string SheetIdString { get; set; }
        public string SectionIdString { get; set; }
    }
}