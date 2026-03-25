using System;
using Autodesk.AutoCAD.Windows;

namespace GntTools.UI.BlockBrowser
{
    public static class BlockBrowserPaletteManager
    {
        private static readonly Guid PaletteGuid =
            new Guid("C8F4A2B3-5E6D-7F80-9012-BCDE01234567");

        private static PaletteSet _ps;
        private static BlockBrowserViewModel _vm;

        public static void Toggle()
        {
            Initialize();
            _ps.Visible = !_ps.Visible;
            if (_ps.Visible)
                _vm.Refresh();
        }

        private static void Initialize()
        {
            if (_ps != null) return;

            _ps = new PaletteSet("블록 브라우저", PaletteGuid);
            _ps.Style = PaletteSetStyles.ShowAutoHideButton
                      | PaletteSetStyles.ShowCloseButton;
            _ps.MinimumSize = new System.Drawing.Size(250, 300);
            _ps.DockEnabled = DockSides.Left | DockSides.Right;

            _vm = new BlockBrowserViewModel();
            var panel = new BlockBrowserPanel { DataContext = _vm };
            ThemeHelper.ApplyTheme(panel);
            _ps.AddVisual("블록", panel);
        }
    }
}
