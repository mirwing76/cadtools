using System;
using System.Collections.Generic;
using System.Linq;
using Autodesk.AutoCAD.ApplicationServices;
using Autodesk.AutoCAD.DatabaseServices;
using Autodesk.AutoCAD.EditorInput;
using Autodesk.AutoCAD.Geometry;
using GntTools.Core.Odt;
using GntTools.Core.Selection;

namespace GntTools.Kepco
{
    /// <summary>오류검증 결과</summary>
    public class CheckResult
    {
        public int TotalChecked { get; set; }
        public int ErrorCount { get; set; }
        public List<string> Errors { get; } = new List<string>();
    }

    /// <summary>
    /// 분기점 오류 검증 — 분기점에서 연결된 관로들의 구경 합산 검증
    /// VB.NET 버그 수정: chkPipe 누적 차감 → 원본 배열 보존 후 비교
    /// </summary>
    public class JunctionChecker
    {
        private readonly OdtManager _odt = new OdtManager();

        /// <summary>선택된 관로들의 분기점 정합성 검증</summary>
        public CheckResult CheckSelected()
        {
            var doc = Application.DocumentManager.MdiActiveDocument;
            var ed = doc.Editor;
            var result = new CheckResult();

            var selector = new EntitySelector();
            var ids = selector.SelectMultiple(
                EntitySelector.PolylineFilter(),
                "\n검증할 전력관로를 선택하세요: ");

            if (ids.Length == 0)
            {
                ed.WriteMessage("\n선택된 관로가 없습니다.");
                return result;
            }

            result.TotalChecked = ids.Length;

            // 각 폴리라인의 시점/종점 좌표와 구경 데이터 수집
            var pipeInfos = new List<PipeEndInfo>();
            var db = doc.Database;

            using (var tr = db.TransactionManager.StartTransaction())
            {
                foreach (var id in ids)
                {
                    var ent = tr.GetObject(id, OpenMode.ForRead) as Curve;
                    if (ent == null) continue;

                    // KEPCO_EXT에서 PIPDAT 읽기
                    var extData = _odt.ReadRecord("KEPCO_EXT", id);
                    if (extData == null || extData.Length < 1) continue;

                    var pipeData = KepcoExtRecord.ParsePipeDataJson(extData[0]);

                    pipeInfos.Add(new PipeEndInfo
                    {
                        EntityId = id,
                        StartPoint = ent.StartPoint,
                        EndPoint = ent.EndPoint,
                        PipeData = pipeData,
                    });
                }
                tr.Commit();
            }

            // 분기점 찾기: 동일 좌표(허용오차 내)에서 만나는 관로들
            double tolerance = 0.01;
            var junctions = FindJunctions(pipeInfos, tolerance);

            foreach (var junction in junctions)
            {
                if (junction.Count < 2) continue;

                // 분기점에서 들어오는 관과 나가는 관의 구경 합 비교
                // VB.NET 버그 수정: 원본 Dictionary 복제하여 비교
                var incoming = new Dictionary<string, int>();
                var outgoing = new Dictionary<string, int>();

                foreach (var pipe in junction)
                {
                    var targetDict = pipe.IsStart ? outgoing : incoming;
                    foreach (var kv in pipe.Info.PipeData)
                    {
                        // "3(2)" → 원 개수(3) 추출
                        int count = ParseCircleCount(kv.Value);
                        if (!targetDict.ContainsKey(kv.Key))
                            targetDict[kv.Key] = 0;
                        targetDict[kv.Key] += count;
                    }
                }

                // 검증: 들어오는 합 = 나가는 합
                var allKeys = incoming.Keys.Union(outgoing.Keys).Distinct();
                foreach (var key in allKeys)
                {
                    int inCount = incoming.ContainsKey(key) ? incoming[key] : 0;
                    int outCount = outgoing.ContainsKey(key) ? outgoing[key] : 0;

                    if (inCount != outCount)
                    {
                        result.ErrorCount++;
                        result.Errors.Add(
                            $"분기점({junction[0].Point:F2}): {key} 불일치 " +
                            $"(유입={inCount}, 유출={outCount})");
                    }
                }
            }

            // 결과 출력
            ed.WriteMessage($"\n검증 완료: {result.TotalChecked}개 관로, " +
                $"{result.ErrorCount}개 오류");
            foreach (var err in result.Errors)
                ed.WriteMessage($"\n  ! {err}");

            return result;
        }

        private int ParseCircleCount(string countStr)
        {
            // "3(2)" → 3
            if (string.IsNullOrEmpty(countStr)) return 0;
            int paren = countStr.IndexOf('(');
            string num = paren > 0 ? countStr.Substring(0, paren) : countStr;
            int.TryParse(num, out int c);
            return c;
        }

        private List<List<JunctionPipe>> FindJunctions(
            List<PipeEndInfo> pipes, double tolerance)
        {
            var endpoints = new List<(Point3d point, PipeEndInfo info, bool isStart)>();
            foreach (var p in pipes)
            {
                endpoints.Add((p.StartPoint, p, true));
                endpoints.Add((p.EndPoint, p, false));
            }

            var junctions = new List<List<JunctionPipe>>();
            var used = new HashSet<int>();

            for (int i = 0; i < endpoints.Count; i++)
            {
                if (used.Contains(i)) continue;
                var group = new List<JunctionPipe>
                {
                    new JunctionPipe
                    {
                        Point = endpoints[i].point,
                        Info = endpoints[i].info,
                        IsStart = endpoints[i].isStart
                    }
                };
                used.Add(i);

                for (int j = i + 1; j < endpoints.Count; j++)
                {
                    if (used.Contains(j)) continue;
                    if (endpoints[i].point.DistanceTo(endpoints[j].point) < tolerance)
                    {
                        group.Add(new JunctionPipe
                        {
                            Point = endpoints[j].point,
                            Info = endpoints[j].info,
                            IsStart = endpoints[j].isStart
                        });
                        used.Add(j);
                    }
                }

                if (group.Count >= 2)
                    junctions.Add(group);
            }

            return junctions;
        }

        private class PipeEndInfo
        {
            public ObjectId EntityId { get; set; }
            public Point3d StartPoint { get; set; }
            public Point3d EndPoint { get; set; }
            public Dictionary<string, string> PipeData { get; set; }
        }

        private class JunctionPipe
        {
            public Point3d Point { get; set; }
            public PipeEndInfo Info { get; set; }
            public bool IsStart { get; set; }
        }
    }
}
