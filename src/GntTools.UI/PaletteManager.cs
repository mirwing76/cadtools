using System;
using Autodesk.AutoCAD.Windows;
using GntTools.UI.Controls;
using GntTools.UI.ViewModels;

namespace GntTools.UI
{
    /// <summary>PaletteSet 싱글 인스턴스 관리</summary>
    public static class PaletteManager
    {
        private static readonly Guid PaletteGuid =
            new Guid("B7E3F1A2-4D5C-6E7F-8901-ABCDEF012345");

        private static PaletteSet _ps;

        public static WtlViewModel WtlVm { get; private set; }
        public static SwlViewModel SwlVm { get; private set; }
        public static KepcoViewModel KepcoVm { get; private set; }
        public static SettingsViewModel SettingsVm { get; private set; }

        public static PaletteSet PaletteSet => _ps;

        /// <summary>팔레트 초기화 (최초 1회)</summary>
        public static void Initialize()
        {
            if (_ps != null) return;

            _ps = new PaletteSet("GNT Tools", PaletteGuid);
            _ps.Style = PaletteSetStyles.ShowAutoHideButton
                      | PaletteSetStyles.ShowCloseButton
                      | PaletteSetStyles.ShowPropertiesMenu;
            _ps.MinimumSize = new System.Drawing.Size(280, 400);
            _ps.DockEnabled = DockSides.Left | DockSides.Right;

            WtlVm = new WtlViewModel();
            SwlVm = new SwlViewModel();
            KepcoVm = new KepcoViewModel();
            SettingsVm = new SettingsViewModel();

            _ps.AddVisual("상수", new WtlPanel { DataContext = WtlVm });
            _ps.AddVisual("하수", new SwlPanel { DataContext = SwlVm });
            _ps.AddVisual("전력통신", new KepcoPanel { DataContext = KepcoVm });
            _ps.AddVisual("환경설정", new SettingsPanel { DataContext = SettingsVm });
        }

        /// <summary>팔레트 표시/토글</summary>
        public static void Toggle()
        {
            Initialize();
            _ps.Visible = !_ps.Visible;
        }

        /// <summary>팔레트 표시</summary>
        public static void Show()
        {
            Initialize();
            _ps.Visible = true;
        }

        /// <summary>특정 탭 활성화</summary>
        public static void ActivateTab(int index)
        {
            Initialize();
            _ps.Visible = true;
            if (index >= 0 && index < _ps.Count)
                _ps.Activate(index);
        }
    }
}
