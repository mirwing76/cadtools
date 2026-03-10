using System;
using System.Collections.Generic;
using System.Linq;
using Autodesk.AutoCAD.ApplicationServices;
using Autodesk.AutoCAD.DatabaseServices;
using Autodesk.AutoCAD.EditorInput;
using Autodesk.AutoCAD.Geometry;
using GntTools.Core.Selection;

namespace GntTools.Core.Geometry
{
    /// <summary>심도 자동/수동 측정</summary>
    public class DepthCalculator
    {
        /// <summary>
        /// 자동: 폴리라인 정점 근처의 심도 텍스트를 읽어서 계산
        /// </summary>
        public DepthResult MeasureAtVertices(ObjectId polylineId,
            string depthLayer, double tolerance = 5.0)
        {
            var vertices = PolylineHelper.GetVertices(polylineId);
            if (vertices.Count < 2) return Undetected();

            var doc = Application.DocumentManager.MdiActiveDocument;
            var db = doc.Database;
            var ed = doc.Editor;

            var depths = new List<double>();

            using (var tr = db.TransactionManager.StartTransaction())
            {
                var filter = EntitySelector.TextOnLayerFilter(depthLayer);
                var ssResult = ed.SelectAll(filter);
                if (ssResult.Status != PromptStatus.OK)
                {
                    tr.Commit();
                    return Undetected();
                }

                // 심도 레이어의 모든 텍스트 수집
                var textEntities = new List<(Point3d pos, double val)>();
                foreach (var id in ssResult.Value.GetObjectIds())
                {
                    var ent = tr.GetObject(id, OpenMode.ForRead);
                    if (ent is DBText txt)
                    {
                        if (double.TryParse(txt.TextString, out double v))
                            textEntities.Add((txt.Position, v));
                    }
                }

                // 각 정점에 가장 가까운 심도 텍스트 찾기
                foreach (var vertex in vertices)
                {
                    double minDist = double.MaxValue;
                    double closestVal = 0;
                    bool found = false;

                    foreach (var (pos, val) in textEntities)
                    {
                        // Point3d.DistanceTo (autocad-geometry.md 참고)
                        double dist = vertex.DistanceTo(pos);
                        if (dist < tolerance && dist < minDist)
                        {
                            minDist = dist;
                            closestVal = val;
                            found = true;
                        }
                    }

                    if (found) depths.Add(closestVal);
                }

                tr.Commit();
            }

            if (depths.Count == 0) return Undetected();

            return new DepthResult
            {
                BeginDepth = depths.First(),
                EndDepth = depths.Last(),
                AverageDepth = Math.Round(depths.Average(), 2),
                MaxDepth = depths.Max(),
                MinDepth = depths.Min(),
                IsUndetected = false
            };
        }

        /// <summary>수동: 사용자 입력 시점/종점 심도</summary>
        public DepthResult FromManualInput(double beginDepth, double endDepth)
        {
            double avg = Math.Round((beginDepth + endDepth) / 2.0, 2);
            double max = Math.Max(beginDepth, endDepth);
            double min = Math.Min(beginDepth, endDepth);

            return new DepthResult
            {
                BeginDepth = beginDepth,
                EndDepth = endDepth,
                AverageDepth = avg,
                MaxDepth = max,
                MinDepth = min,
                IsUndetected = false
            };
        }

        /// <summary>불탐</summary>
        public DepthResult Undetected()
        {
            return new DepthResult
            {
                BeginDepth = 0,
                EndDepth = 0,
                AverageDepth = 0,
                MaxDepth = 0,
                MinDepth = 0,
                IsUndetected = true
            };
        }
    }
}
