using Autodesk.AutoCAD.ApplicationServices;
using Autodesk.AutoCAD.EditorInput;
using Autodesk.AutoCAD.Runtime;
using GntTools.Core.Selection;
using GntTools.Core.Settings;

namespace GntTools.Wtl
{
    public class WtlCommands
    {
        private static readonly WtlService _service = new WtlService();

        [CommandMethod("GNTTOOLS_WTL_ATT")]
        public void WtlAttach()
        {
            var doc = Application.DocumentManager.MdiActiveDocument;
            var ed = doc.Editor;

            _service.EnsureTables();

            var input = CollectInputFromPrompt(ed);
            if (input == null) return;

            var selector = new EntitySelector();
            var polyId = selector.SelectOne(
                EntitySelector.PolylineFilter(),
                "\n상수관로 폴리라인을 선택하세요: ");

            if (polyId.IsNull)
            {
                ed.WriteMessage("\n선택이 취소되었습니다.");
                return;
            }

            _service.CreateAttribute(input, polyId);
        }

        [CommandMethod("GNTTOOLS_WTL_EDIT")]
        public void WtlEdit()
        {
            var doc = Application.DocumentManager.MdiActiveDocument;
            var ed = doc.Editor;

            _service.EnsureTables();

            var selector = new EntitySelector();
            var polyId = selector.SelectOne(
                EntitySelector.PolylineFilter(),
                "\n수정할 상수관로 폴리라인을 선택하세요: ");

            if (polyId.IsNull)
            {
                ed.WriteMessage("\n선택이 취소되었습니다.");
                return;
            }

            var input = CollectInputFromPrompt(ed);
            if (input == null) return;

            _service.ModifyAttribute(input, polyId);
        }

        private WtlInputData CollectInputFromPrompt(Editor ed)
        {
            var settings = AppSettings.Instance;
            var input = new WtlInputData();

            var prYear = ed.GetString($"\n설치년도 <{settings.Wtl.Defaults.Year}>: ");
            if (prYear.Status == PromptStatus.Cancel) return null;
            input.InstallYear = string.IsNullOrEmpty(prYear.StringResult)
                ? settings.Wtl.Defaults.Year : prYear.StringResult;

            var prMat = ed.GetString($"\n관재질 <{settings.Wtl.Defaults.Material}>: ");
            if (prMat.Status == PromptStatus.Cancel) return null;
            input.Material = string.IsNullOrEmpty(prMat.StringResult)
                ? settings.Wtl.Defaults.Material : prMat.StringResult;

            var prDia = ed.GetString($"\n구경 <{settings.Wtl.Defaults.Diameter}>: ");
            if (prDia.Status == PromptStatus.Cancel) return null;
            input.Diameter = string.IsNullOrEmpty(prDia.StringResult)
                ? settings.Wtl.Defaults.Diameter : prDia.StringResult;

            var prDepthMode = new PromptKeywordOptions("\n심도 입력방식 [자동(A)/수동(M)/불탐(U)] <M>: ");
            prDepthMode.Keywords.Add("Auto");
            prDepthMode.Keywords.Add("Manual");
            prDepthMode.Keywords.Add("Undetected");
            prDepthMode.Keywords.Default = "Manual";
            var depthResult = ed.GetKeywords(prDepthMode);
            if (depthResult.Status == PromptStatus.Cancel) return null;

            switch (depthResult.StringResult)
            {
                case "Auto":
                    input.IsAutoDepth = true;
                    break;
                case "Undetected":
                    input.IsUndetected = true;
                    break;
                default:
                    var prBeg = ed.GetDouble("\n시점심도: ");
                    if (prBeg.Status != PromptStatus.OK) return null;
                    input.ManualBeginDepth = prBeg.Value;

                    var prEnd = ed.GetDouble("\n종점심도: ");
                    if (prEnd.Status != PromptStatus.OK) return null;
                    input.ManualEndDepth = prEnd.Value;
                    break;
            }

            var prSbk = ed.GetDouble("\n시점지반고 <0>: ");
            input.BeginGroundHeight = prSbk.Status == PromptStatus.OK ? prSbk.Value : 0;
            var prSbl = ed.GetDouble("\n종점지반고 <0>: ");
            input.EndGroundHeight = prSbl.Status == PromptStatus.OK ? prSbl.Value : 0;

            var prLabel = new PromptKeywordOptions("\n라벨방식 [지시선(L)/평행(P)] <P>: ");
            prLabel.Keywords.Add("Leader");
            prLabel.Keywords.Add("Parallel");
            prLabel.Keywords.Default = "Parallel";
            var labelResult = ed.GetKeywords(prLabel);
            input.UseLeader = labelResult.StringResult == "Leader";

            return input;
        }
    }
}
