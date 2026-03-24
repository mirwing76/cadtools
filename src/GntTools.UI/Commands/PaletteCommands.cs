using Autodesk.AutoCAD.Runtime;

namespace GntTools.UI.Commands
{
    public class PaletteCommands
    {
        [CommandMethod("GNTTOOLS_SHOW")]
        public void ShowPalette()
        {
            PaletteManager.Toggle();
        }

        [CommandMethod("GNTTOOLS_SETTINGS")]
        public void ShowSettings()
        {
            int lastTab = (PaletteManager.PaletteSet?.Count ?? 1) - 1;
            PaletteManager.ActivateTab(lastTab);
        }

        [CommandMethod("GNTBLOCKS")]
        public void ShowBlockBrowser()
        {
            BlockBrowser.BlockBrowserPaletteManager.Toggle();
        }
    }
}
