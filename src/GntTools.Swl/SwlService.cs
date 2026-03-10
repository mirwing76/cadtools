using System;
using Autodesk.AutoCAD.ApplicationServices;
using Autodesk.AutoCAD.DatabaseServices;
using Autodesk.AutoCAD.EditorInput;
using Autodesk.AutoCAD.Geometry;
using GntTools.Core.Drawing;
using GntTools.Core.Geometry;
using GntTools.Core.Odt;
using GntTools.Core.Settings;
using GntTools.Core.XData;

namespace GntTools.Swl
{
    /// <summary>하수관로 입력 데이터</summary>
    public class SwlInputData
    {
        public string InstallYear { get; set; }
        public string Material { get; set; }
        public string Diameter { get; set; }
        public string UseCode { get; set; } = "";
        public bool IsBoxPipe { get; set; }
        public double BoxWidth { get; set; }
        public double BoxHeight { get; set; }
        public int LineCount { get; set; } = 1;
        public bool IsAutoDepth { get; set; }
        public double ManualBeginDepth { get; set; }
        public double ManualEndDepth { get; set; }
        public bool IsUndetected { get; set; }
        public double BeginGroundHeight { get; set; }
        public double EndGroundHeight { get; set; }
        public bool UseLeader { get; set; }
    }

    /// <summary>하수관로 비즈니스 로직</summary>
    public class SwlService
    {
        private readonly OdtManager _odt = new OdtManager();
        private readonly DepthCalculator _depth = new DepthCalculator();
        private readonly PipeCommonSchema _commonSchema = new PipeCommonSchema();
        private readonly SwlExtSchema _extSchema = new SwlExtSchema();

        public void EnsureTables()
        {
            _odt.EnsureTable(_commonSchema);
            _odt.EnsureTable(_extSchema);
        }

        /// <summary>신규 속성 입력</summary>
        public bool CreateAttribute(SwlInputData input, ObjectId polylineId)
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
                depthResult = _depth.MeasureAtVertices(polylineId,
                    settings.Swl.Layers.Depth);
            else
                depthResult = _depth.FromManualInput(
                    input.ManualBeginDepth, input.ManualEndDepth);

            // 3. 표고 계산
            double diamMeter = ElevationCalculator.DiameterToMeter(input.Diameter);
            var elev = ElevationCalculator.Calculate(
                input.BeginGroundHeight, input.EndGroundHeight,
                depthResult.BeginDepth, depthResult.EndDepth,
                diamMeter, length);

            // 4. PIPE_COMMON
            string label = input.IsBoxPipe
                ? SwlLabelBuilder.BuildBoxLabel(input.Material,
                    input.BoxWidth, input.BoxHeight, length)
                : SwlLabelBuilder.BuildPipeLabel(input.Material,
                    input.Diameter, length);

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
                Label = label,
            };

            _odt.AttachRecord(_commonSchema.TableName, polylineId);
            _odt.UpdateRecord(_commonSchema.TableName, polylineId,
                commonRec.ToDictionary());

            // 5. SWL_EXT
            string groupId = XDataManager.GenerateGroupId();
            var extRec = new SwlExtRecord
            {
                UseCode = input.UseCode,
                BoxWidth = input.BoxWidth,
                BoxHeight = input.BoxHeight,
                LineCount = input.LineCount,
                BeginGroundHeight = input.BeginGroundHeight,
                EndGroundHeight = input.EndGroundHeight,
                BeginInvertLevel = elev.BeginInvertLevel,
                EndInvertLevel = elev.EndInvertLevel,
                BeginCrownLevel = elev.BeginCrownLevel,
                EndCrownLevel = elev.EndCrownLevel,
                Slope = elev.Slope,
                GroupId = groupId,
            };

            _odt.AttachRecord(_extSchema.TableName, polylineId);
            _odt.UpdateRecord(_extSchema.TableName, polylineId,
                extRec.ToDictionary());

            // 6. 라벨/심도 텍스트 생성
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
                    settings.Swl.Layers.Leader);
                TextWriter.Create(label, ppr.Value,
                    settings.Common.TextSize, 0,
                    settings.Swl.Layers.Label, styleId);
            }
            else
            {
                var midPt = new Point3d(
                    (endpoints.start.X + endpoints.end.X) / 2,
                    (endpoints.start.Y + endpoints.end.Y) / 2, 0);
                double angle = TextWriter.CalcReadableAngle(
                    endpoints.start, endpoints.end);
                TextWriter.Create(label, midPt,
                    settings.Common.TextSize, angle,
                    settings.Swl.Layers.Label, styleId);
            }

            // 심도 텍스트
            if (!depthResult.IsUndetected)
            {
                TextWriter.Create(
                    SwlLabelBuilder.BuildDepthLabel(depthResult.BeginDepth),
                    endpoints.start, settings.Common.TextSize, 0,
                    settings.Swl.Layers.Depth, styleId);
                TextWriter.Create(
                    SwlLabelBuilder.BuildDepthLabel(depthResult.EndDepth),
                    endpoints.end, settings.Common.TextSize, 0,
                    settings.Swl.Layers.Depth, styleId);
            }

            // 7. XData + 색상
            XDataManager.WriteGroupId(polylineId, groupId);
            ColorHelper.SetColor(polylineId, 3); // 녹색

            ed.WriteMessage($"\n하수관로 속성 입력 완료: {label}");
            ed.WriteMessage($"\n  관저고: {elev.BeginInvertLevel:F3} → {elev.EndInvertLevel:F3}");
            ed.WriteMessage($"\n  구배: {elev.Slope:F3}%");
            return true;
        }

        /// <summary>기존 속성 수정</summary>
        public bool ModifyAttribute(SwlInputData input, ObjectId polylineId)
        {
            if (!_odt.RecordExists(_commonSchema.TableName, polylineId))
            {
                var ed = Application.DocumentManager.MdiActiveDocument.Editor;
                ed.WriteMessage("\n선택한 폴리라인에 하수 속성이 없습니다.");
                return false;
            }

            _odt.RemoveRecord(_commonSchema.TableName, polylineId);
            _odt.RemoveRecord(_extSchema.TableName, polylineId);
            return CreateAttribute(input, polylineId);
        }
    }
}
