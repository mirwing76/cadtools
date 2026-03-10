using Autodesk.AutoCAD.ApplicationServices;
using Autodesk.AutoCAD.EditorInput;
using Autodesk.AutoCAD.Runtime;
using GntTools.Core.Selection;
using GntTools.Core.Settings;

namespace GntTools.Kepco
{
    public class KepcoCommands
    {
        private static readonly KepcoService _service = new KepcoService();
        private static readonly SectionCounter _counter = new SectionCounter();
        private static readonly BxHCalculator _bxh = new BxHCalculator();
        private static readonly JunctionChecker _checker = new JunctionChecker();

        [CommandMethod("GNTTOOLS_KEPCO_ATT")]
        public void KepcoAttach()
        {
            var doc = Application.DocumentManager.MdiActiveDocument;
            var ed = doc.Editor;
            _service.EnsureTables();

            // 1. 단면 카운팅
            ed.WriteMessage("\n== 단면 정보 수집 ==");
            var countResult = _counter.CountInSelection();

            // 2. BxH
            var (b, h, bxhStr) = _bxh.MeasureFromSelection();

            // 3. 기본 정보
            var settings = AppSettings.Instance;
            var prYear = ed.GetString(
                $"\n설치년도 <{settings.Kepco.Defaults.Year}>: ");
            string year = string.IsNullOrEmpty(prYear.StringResult)
                ? settings.Kepco.Defaults.Year : prYear.StringResult;

            var prMat = ed.GetString(
                $"\n관재질 <{settings.Kepco.Defaults.Material}>: ");
            string material = string.IsNullOrEmpty(prMat.StringResult)
                ? settings.Kepco.Defaults.Material : prMat.StringResult;

            // 불탐
            var prBt = new PromptKeywordOptions("\n불탐여부 [Y/N] <N>: ");
            prBt.Keywords.Add("Y");
            prBt.Keywords.Add("N");
            prBt.Keywords.Default = "N";
            var btResult = ed.GetKeywords(prBt);
            bool isUndetected = btResult.StringResult == "Y";

            var input = new KepcoInputData
            {
                InstallYear = year,
                Material = material,
                PipeData = countResult.ToPipeData(),
                BxH = bxhStr,
                IsUndetected = isUndetected,
                UseLeader = false,
            };

            // 4. 관로 선택
            var selector = new EntitySelector();
            var polyId = selector.SelectOne(
                EntitySelector.PolylineFilter(),
                "\n전력관로 폴리라인을 선택하세요: ");

            if (polyId.IsNull) { ed.WriteMessage("\n취소됨."); return; }
            _service.CreateAttribute(input, polyId);
        }

        [CommandMethod("GNTTOOLS_KEPCO_EDIT")]
        public void KepcoEdit()
        {
            var doc = Application.DocumentManager.MdiActiveDocument;
            var ed = doc.Editor;
            _service.EnsureTables();

            var selector = new EntitySelector();
            var polyId = selector.SelectOne(
                EntitySelector.PolylineFilter(),
                "\n수정할 전력관로를 선택하세요: ");

            if (polyId.IsNull) { ed.WriteMessage("\n취소됨."); return; }

            // 재카운팅
            var countResult = _counter.CountInSelection();
            var (_, _, bxhStr) = _bxh.MeasureFromSelection();

            var settings = AppSettings.Instance;
            var input = new KepcoInputData
            {
                InstallYear = settings.Kepco.Defaults.Year,
                Material = settings.Kepco.Defaults.Material,
                PipeData = countResult.ToPipeData(),
                BxH = bxhStr,
            };

            _service.ModifyAttribute(input, polyId);
        }

        [CommandMethod("GNTTOOLS_KEPCO_CHK")]
        public void KepcoCheck()
        {
            _checker.CheckSelected();
        }
    }
}
