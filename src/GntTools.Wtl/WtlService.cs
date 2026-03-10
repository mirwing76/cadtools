using Autodesk.AutoCAD.ApplicationServices;
using Autodesk.AutoCAD.DatabaseServices;
using Autodesk.AutoCAD.EditorInput;
using Autodesk.AutoCAD.Geometry;
using GntTools.Core.Drawing;
using GntTools.Core.Geometry;
using GntTools.Core.Odt;
using GntTools.Core.Settings;
using GntTools.Core.XData;

namespace GntTools.Wtl
{
    /// <summary>상수관로 입력 데이터</summary>
    public class WtlInputData
    {
        public string InstallYear { get; set; }
        public string Material { get; set; }
        public string Diameter { get; set; }
        public bool IsAutoDepth { get; set; }
        public double ManualBeginDepth { get; set; }
        public double ManualEndDepth { get; set; }
        public bool IsUndetected { get; set; }
        public double BeginGroundHeight { get; set; }
        public double EndGroundHeight { get; set; }
        public bool UseLeader { get; set; }
    }

    /// <summary>상수관로 비즈니스 로직</summary>
    public class WtlService
    {
        private readonly OdtManager _odt = new OdtManager();
        private readonly DepthCalculator _depth = new DepthCalculator();
        private readonly PipeCommonSchema _commonSchema = new PipeCommonSchema();
        private readonly WtlExtSchema _extSchema = new WtlExtSchema();

        /// <summary>ODT 테이블 초기화</summary>
        public void EnsureTables()
        {
            _odt.EnsureTable(_commonSchema);
            _odt.EnsureTable(_extSchema);
        }

        /// <summary>신규 속성 입력</summary>
        public bool CreateAttribute(WtlInputData input, ObjectId polylineId)
        {
            var doc = Application.DocumentManager.MdiActiveDocument;
            var ed = doc.Editor;
            var settings = AppSettings.Instance;

            // 1. 길이
            double length = PolylineHelper.GetLength(polylineId);

            // 2. 심도
            DepthResult depthResult;
            if (input.IsUndetected)
                depthResult = _depth.Undetected();
            else if (input.IsAutoDepth)
                depthResult = _depth.MeasureAtVertices(polylineId, settings.Wtl.Layers.Depth);
            else
                depthResult = _depth.FromManualInput(input.ManualBeginDepth, input.ManualEndDepth);

            // 3. PIPE_COMMON 레코드
            var commonRec = new PipeCommonRecord
            {
                InstallDate = input.InstallYear,
                Material = input.Material,
                Diameter = input.Diameter,
                Length = length,
                Undetected = depthResult.IsUndetected ? "Y" : "N",
                BeginDepth = depthResult.BeginDepth,
                EndDepth = depthResult.EndDepth,
                AverageDepth = depthResult.AverageDepth,
                MaxDepth = depthResult.MaxDepth,
                MinDepth = depthResult.MinDepth,
                Label = WtlLabelBuilder.BuildPipeLabel(input.Material, input.Diameter, length),
            };

            _odt.AttachRecord(_commonSchema.TableName, polylineId);
            _odt.UpdateRecord(_commonSchema.TableName, polylineId, commonRec.ToDictionary());

            // 4. WTL_EXT 레코드
            var extRec = new WtlExtRecord
            {
                BeginGroundHeight = input.BeginGroundHeight,
                EndGroundHeight = input.EndGroundHeight,
            };

            _odt.AttachRecord(_extSchema.TableName, polylineId);
            _odt.UpdateRecord(_extSchema.TableName, polylineId, extRec.ToDictionary());

            // 5. 라벨 생성
            var styleId = TextStyleHelper.EnsureStyle(
                settings.Common.TextStyle, settings.Common.ShxFont, settings.Common.BigFont);
            var endpoints = PolylineHelper.GetEndpoints(polylineId);

            if (input.UseLeader)
            {
                var ppr = ed.GetPoint("\n지시선 끝점을 지정하세요: ");
                if (ppr.Status != PromptStatus.OK) return false;

                var midPt = new Point3d(
                    (endpoints.start.X + endpoints.end.X) / 2,
                    (endpoints.start.Y + endpoints.end.Y) / 2, 0);

                LeaderWriter.Create(midPt, ppr.Value, settings.Wtl.Layers.Leader);
                TextWriter.Create(commonRec.Label, ppr.Value,
                    settings.Common.TextSize, 0, settings.Wtl.Layers.Label, styleId);
            }
            else
            {
                var midPt = new Point3d(
                    (endpoints.start.X + endpoints.end.X) / 2,
                    (endpoints.start.Y + endpoints.end.Y) / 2, 0);
                double angle = TextWriter.CalcReadableAngle(endpoints.start, endpoints.end);

                TextWriter.Create(commonRec.Label, midPt,
                    settings.Common.TextSize, angle, settings.Wtl.Layers.Label, styleId);
            }

            // 6. 심도 텍스트
            if (!depthResult.IsUndetected)
            {
                TextWriter.Create(
                    WtlLabelBuilder.BuildDepthLabel(depthResult.BeginDepth),
                    endpoints.start, settings.Common.TextSize, 0,
                    settings.Wtl.Layers.Depth, styleId);
                TextWriter.Create(
                    WtlLabelBuilder.BuildDepthLabel(depthResult.EndDepth),
                    endpoints.end, settings.Common.TextSize, 0,
                    settings.Wtl.Layers.Depth, styleId);
            }

            // 7. XData + 색상
            string groupId = XDataManager.GenerateGroupId();
            XDataManager.WriteGroupId(polylineId, groupId);
            ColorHelper.SetColor(polylineId, 2);

            ed.WriteMessage($"\n상수관로 속성 입력 완료: {commonRec.Label}");
            return true;
        }

        /// <summary>기존 속성 수정</summary>
        public bool ModifyAttribute(WtlInputData input, ObjectId polylineId)
        {
            if (!_odt.RecordExists(_commonSchema.TableName, polylineId))
            {
                var ed = Application.DocumentManager.MdiActiveDocument.Editor;
                ed.WriteMessage("\n선택한 폴리라인에 상수 속성이 없습니다.");
                return false;
            }

            _odt.RemoveRecord(_commonSchema.TableName, polylineId);
            _odt.RemoveRecord(_extSchema.TableName, polylineId);
            return CreateAttribute(input, polylineId);
        }
    }
}
