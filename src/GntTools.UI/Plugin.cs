using Autodesk.AutoCAD.Runtime;
using AcApp = Autodesk.AutoCAD.ApplicationServices.Application;

[assembly: ExtensionApplication(typeof(GntTools.UI.Plugin))]
[assembly: CommandClass(typeof(GntTools.UI.Commands.PaletteCommands))]

namespace GntTools.UI
{
    /// <summary>AutoCAD 플러그인 진입점</summary>
    public class Plugin : IExtensionApplication
    {
        public void Initialize()
        {
            var ed = AcApp.DocumentManager.MdiActiveDocument.Editor;
            var ver = System.Reflection.Assembly.GetExecutingAssembly().GetName().Version;
            ed.WriteMessage($"\nGntTools v{ver.Major}.{ver.Minor}.{ver.Build} loaded. Use GNTTOOLS_SHOW to open palette.\n");

            // COLORTHEME 변경 감지
            AcApp.SystemVariableChanged += OnSystemVariableChanged;
        }

        public void Terminate()
        {
            AcApp.SystemVariableChanged -= OnSystemVariableChanged;

            // Save palette state before exit
            PaletteManager.WtlVm?.SaveState();
            PaletteManager.SwlVm?.SaveState();
            PaletteManager.KepcoVm?.SaveState();
            Core.Settings.AppSettings.Instance.Save();
        }

        private void OnSystemVariableChanged(object sender,
            Autodesk.AutoCAD.ApplicationServices.SystemVariableChangedEventArgs e)
        {
            if (e.Name.Equals("COLORTHEME", System.StringComparison.OrdinalIgnoreCase))
            {
                PaletteManager.ReapplyTheme();
                BlockBrowser.BlockBrowserPaletteManager.ReapplyTheme();
            }
        }
    }
}
