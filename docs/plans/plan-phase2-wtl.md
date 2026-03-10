# Phase 2: WTL 상수 도메인 + UI 기본틀 구현 계획

> **For agentic workers:** REQUIRED: Use superpowers:subagent-driven-development (if subagents available) or superpowers:executing-plans to implement this plan. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** 가장 단순한 도메인(WTL 상수)을 먼저 구현하고, PaletteSet UI 기본틀 + 환경설정 탭까지 완성

**선행 조건:** Phase 1 (Core 라이브러리) 완료

**Spec:** `docs/specs/2026-03-10-gnttools-integration-design.md` §3~§7

---

## File Map

| Task | 파일 | 역할 |
|------|------|------|
| Task 1 | `src/GntTools.Wtl/WtlExtSchema.cs` | WTL_EXT 스키마 (2 fields) |
| Task 1 | `src/GntTools.Wtl/WtlExtRecord.cs` | WTL_EXT 레코드 DTO |
| Task 2 | `src/GntTools.Wtl/WtlLabelBuilder.cs` | 상수 라벨 포맷팅 |
| Task 3 | `src/GntTools.Wtl/WtlService.cs` | 상수 비즈니스 로직 (신규입력/수정) |
| Task 4 | `src/GntTools.Wtl/WtlCommands.cs` | CLI 명령 등록 |
| Task 5 | `src/GntTools.UI/Plugin.cs` | IExtensionApplication 진입점 |
| Task 5 | `src/GntTools.UI/PaletteManager.cs` | PaletteSet 싱글 인스턴스 |
| Task 5 | `src/GntTools.UI/Commands/PaletteCommands.cs` | GNTTOOLS_SHOW 명령 |
| Task 6 | `src/GntTools.UI/ViewModels/ViewModelBase.cs` | INotifyPropertyChanged 기반 |
| Task 6 | `src/GntTools.UI/ViewModels/RelayCommand.cs` | ICommand 구현 |
| Task 7 | `src/GntTools.UI/ViewModels/SettingsViewModel.cs` | 환경설정 VM |
| Task 7 | `src/GntTools.UI/Controls/SettingsPanel.xaml(.cs)` | 환경설정 탭 |
| Task 8 | `src/GntTools.UI/ViewModels/WtlViewModel.cs` | 상수 탭 VM |
| Task 8 | `src/GntTools.UI/Controls/WtlPanel.xaml(.cs)` | 상수 탭 UI |
| Task 9 | (통합 테스트) | NETLOAD → 팔레트 표시 → WTL 입력 |

---

## Chunk 1: WTL 도메인 로직

### Task 1: WTL_EXT 스키마 및 레코드 DTO

**Files:**
- Create: `src/GntTools.Wtl/WtlExtSchema.cs`
- Create: `src/GntTools.Wtl/WtlExtRecord.cs`

- [ ] **Step 1: WtlExtSchema.cs 작성**

```csharp
// src/GntTools.Wtl/WtlExtSchema.cs
using System.Collections.Generic;
using Autodesk.Gis.Map.Constants;
using GntTools.Core.Odt;

namespace GntTools.Wtl
{
    /// <summary>WTL_EXT 상수관로 확장 테이블 (2 fields)</summary>
    public class WtlExtSchema : IOdtSchema
    {
        public string TableName => "WTL_EXT";
        public string Description => "상수관로 확장 속성";

        public IReadOnlyList<OdtFieldDef> Fields { get; } = new List<OdtFieldDef>
        {
            new OdtFieldDef("SBKHLT", "시점지반고(m)", DataType.Real),
            new OdtFieldDef("SBLHLT", "종점지반고(m)", DataType.Real),
        }.AsReadOnly();
    }
}
```

- [ ] **Step 2: WtlExtRecord.cs 작성**

```csharp
// src/GntTools.Wtl/WtlExtRecord.cs
using System.Collections.Generic;

namespace GntTools.Wtl
{
    /// <summary>WTL_EXT 레코드 DTO</summary>
    public class WtlExtRecord
    {
        public double BeginGroundHeight { get; set; }  // SBKHLT
        public double EndGroundHeight { get; set; }    // SBLHLT

        public Dictionary<string, object> ToDictionary()
        {
            return new Dictionary<string, object>
            {
                ["SBKHLT"] = BeginGroundHeight,
                ["SBLHLT"] = EndGroundHeight,
            };
        }
    }
}
```

- [ ] **Step 3: .csproj에 Compile 항목 추가 후 빌드 확인**

```bash
# GntTools.Wtl.csproj의 ItemGroup에 추가:
# <Compile Include="WtlExtSchema.cs" />
# <Compile Include="WtlExtRecord.cs" />
msbuild src\GntTools.Wtl\GntTools.Wtl.csproj /t:Build /p:Configuration=Debug
```

- [ ] **Step 4: 커밋**

```bash
git add src/GntTools.Wtl/
git commit -m "feat(wtl): add WTL_EXT schema and record DTO

- WtlExtSchema: 2 fields (SBKHLT 시점지반고, SBLHLT 종점지반고)
- WtlExtRecord: DTO with ToDictionary()"
```

---

### Task 2: WtlLabelBuilder — 상수 라벨 포맷

**Files:**
- Create: `src/GntTools.Wtl/WtlLabelBuilder.cs`
- Ref: `old_make/WTL_TOOLS/` 기존 라벨 포맷 참고

- [ ] **Step 1: WtlLabelBuilder.cs 작성**

```csharp
// src/GntTools.Wtl/WtlLabelBuilder.cs
using GntTools.Core.Settings;

namespace GntTools.Wtl
{
    /// <summary>상수관로 라벨 포맷 빌더</summary>
    public static class WtlLabelBuilder
    {
        /// <summary>
        /// 관라벨 생성: "PE ∅200 L=12.3m"
        /// </summary>
        public static string BuildPipeLabel(string material, string diameter,
            double length)
        {
            var settings = AppSettings.Instance.Common;
            string lenStr = length.ToString($"F{settings.LengthDecimals}");
            return $"{material} ∅{diameter} L={lenStr}m";
        }

        /// <summary>
        /// 심도 라벨: "1.2" (시점/종점/중간 각각)
        /// </summary>
        public static string BuildDepthLabel(double depth)
        {
            var settings = AppSettings.Instance.Common;
            return depth.ToString($"F{settings.DepthDecimals}");
        }

        /// <summary>
        /// 지반고 라벨: "EL.25.30"
        /// </summary>
        public static string BuildGroundHeightLabel(double height)
        {
            return $"EL.{height:F2}";
        }
    }
}
```

- [ ] **Step 2: 빌드 확인 후 커밋**

```bash
git add src/GntTools.Wtl/WtlLabelBuilder.cs
git commit -m "feat(wtl): implement WtlLabelBuilder for pipe/depth/height labels"
```

---

### Task 3: WtlService — 상수 비즈니스 로직

**Files:**
- Create: `src/GntTools.Wtl/WtlService.cs`
- Ref: 스펙 §4 WtlService 설계, §5 워크플로우

- [ ] **Step 1: WtlService.cs 작성**

```csharp
// src/GntTools.Wtl/WtlService.cs
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
        public bool UseLeader { get; set; }  // true=지시선, false=관로평행
    }

    /// <summary>상수관로 비즈니스 로직</summary>
    public class WtlService
    {
        private readonly OdtManager _odt = new OdtManager();
        private readonly DepthCalculator _depth = new DepthCalculator();
        private readonly PipeCommonSchema _commonSchema = new PipeCommonSchema();
        private readonly WtlExtSchema _extSchema = new WtlExtSchema();

        /// <summary>ODT 테이블 초기화 (PIPE_COMMON + WTL_EXT)</summary>
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

            // 1. 폴리라인 길이 측정
            double length = PolylineHelper.GetLength(polylineId);

            // 2. 심도 측정
            DepthResult depthResult;
            if (input.IsUndetected)
            {
                depthResult = _depth.Undetected();
            }
            else if (input.IsAutoDepth)
            {
                depthResult = _depth.MeasureAtVertices(polylineId,
                    settings.Wtl.Layers.Depth);
            }
            else
            {
                depthResult = _depth.FromManualInput(
                    input.ManualBeginDepth, input.ManualEndDepth);
            }

            // 3. PIPE_COMMON 레코드 생성
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
                Label = WtlLabelBuilder.BuildPipeLabel(
                    input.Material, input.Diameter, length),
            };

            _odt.AttachRecord(_commonSchema.TableName, polylineId);
            _odt.UpdateRecord(_commonSchema.TableName, polylineId,
                commonRec.ToDictionary());

            // 4. WTL_EXT 레코드 생성
            var extRec = new WtlExtRecord
            {
                BeginGroundHeight = input.BeginGroundHeight,
                EndGroundHeight = input.EndGroundHeight,
            };

            _odt.AttachRecord(_extSchema.TableName, polylineId);
            _odt.UpdateRecord(_extSchema.TableName, polylineId,
                extRec.ToDictionary());

            // 5. 라벨 생성
            var styleId = TextStyleHelper.EnsureStyle(
                settings.Common.TextStyle,
                settings.Common.ShxFont,
                settings.Common.BigFont);

            var endpoints = PolylineHelper.GetEndpoints(polylineId);

            if (input.UseLeader)
            {
                // 지시선 모드: 사용자에게 지시선 끝점 입력받기
                var ppr = ed.GetPoint("\n지시선 끝점을 지정하세요: ");
                if (ppr.Status != PromptStatus.OK) return false;

                var leaderEnd = ppr.Value;
                var midPt = new Point3d(
                    (endpoints.start.X + endpoints.end.X) / 2,
                    (endpoints.start.Y + endpoints.end.Y) / 2,
                    0);

                // 지시선 생성
                LeaderWriter.Create(midPt, leaderEnd,
                    settings.Wtl.Layers.Leader);

                // 라벨 텍스트
                TextWriter.Create(commonRec.Label, leaderEnd,
                    settings.Common.TextSize, 0,
                    settings.Wtl.Layers.Label, styleId);
            }
            else
            {
                // 관로 평행: 중점에 관로 방향으로 텍스트
                var midPt = new Point3d(
                    (endpoints.start.X + endpoints.end.X) / 2,
                    (endpoints.start.Y + endpoints.end.Y) / 2,
                    0);
                double angle = TextWriter.CalcReadableAngle(
                    endpoints.start, endpoints.end);

                TextWriter.Create(commonRec.Label, midPt,
                    settings.Common.TextSize, angle,
                    settings.Wtl.Layers.Label, styleId);
            }

            // 6. 심도 텍스트 (시점/종점)
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

            // 7. XData 그룹ID
            string groupId = XDataManager.GenerateGroupId();
            XDataManager.WriteGroupId(polylineId, groupId);

            // 8. 색상 변경 (완료 표시)
            ColorHelper.SetColor(polylineId, 2); // 노란색

            ed.WriteMessage($"\n상수관로 속성 입력 완료: {commonRec.Label}");
            return true;
        }

        /// <summary>기존 속성 수정</summary>
        public bool ModifyAttribute(WtlInputData input, ObjectId polylineId)
        {
            // 기존 레코드 읽어서 수정
            if (!_odt.RecordExists(_commonSchema.TableName, polylineId))
            {
                var ed = Application.DocumentManager.MdiActiveDocument.Editor;
                ed.WriteMessage("\n선택한 폴리라인에 상수 속성이 없습니다.");
                return false;
            }

            // 기존 레코드 삭제 후 재생성
            _odt.RemoveRecord(_commonSchema.TableName, polylineId);
            _odt.RemoveRecord(_extSchema.TableName, polylineId);

            return CreateAttribute(input, polylineId);
        }
    }
}
```

- [ ] **Step 2: 빌드 확인 후 커밋**

```bash
git add src/GntTools.Wtl/WtlService.cs
git commit -m "feat(wtl): implement WtlService with create/modify workflow

- CreateAttribute: ODT 기록 + 라벨 생성 + 심도 텍스트 + XData + 색상
- ModifyAttribute: 기존 레코드 삭제 후 재생성
- 지시선/관로평행 양쪽 라벨 모드 지원"
```

---

### Task 4: WtlCommands — CLI 명령 등록

**Files:**
- Create: `src/GntTools.Wtl/WtlCommands.cs`
- Ref: 스펙 §6 CLI 명령 (GNTTOOLS_WTL_ATT, GNTTOOLS_WTL_EDIT)

- [ ] **Step 1: WtlCommands.cs 작성**

```csharp
// src/GntTools.Wtl/WtlCommands.cs
using Autodesk.AutoCAD.ApplicationServices;
using Autodesk.AutoCAD.EditorInput;
using Autodesk.AutoCAD.Runtime;
using GntTools.Core.Selection;
using GntTools.Core.Settings;

namespace GntTools.Wtl
{
    public class WtlCommands
    {
        private static readonly WtlService _service = new WtlService();

        [CommandMethod("GNTTOOLS_WTL_ATT")]
        public void WtlAttach()
        {
            var doc = Application.DocumentManager.MdiActiveDocument;
            var ed = doc.Editor;

            _service.EnsureTables();

            // 팔레트 ViewModel이 있으면 그 값 사용, 없으면 프롬프트
            var input = CollectInputFromPrompt(ed);
            if (input == null) return;

            // 폴리라인 선택
            var selector = new EntitySelector();
            var polyId = selector.SelectOne(
                EntitySelector.PolylineFilter(),
                "\n상수관로 폴리라인을 선택하세요: ");

            if (polyId.IsNull)
            {
                ed.WriteMessage("\n선택이 취소되었습니다.");
                return;
            }

            _service.CreateAttribute(input, polyId);
        }

        [CommandMethod("GNTTOOLS_WTL_EDIT")]
        public void WtlEdit()
        {
            var doc = Application.DocumentManager.MdiActiveDocument;
            var ed = doc.Editor;

            _service.EnsureTables();

            var selector = new EntitySelector();
            var polyId = selector.SelectOne(
                EntitySelector.PolylineFilter(),
                "\n수정할 상수관로 폴리라인을 선택하세요: ");

            if (polyId.IsNull)
            {
                ed.WriteMessage("\n선택이 취소되었습니다.");
                return;
            }

            var input = CollectInputFromPrompt(ed);
            if (input == null) return;

            _service.ModifyAttribute(input, polyId);
        }

        /// <summary>CLI 프롬프트로 입력값 수집</summary>
        private WtlInputData CollectInputFromPrompt(Editor ed)
        {
            var settings = AppSettings.Instance;
            var input = new WtlInputData();

            // 설치년도
            var prYear = ed.GetString(
                $"\n설치년도 <{settings.Wtl.Defaults.Year}>: ");
            if (prYear.Status == PromptStatus.Cancel) return null;
            input.InstallYear = string.IsNullOrEmpty(prYear.StringResult)
                ? settings.Wtl.Defaults.Year : prYear.StringResult;

            // 관재질
            var prMat = ed.GetString(
                $"\n관재질 <{settings.Wtl.Defaults.Material}>: ");
            if (prMat.Status == PromptStatus.Cancel) return null;
            input.Material = string.IsNullOrEmpty(prMat.StringResult)
                ? settings.Wtl.Defaults.Material : prMat.StringResult;

            // 구경
            var prDia = ed.GetString(
                $"\n구경 <{settings.Wtl.Defaults.Diameter}>: ");
            if (prDia.Status == PromptStatus.Cancel) return null;
            input.Diameter = string.IsNullOrEmpty(prDia.StringResult)
                ? settings.Wtl.Defaults.Diameter : prDia.StringResult;

            // 심도 모드
            var prDepthMode = new PromptKeywordOptions(
                "\n심도 입력방식 [자동(A)/수동(M)/불탐(U)] <M>: ");
            prDepthMode.Keywords.Add("Auto");
            prDepthMode.Keywords.Add("Manual");
            prDepthMode.Keywords.Add("Undetected");
            prDepthMode.Keywords.Default = "Manual";
            var depthResult = ed.GetKeywords(prDepthMode);
            if (depthResult.Status == PromptStatus.Cancel) return null;

            switch (depthResult.StringResult)
            {
                case "Auto":
                    input.IsAutoDepth = true;
                    break;
                case "Undetected":
                    input.IsUndetected = true;
                    break;
                default: // Manual
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
git add src/GntTools.Wtl/WtlCommands.cs
git commit -m "feat(wtl): implement CLI commands GNTTOOLS_WTL_ATT/EDIT

- WtlAttach: 프롬프트로 입력받아 신규 속성 부착
- WtlEdit: 기존 속성 수정
- 기본값은 AppSettings에서 로드"
```

---

## Chunk 2: PaletteSet UI 기본틀 + WTL/Settings 탭

### Task 5: Plugin.cs + PaletteManager — UI 진입점

**Files:**
- Create: `src/GntTools.UI/Plugin.cs`
- Create: `src/GntTools.UI/PaletteManager.cs`
- Create: `src/GntTools.UI/Commands/PaletteCommands.cs`
- Ref: `references/autocad-palette.md` §2 기본 생성 패턴, §8 실무 패턴

- [ ] **Step 1: Plugin.cs 작성**

```csharp
// src/GntTools.UI/Plugin.cs
using Autodesk.AutoCAD.Runtime;

[assembly: ExtensionApplication(typeof(GntTools.UI.Plugin))]
[assembly: CommandClass(typeof(GntTools.UI.Commands.PaletteCommands))]

namespace GntTools.UI
{
    /// <summary>AutoCAD 플러그인 진입점</summary>
    public class Plugin : IExtensionApplication
    {
        public void Initialize()
        {
            var ed = Autodesk.AutoCAD.ApplicationServices
                .Application.DocumentManager.MdiActiveDocument.Editor;
            ed.WriteMessage("\nGntTools v1.0 로드됨. GNTTOOLS_SHOW로 팔레트를 엽니다.\n");
        }

        public void Terminate()
        {
            // 설정 자동 저장
            Core.Settings.AppSettings.Instance.Save();
        }
    }
}
```

- [ ] **Step 2: PaletteManager.cs 작성**

```csharp
// src/GntTools.UI/PaletteManager.cs
using System;
using Autodesk.AutoCAD.Windows;
using GntTools.UI.Controls;

namespace GntTools.UI
{
    /// <summary>PaletteSet 싱글 인스턴스 관리</summary>
    public static class PaletteManager
    {
        // 고정 GUID — 사용자 설정(위치,크기,도킹) 영구 저장
        private static readonly Guid PaletteGuid =
            new Guid("B7E3F1A2-4D5C-6E7F-8901-ABCDEF012345");

        private static PaletteSet _ps;

        /// <summary>WTL 탭 ViewModel (CLI 명령과 공유)</summary>
        public static ViewModels.WtlViewModel WtlVm { get; private set; }
        public static ViewModels.SettingsViewModel SettingsVm { get; private set; }

        public static PaletteSet PaletteSet => _ps;

        /// <summary>팔레트 초기화 (최초 1회)</summary>
        public static void Initialize()
        {
            if (_ps != null) return;

            _ps = new PaletteSet("GNT Tools", PaletteGuid);
            _ps.Style = PaletteSetStyles.ShowAutoHideButton
                      | PaletteSetStyles.ShowCloseButton
                      | PaletteSetStyles.ShowPropertiesMenu;
            _ps.MinimumSize = new System.Drawing.Size(280, 400);
            _ps.DockEnabled = DockSides.Left | DockSides.Right;

            // ViewModels 생성
            WtlVm = new ViewModels.WtlViewModel();
            SettingsVm = new ViewModels.SettingsViewModel();

            // WPF 탭 추가 (AddVisual)
            _ps.AddVisual("상수", new WtlPanel { DataContext = WtlVm });
            // SWL, KEPCO 탭은 Phase 3, 4에서 추가
            _ps.AddVisual("환경설정",
                new SettingsPanel { DataContext = SettingsVm });
        }

        /// <summary>팔레트 표시/토글</summary>
        public static void Toggle()
        {
            Initialize();
            _ps.Visible = !_ps.Visible;
        }

        /// <summary>팔레트 표시</summary>
        public static void Show()
        {
            Initialize();
            _ps.Visible = true;
        }

        /// <summary>특정 탭 활성화</summary>
        public static void ActivateTab(int index)
        {
            Initialize();
            _ps.Visible = true;
            if (index >= 0 && index < _ps.Count)
                _ps.Activate(index);
        }
    }
}
```

- [ ] **Step 3: PaletteCommands.cs 작성**

```csharp
// src/GntTools.UI/Commands/PaletteCommands.cs
using Autodesk.AutoCAD.Runtime;

namespace GntTools.UI.Commands
{
    public class PaletteCommands
    {
        [CommandMethod("GNTTOOLS_SHOW")]
        public void ShowPalette()
        {
            PaletteManager.Toggle();
        }

        [CommandMethod("GNTTOOLS_SETTINGS")]
        public void ShowSettings()
        {
            // 마지막 탭 = 환경설정
            PaletteManager.ActivateTab(PaletteManager.PaletteSet?.Count - 1 ?? 0);
        }
    }
}
```

- [ ] **Step 4: 빌드 확인 후 커밋**

```bash
git add src/GntTools.UI/Plugin.cs
git add src/GntTools.UI/PaletteManager.cs
git add src/GntTools.UI/Commands/
git commit -m "feat(ui): implement Plugin entry point and PaletteManager

- Plugin: IExtensionApplication (Initialize/Terminate)
- PaletteManager: PaletteSet singleton with GUID persistence
- PaletteCommands: GNTTOOLS_SHOW (toggle), GNTTOOLS_SETTINGS"
```

---

### Task 6: MVVM 기반 클래스 — ViewModelBase + RelayCommand

**Files:**
- Create: `src/GntTools.UI/ViewModels/ViewModelBase.cs`
- Create: `src/GntTools.UI/ViewModels/RelayCommand.cs`

> 외부 의존성 없이 수동 MVVM 구현 (스펙 §10)

- [ ] **Step 1: ViewModelBase.cs 작성**

```csharp
// src/GntTools.UI/ViewModels/ViewModelBase.cs
using System.ComponentModel;
using System.Runtime.CompilerServices;

namespace GntTools.UI.ViewModels
{
    public abstract class ViewModelBase : INotifyPropertyChanged
    {
        public event PropertyChangedEventHandler PropertyChanged;

        protected void OnPropertyChanged([CallerMemberName] string name = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
        }

        protected bool SetProperty<T>(ref T field, T value,
            [CallerMemberName] string name = null)
        {
            if (Equals(field, value)) return false;
            field = value;
            OnPropertyChanged(name);
            return true;
        }
    }
}
```

- [ ] **Step 2: RelayCommand.cs 작성**

```csharp
// src/GntTools.UI/ViewModels/RelayCommand.cs
using System;
using System.Windows.Input;

namespace GntTools.UI.ViewModels
{
    public class RelayCommand : ICommand
    {
        private readonly Action _execute;
        private readonly Func<bool> _canExecute;

        public RelayCommand(Action execute, Func<bool> canExecute = null)
        {
            _execute = execute ?? throw new ArgumentNullException(nameof(execute));
            _canExecute = canExecute;
        }

        public event EventHandler CanExecuteChanged
        {
            add { CommandManager.RequerySuggested += value; }
            remove { CommandManager.RequerySuggested -= value; }
        }

        public bool CanExecute(object parameter) =>
            _canExecute?.Invoke() ?? true;

        public void Execute(object parameter) => _execute();
    }
}
```

- [ ] **Step 3: 빌드 확인 후 커밋**

```bash
git add src/GntTools.UI/ViewModels/ViewModelBase.cs
git add src/GntTools.UI/ViewModels/RelayCommand.cs
git commit -m "feat(ui): add MVVM base classes (ViewModelBase, RelayCommand)

- ViewModelBase: INotifyPropertyChanged with SetProperty helper
- RelayCommand: ICommand implementation (no external deps)"
```

---

### Task 7: SettingsPanel — 환경설정 탭

**Files:**
- Create: `src/GntTools.UI/ViewModels/SettingsViewModel.cs`
- Create: `src/GntTools.UI/Controls/SettingsPanel.xaml`
- Create: `src/GntTools.UI/Controls/SettingsPanel.xaml.cs`
- Ref: 스펙 §5 환경설정 탭, §7 settings.json 구조

- [ ] **Step 1: SettingsViewModel.cs 작성**

```csharp
// src/GntTools.UI/ViewModels/SettingsViewModel.cs
using System.Windows.Input;
using GntTools.Core.Settings;

namespace GntTools.UI.ViewModels
{
    public class SettingsViewModel : ViewModelBase
    {
        private AppSettings Settings => AppSettings.Instance;

        // 공통
        public string TextStyle
        {
            get => Settings.Common.TextStyle;
            set { Settings.Common.TextStyle = value; OnPropertyChanged(); }
        }
        public double TextSize
        {
            get => Settings.Common.TextSize;
            set { Settings.Common.TextSize = value; OnPropertyChanged(); }
        }
        public int LengthDecimals
        {
            get => Settings.Common.LengthDecimals;
            set { Settings.Common.LengthDecimals = value; OnPropertyChanged(); }
        }
        public int DepthDecimals
        {
            get => Settings.Common.DepthDecimals;
            set { Settings.Common.DepthDecimals = value; OnPropertyChanged(); }
        }

        // WTL 레이어
        public string WtlDepthLayer
        {
            get => Settings.Wtl.Layers.Depth;
            set { Settings.Wtl.Layers.Depth = value; OnPropertyChanged(); }
        }
        public string WtlHeightLayer
        {
            get => Settings.Wtl.Layers.GroundHeight;
            set { Settings.Wtl.Layers.GroundHeight = value; OnPropertyChanged(); }
        }
        public string WtlLabelLayer
        {
            get => Settings.Wtl.Layers.Label;
            set { Settings.Wtl.Layers.Label = value; OnPropertyChanged(); }
        }
        public string WtlLeaderLayer
        {
            get => Settings.Wtl.Layers.Leader;
            set { Settings.Wtl.Layers.Leader = value; OnPropertyChanged(); }
        }
        public string WtlUndetectedLayer
        {
            get => Settings.Wtl.Layers.Undetected;
            set { Settings.Wtl.Layers.Undetected = value; OnPropertyChanged(); }
        }

        // SWL 레이어 (Phase 3에서 추가)
        public string SwlDepthLayer
        {
            get => Settings.Swl.Layers.Depth;
            set { Settings.Swl.Layers.Depth = value; OnPropertyChanged(); }
        }
        public string SwlLabelLayer
        {
            get => Settings.Swl.Layers.Label;
            set { Settings.Swl.Layers.Label = value; OnPropertyChanged(); }
        }

        // KEPCO 레이어 (Phase 4에서 추가)
        public string KepcoDepthLayer
        {
            get => Settings.Kepco.Layers.Depth;
            set { Settings.Kepco.Layers.Depth = value; OnPropertyChanged(); }
        }
        public string KepcoDrawingLayer
        {
            get => Settings.Kepco.Layers.Drawing;
            set { Settings.Kepco.Layers.Drawing = value; OnPropertyChanged(); }
        }

        // 저장 명령
        public ICommand SaveCommand { get; }

        public SettingsViewModel()
        {
            SaveCommand = new RelayCommand(() =>
            {
                Settings.Save();
                Autodesk.AutoCAD.ApplicationServices.Application
                    .DocumentManager.MdiActiveDocument.Editor
                    .WriteMessage("\n환경설정이 저장되었습니다.");
            });
        }
    }
}
```

- [ ] **Step 2: SettingsPanel.xaml 작성**

```xml
<!-- src/GntTools.UI/Controls/SettingsPanel.xaml -->
<UserControl x:Class="GntTools.UI.Controls.SettingsPanel"
             xmlns="http://schemas.microsoft.com/winfx/2006/xaml/presentation"
             xmlns:x="http://schemas.microsoft.com/winfx/2006/xaml"
             Padding="8">
    <ScrollViewer VerticalScrollBarVisibility="Auto">
        <StackPanel>
            <!-- 공통 설정 -->
            <GroupBox Header="공통" Margin="0,0,0,8">
                <Grid>
                    <Grid.ColumnDefinitions>
                        <ColumnDefinition Width="100"/>
                        <ColumnDefinition Width="*"/>
                    </Grid.ColumnDefinitions>
                    <Grid.RowDefinitions>
                        <RowDefinition Height="Auto"/>
                        <RowDefinition Height="Auto"/>
                        <RowDefinition Height="Auto"/>
                        <RowDefinition Height="Auto"/>
                    </Grid.RowDefinitions>
                    <TextBlock Text="텍스트 스타일" Grid.Row="0" VerticalAlignment="Center" Margin="0,2"/>
                    <TextBox Text="{Binding TextStyle}" Grid.Row="0" Grid.Column="1" Margin="4,2"/>
                    <TextBlock Text="텍스트 크기" Grid.Row="1" VerticalAlignment="Center" Margin="0,2"/>
                    <TextBox Text="{Binding TextSize}" Grid.Row="1" Grid.Column="1" Margin="4,2"/>
                    <TextBlock Text="연장 자릿수" Grid.Row="2" VerticalAlignment="Center" Margin="0,2"/>
                    <TextBox Text="{Binding LengthDecimals}" Grid.Row="2" Grid.Column="1" Margin="4,2" Width="50" HorizontalAlignment="Left"/>
                    <TextBlock Text="심도 자릿수" Grid.Row="3" VerticalAlignment="Center" Margin="0,2"/>
                    <TextBox Text="{Binding DepthDecimals}" Grid.Row="3" Grid.Column="1" Margin="4,2" Width="50" HorizontalAlignment="Left"/>
                </Grid>
            </GroupBox>

            <!-- 상수 레이어 -->
            <GroupBox Header="상수 레이어" Margin="0,0,0,8">
                <Grid>
                    <Grid.ColumnDefinitions>
                        <ColumnDefinition Width="100"/>
                        <ColumnDefinition Width="*"/>
                    </Grid.ColumnDefinitions>
                    <Grid.RowDefinitions>
                        <RowDefinition Height="Auto"/>
                        <RowDefinition Height="Auto"/>
                        <RowDefinition Height="Auto"/>
                        <RowDefinition Height="Auto"/>
                        <RowDefinition Height="Auto"/>
                    </Grid.RowDefinitions>
                    <TextBlock Text="심도텍스트" Grid.Row="0" VerticalAlignment="Center" Margin="0,2"/>
                    <TextBox Text="{Binding WtlDepthLayer}" Grid.Row="0" Grid.Column="1" Margin="4,2"/>
                    <TextBlock Text="지반고텍스트" Grid.Row="1" VerticalAlignment="Center" Margin="0,2"/>
                    <TextBox Text="{Binding WtlHeightLayer}" Grid.Row="1" Grid.Column="1" Margin="4,2"/>
                    <TextBlock Text="제원텍스트" Grid.Row="2" VerticalAlignment="Center" Margin="0,2"/>
                    <TextBox Text="{Binding WtlLabelLayer}" Grid.Row="2" Grid.Column="1" Margin="4,2"/>
                    <TextBlock Text="지시선" Grid.Row="3" VerticalAlignment="Center" Margin="0,2"/>
                    <TextBox Text="{Binding WtlLeaderLayer}" Grid.Row="3" Grid.Column="1" Margin="4,2"/>
                    <TextBlock Text="불탐관로" Grid.Row="4" VerticalAlignment="Center" Margin="0,2"/>
                    <TextBox Text="{Binding WtlUndetectedLayer}" Grid.Row="4" Grid.Column="1" Margin="4,2"/>
                </Grid>
            </GroupBox>

            <!-- 하수 레이어 (Phase 3 확장) -->
            <GroupBox Header="하수 레이어" Margin="0,0,0,8">
                <Grid>
                    <Grid.ColumnDefinitions>
                        <ColumnDefinition Width="100"/>
                        <ColumnDefinition Width="*"/>
                    </Grid.ColumnDefinitions>
                    <Grid.RowDefinitions>
                        <RowDefinition Height="Auto"/>
                        <RowDefinition Height="Auto"/>
                    </Grid.RowDefinitions>
                    <TextBlock Text="심도텍스트" Grid.Row="0" VerticalAlignment="Center" Margin="0,2"/>
                    <TextBox Text="{Binding SwlDepthLayer}" Grid.Row="0" Grid.Column="1" Margin="4,2"/>
                    <TextBlock Text="제원텍스트" Grid.Row="1" VerticalAlignment="Center" Margin="0,2"/>
                    <TextBox Text="{Binding SwlLabelLayer}" Grid.Row="1" Grid.Column="1" Margin="4,2"/>
                </Grid>
            </GroupBox>

            <!-- 전력통신 레이어 (Phase 4 확장) -->
            <GroupBox Header="전력통신 레이어" Margin="0,0,0,8">
                <Grid>
                    <Grid.ColumnDefinitions>
                        <ColumnDefinition Width="100"/>
                        <ColumnDefinition Width="*"/>
                    </Grid.ColumnDefinitions>
                    <Grid.RowDefinitions>
                        <RowDefinition Height="Auto"/>
                        <RowDefinition Height="Auto"/>
                    </Grid.RowDefinitions>
                    <TextBlock Text="심도텍스트" Grid.Row="0" VerticalAlignment="Center" Margin="0,2"/>
                    <TextBox Text="{Binding KepcoDepthLayer}" Grid.Row="0" Grid.Column="1" Margin="4,2"/>
                    <TextBlock Text="도형레이어" Grid.Row="1" VerticalAlignment="Center" Margin="0,2"/>
                    <TextBox Text="{Binding KepcoDrawingLayer}" Grid.Row="1" Grid.Column="1" Margin="4,2"/>
                </Grid>
            </GroupBox>

            <!-- 저장 버튼 -->
            <Button Content="저장" Command="{Binding SaveCommand}"
                    Width="80" Height="30" HorizontalAlignment="Center"
                    Margin="0,8"/>
        </StackPanel>
    </ScrollViewer>
</UserControl>
```

- [ ] **Step 3: SettingsPanel.xaml.cs 작성**

```csharp
// src/GntTools.UI/Controls/SettingsPanel.xaml.cs
using System.Windows.Controls;

namespace GntTools.UI.Controls
{
    public partial class SettingsPanel : UserControl
    {
        public SettingsPanel()
        {
            InitializeComponent();
        }
    }
}
```

- [ ] **Step 4: 빌드 확인 후 커밋**

```bash
git add src/GntTools.UI/ViewModels/SettingsViewModel.cs
git add src/GntTools.UI/Controls/SettingsPanel.xaml
git add src/GntTools.UI/Controls/SettingsPanel.xaml.cs
git commit -m "feat(ui): implement Settings tab with JSON persistence

- SettingsViewModel: 공통/WTL/SWL/KEPCO 레이어 설정 바인딩
- SettingsPanel.xaml: GroupBox 레이아웃, 저장 버튼
- Save → AppSettings.Save() → %AppData%/GntTools/settings.json"
```

---

### Task 8: WtlPanel — 상수 탭 UI

**Files:**
- Create: `src/GntTools.UI/ViewModels/WtlViewModel.cs`
- Create: `src/GntTools.UI/Controls/WtlPanel.xaml`
- Create: `src/GntTools.UI/Controls/WtlPanel.xaml.cs`
- Ref: 스펙 §5 상수(WTL) 탭 레이아웃

- [ ] **Step 1: WtlViewModel.cs 작성**

```csharp
// src/GntTools.UI/ViewModels/WtlViewModel.cs
using System.Windows.Input;
using Autodesk.AutoCAD.ApplicationServices;
using GntTools.Core.Selection;
using GntTools.Core.Settings;
using GntTools.Wtl;

namespace GntTools.UI.ViewModels
{
    public class WtlViewModel : ViewModelBase
    {
        private readonly WtlService _service = new WtlService();

        // 기본정보
        private string _installYear;
        public string InstallYear
        {
            get => _installYear;
            set => SetProperty(ref _installYear, value);
        }

        private string _material;
        public string Material
        {
            get => _material;
            set => SetProperty(ref _material, value);
        }

        private string _diameter;
        public string Diameter
        {
            get => _diameter;
            set => SetProperty(ref _diameter, value);
        }

        // 심도
        private bool _isAutoDepth;
        public bool IsAutoDepth
        {
            get => _isAutoDepth;
            set => SetProperty(ref _isAutoDepth, value);
        }

        private bool _isManualDepth = true;
        public bool IsManualDepth
        {
            get => _isManualDepth;
            set => SetProperty(ref _isManualDepth, value);
        }

        private double _beginDepth;
        public double BeginDepth
        {
            get => _beginDepth;
            set => SetProperty(ref _beginDepth, value);
        }

        private double _endDepth;
        public double EndDepth
        {
            get => _endDepth;
            set => SetProperty(ref _endDepth, value);
        }

        private bool _isUndetected;
        public bool IsUndetected
        {
            get => _isUndetected;
            set => SetProperty(ref _isUndetected, value);
        }

        // 지반고
        private double _beginGroundHeight;
        public double BeginGroundHeight
        {
            get => _beginGroundHeight;
            set => SetProperty(ref _beginGroundHeight, value);
        }

        private double _endGroundHeight;
        public double EndGroundHeight
        {
            get => _endGroundHeight;
            set => SetProperty(ref _endGroundHeight, value);
        }

        // 라벨 모드
        private bool _useLeader;
        public bool UseLeader
        {
            get => _useLeader;
            set => SetProperty(ref _useLeader, value);
        }

        // Commands
        public ICommand CreateCommand { get; }
        public ICommand ModifyCommand { get; }

        public WtlViewModel()
        {
            // 기본값 로드
            var settings = AppSettings.Instance;
            InstallYear = settings.Wtl.Defaults.Year;
            Material = settings.Wtl.Defaults.Material;
            Diameter = settings.Wtl.Defaults.Diameter;

            CreateCommand = new RelayCommand(ExecuteCreate);
            ModifyCommand = new RelayCommand(ExecuteModify);
        }

        /// <summary>팔레트 값 → WtlInputData 변환</summary>
        public WtlInputData ToInputData()
        {
            return new WtlInputData
            {
                InstallYear = InstallYear,
                Material = Material,
                Diameter = Diameter,
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
                    "\n상수관로 폴리라인을 선택하세요: ");

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
                    "\n수정할 상수관로를 선택하세요: ");

                if (polyId.IsNull) return;
                _service.ModifyAttribute(ToInputData(), polyId);
            }
        }
    }
}
```

- [ ] **Step 2: WtlPanel.xaml 작성**

```xml
<!-- src/GntTools.UI/Controls/WtlPanel.xaml -->
<UserControl x:Class="GntTools.UI.Controls.WtlPanel"
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
                    </Grid.RowDefinitions>
                    <TextBlock Text="설치년도" Grid.Row="0" VerticalAlignment="Center" Margin="0,2"/>
                    <TextBox Text="{Binding InstallYear, UpdateSourceTrigger=PropertyChanged}"
                             Grid.Row="0" Grid.Column="1" Margin="4,2"/>
                    <TextBlock Text="관재질" Grid.Row="1" VerticalAlignment="Center" Margin="0,2"/>
                    <TextBox Text="{Binding Material, UpdateSourceTrigger=PropertyChanged}"
                             Grid.Row="1" Grid.Column="1" Margin="4,2"/>
                    <TextBlock Text="구경" Grid.Row="2" VerticalAlignment="Center" Margin="0,2"/>
                    <TextBox Text="{Binding Diameter, UpdateSourceTrigger=PropertyChanged}"
                             Grid.Row="2" Grid.Column="1" Margin="4,2"/>
                </Grid>
            </GroupBox>

            <!-- 심도 -->
            <GroupBox Header="심도" Margin="0,0,0,8">
                <StackPanel>
                    <RadioButton Content="자동측정 (정점별 텍스트)"
                                 IsChecked="{Binding IsAutoDepth}" Margin="0,2"/>
                    <RadioButton Content="수동입력"
                                 IsChecked="{Binding IsManualDepth}" Margin="0,2"/>
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
                        <TextBox Text="{Binding BeginDepth}" Grid.Row="0" Grid.Column="1"
                                 Margin="4,2" IsEnabled="{Binding IsManualDepth}"/>
                        <TextBlock Text="종점심도" Grid.Row="1" VerticalAlignment="Center"/>
                        <TextBox Text="{Binding EndDepth}" Grid.Row="1" Grid.Column="1"
                                 Margin="4,2" IsEnabled="{Binding IsManualDepth}"/>
                    </Grid>
                    <CheckBox Content="불탐" IsChecked="{Binding IsUndetected}" Margin="0,4"/>
                </StackPanel>
            </GroupBox>

            <!-- 지반고 -->
            <GroupBox Header="지반고" Margin="0,0,0,8">
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
                <Button Content="신규입력" Command="{Binding CreateCommand}"
                        Width="80" Height="30" Margin="0,0,8,0"/>
                <Button Content="수정" Command="{Binding ModifyCommand}"
                        Width="80" Height="30"/>
            </StackPanel>
        </StackPanel>
    </ScrollViewer>
</UserControl>
```

- [ ] **Step 3: WtlPanel.xaml.cs 작성**

```csharp
// src/GntTools.UI/Controls/WtlPanel.xaml.cs
using System.Windows.Controls;

namespace GntTools.UI.Controls
{
    public partial class WtlPanel : UserControl
    {
        public WtlPanel()
        {
            InitializeComponent();
        }
    }
}
```

- [ ] **Step 4: .csproj에 Page/Compile 항목 추가 후 빌드**

```xml
<!-- GntTools.UI.csproj ItemGroup에 추가 -->
<Page Include="Controls\WtlPanel.xaml">
    <Generator>MSBuild:Compile</Generator>
    <SubType>Designer</SubType>
</Page>
<Compile Include="Controls\WtlPanel.xaml.cs">
    <DependentUpon>WtlPanel.xaml</DependentUpon>
</Compile>
<Page Include="Controls\SettingsPanel.xaml">
    <Generator>MSBuild:Compile</Generator>
    <SubType>Designer</SubType>
</Page>
<Compile Include="Controls\SettingsPanel.xaml.cs">
    <DependentUpon>SettingsPanel.xaml</DependentUpon>
</Compile>
```

```bash
msbuild src\GntTools.UI\GntTools.UI.csproj /t:Build /p:Configuration=Debug
```

- [ ] **Step 5: 커밋**

```bash
git add src/GntTools.UI/ViewModels/WtlViewModel.cs
git add src/GntTools.UI/Controls/WtlPanel.xaml
git add src/GntTools.UI/Controls/WtlPanel.xaml.cs
git commit -m "feat(ui): implement WTL tab with full MVVM binding

- WtlViewModel: 기본정보/심도/지반고/라벨 모든 필드 바인딩
- WtlPanel.xaml: GroupBox 레이아웃 (스펙 §5 상수 탭)
- 신규입력/수정 버튼 → WtlService 호출 with DocumentLock"
```

---

### Task 9: Phase 2 통합 테스트

- [ ] **Step 1: 전체 솔루션 빌드**

```bash
msbuild src\GntTools.sln /t:Build /p:Configuration=Debug
```
Expected: 5개 프로젝트 전부 빌드 성공

- [ ] **Step 2: AutoCAD Map 3D 2020에서 수동 테스트**

1. AutoCAD 실행 → NETLOAD → `GntTools.UI.dll` 선택
2. Expected: "GntTools v1.0 로드됨" 메시지
3. `GNTTOOLS_SHOW` 명령 실행 → 팔레트 표시 확인
4. 상수 탭: 기본값(2024, PE, 200) 표시 확인
5. 환경설정 탭: 레이어명 표시 확인
6. 환경설정 → 저장 → `%AppData%/GntTools/settings.json` 생성 확인
7. 상수 탭 → 수동입력 → 시점심도 1.2, 종점심도 0.9 → [신규입력]
8. 폴리라인 선택 → 라벨 생성 확인
9. `GNTTOOLS_WTL_ATT` CLI 명령도 동일 동작 확인

- [ ] **Step 3: 커밋 (테스트 후 수정사항 있으면)**

```bash
git add -A
git commit -m "fix: phase 2 integration test fixes"
```

---

## Phase 2 완료 체크리스트

- [ ] GntTools.Wtl: 4개 소스 파일 (Schema, Record, Label, Service, Commands)
- [ ] GntTools.UI: Plugin + PaletteManager + PaletteCommands
- [ ] MVVM 기반: ViewModelBase + RelayCommand
- [ ] WTL 탭: 기본정보/심도/지반고/라벨 전체 UI
- [ ] 환경설정 탭: 공통 + 3개 도메인 레이어 설정
- [ ] CLI: GNTTOOLS_SHOW, GNTTOOLS_WTL_ATT, GNTTOOLS_WTL_EDIT 동작
- [ ] AutoCAD NETLOAD → 팔레트 정상 표시

**Phase 3 → `plan-phase3-swl.md` (SWL 하수 도메인)**
