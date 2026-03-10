using System;
using System.Collections.Generic;
using System.Linq;
using Autodesk.AutoCAD.ApplicationServices;
using Autodesk.AutoCAD.DatabaseServices;
using Autodesk.AutoCAD.EditorInput;
using Autodesk.AutoCAD.Geometry;
using GntTools.Core.Settings;

namespace GntTools.Kepco
{
    /// <summary>구경별 카운팅 결과</summary>
    public class SectionCountResult
    {
        /// <summary>key: "D200", value: (원 개수, 해치 개수)</summary>
        public Dictionary<string, (int circles, int hatches)> Counts { get; }
            = new Dictionary<string, (int, int)>();

        /// <summary>"3(2)" 형식 문자열</summary>
        public string GetCountString(string diameterKey)
        {
            if (Counts.TryGetValue(diameterKey, out var c))
                return $"{c.circles}({c.hatches})";
            return "0(0)";
        }

        /// <summary>PipeData Dictionary로 변환</summary>
        public Dictionary<string, string> ToPipeData()
        {
            return Counts.ToDictionary(
                kv => kv.Key,
                kv => $"{kv.Value.circles}({kv.Value.hatches})");
        }
    }

    /// <summary>단면 영역 내 원/해치 구경별 카운팅</summary>
    public class SectionCounter
    {
        /// <summary>
        /// 사용자가 크로싱 윈도우로 단면 선택 → 내부 원/해치 카운팅
        /// </summary>
        public SectionCountResult CountInSelection()
        {
            var doc = Application.DocumentManager.MdiActiveDocument;
            var ed = doc.Editor;
            var db = doc.Database;
            var settings = AppSettings.Instance;

            var result = new SectionCountResult();

            // 구경 목록 초기화
            foreach (int d in settings.Kepco.Diameters)
                result.Counts[$"D{d}"] = (0, 0);

            // 크로싱 선택으로 단면 내부 객체 선택
            var prPt1 = ed.GetPoint("\n단면 영역 첫번째 코너: ");
            if (prPt1.Status != PromptStatus.OK) return result;
            var prPt2 = ed.GetCorner("\n단면 영역 반대쪽 코너: ", prPt1.Value);
            if (prPt2.Status != PromptStatus.OK) return result;

            // 원 선택
            var circleFilter = new SelectionFilter(new[]
            {
                new TypedValue((int)DxfCode.Start, "CIRCLE")
            });
            var circleResult = ed.SelectCrossingWindow(
                prPt1.Value, prPt2.Value, circleFilter);

            // 해치 선택
            var hatchFilter = new SelectionFilter(new[]
            {
                new TypedValue((int)DxfCode.Start, "HATCH")
            });
            var hatchResult = ed.SelectCrossingWindow(
                prPt1.Value, prPt2.Value, hatchFilter);

            using (var tr = db.TransactionManager.StartTransaction())
            {
                // 원 카운팅 (반지름 → 구경)
                if (circleResult.Status == PromptStatus.OK)
                {
                    foreach (var id in circleResult.Value.GetObjectIds())
                    {
                        var circle = tr.GetObject(id, OpenMode.ForRead) as Circle;
                        if (circle == null) continue;

                        int diamMm = (int)Math.Round(circle.Radius * 2 * 1000);
                        string key = MatchToStandardDiameter(diamMm, settings.Kepco.Diameters);

                        if (result.Counts.ContainsKey(key))
                        {
                            var (c, h) = result.Counts[key];
                            result.Counts[key] = (c + 1, h);
                        }
                    }
                }

                // 해치 카운팅 (해치 위치와 가장 가까운 원의 구경으로 매칭)
                if (hatchResult.Status == PromptStatus.OK &&
                    circleResult.Status == PromptStatus.OK)
                {
                    var circles = new List<(Point3d center, string key)>();
                    foreach (var id in circleResult.Value.GetObjectIds())
                    {
                        var circle = tr.GetObject(id, OpenMode.ForRead) as Circle;
                        if (circle == null) continue;
                        int diamMm = (int)Math.Round(circle.Radius * 2 * 1000);
                        string key = MatchToStandardDiameter(
                            diamMm, settings.Kepco.Diameters);
                        circles.Add((circle.Center, key));
                    }

                    foreach (var hId in hatchResult.Value.GetObjectIds())
                    {
                        var hatch = tr.GetObject(hId, OpenMode.ForRead) as Hatch;
                        if (hatch == null) continue;

                        var hatchCenter = GetHatchCenter(hatch);
                        string closestKey = null;
                        double minDist = double.MaxValue;

                        foreach (var (center, key) in circles)
                        {
                            double dist = hatchCenter.DistanceTo(center);
                            if (dist < minDist)
                            {
                                minDist = dist;
                                closestKey = key;
                            }
                        }

                        if (closestKey != null && result.Counts.ContainsKey(closestKey))
                        {
                            var (c, h) = result.Counts[closestKey];
                            result.Counts[closestKey] = (c, h + 1);
                        }
                    }
                }

                tr.Commit();
            }

            return result;
        }

        /// <summary>구경(mm) → 가장 가까운 표준 구경 키</summary>
        private string MatchToStandardDiameter(int diamMm, List<int> standards)
        {
            int closest = standards.OrderBy(s => Math.Abs(s - diamMm)).First();
            return $"D{closest}";
        }

        /// <summary>해치 중심점 근사</summary>
        private Point3d GetHatchCenter(Hatch hatch)
        {
            try
            {
                var ext = hatch.GeometricExtents;
                return new Point3d(
                    (ext.MinPoint.X + ext.MaxPoint.X) / 2,
                    (ext.MinPoint.Y + ext.MaxPoint.Y) / 2,
                    0);
            }
            catch
            {
                return Point3d.Origin;
            }
        }
    }
}
