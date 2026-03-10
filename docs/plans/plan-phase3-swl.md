# Phase 3: SWL 하수 도메인 구현 계획

> **For agentic workers:** REQUIRED: Use superpowers:subagent-driven-development (if subagents available) or superpowers:executing-plans to implement this plan. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** 하수관로 도메인 구현 — 관저고/관상고/구배 자동계산 포함

**선행 조건:** Phase 1 (Core) + Phase 2 (WTL + UI 기본틀) 완료

**Spec:** `docs/specs/2026-03-10-gnttools-integration-design.md` §2 SWL_EXT, §5 하수 탭

---

## File Map

| Task | 파일 | 역할 |
|------|------|------|
| Task 1 | `src/GntTools.Swl/SwlExtSchema.cs` | SWL_EXT 스키마 (12 fields) |
| Task 1 | `src/GntTools.Swl/SwlExtRecord.cs` | SWL_EXT 레코드 DTO |
| Task 2 | `src/GntTools.Swl/ElevationCalculator.cs` | 관저고/관상고/구배 계산 |
| Task 3 | `src/GntTools.Swl/SwlLabelBuilder.cs` | 하수 라벨 포맷 |
| Task 4 | `src/GntTools.Swl/SwlService.cs` | 하수 비즈니스 로직 |
| Task 5 | `src/GntTools.Swl/SwlCommands.cs` | CLI 명령 등록 |
| Task 6 | `src/GntTools.UI/ViewModels/SwlViewModel.cs` | 하수 탭 VM |
| Task 6 | `src/GntTools.UI/Controls/SwlPanel.xaml(.cs)` | 하수 탭 UI |
| Task 7 | `src/GntTools.UI/PaletteManager.cs` | SWL 탭 추가 (수정) |

---

### Task 1: SWL_EXT 스키마 및 레코드 DTO

**Files:**
- Create: `src/GntTools.Swl/SwlExtSchema.cs`
- Create: `src/GntTools.Swl/SwlExtRecord.cs`

- [ ] **Step 1: SwlExtSchema.cs 작성**

```csharp
// src/GntTools.Swl/SwlExtSchema.cs
using System.Collections.Generic;
using Autodesk.Gis.Map.Constants;
using GntTools.Core.Odt;

namespace GntTools.Swl
{
    /// <summary>SWL_EXT 하수관로 확장 테이블 (12 fields)</summary>
    public class SwlExtSchema : IOdtSchema
    {
        public string TableName => "SWL_EXT";
        public string Description => "하수관로 확장 속성";

        public IReadOnlyList<OdtFieldDef> Fields { get; } = new List<OdtFieldDef>
        {
            new OdtFieldDef("PIPCDE", "용도코드",       DataType.Character),
            new OdtFieldDef("PIPHOL", "가로길이(m)",    DataType.Real),
            new OdtFieldDef("PIPVEL", "세로길이(m)",    DataType.Real),
            new OdtFieldDef("PIPLIN", "통로수",         DataType.Integer),
            new OdtFieldDef("SBKHLT", "시점지반고(m)",  DataType.Real),
            new OdtFieldDef("SBLHLT", "종점지반고(m)",  DataType.Real),
            new OdtFieldDef("SBKALT", "시점관저고(m)",  DataType.Real),
            new OdtFieldDef("SBLALT", "종점관저고(m)",  DataType.Real),
            new OdtFieldDef("ST_ALT", "시점관상고(m)",  DataType.Real),
            new OdtFieldDef("ED_ALT", "종점관상고(m)",  DataType.Real),
            new OdtFieldDef("PIPSLP", "평균구배(%)",    DataType.Real),
            new OdtFieldDef("TMPIDN", "그룹ID",        DataType.Character),
        }.AsReadOnly();
    }
}
```

- [ ] **Step 2: SwlExtRecord.cs 작성**

```csharp
// src/GntTools.Swl/SwlExtRecord.cs
using System.Collections.Generic;

namespace GntTools.Swl
{
    /// <summary>SWL_EXT 레코드 DTO</summary>
    public class SwlExtRecord
    {
        public string UseCode { get; set; } = "";         // PIPCDE
        public double BoxWidth { get; set; }              // PIPHOL (박스관 가로)
        public double BoxHeight { get; set; }             // PIPVEL (박스관 세로)
        public int LineCount { get; set; } = 1;           // PIPLIN (통로수)
        public double BeginGroundHeight { get; set; }     // SBKHLT
        public double EndGroundHeight { get; set; }       // SBLHLT
        public double BeginInvertLevel { get; set; }      // SBKALT (시점관저고)
        public double EndInvertLevel { get; set; }        // SBLALT (종점관저고)
        public double BeginCrownLevel { get; set; }       // ST_ALT (시점관상고)
        public double EndCrownLevel { get; set; }         // ED_ALT (종점관상고)
        public double Slope { get; set; }                 // PIPSLP (구배%)
        public string GroupId { get; set; } = "";         // TMPIDN

        public Dictionary<string, object> ToDictionary()
        {
            return new Dictionary<string, object>
            {
                ["PIPCDE"] = UseCode,
                ["PIPHOL"] = BoxWidth,
                ["PIPVEL"] = BoxHeight,
                ["PIPLIN"] = LineCount,
                ["SBKHLT"] = BeginGroundHeight,
                ["SBLHLT"] = EndGroundHeight,
                ["SBKALT"] = BeginInvertLevel,
                ["SBLALT"] = EndInvertLevel,
                ["ST_ALT"] = BeginCrownLevel,
                ["ED_ALT"] = EndCrownLevel,
                ["PIPSLP"] = Slope,
                ["TMPIDN"] = GroupId,
            };
        }
    }
}
```

- [ ] **Step 3: 빌드 확인 후 커밋**

```bash
git add src/GntTools.Swl/SwlExtSchema.cs src/GntTools.Swl/SwlExtRecord.cs
git commit -m "feat(swl): add SWL_EXT schema (12 fields) and record DTO

- 관저고/관상고/구배 + 박스관(가로/세로) + 용도코드 + 통로수"
```

---

### Task 2: ElevationCalculator — 관저고/관상고/구배 계산

**Files:**
- Create: `src/GntTools.Swl/ElevationCalculator.cs`
- Ref: 스펙 §2 (관저고 = 지반고 - 심도, 관상고 = 관저고 + 구경, 구배 = (시점관저고 - 종점관저고) / 연장 × 100)

- [ ] **Step 1: ElevationCalculator.cs 작성**

```csharp
// src/GntTools.Swl/ElevationCalculator.cs
using System;

namespace GntTools.Swl
{
    /// <summary>하수관로 표고 계산 결과</summary>
    public class ElevationResult
    {
        public double BeginInvertLevel { get; set; }  // 시점관저고
        public double EndInvertLevel { get; set; }    // 종점관저고
        public double BeginCrownLevel { get; set; }   // 시점관상고
        public double EndCrownLevel { get; set; }     // 종점관상고
        public double Slope { get; set; }             // 구배(%)
    }

    /// <summary>관저고/관상고/구배 자동 계산</summary>
    public static class ElevationCalculator
    {
        /// <summary>
        /// 표고 계산
        /// </summary>
        /// <param name="beginGroundHeight">시점지반고 (m)</param>
        /// <param name="endGroundHeight">종점지반고 (m)</param>
        /// <param name="beginDepth">시점심도 (m)</param>
        /// <param name="endDepth">종점심도 (m)</param>
        /// <param name="diameterMeter">구경 (m 단위, 예: 0.3)</param>
        /// <param name="pipeLength">관로 연장 (m)</param>
        public static ElevationResult Calculate(
            double beginGroundHeight, double endGroundHeight,
            double beginDepth, double endDepth,
            double diameterMeter, double pipeLength)
        {
            // 관저고 = 지반고 - 심도
            double beginInvert = beginGroundHeight - beginDepth;
            double endInvert = endGroundHeight - endDepth;

            // 관상고 = 관저고 + 구경(m)
            double beginCrown = beginInvert + diameterMeter;
            double endCrown = endInvert + diameterMeter;

            // 구배 = (시점관저고 - 종점관저고) / 연장 × 100
            double slope = 0;
            if (pipeLength > 0)
                slope = Math.Round(
                    (beginInvert - endInvert) / pipeLength * 100, 3);

            return new ElevationResult
            {
                BeginInvertLevel = Math.Round(beginInvert, 3),
                EndInvertLevel = Math.Round(endInvert, 3),
                BeginCrownLevel = Math.Round(beginCrown, 3),
                EndCrownLevel = Math.Round(endCrown, 3),
                Slope = slope,
            };
        }

        /// <summary>구경 문자열(mm) → m 변환</summary>
        public static double DiameterToMeter(string diameterStr)
        {
            if (double.TryParse(diameterStr, out double mm))
                return mm / 1000.0;
            return 0;
        }
    }
}
```

- [ ] **Step 2: 빌드 확인 후 커밋**

```bash
git add src/GntTools.Swl/ElevationCalculator.cs
git commit -m "feat(swl): implement ElevationCalculator

- 관저고 = 지반고 - 심도
- 관상고 = 관저고 + 구경(m)
- 구배 = (시점관저고 - 종점관저고) / 연장 × 100"
```

---

### Task 3: SwlLabelBuilder — 하수 라벨 포맷

**Files:**
- Create: `src/GntTools.Swl/SwlLabelBuilder.cs`

- [ ] **Step 1: SwlLabelBuilder.cs 작성**

```csharp
// src/GntTools.Swl/SwlLabelBuilder.cs
using GntTools.Core.Settings;

namespace GntTools.Swl
{
    /// <summary>하수관로 라벨 포맷 빌더</summary>
    public static class SwlLabelBuilder
    {
        /// <summary>관라벨: "HP ∅300 L=15.2m"</summary>
        public static string BuildPipeLabel(string material, string diameter,
            double length)
        {
            var s = AppSettings.Instance.Common;
            return $"{material} ∅{diameter} L={length.ToString($"F{s.LengthDecimals}")}m";
        }

        /// <summary>박스관 라벨: "HP □1200x800 L=15.2m"</summary>
        public static string BuildBoxLabel(string material,
            double width, double height, double length)
        {
            var s = AppSettings.Instance.Common;
            return $"{material} □{width:F0}x{height:F0} L={length.ToString($"F{s.LengthDecimals}")}m";
        }

        /// <summary>심도 라벨: "1.2"</summary>
        public static string BuildDepthLabel(double depth)
        {
            var s = AppSettings.Instance.Common;
            return depth.ToString($"F{s.DepthDecimals}");
        }

        /// <summary>관저고 라벨: "IL=25.300"</summary>
        public static string BuildInvertLabel(double invertLevel)
        {
            return $"IL={invertLevel:F3}";
        }

        /// <summary>구배 라벨: "S=0.5%"</summary>
        public static string BuildSlopeLabel(double slope)
        {
            return $"S={slope:F3}%";
        }
    }
}
```

- [ ] **Step 2: 빌드 확인 후 커밋**

```bash
git add src/GntTools.Swl/SwlLabelBuilder.cs
git commit -m "feat(swl): implement SwlLabelBuilder with box/slope labels"
```

---

### Task 4: SwlService — 하수 비즈니스 로직

**Files:**
- Create: `src/GntTools.Swl/SwlService.cs`

- [ ] **Step 1: SwlService.cs 작성**

```csharp
// src/GntTools.Swl/SwlService.cs
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
```

- [ ] **Step 2: 빌드 확인 후 커밋**

```bash
git add src/GntTools.Swl/SwlService.cs
git commit -m "feat(swl): implement SwlService with elevation calculation

- CreateAttribute: ODT + 관저고/관상고/구배 자동계산 + 라벨
- ModifyAttribute: 삭제 후 재생성
- 박스관/원형관 라벨 분기 처리"
```

---

### Task 5: SwlCommands — CLI 명령

**Files:**
- Create: `src/GntTools.Swl/SwlCommands.cs`

- [ ] **Step 1: SwlCommands.cs 작성**

WtlCommands와 동일 패턴. 추가 프롬프트: 용도코드, 박스관 여부, 가로/세로, 통로수.

```csharp
// src/GntTools.Swl/SwlCommands.cs
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

            // 설치년도/관재질/구경 (WTL과 동일 패턴)
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

            // 심도 (WTL과 동일)
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
```

- [ ] **Step 2: 빌드 확인 후 커밋**

```bash
git add src/GntTools.Swl/SwlCommands.cs
git commit -m "feat(swl): implement CLI commands GNTTOOLS_SWL_ATT/EDIT

- 원형관/박스관 분기 프롬프트
- 용도코드, 통로수 추가 입력"
```

---

### Task 6: SwlPanel — 하수 탭 UI

**Files:**
- Create: `src/GntTools.UI/ViewModels/SwlViewModel.cs`
- Create: `src/GntTools.UI/Controls/SwlPanel.xaml`
- Create: `src/GntTools.UI/Controls/SwlPanel.xaml.cs`

- [ ] **Step 1: SwlViewModel.cs 작성**

WtlViewModel과 동일 구조 + 용도코드, 박스관, 통로수, 관저고/관상고/구배 읽기전용 표시.

```csharp
// src/GntTools.UI/ViewModels/SwlViewModel.cs
using System.Windows.Input;
using Autodesk.AutoCAD.ApplicationServices;
using GntTools.Core.Selection;
using GntTools.Core.Settings;
using GntTools.Swl;

namespace GntTools.UI.ViewModels
{
    public class SwlViewModel : ViewModelBase
    {
        private readonly SwlService _service = new SwlService();

        // 기본정보
        private string _installYear;
        public string InstallYear { get => _installYear; set => SetProperty(ref _installYear, value); }
        private string _material;
        public string Material { get => _material; set => SetProperty(ref _material, value); }
        private string _diameter;
        public string Diameter { get => _diameter; set => SetProperty(ref _diameter, value); }
        private string _useCode;
        public string UseCode { get => _useCode; set => SetProperty(ref _useCode, value); }

        // 박스관
        private bool _isBoxPipe;
        public bool IsBoxPipe { get => _isBoxPipe; set => SetProperty(ref _isBoxPipe, value); }
        private double _boxWidth;
        public double BoxWidth { get => _boxWidth; set => SetProperty(ref _boxWidth, value); }
        private double _boxHeight;
        public double BoxHeight { get => _boxHeight; set => SetProperty(ref _boxHeight, value); }
        private int _lineCount = 1;
        public int LineCount { get => _lineCount; set => SetProperty(ref _lineCount, value); }

        // 심도
        private bool _isAutoDepth;
        public bool IsAutoDepth { get => _isAutoDepth; set => SetProperty(ref _isAutoDepth, value); }
        private bool _isManualDepth = true;
        public bool IsManualDepth { get => _isManualDepth; set => SetProperty(ref _isManualDepth, value); }
        private double _beginDepth;
        public double BeginDepth { get => _beginDepth; set => SetProperty(ref _beginDepth, value); }
        private double _endDepth;
        public double EndDepth { get => _endDepth; set => SetProperty(ref _endDepth, value); }
        private bool _isUndetected;
        public bool IsUndetected { get => _isUndetected; set => SetProperty(ref _isUndetected, value); }

        // 지반고
        private double _beginGroundHeight;
        public double BeginGroundHeight { get => _beginGroundHeight; set => SetProperty(ref _beginGroundHeight, value); }
        private double _endGroundHeight;
        public double EndGroundHeight { get => _endGroundHeight; set => SetProperty(ref _endGroundHeight, value); }

        // 계산결과 (읽기전용 표시)
        private string _elevationInfo = "";
        public string ElevationInfo { get => _elevationInfo; set => SetProperty(ref _elevationInfo, value); }

        private bool _useLeader;
        public bool UseLeader { get => _useLeader; set => SetProperty(ref _useLeader, value); }

        public ICommand CreateCommand { get; }
        public ICommand ModifyCommand { get; }
        public ICommand CalcElevationCommand { get; }

        public SwlViewModel()
        {
            var s = AppSettings.Instance;
            InstallYear = s.Swl.Defaults.Year;
            Material = s.Swl.Defaults.Material;
            Diameter = s.Swl.Defaults.Diameter;
            UseCode = s.Swl.Defaults.UseCode;

            CreateCommand = new RelayCommand(ExecuteCreate);
            ModifyCommand = new RelayCommand(ExecuteModify);
            CalcElevationCommand = new RelayCommand(CalcPreview);
        }

        /// <summary>표고 미리보기 계산</summary>
        private void CalcPreview()
        {
            double diamM = ElevationCalculator.DiameterToMeter(Diameter);
            var elev = ElevationCalculator.Calculate(
                BeginGroundHeight, EndGroundHeight,
                BeginDepth, EndDepth, diamM, 1.0);
            ElevationInfo = $"관저고: {elev.BeginInvertLevel:F3}→{elev.EndInvertLevel:F3}  구배: {elev.Slope:F3}%";
        }

        public SwlInputData ToInputData()
        {
            return new SwlInputData
            {
                InstallYear = InstallYear,
                Material = Material,
                Diameter = Diameter,
                UseCode = UseCode,
                IsBoxPipe = IsBoxPipe,
                BoxWidth = BoxWidth,
                BoxHeight = BoxHeight,
                LineCount = LineCount,
                IsAutoDepth = IsAutoDepth,
                ManualBeginDepth = BeginDepth,
                ManualEndDepth = EndDepth,
                IsUndetected = IsUndetected,
                BeginGroundHeight = BeginGroundHeight,
                EndGroundHeight = EndGroundHeight,
                UseLeader = UseLeader,
            };
        }

        private void ExecuteCreate()
        {
            var doc = Application.DocumentManager.MdiActiveDocument;
            using (doc.LockDocument())
            {
                _service.EnsureTables();
                var selector = new EntitySelector();
                var polyId = selector.SelectOne(
                    EntitySelector.PolylineFilter(),
                    "\n하수관로 폴리라인을 선택하세요: ");
                if (polyId.IsNull) return;
                _service.CreateAttribute(ToInputData(), polyId);
            }
        }

        private void ExecuteModify()
        {
            var doc = Application.DocumentManager.MdiActiveDocument;
            using (doc.LockDocument())
            {
                _service.EnsureTables();
                var selector = new EntitySelector();
                var polyId = selector.SelectOne(
                    EntitySelector.PolylineFilter(),
                    "\n수정할 하수관로를 선택하세요: ");
                if (polyId.IsNull) return;
                _service.ModifyAttribute(ToInputData(), polyId);
            }
        }
    }
}
```

- [ ] **Step 2: SwlPanel.xaml 작성**

```xml
<!-- src/GntTools.UI/Controls/SwlPanel.xaml -->
<UserControl x:Class="GntTools.UI.Controls.SwlPanel"
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
                        <RowDefinition Height="Auto"/>
                        <RowDefinition Height="Auto"/>
                    </Grid.RowDefinitions>
                    <TextBlock Text="용도코드" Grid.Row="0" VerticalAlignment="Center" Margin="0,2"/>
                    <TextBox Text="{Binding UseCode}" Grid.Row="0" Grid.Column="1" Margin="4,2"/>
                    <TextBlock Text="설치년도" Grid.Row="1" VerticalAlignment="Center" Margin="0,2"/>
                    <TextBox Text="{Binding InstallYear}" Grid.Row="1" Grid.Column="1" Margin="4,2"/>
                    <TextBlock Text="관재질" Grid.Row="2" VerticalAlignment="Center" Margin="0,2"/>
                    <TextBox Text="{Binding Material}" Grid.Row="2" Grid.Column="1" Margin="4,2"/>
                    <TextBlock Text="구경" Grid.Row="3" VerticalAlignment="Center" Margin="0,2"/>
                    <TextBox Text="{Binding Diameter}" Grid.Row="3" Grid.Column="1" Margin="4,2"/>
                </Grid>
            </GroupBox>

            <!-- 박스관 -->
            <GroupBox Header="박스관" Margin="0,0,0,8">
                <StackPanel>
                    <CheckBox Content="박스관" IsChecked="{Binding IsBoxPipe}" Margin="0,2"/>
                    <Grid Margin="16,4,0,0" IsEnabled="{Binding IsBoxPipe}">
                        <Grid.ColumnDefinitions>
                            <ColumnDefinition Width="60"/>
                            <ColumnDefinition Width="*"/>
                        </Grid.ColumnDefinitions>
                        <Grid.RowDefinitions>
                            <RowDefinition Height="Auto"/>
                            <RowDefinition Height="Auto"/>
                            <RowDefinition Height="Auto"/>
                        </Grid.RowDefinitions>
                        <TextBlock Text="가로(mm)" Grid.Row="0" VerticalAlignment="Center"/>
                        <TextBox Text="{Binding BoxWidth}" Grid.Row="0" Grid.Column="1" Margin="4,2"/>
                        <TextBlock Text="세로(mm)" Grid.Row="1" VerticalAlignment="Center"/>
                        <TextBox Text="{Binding BoxHeight}" Grid.Row="1" Grid.Column="1" Margin="4,2"/>
                        <TextBlock Text="통로수" Grid.Row="2" VerticalAlignment="Center"/>
                        <TextBox Text="{Binding LineCount}" Grid.Row="2" Grid.Column="1" Margin="4,2" Width="50" HorizontalAlignment="Left"/>
                    </Grid>
                </StackPanel>
            </GroupBox>

            <!-- 심도 -->
            <GroupBox Header="심도" Margin="0,0,0,8">
                <StackPanel>
                    <RadioButton Content="자동측정" IsChecked="{Binding IsAutoDepth}" Margin="0,2"/>
                    <RadioButton Content="수동입력" IsChecked="{Binding IsManualDepth}" Margin="0,2"/>
                    <Grid Margin="16,4,0,0">
                        <Grid.ColumnDefinitions>
                            <ColumnDefinition Width="70"/>
                            <ColumnDefinition Width="*"/>
                        </Grid.ColumnDefinitions>
                        <Grid.RowDefinitions>
                            <RowDefinition Height="Auto"/>
                            <RowDefinition Height="Auto"/>
                        </Grid.RowDefinitions>
                        <TextBlock Text="시점심도" Grid.Row="0" VerticalAlignment="Center"/>
                        <TextBox Text="{Binding BeginDepth}" Grid.Row="0" Grid.Column="1" Margin="4,2" IsEnabled="{Binding IsManualDepth}"/>
                        <TextBlock Text="종점심도" Grid.Row="1" VerticalAlignment="Center"/>
                        <TextBox Text="{Binding EndDepth}" Grid.Row="1" Grid.Column="1" Margin="4,2" IsEnabled="{Binding IsManualDepth}"/>
                    </Grid>
                    <CheckBox Content="불탐" IsChecked="{Binding IsUndetected}" Margin="0,4"/>
                </StackPanel>
            </GroupBox>

            <!-- 지반고 + 계산결과 -->
            <GroupBox Header="지반고 / 표고" Margin="0,0,0,8">
                <StackPanel>
                    <Grid>
                        <Grid.ColumnDefinitions>
                            <ColumnDefinition Width="80"/>
                            <ColumnDefinition Width="*"/>
                        </Grid.ColumnDefinitions>
                        <Grid.RowDefinitions>
                            <RowDefinition Height="Auto"/>
                            <RowDefinition Height="Auto"/>
                        </Grid.RowDefinitions>
                        <TextBlock Text="시점지반고" Grid.Row="0" VerticalAlignment="Center" Margin="0,2"/>
                        <TextBox Text="{Binding BeginGroundHeight}" Grid.Row="0" Grid.Column="1" Margin="4,2"/>
                        <TextBlock Text="종점지반고" Grid.Row="1" VerticalAlignment="Center" Margin="0,2"/>
                        <TextBox Text="{Binding EndGroundHeight}" Grid.Row="1" Grid.Column="1" Margin="4,2"/>
                    </Grid>
                    <Button Content="표고 미리보기" Command="{Binding CalcElevationCommand}"
                            Width="100" HorizontalAlignment="Left" Margin="0,4"/>
                    <TextBlock Text="{Binding ElevationInfo}" Margin="0,4"
                               Foreground="Gray" FontSize="11"/>
                </StackPanel>
            </GroupBox>

            <!-- 라벨 -->
            <GroupBox Header="라벨" Margin="0,0,0,8">
                <StackPanel Orientation="Horizontal">
                    <RadioButton Content="지시선" IsChecked="{Binding UseLeader}" Margin="0,0,16,0"/>
                    <RadioButton Content="관로평행"/>
                </StackPanel>
            </GroupBox>

            <!-- 버튼 -->
            <StackPanel Orientation="Horizontal" HorizontalAlignment="Center" Margin="0,8">
                <Button Content="신규입력" Command="{Binding CreateCommand}" Width="80" Height="30" Margin="0,0,8,0"/>
                <Button Content="수정" Command="{Binding ModifyCommand}" Width="80" Height="30"/>
            </StackPanel>
        </StackPanel>
    </ScrollViewer>
</UserControl>
```

- [ ] **Step 3: SwlPanel.xaml.cs 작성**

```csharp
// src/GntTools.UI/Controls/SwlPanel.xaml.cs
using System.Windows.Controls;

namespace GntTools.UI.Controls
{
    public partial class SwlPanel : UserControl
    {
        public SwlPanel()
        {
            InitializeComponent();
        }
    }
}
```

- [ ] **Step 4: 빌드 확인 후 커밋**

```bash
git add src/GntTools.UI/ViewModels/SwlViewModel.cs
git add src/GntTools.UI/Controls/SwlPanel.xaml
git add src/GntTools.UI/Controls/SwlPanel.xaml.cs
git commit -m "feat(ui): implement SWL tab with elevation preview

- SwlViewModel: 박스관/원형, 표고 미리보기, 전체 필드 바인딩
- SwlPanel.xaml: 기본정보/박스관/심도/지반고/라벨 GroupBox"
```

---

### Task 7: PaletteManager에 SWL 탭 추가

**Files:**
- Modify: `src/GntTools.UI/PaletteManager.cs`

- [ ] **Step 1: PaletteManager.cs 수정**

Initialize() 메서드에서 SWL 탭 추가:

```csharp
// 기존 WTL 탭 추가 아래에:
public static ViewModels.SwlViewModel SwlVm { get; private set; }

// Initialize() 내부:
SwlVm = new ViewModels.SwlViewModel();
_ps.AddVisual("하수", new SwlPanel { DataContext = SwlVm });
```

탭 순서: 상수(0), 하수(1), 환경설정(2)
(KEPCO는 Phase 4에서 인덱스 2에 삽입 → 환경설정이 3으로 밀림)

- [ ] **Step 2: 빌드 + 수동 테스트**

AutoCAD에서 NETLOAD → 팔레트 → 하수 탭 표시 확인

- [ ] **Step 3: 커밋**

```bash
git add src/GntTools.UI/PaletteManager.cs
git commit -m "feat(ui): add SWL tab to PaletteSet"
```

---

## Phase 3 완료 체크리스트

- [ ] GntTools.Swl: 5개 소스 파일
- [ ] ElevationCalculator: 관저고/관상고/구배 계산 정확
- [ ] SWL 탭 UI: 박스관/원형관 전환, 표고 미리보기
- [ ] CLI: GNTTOOLS_SWL_ATT, GNTTOOLS_SWL_EDIT 동작
- [ ] 팔레트 3탭: 상수, 하수, 환경설정

**Phase 4 → `plan-phase4-kepco.md` (KEPCO 전력통신 도메인)**
