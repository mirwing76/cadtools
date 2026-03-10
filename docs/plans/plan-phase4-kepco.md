# Phase 4: KEPCO 전력통신 도메인 구현 계획

> **For agentic workers:** REQUIRED: Use superpowers:subagent-driven-development (if subagents available) or superpowers:executing-plans to implement this plan. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** 전력통신 도메인 구현 — 단면 원/해치 카운팅, BxH 계산, 분기점 오류검증 포함

**선행 조건:** Phase 1~3 완료

**Spec:** `docs/specs/2026-03-10-gnttools-integration-design.md` §2 KEPCO_EXT, §5 KEPCO 탭

---

## File Map

| Task | 파일 | 역할 |
|------|------|------|
| Task 1 | `src/GntTools.Kepco/KepcoExtSchema.cs` | KEPCO_EXT 스키마 (3 fields) |
| Task 1 | `src/GntTools.Kepco/KepcoExtRecord.cs` | KEPCO_EXT 레코드 DTO |
| Task 2 | `src/GntTools.Kepco/SectionCounter.cs` | 단면 원/해치 카운팅 |
| Task 3 | `src/GntTools.Kepco/BxHCalculator.cs` | BxH 선택 및 계산 |
| Task 4 | `src/GntTools.Kepco/KepcoLabelBuilder.cs` | 전력 라벨 포맷 |
| Task 5 | `src/GntTools.Kepco/KepcoService.cs` | 전력 비즈니스 로직 |
| Task 6 | `src/GntTools.Kepco/JunctionChecker.cs` | 분기점 오류 검증 |
| Task 7 | `src/GntTools.Kepco/KepcoCommands.cs` | CLI 명령 |
| Task 8 | `src/GntTools.UI/ViewModels/KepcoViewModel.cs` | KEPCO 탭 VM |
| Task 8 | `src/GntTools.UI/Controls/KepcoPanel.xaml(.cs)` | KEPCO 탭 UI |

---

### Task 1: KEPCO_EXT 스키마 및 레코드

**Files:**
- Create: `src/GntTools.Kepco/KepcoExtSchema.cs`
- Create: `src/GntTools.Kepco/KepcoExtRecord.cs`

- [ ] **Step 1: KepcoExtSchema.cs 작성**

```csharp
// src/GntTools.Kepco/KepcoExtSchema.cs
using System.Collections.Generic;
using Autodesk.Gis.Map.Constants;
using GntTools.Core.Odt;

namespace GntTools.Kepco
{
    /// <summary>KEPCO_EXT 전력통신 확장 테이블 (3 fields)</summary>
    public class KepcoExtSchema : IOdtSchema
    {
        public string TableName => "KEPCO_EXT";
        public string Description => "전력통신 확장 속성";

        public IReadOnlyList<OdtFieldDef> Fields { get; } = new List<OdtFieldDef>
        {
            new OdtFieldDef("PIPDAT", "구경별데이터(JSON)", DataType.Character),
            new OdtFieldDef("BXH",    "가로x세로",         DataType.Character),
            new OdtFieldDef("TMPIDN", "그룹ID",            DataType.Character),
        }.AsReadOnly();
    }
}
```

- [ ] **Step 2: KepcoExtRecord.cs 작성**

```csharp
// src/GntTools.Kepco/KepcoExtRecord.cs
using System.Collections.Generic;
using System.Linq;
using System.Runtime.Serialization;
using System.Runtime.Serialization.Json;
using System.IO;
using System.Text;

namespace GntTools.Kepco
{
    /// <summary>KEPCO_EXT 레코드 DTO</summary>
    public class KepcoExtRecord
    {
        /// <summary>구경별 데이터: key=D200, value="3(2)"</summary>
        public Dictionary<string, string> PipeData { get; set; }
            = new Dictionary<string, string>();

        public string BxH { get; set; } = "";   // "1.36x0.86"
        public string GroupId { get; set; } = "";

        /// <summary>PipeData → JSON 문자열</summary>
        public string PipeDataToJson()
        {
            if (PipeData == null || PipeData.Count == 0) return "{}";
            // 간단 직렬화 (0이 아닌 항목만)
            var parts = PipeData
                .Where(kv => kv.Value != "0(0)" && !string.IsNullOrEmpty(kv.Value))
                .Select(kv => $"\"{kv.Key}\":\"{kv.Value}\"");
            return "{" + string.Join(",", parts) + "}";
        }

        /// <summary>JSON → PipeData 역직렬화</summary>
        public static Dictionary<string, string> ParsePipeDataJson(string json)
        {
            var result = new Dictionary<string, string>();
            if (string.IsNullOrEmpty(json) || json == "{}") return result;

            // 간단 파싱 (정규식 없이)
            json = json.Trim('{', '}');
            var pairs = json.Split(',');
            foreach (var pair in pairs)
            {
                var kv = pair.Split(':');
                if (kv.Length == 2)
                {
                    string key = kv[0].Trim().Trim('"');
                    string val = kv[1].Trim().Trim('"');
                    result[key] = val;
                }
            }
            return result;
        }

        public Dictionary<string, object> ToDictionary()
        {
            return new Dictionary<string, object>
            {
                ["PIPDAT"] = PipeDataToJson(),
                ["BXH"] = BxH,
                ["TMPIDN"] = GroupId,
            };
        }
    }
}
```

- [ ] **Step 3: 빌드 확인 후 커밋**

```bash
git add src/GntTools.Kepco/KepcoExtSchema.cs src/GntTools.Kepco/KepcoExtRecord.cs
git commit -m "feat(kepco): add KEPCO_EXT schema (3 fields) and record DTO

- PIPDAT: JSON 직렬화/역직렬화 지원
- BXH: 가로x세로 문자열
- TMPIDN: 그룹ID"
```

---

### Task 2: SectionCounter — 단면 원/해치 카운팅

**Files:**
- Create: `src/GntTools.Kepco/SectionCounter.cs`
- Ref: `old_make/Kepco_tools2/` 기존 카운팅 로직

> 기존 VB.NET: 사용자가 단면 영역을 선택하면, 그 안의 원과 해치를 구경별로 카운팅

- [ ] **Step 1: SectionCounter.cs 작성**

```csharp
// src/GntTools.Kepco/SectionCounter.cs
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
                        string key = $"D{diamMm}";

                        // 가장 가까운 표준 구경으로 매칭
                        key = MatchToStandardDiameter(diamMm, settings.Kepco.Diameters);

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
                    // 원 위치/구경 목록
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

                        // 해치 중심 → 가장 가까운 원 찾기
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
```

- [ ] **Step 2: 빌드 확인 후 커밋**

```bash
git add src/GntTools.Kepco/SectionCounter.cs
git commit -m "feat(kepco): implement SectionCounter for circle/hatch counting

- 크로싱 윈도우로 단면 선택
- 원: 반지름 → mm → 표준 구경 매칭
- 해치: 가장 가까운 원의 구경에 매칭"
```

---

### Task 3: BxHCalculator — 가로x세로 측정

**Files:**
- Create: `src/GntTools.Kepco/BxHCalculator.cs`

- [ ] **Step 1: BxHCalculator.cs 작성**

```csharp
// src/GntTools.Kepco/BxHCalculator.cs
using System;
using Autodesk.AutoCAD.ApplicationServices;
using Autodesk.AutoCAD.DatabaseServices;
using Autodesk.AutoCAD.EditorInput;
using GntTools.Core.Selection;

namespace GntTools.Kepco
{
    /// <summary>BxH (가로×세로) 측정</summary>
    public class BxHCalculator
    {
        /// <summary>사용자가 B(가로)와 H(세로) 객체를 선택하여 치수 측정</summary>
        public (double b, double h, string bxhStr) MeasureFromSelection()
        {
            var doc = Application.DocumentManager.MdiActiveDocument;
            var ed = doc.Editor;
            var db = doc.Database;

            // B(가로) 폴리라인 선택
            ed.WriteMessage("\nB(가로) 선을 선택하세요.");
            var selector = new EntitySelector();
            var bId = selector.SelectOne(null, "\nB(가로) 선택: ");
            double b = 0;

            if (!bId.IsNull)
            {
                using (var tr = db.TransactionManager.StartTransaction())
                {
                    var ent = tr.GetObject(bId, OpenMode.ForRead) as Curve;
                    if (ent != null)
                        b = Math.Round(ent.GetDistAtPoint(ent.EndPoint), 2);
                    tr.Commit();
                }
            }

            // H(세로) 폴리라인 선택
            ed.WriteMessage("\nH(세로) 선을 선택하세요.");
            var hId = selector.SelectOne(null, "\nH(세로) 선택: ");
            double h = 0;

            if (!hId.IsNull)
            {
                using (var tr = db.TransactionManager.StartTransaction())
                {
                    var ent = tr.GetObject(hId, OpenMode.ForRead) as Curve;
                    if (ent != null)
                        h = Math.Round(ent.GetDistAtPoint(ent.EndPoint), 2);
                    tr.Commit();
                }
            }

            string bxhStr = $"{b:F2}x{h:F2}";
            ed.WriteMessage($"\nBxH: {bxhStr}");
            return (b, h, bxhStr);
        }
    }
}
```

- [ ] **Step 2: 빌드 확인 후 커밋**

```bash
git add src/GntTools.Kepco/BxHCalculator.cs
git commit -m "feat(kepco): implement BxHCalculator for section dimension"
```

---

### Task 4: KepcoLabelBuilder

**Files:**
- Create: `src/GntTools.Kepco/KepcoLabelBuilder.cs`

- [ ] **Step 1: KepcoLabelBuilder.cs 작성**

```csharp
// src/GntTools.Kepco/KepcoLabelBuilder.cs
using GntTools.Core.Settings;

namespace GntTools.Kepco
{
    /// <summary>전력통신 라벨 포맷</summary>
    public static class KepcoLabelBuilder
    {
        /// <summary>관라벨: "ELP L=12.3m"</summary>
        public static string BuildPipeLabel(string material, double length)
        {
            var s = AppSettings.Instance.Common;
            return $"{material} L={length.ToString($"F{s.LengthDecimals}")}m";
        }

        /// <summary>심도 라벨: "1.2"</summary>
        public static string BuildDepthLabel(double depth)
        {
            var s = AppSettings.Instance.Common;
            return depth.ToString($"F{s.DepthDecimals}");
        }

        /// <summary>BxH 라벨: "1.36×0.86"</summary>
        public static string BuildBxHLabel(string bxh)
        {
            return bxh.Replace("x", "×");
        }
    }
}
```

- [ ] **Step 2: 빌드 확인 후 커밋**

```bash
git add src/GntTools.Kepco/KepcoLabelBuilder.cs
git commit -m "feat(kepco): implement KepcoLabelBuilder"
```

---

### Task 5: KepcoService — 전력통신 비즈니스 로직

**Files:**
- Create: `src/GntTools.Kepco/KepcoService.cs`

- [ ] **Step 1: KepcoService.cs 작성**

```csharp
// src/GntTools.Kepco/KepcoService.cs
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
```

- [ ] **Step 2: 빌드 확인 후 커밋**

```bash
git add src/GntTools.Kepco/KepcoService.cs
git commit -m "feat(kepco): implement KepcoService with JSON pipe data

- 구경별 PipeData JSON 직렬화하여 ODT 저장
- BxH 저장
- 자동 심도 (정점별 텍스트)"
```

---

### Task 6: JunctionChecker — 분기점 오류검증

**Files:**
- Create: `src/GntTools.Kepco/JunctionChecker.cs`
- Bug fix: `chkPipe` 누적 차감 로직 오류 → 원본 배열 보존 (스펙 §8)

- [ ] **Step 1: JunctionChecker.cs 작성**

```csharp
// src/GntTools.Kepco/JunctionChecker.cs
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
```

- [ ] **Step 2: 빌드 확인 후 커밋**

```bash
git add src/GntTools.Kepco/JunctionChecker.cs
git commit -m "feat(kepco): implement JunctionChecker with VB.NET bug fix

- 분기점 자동 탐색 (좌표 허용오차 0.01)
- 구경별 유입/유출 카운트 비교
- fix: 원본 배열 보존 후 비교 (누적 차감 버그 수정)"
```

---

### Task 7: KepcoCommands

**Files:**
- Create: `src/GntTools.Kepco/KepcoCommands.cs`

- [ ] **Step 1: KepcoCommands.cs 작성**

```csharp
// src/GntTools.Kepco/KepcoCommands.cs
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
```

- [ ] **Step 2: 빌드 확인 후 커밋**

```bash
git add src/GntTools.Kepco/KepcoCommands.cs
git commit -m "feat(kepco): implement CLI commands ATT/EDIT/CHK"
```

---

### Task 8: KepcoPanel — KEPCO 탭 UI

**Files:**
- Create: `src/GntTools.UI/ViewModels/KepcoViewModel.cs`
- Create: `src/GntTools.UI/Controls/KepcoPanel.xaml(.cs)`
- Modify: `src/GntTools.UI/PaletteManager.cs` (KEPCO 탭 추가)

- [ ] **Step 1: KepcoViewModel.cs 작성**

```csharp
// src/GntTools.UI/ViewModels/KepcoViewModel.cs
using System.Collections.Generic;
using System.Windows.Input;
using Autodesk.AutoCAD.ApplicationServices;
using GntTools.Core.Selection;
using GntTools.Core.Settings;
using GntTools.Kepco;

namespace GntTools.UI.ViewModels
{
    public class KepcoViewModel : ViewModelBase
    {
        private readonly KepcoService _service = new KepcoService();
        private readonly SectionCounter _counter = new SectionCounter();
        private readonly BxHCalculator _bxhCalc = new BxHCalculator();
        private readonly JunctionChecker _checker = new JunctionChecker();

        private string _installYear;
        public string InstallYear { get => _installYear; set => SetProperty(ref _installYear, value); }
        private string _material;
        public string Material { get => _material; set => SetProperty(ref _material, value); }

        // 단면 카운팅 결과 표시
        private string _sectionInfo = "";
        public string SectionInfo { get => _sectionInfo; set => SetProperty(ref _sectionInfo, value); }

        private string _bxh = "";
        public string BxH { get => _bxh; set => SetProperty(ref _bxh, value); }

        private bool _isUndetected;
        public bool IsUndetected { get => _isUndetected; set => SetProperty(ref _isUndetected, value); }

        private Dictionary<string, string> _pipeData = new Dictionary<string, string>();

        public ICommand SelectSectionCommand { get; }
        public ICommand SelectBCommand { get; }
        public ICommand SelectHCommand { get; }
        public ICommand CreateCommand { get; }
        public ICommand ModifyCommand { get; }
        public ICommand CheckCommand { get; }

        public KepcoViewModel()
        {
            var s = AppSettings.Instance;
            InstallYear = s.Kepco.Defaults.Year;
            Material = s.Kepco.Defaults.Material;

            SelectSectionCommand = new RelayCommand(DoSelectSection);
            SelectBCommand = new RelayCommand(DoSelectBH);
            CreateCommand = new RelayCommand(DoCreate);
            ModifyCommand = new RelayCommand(DoModify);
            CheckCommand = new RelayCommand(DoCheck);
        }

        private void DoSelectSection()
        {
            var doc = Application.DocumentManager.MdiActiveDocument;
            using (doc.LockDocument())
            {
                var result = _counter.CountInSelection();
                _pipeData = result.ToPipeData();

                // 표시용 문자열
                var lines = new List<string>();
                var settings = AppSettings.Instance;
                foreach (int d in settings.Kepco.Diameters)
                {
                    string key = $"D{d}";
                    lines.Add($"{key}: {result.GetCountString(key)}");
                }
                SectionInfo = string.Join("  ", lines);
            }
        }

        private void DoSelectBH()
        {
            var doc = Application.DocumentManager.MdiActiveDocument;
            using (doc.LockDocument())
            {
                var (b, h, bxhStr) = _bxhCalc.MeasureFromSelection();
                BxH = bxhStr;
            }
        }

        private void DoCreate()
        {
            var doc = Application.DocumentManager.MdiActiveDocument;
            using (doc.LockDocument())
            {
                _service.EnsureTables();
                var selector = new EntitySelector();
                var polyId = selector.SelectOne(
                    EntitySelector.PolylineFilter(),
                    "\n전력관로 폴리라인을 선택하세요: ");
                if (polyId.IsNull) return;

                _service.CreateAttribute(new KepcoInputData
                {
                    InstallYear = InstallYear,
                    Material = Material,
                    PipeData = _pipeData,
                    BxH = BxH,
                    IsUndetected = IsUndetected,
                }, polyId);
            }
        }

        private void DoModify()
        {
            var doc = Application.DocumentManager.MdiActiveDocument;
            using (doc.LockDocument())
            {
                _service.EnsureTables();
                var selector = new EntitySelector();
                var polyId = selector.SelectOne(
                    EntitySelector.PolylineFilter(),
                    "\n수정할 전력관로를 선택하세요: ");
                if (polyId.IsNull) return;

                _service.ModifyAttribute(new KepcoInputData
                {
                    InstallYear = InstallYear,
                    Material = Material,
                    PipeData = _pipeData,
                    BxH = BxH,
                    IsUndetected = IsUndetected,
                }, polyId);
            }
        }

        private void DoCheck()
        {
            var doc = Application.DocumentManager.MdiActiveDocument;
            using (doc.LockDocument())
            {
                _checker.CheckSelected();
            }
        }
    }
}
```

- [ ] **Step 2: KepcoPanel.xaml 작성**

```xml
<!-- src/GntTools.UI/Controls/KepcoPanel.xaml -->
<UserControl x:Class="GntTools.UI.Controls.KepcoPanel"
             xmlns="http://schemas.microsoft.com/winfx/2006/xaml/presentation"
             xmlns:x="http://schemas.microsoft.com/winfx/2006/xaml"
             Padding="8">
    <ScrollViewer VerticalScrollBarVisibility="Auto">
        <StackPanel>
            <!-- 기본정보 -->
            <GroupBox Header="기본정보" Margin="0,0,0,8">
                <Grid>
                    <Grid.ColumnDefinitions>
                        <ColumnDefinition Width="70"/>
                        <ColumnDefinition Width="*"/>
                    </Grid.ColumnDefinitions>
                    <Grid.RowDefinitions>
                        <RowDefinition Height="Auto"/>
                        <RowDefinition Height="Auto"/>
                    </Grid.RowDefinitions>
                    <TextBlock Text="설치년도" Grid.Row="0" VerticalAlignment="Center" Margin="0,2"/>
                    <TextBox Text="{Binding InstallYear}" Grid.Row="0" Grid.Column="1" Margin="4,2"/>
                    <TextBlock Text="관재질" Grid.Row="1" VerticalAlignment="Center" Margin="0,2"/>
                    <TextBox Text="{Binding Material}" Grid.Row="1" Grid.Column="1" Margin="4,2"/>
                </Grid>
            </GroupBox>

            <!-- 단면정보 -->
            <GroupBox Header="단면정보" Margin="0,0,0,8">
                <StackPanel>
                    <Button Content="단면선택" Command="{Binding SelectSectionCommand}"
                            Width="80" HorizontalAlignment="Left" Margin="0,2"/>
                    <TextBlock Text="{Binding SectionInfo}" TextWrapping="Wrap"
                               Margin="0,4" FontSize="11"/>
                    <Separator Margin="0,4"/>
                    <Button Content="BxH 선택" Command="{Binding SelectBCommand}"
                            Width="80" HorizontalAlignment="Left" Margin="0,2"/>
                    <StackPanel Orientation="Horizontal" Margin="0,4">
                        <TextBlock Text="BxH: " FontWeight="Bold"/>
                        <TextBlock Text="{Binding BxH}"/>
                    </StackPanel>
                </StackPanel>
            </GroupBox>

            <!-- 심도 -->
            <GroupBox Header="심도" Margin="0,0,0,8">
                <StackPanel>
                    <TextBlock Text="자동측정 (정점별)" Margin="0,2"/>
                    <CheckBox Content="불탐" IsChecked="{Binding IsUndetected}" Margin="0,4"/>
                </StackPanel>
            </GroupBox>

            <!-- 버튼 -->
            <StackPanel Orientation="Horizontal" HorizontalAlignment="Center" Margin="0,8">
                <Button Content="신규입력" Command="{Binding CreateCommand}"
                        Width="70" Height="30" Margin="0,0,4,0"/>
                <Button Content="수정" Command="{Binding ModifyCommand}"
                        Width="50" Height="30" Margin="0,0,4,0"/>
                <Button Content="오류검증" Command="{Binding CheckCommand}"
                        Width="70" Height="30"/>
            </StackPanel>
        </StackPanel>
    </ScrollViewer>
</UserControl>
```

- [ ] **Step 3: KepcoPanel.xaml.cs + PaletteManager 수정**

```csharp
// KepcoPanel.xaml.cs
using System.Windows.Controls;
namespace GntTools.UI.Controls
{
    public partial class KepcoPanel : UserControl
    {
        public KepcoPanel() { InitializeComponent(); }
    }
}
```

PaletteManager.Initialize()에 추가:
```csharp
public static ViewModels.KepcoViewModel KepcoVm { get; private set; }

// Initialize() 내:
KepcoVm = new ViewModels.KepcoViewModel();
_ps.AddVisual("전력통신", new KepcoPanel { DataContext = KepcoVm });
```

탭 최종 순서: 상수(0), 하수(1), 전력통신(2), 환경설정(3)

- [ ] **Step 4: 빌드 확인 후 커밋**

```bash
git add src/GntTools.UI/ViewModels/KepcoViewModel.cs
git add src/GntTools.UI/Controls/KepcoPanel.xaml
git add src/GntTools.UI/Controls/KepcoPanel.xaml.cs
git add src/GntTools.UI/PaletteManager.cs
git commit -m "feat(ui): implement KEPCO tab with section counter and BxH

- KepcoViewModel: 단면선택/BxH선택/오류검증 버튼
- KepcoPanel.xaml: 기본정보/단면정보/심도 GroupBox
- PaletteManager: 4탭 구성 완성 (상수/하수/전력통신/환경설정)"
```

---

## Phase 4 완료 체크리스트

- [ ] GntTools.Kepco: 7개 소스 파일
- [ ] SectionCounter: 원/해치 구경별 카운팅
- [ ] BxHCalculator: B/H 선 선택 → 치수 측정
- [ ] JunctionChecker: 분기점 정합성 검증 (VB.NET 버그 수정)
- [ ] KEPCO 탭 UI: 단면선택/BxH/불탐/오류검증
- [ ] CLI: GNTTOOLS_KEPCO_ATT, EDIT, CHK 동작
- [ ] 팔레트 4탭 완성: 상수, 하수, 전력통신, 환경설정

**Phase 5 → `plan-phase5-integration.md` (통합 테스트 및 배포)**
