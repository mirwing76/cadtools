using Autodesk.AutoCAD.ApplicationServices;
using Autodesk.AutoCAD.EditorInput;
using Autodesk.AutoCAD.Runtime;
using GntTools.Core.Selection;
using GntTools.Core.Settings;

namespace GntTools.Swl
{
    public class SwlCommands
    {
        private static readonly SwlService _service = new SwlService();

        [CommandMethod("GNTTOOLS_SWL_ATT")]
        public void SwlAttach()
        {
            var doc = Application.DocumentManager.MdiActiveDocument;
            var ed = doc.Editor;
            _service.EnsureTables();

            var input = CollectInputFromPrompt(ed);
            if (input == null) return;

            var selector = new EntitySelector();
            var polyId = selector.SelectOne(
                EntitySelector.PolylineFilter(),
                "\n하수관로 폴리라인을 선택하세요: ");

            if (polyId.IsNull) { ed.WriteMessage("\n취소됨."); return; }
            _service.CreateAttribute(input, polyId);
        }

        [CommandMethod("GNTTOOLS_SWL_EDIT")]
        public void SwlEdit()
        {
            var doc = Application.DocumentManager.MdiActiveDocument;
            var ed = doc.Editor;
            _service.EnsureTables();

            var selector = new EntitySelector();
            var polyId = selector.SelectOne(
                EntitySelector.PolylineFilter(),
                "\n수정할 하수관로를 선택하세요: ");

            if (polyId.IsNull) { ed.WriteMessage("\n취소됨."); return; }

            var input = CollectInputFromPrompt(ed);
            if (input == null) return;

            _service.ModifyAttribute(input, polyId);
        }

        private SwlInputData CollectInputFromPrompt(Editor ed)
        {
            var settings = AppSettings.Instance;
            var input = new SwlInputData();

            // 용도코드
            var prCode = ed.GetString(
                $"\n용도코드 <{settings.Swl.Defaults.UseCode}>: ");
            if (prCode.Status == PromptStatus.Cancel) return null;
            input.UseCode = string.IsNullOrEmpty(prCode.StringResult)
                ? settings.Swl.Defaults.UseCode : prCode.StringResult;

            // 설치년도/관재질/구경
            var prYear = ed.GetString(
                $"\n설치년도 <{settings.Swl.Defaults.Year}>: ");
            if (prYear.Status == PromptStatus.Cancel) return null;
            input.InstallYear = string.IsNullOrEmpty(prYear.StringResult)
                ? settings.Swl.Defaults.Year : prYear.StringResult;

            var prMat = ed.GetString(
                $"\n관재질 <{settings.Swl.Defaults.Material}>: ");
            if (prMat.Status == PromptStatus.Cancel) return null;
            input.Material = string.IsNullOrEmpty(prMat.StringResult)
                ? settings.Swl.Defaults.Material : prMat.StringResult;

            // 박스관 여부
            var prBox = new PromptKeywordOptions(
                "\n관종 [원형(C)/박스(B)] <C>: ");
            prBox.Keywords.Add("Circle");
            prBox.Keywords.Add("Box");
            prBox.Keywords.Default = "Circle";
            var boxResult = ed.GetKeywords(prBox);
            if (boxResult.Status == PromptStatus.Cancel) return null;

            if (boxResult.StringResult == "Box")
            {
                input.IsBoxPipe = true;
                var prW = ed.GetDouble("\n가로(mm): ");
                if (prW.Status != PromptStatus.OK) return null;
                input.BoxWidth = prW.Value;
                var prH = ed.GetDouble("\n세로(mm): ");
                if (prH.Status != PromptStatus.OK) return null;
                input.BoxHeight = prH.Value;
                input.Diameter = $"{prW.Value:F0}x{prH.Value:F0}";
            }
            else
            {
                var prDia = ed.GetString(
                    $"\n구경 <{settings.Swl.Defaults.Diameter}>: ");
                if (prDia.Status == PromptStatus.Cancel) return null;
                input.Diameter = string.IsNullOrEmpty(prDia.StringResult)
                    ? settings.Swl.Defaults.Diameter : prDia.StringResult;
            }

            // 통로수
            var prLine = ed.GetInteger("\n통로수 <1>: ");
            input.LineCount = prLine.Status == PromptStatus.OK
                ? prLine.Value : 1;

            // 심도
            var prMode = new PromptKeywordOptions(
                "\n심도 [자동(A)/수동(M)/불탐(U)] <M>: ");
            prMode.Keywords.Add("Auto");
            prMode.Keywords.Add("Manual");
            prMode.Keywords.Add("Undetected");
            prMode.Keywords.Default = "Manual";
            var modeResult = ed.GetKeywords(prMode);
            if (modeResult.Status == PromptStatus.Cancel) return null;

            switch (modeResult.StringResult)
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

            // 지반고
            var prSbk = ed.GetDouble("\n시점지반고 <0>: ");
            input.BeginGroundHeight = prSbk.Status == PromptStatus.OK
                ? prSbk.Value : 0;
            var prSbl = ed.GetDouble("\n종점지반고 <0>: ");
            input.EndGroundHeight = prSbl.Status == PromptStatus.OK
                ? prSbl.Value : 0;

            // 라벨 모드
            var prLabel = new PromptKeywordOptions(
                "\n라벨방식 [지시선(L)/평행(P)] <P>: ");
            prLabel.Keywords.Add("Leader");
            prLabel.Keywords.Add("Parallel");
            prLabel.Keywords.Default = "Parallel";
            var labelResult = ed.GetKeywords(prLabel);
            input.UseLeader = labelResult.StringResult == "Leader";

            return input;
        }
    }
}
