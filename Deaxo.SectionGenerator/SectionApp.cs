using Autodesk.Revit.UI;
using System;
using System.IO;
using System.Reflection;
using System.Drawing;
using System.Windows.Interop;
using System.Windows.Media.Imaging;

namespace Deaxo.SectionGenerator
{
    public class SectionApp : IExternalApplication
    {
        public Result OnStartup(UIControlledApplication application)
        {
            try
            {
                string tabName = "DEAXO Sections";

                // Create Ribbon tab if it doesn't exist
                try { application.CreateRibbonTab(tabName); } catch { }

                // Create Ribbon panel
                RibbonPanel panel = application.CreateRibbonPanel(tabName, "Section Generator");

                // PushButton data
                string assemblyPath = Assembly.GetExecutingAssembly().Location;
                var buttonData = new PushButtonData(
                    "SectionGenerator",
                    "Section\nGenerator",
                    assemblyPath,
                    "Deaxo.SectionGenerator.Commands.SectionGeneratorCommand"
                );
                buttonData.ToolTip = "Generate cross-sections for selected elements and place on sheets";

                // Load large and small icons using GDI → WPF conversion
                buttonData.LargeImage = LoadBitmapFromEmbeddedResource("Deaxo.SectionGenerator.Resources.section32.png");
                buttonData.Image = LoadBitmapFromEmbeddedResource("Deaxo.SectionGenerator.Resources.section16.png");

                // Add button to panel
                panel.AddItem(buttonData);

                return Result.Succeeded;
            }
            catch (Exception ex)
            {
                TaskDialog.Show("DEAXO - Section Ribbon Error", ex.Message);
                return Result.Failed;
            }
        }

        public Result OnShutdown(UIControlledApplication application)
        {
            return Result.Succeeded;
        }

        /// <summary>
        /// Loads an embedded PNG and converts it to BitmapSource for Revit ribbon.
        /// </summary>
        /// <param name="resourceName">Namespace + folder + filename</param>
        /// <returns>BitmapSource or null</returns>
        private BitmapSource LoadBitmapFromEmbeddedResource(string resourceName)
        {
            var assembly = Assembly.GetExecutingAssembly();
            using (Stream stream = assembly.GetManifestResourceStream(resourceName))
            {
                if (stream == null) return null;

                using (var bmp = new Bitmap(stream))
                {
                    var bitmapSource = Imaging.CreateBitmapSourceFromHBitmap(
                        bmp.GetHbitmap(),
                        IntPtr.Zero,
                        System.Windows.Int32Rect.Empty,
                        BitmapSizeOptions.FromWidthAndHeight(bmp.Width, bmp.Height)
                    );
                    bitmapSource.Freeze();
                    return bitmapSource;
                }
            }
        }
    }
}