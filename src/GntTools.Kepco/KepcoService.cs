using System.Collections.Generic;
using Autodesk.AutoCAD.ApplicationServices;
using Autodesk.AutoCAD.DatabaseServices;
using Autodesk.AutoCAD.EditorInput;
using Autodesk.AutoCAD.Geometry;
using GntTools.Core.Drawing;
using GntTools.Core.Geometry;
using GntTools.Core.Odt;
using GntTools.Core.Settings;
using GntTools.Core.XData;

namespace GntTools.Kepco
{
    /// <summary>전력통신 입력 데이터</summary>
    public class KepcoInputData
    {
        public string InstallYear { get; set; }
        public string Material { get; set; }
        public Dictionary<string, string> PipeData { get; set; }
            = new Dictionary<string, string>();
        public string BxH { get; set; } = "";
        public bool IsUndetected { get; set; }
        public bool UseLeader { get; set; }
    }

    /// <summary>전력통신 비즈니스 로직</summary>
    public class KepcoService
    {
        private readonly OdtManager _odt = new OdtManager();
        private readonly DepthCalculator _depth = new DepthCalculator();
        private readonly PipeCommonSchema _commonSchema = new PipeCommonSchema();
        private readonly KepcoExtSchema _extSchema = new KepcoExtSchema();

        public void EnsureTables()
        {
            _odt.EnsureTable(_commonSchema);
            _odt.EnsureTable(_extSchema);
        }

        public bool CreateAttribute(KepcoInputData input, ObjectId polylineId)
        {
            var doc = Application.DocumentManager.MdiActiveDocument;
            var ed = doc.Editor;
            var settings = AppSettings.Instance;

            double length = PolylineHelper.GetLength(polylineId);

            // 심도: 항상 자동 (정점별 텍스트)
            DepthResult depthResult;
            if (input.IsUndetected)
                depthResult = _depth.Undetected();
            else
                depthResult = _depth.MeasureAtVertices(polylineId,
                    settings.Kepco.Layers.Depth);

            // 대표 구경 = PipeData에서 가장 큰 구경
            string mainDiameter = "";
            foreach (int d in settings.Kepco.Diameters)
            {
                string key = $"D{d}";
                if (input.PipeData.ContainsKey(key) &&
                    input.PipeData[key] != "0(0)")
                {
                    mainDiameter = d.ToString();
                    break; // 내림차순이므로 첫 번째가 최대
                }
            }

            // PIPE_COMMON
            var commonRec = new PipeCommonRecord
            {
                InstallDate = input.InstallYear,
                Material = input.Material,
                Diameter = mainDiameter,
                Length = length,
                Undetected = depthResult.IsUndetected ? "Y" : "N",
                BeginDepth = depthResult.BeginDepth,
                EndDepth = depthResult.EndDepth,
                AverageDepth = depthResult.AverageDepth,
                MaxDepth = depthResult.MaxDepth,
                MinDepth = depthResult.MinDepth,
                Label = KepcoLabelBuilder.BuildPipeLabel(input.Material, length),
            };

            _odt.AttachRecord(_commonSchema.TableName, polylineId);
            _odt.UpdateRecord(_commonSchema.TableName, polylineId,
                commonRec.ToDictionary());

            // KEPCO_EXT
            string groupId = XDataManager.GenerateGroupId();
            var extRec = new KepcoExtRecord
            {
                PipeData = input.PipeData,
                BxH = input.BxH,
                GroupId = groupId,
            };

            _odt.AttachRecord(_extSchema.TableName, polylineId);
            _odt.UpdateRecord(_extSchema.TableName, polylineId,
                extRec.ToDictionary());

            // 라벨
            var styleId = TextStyleHelper.EnsureStyle(
                settings.Common.TextStyle,
                settings.Common.ShxFont,
                settings.Common.BigFont);
            var endpoints = PolylineHelper.GetEndpoints(polylineId);

            if (input.UseLeader)
            {
                var ppr = ed.GetPoint("\n지시선 끝점을 지정하세요: ");
                if (ppr.Status != PromptStatus.OK) return false;

                var midPt = new Point3d(
                    (endpoints.start.X + endpoints.end.X) / 2,
                    (endpoints.start.Y + endpoints.end.Y) / 2, 0);

                LeaderWriter.Create(midPt, ppr.Value,
                    settings.Kepco.Layers.Leader);
                TextWriter.Create(commonRec.Label, ppr.Value,
                    settings.Common.TextSize, 0,
                    settings.Kepco.Layers.Label, styleId);
            }
            else
            {
                var midPt = new Point3d(
                    (endpoints.start.X + endpoints.end.X) / 2,
                    (endpoints.start.Y + endpoints.end.Y) / 2, 0);
                double angle = TextWriter.CalcReadableAngle(
                    endpoints.start, endpoints.end);
                TextWriter.Create(commonRec.Label, midPt,
                    settings.Common.TextSize, angle,
                    settings.Kepco.Layers.Label, styleId);
            }

            // 심도 텍스트
            if (!depthResult.IsUndetected)
            {
                TextWriter.Create(
                    KepcoLabelBuilder.BuildDepthLabel(depthResult.BeginDepth),
                    endpoints.start, settings.Common.TextSize, 0,
                    settings.Kepco.Layers.Depth, styleId);
                TextWriter.Create(
                    KepcoLabelBuilder.BuildDepthLabel(depthResult.EndDepth),
                    endpoints.end, settings.Common.TextSize, 0,
                    settings.Kepco.Layers.Depth, styleId);
            }

            // XData + 색상
            XDataManager.WriteGroupId(polylineId, groupId);
            ColorHelper.SetColor(polylineId, 1); // 빨간색

            ed.WriteMessage($"\n전력관로 속성 입력 완료: {commonRec.Label}");
            return true;
        }

        public bool ModifyAttribute(KepcoInputData input, ObjectId polylineId)
        {
            if (!_odt.RecordExists(_commonSchema.TableName, polylineId))
            {
                var ed = Application.DocumentManager.MdiActiveDocument.Editor;
                ed.WriteMessage("\n선택한 폴리라인에 전력 속성이 없습니다.");
                return false;
            }

            _odt.RemoveRecord(_commonSchema.TableName, polylineId);
            _odt.RemoveRecord(_extSchema.TableName, polylineId);
            return CreateAttribute(input, polylineId);
        }
    }
}
