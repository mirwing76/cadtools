using Autodesk.AutoCAD.Runtime;

[assembly: ExtensionApplication(typeof(GntTools.UI.Plugin))]
[assembly: CommandClass(typeof(GntTools.UI.Commands.PaletteCommands))]
[assembly: CommandClass(typeof(GntTools.Wtl.WtlCommands))]
[assembly: CommandClass(typeof(GntTools.Swl.SwlCommands))]
[assembly: CommandClass(typeof(GntTools.Kepco.KepcoCommands))]

namespace GntTools.UI
{
    /// <summary>AutoCAD 플러그인 진입점</summary>
    public class Plugin : IExtensionApplication
    {
        public void Initialize()
        {
            var ed = Autodesk.AutoCAD.ApplicationServices
                .Application.DocumentManager.MdiActiveDocument.Editor;
            var ver = System.Reflection.Assembly.GetExecutingAssembly().GetName().Version;
            ed.WriteMessage($"\nGntTools v{ver.Major}.{ver.Minor}.{ver.Build} 로드됨. GNTTOOLS_SHOW로 팔레트를 엽니다.\n");
        }

        public void Terminate()
        {
            Core.Settings.AppSettings.Instance.Save();
        }
    }
}
