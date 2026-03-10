# GntTools 통합 프로젝트 설계 문서

> **작성일:** 2026-03-10
> **프로젝트:** AutoCAD Map 3D 2020+ 관로 속성입력 도구 통합
> **언어:** C# (.NET Framework 4.7)
> **원본:** old_make/ 의 Kepco_tools2, SWL_TOOLS, WTL_TOOLS (VB.NET)

---

## 1. 배경 및 목적

### 기존 현황

| 프로젝트 | 용도 | 버전 | 언어 | ODT 테이블 |
|----------|------|------|------|-----------|
| Kepco_tools2 | 전력관로 속성입력 | 0.6.0.2 (2021) | VB.NET 4.7 | PIPE_LM (15필드) |
| SWL_TOOLS | 하수관로 속성입력 | 1.2.0.0 (2018) | VB.NET 4.7.2 | PIPE_LM (22필드) |
| WTL_TOOLS | 상수관로 속성입력 | 0.7.0.0 (2016) | VB.NET 4.7 | WTL_PIPE (11필드) |

3개 프로젝트가 동일한 개발자가 유사 패턴으로 만들었으나, 별도 솔루션으로 관리되어 코드 중복이 심하고 공통 버그가 3곳에 존재.

### 통합 목표

1. **C# 단일 솔루션**으로 통합
2. **공통 라이브러리** + **도메인별 모듈** 분리
3. CLI 명령 프롬프트 → **PaletteSet 기반 UI** + CLI 병행
4. **ODT 스키마 재설계** (공통 테이블 + 도메인 확장 테이블)
5. 기존 VB.NET 버그 수정

---

## 2. ODT 스키마 설계

### 설계 원칙

- **B안: 공통 테이블 + 도메인 확장 테이블**
- 한 엔티티에 PIPE_COMMON + 도메인_EXT 2개 레코드 부착
- SHP 내보내기 시 두 테이블 선택 → 필드명 겹침 없이 합산
- 숫자 데이터는 Real/Integer 타입 사용 (기존 전부 Character → 개선)

### PIPE_COMMON (공통 — 13개 필드)

모든 도메인(상수/하수/전력통신)의 관로 엔티티에 부착.

| # | 필드명 | 타입 | 설명 |
|---|--------|------|------|
| 0 | ISTYMD | Character | 설치일자 |
| 1 | MOPCDE | Character | 관재질 |
| 2 | PIPDIP | Character | 구경 |
| 3 | PIPLEN | Real | 연장 (m) |
| 4 | BTCDE | Character | 불탐여부 (Y/N) |
| 5 | BEGDEP | Real | 시점심도 (m) |
| 6 | ENDDEP | Real | 종점심도 (m) |
| 7 | AVEDEP | Real | 평균심도 (m) |
| 8 | HGHDEP | Real | 최고심도 (m) |
| 9 | LOWDEP | Real | 최저심도 (m) |
| 10 | PIPLBL | Character | 관라벨 |
| 11 | WRKDTE | Character | 준공일자 |
| 12 | REMARK | Character | 비고 |

- `BTCDE = "Y"` 일 때 심도값 0.0은 "미측정"을 의미
- `BTCDE = "N"` 일 때 심도값 0.0은 실제 지표면 관로
- 불탐 여부는 환경설정의 불탐레이어 목록으로 자동 판별

### KEPCO_EXT (전력통신 확장 — 3개 필드)

| # | 필드명 | 타입 | 설명 |
|---|--------|------|------|
| 0 | PIPDAT | Character | 구경별 데이터 (JSON) |
| 1 | BXH | Character | 가로x세로 |
| 2 | TMPIDN | Character | 그룹ID (XData 타임스탬프) |

PIPDAT JSON 형식:
```json
{"D200":"3(2)","D100":"2(1)"}
```
- key: 구경명 (D200, D175, D150, D125, D100, D80 등)
- value: "원개수(해치개수)" 형식
- 구경 종류 추가/변경에도 스키마 변경 불필요

### SWL_EXT (하수관로 확장 — 12개 필드)

| # | 필드명 | 타입 | 설명 |
|---|--------|------|------|
| 0 | PIPCDE | Character | 하수관 용도코드 |
| 1 | PIPHOL | Real | 가로길이 (박스관) |
| 2 | PIPVEL | Real | 세로길이 (박스관) |
| 3 | PIPLIN | Integer | 통로수 |
| 4 | SBKHLT | Real | 시점지반고 (m) |
| 5 | SBLHLT | Real | 종점지반고 (m) |
| 6 | SBKALT | Real | 시점관저고 (m) |
| 7 | SBLALT | Real | 종점관저고 (m) |
| 8 | ST_ALT | Real | 시점관상고 (m) |
| 9 | ED_ALT | Real | 종점관상고 (m) |
| 10 | PIPSLP | Real | 평균구배 (%) |
| 11 | TMPIDN | Character | 그룹ID |

관저고/관상고/구배 계산:
- 관저고 = 지반고 - 심도
- 관상고 = 관저고 + 구경(m)
- 구배 = (시점관저고 - 종점관저고) / 연장 × 100

### WTL_EXT (상수관로 확장 — 2개 필드)

| # | 필드명 | 타입 | 설명 |
|---|--------|------|------|
| 0 | SBKHLT | Real | 시점지반고 (m) |
| 1 | SBLHLT | Real | 종점지반고 (m) |

상수관로는 심도 + 지반고만으로 충분. 관저고/관상고는 필요 시 계산 가능.

### SHP 내보내기 결과

```
KEPCO: PIPE_COMMON + KEPCO_EXT 선택
 → ISTYMD|MOPCDE|PIPDIP|PIPLEN|BTCDE|BEGDEP|...|PIPDAT|BXH|TMPIDN

SWL: PIPE_COMMON + SWL_EXT 선택
 → ISTYMD|MOPCDE|PIPDIP|PIPLEN|BTCDE|BEGDEP|...|PIPCDE|SBKHLT|SBLHLT|SBKALT|...

WTL: PIPE_COMMON + WTL_EXT 선택
 → ISTYMD|MOPCDE|PIPDIP|PIPLEN|BTCDE|BEGDEP|...|SBKHLT|SBLHLT
```

---

## 3. 솔루션 아키텍처

### 프로젝트 구조

```
GntTools.sln
│
├── src/
│   ├── GntTools.Core/              공통 라이브러리 (Class Library)
│   │   ├── Odt/
│   │   │   ├── IOdtSchema.cs           스키마 인터페이스
│   │   │   ├── PipeCommonSchema.cs     PIPE_COMMON 정의
│   │   │   ├── PipeCommonRecord.cs     공통 레코드 DTO
│   │   │   └── OdtManager.cs           ODT CRUD 통합 관리
│   │   ├── Selection/
│   │   │   └── EntitySelector.cs       사용자/자동 선택 통합
│   │   ├── Geometry/
│   │   │   ├── DepthCalculator.cs      심도 측정 (자동/수동)
│   │   │   ├── PolylineHelper.cs       정점 추출, 길이 계산
│   │   │   └── ViewportManager.cs      줌 저장/복원/이동
│   │   ├── Drawing/
│   │   │   ├── TextWriter.cs           DBText 생성/수정/이동/회전
│   │   │   ├── LeaderWriter.cs         지시선 폴리라인 생성
│   │   │   ├── LayerHelper.cs          레이어 존재확인/생성
│   │   │   ├── TextStyleHelper.cs      텍스트 스타일 관리
│   │   │   └── ColorHelper.cs          색상 변경
│   │   ├── XData/
│   │   │   └── XDataManager.cs         RegApp, 그룹ID 읽기/쓰기
│   │   └── Settings/
│   │       ├── AppSettings.cs          전체 설정 관리 (JSON)
│   │       └── DomainSettings.cs       도메인별 설정 모델
│   │
│   ├── GntTools.Wtl/               상수 도메인 (Class Library)
│   │   ├── WtlExtSchema.cs            WTL_EXT 스키마 정의
│   │   ├── WtlExtRecord.cs            확장 레코드 DTO
│   │   ├── WtlService.cs              상수 비즈니스 로직
│   │   ├── WtlLabelBuilder.cs         상수 라벨 포맷
│   │   └── WtlCommands.cs             CLI 명령 등록
│   │
│   ├── GntTools.Swl/               하수 도메인 (Class Library)
│   │   ├── SwlExtSchema.cs            SWL_EXT 스키마 정의
│   │   ├── SwlExtRecord.cs            확장 레코드 DTO
│   │   ├── SwlService.cs              하수 비즈니스 로직
│   │   ├── SwlLabelBuilder.cs         하수 라벨 포맷
│   │   ├── ElevationCalculator.cs     관저고/관상고/구배 계산
│   │   └── SwlCommands.cs             CLI 명령 등록
│   │
│   ├── GntTools.Kepco/             전력통신 도메인 (Class Library)
│   │   ├── KepcoExtSchema.cs          KEPCO_EXT 스키마 정의
│   │   ├── KepcoExtRecord.cs          확장 레코드 DTO
│   │   ├── KepcoService.cs            전력 비즈니스 로직
│   │   ├── KepcoLabelBuilder.cs       전력 라벨 포맷
│   │   ├── SectionCounter.cs          단면 원/해치 카운팅
│   │   ├── BxHCalculator.cs           BxH 계산
│   │   ├── JunctionChecker.cs         분기점 오류검증
│   │   └── KepcoCommands.cs           CLI 명령 등록
│   │
│   └── GntTools.UI/                팔레트 UI (Class Library, NETLOAD 진입점)
│       ├── Plugin.cs                  IExtensionApplication 구현
│       ├── PaletteManager.cs          PaletteSet 싱글 인스턴스 관리
│       ├── Commands/
│       │   └── PaletteCommands.cs     GNTTOOLS_SHOW 등 UI 명령
│       ├── Controls/                  WPF UserControl
│       │   ├── WtlPanel.xaml(.cs)     상수 탭
│       │   ├── SwlPanel.xaml(.cs)     하수 탭
│       │   ├── KepcoPanel.xaml(.cs)   전력통신 탭
│       │   └── SettingsPanel.xaml(.cs) 환경설정 탭
│       └── ViewModels/                MVVM ViewModel
│           ├── WtlViewModel.cs
│           ├── SwlViewModel.cs
│           ├── KepcoViewModel.cs
│           └── SettingsViewModel.cs
│
├── docs/
│   └── specs/                       설계 문서
├── references/                      API 레퍼런스 (기존)
└── old_make/                        기존 VB.NET (참고용)
```

### 프로젝트 의존성

```
GntTools.UI (진입점, AutoCAD NETLOAD 대상)
  ├── GntTools.Core       (공통 라이브러리)
  ├── GntTools.Wtl        (상수) → GntTools.Core
  ├── GntTools.Swl        (하수) → GntTools.Core
  └── GntTools.Kepco      (전력) → GntTools.Core
```

### 어셈블리 참조

| DLL | 용도 | 참조 프로젝트 |
|-----|------|-------------|
| `AcDbMgd.dll` | DatabaseServices | Core, 도메인 모듈 |
| `AcMgd.dll` | ApplicationServices | Core, UI |
| `AcCoreMgd.dll` | EditorInput, Runtime | Core, 도메인 모듈 |
| `ManagedMapApi.dll` | Map 3D ODT API | Core |
| `PresentationCore` | WPF | UI |
| `PresentationFramework` | WPF | UI |

모든 AutoCAD/Map DLL: `Copy Local = false`

---

## 4. 핵심 클래스 설계

### OdtManager (Core/Odt/)

```csharp
public class OdtManager
{
    // 테이블 관리
    bool EnsureTable(IOdtSchema schema);
    bool RemoveTable(string tableName);
    bool TableExists(string tableName);

    // 레코드 CRUD
    bool AttachRecord(string tableName, ObjectId entityId);
    bool UpdateRecord(string tableName, ObjectId entityId, Dictionary<string, object> values);
    string[] ReadRecord(string tableName, ObjectId entityId);
    bool RemoveRecord(string tableName, ObjectId entityId);
    bool RecordExists(string tableName, ObjectId entityId);
}
```

### IOdtSchema (Core/Odt/)

```csharp
public interface IOdtSchema
{
    string TableName { get; }
    string Description { get; }
    IReadOnlyList<OdtFieldDef> Fields { get; }
}

public class OdtFieldDef
{
    public string Name { get; }
    public string Description { get; }
    public int DataType { get; }  // Constants.DataType.*
}
```

### EntitySelector (Core/Selection/)

```csharp
public class EntitySelector
{
    // 사용자 대화형 선택
    ObjectId SelectOne(SelectionFilter filter, string message = null);
    ObjectId[] SelectMultiple(SelectionFilter filter, string message = null);

    // 프로그래밍 방식 선택
    ObjectId[] SelectAll(SelectionFilter filter);
    ObjectId[] SelectAtPoint(Point3d point, double tolerance, SelectionFilter filter);
    ObjectId[] SelectInWindow(Point3d pt1, Point3d pt2, SelectionFilter filter);
    ObjectId[] SelectCrossing(Point3d pt1, Point3d pt2, SelectionFilter filter);
}
```

### DepthCalculator (Core/Geometry/)

```csharp
public class DepthResult
{
    public double BeginDepth { get; set; }
    public double EndDepth { get; set; }
    public double AverageDepth { get; set; }
    public double MaxDepth { get; set; }
    public double MinDepth { get; set; }
    public bool IsUndetected { get; set; }
}

public class DepthCalculator
{
    // 자동: 정점별 심도 텍스트 읽기 (KEPCO/SWL/WTL 공통)
    DepthResult MeasureAtVertices(ObjectId polylineId, string depthLayerFilter);

    // 수동: 사용자 입력값
    DepthResult FromManualInput(double beginDepth, double endDepth);

    // 불탐
    DepthResult Undetected();
}
```

### 도메인 Service 패턴 (예: WtlService)

```csharp
public class WtlService
{
    private readonly OdtManager _odt;
    private readonly DepthCalculator _depth;
    private readonly TextWriter _text;
    private readonly LeaderWriter _leader;

    // 신규 입력
    public bool CreateAttribute(WtlInputData input, ObjectId polylineId);

    // 수정
    public bool ModifyAttribute(WtlInputData input, ObjectId polylineId);

    // 내부: 라벨 생성, ODT 기록, 색상 변경
}
```

---

## 5. 툴 팔레트 UI 설계

### PaletteSet 구성

- **PaletteSet 이름:** "GNT Tools"
- **GUID:** 고정 (사용자 설정 영구 저장)
- **탭 4개:** 상수, 하수, 전력통신, 환경설정
- **WPF UserControl** 사용 (`AddVisual`)
- **MVVM 패턴:** ViewModel이 팔레트 데이터 보유, CLI 명령과 공유

### 상수(WTL) 탭

```
┌─────────────────────────────────────┐
│  ┌─ 기본정보 ──────────────────┐  │
│  │ 설치년도 [2024      ]  ▼    │  │
│  │ 관재질   [PE        ]  ▼    │  │
│  │ 구  경   [200       ]  ▼    │  │
│  └──────────────────────────────┘  │
│                                     │
│  ┌─ 심도 ──────────────────────┐  │
│  │ ○ 자동측정 (정점별 텍스트)   │  │
│  │ ● 수동입력                   │  │
│  │ 시점심도 [1.2  ]             │  │
│  │ 종점심도 [0.9  ]             │  │
│  │ □ 불탐                       │  │
│  └──────────────────────────────┘  │
│                                     │
│  ┌─ 지반고 ────────────────────┐  │
│  │ 시점지반고 [     ] (자동읽기) │  │
│  │ 종점지반고 [     ] (자동읽기) │  │
│  └──────────────────────────────┘  │
│                                     │
│  ┌─ 라벨 ──────────────────────┐  │
│  │ ○ 지시선       ● 관로평행    │  │
│  └──────────────────────────────┘  │
│                                     │
│  ┌────────┐  ┌────────┐          │
│  │ 신규입력 │  │  수정  │          │
│  └────────┘  └────────┘          │
└─────────────────────────────────────┘
```

### 하수(SWL) 탭

상수와 동일 구조 + 추가 필드:
- 용도코드 (드롭다운)
- 박스관: 가로/세로 (구경 타입이 박스일 때 활성화)
- 통로수
- 관저고/관상고/구배 (자동계산 표시, 읽기전용)

### 전력통신(KEPCO) 탭

```
┌─────────────────────────────────────┐
│  ┌─ 기본정보 ──────────────────┐  │
│  │ 설치년도 [2024      ]  ▼    │  │
│  │ 관재질   [ELP       ]  ▼    │  │
│  └──────────────────────────────┘  │
│                                     │
│  ┌─ 단면정보 ──────────────────┐  │
│  │ [단면선택] ← 원/해치 카운팅  │  │
│  │ D200: 3(2)  D175: 0(0)      │  │
│  │ D150: 1(0)  D125: 0(0)      │  │
│  │ D100: 2(1)  D80:  0(0)      │  │
│  │                               │  │
│  │ [B선택]  B: 1.36             │  │
│  │ [H선택]  H: 0.86             │  │
│  │ BxH: 1.36x0.86               │  │
│  └──────────────────────────────┘  │
│                                     │
│  ┌─ 심도 ──────────────────────┐  │
│  │ (자동측정 — 정점별)           │  │
│  │ □ 불탐                       │  │
│  └──────────────────────────────┘  │
│                                     │
│  ┌────────┐ ┌────┐ ┌──────┐    │
│  │ 신규입력 │ │수정│ │오류검증│    │
│  └────────┘ └────┘ └──────┘    │
└─────────────────────────────────────┘
```

### 환경설정 탭

```
┌─────────────────────────────────────┐
│  ┌─ 공통 ──────────────────────┐  │
│  │ 텍스트 스타일  [GHS      ] ▼ │  │
│  │ 텍스트 크기    [1.0      ]   │  │
│  │ 연장 자릿수    [0 ▲▼]       │  │
│  │ 심도 자릿수    [1 ▲▼]       │  │
│  └──────────────────────────────┘  │
│  ┌─ 상수 레이어 ───────────────┐  │
│  │ 심도텍스트   [WS_DEP     ]  │  │
│  │ 지반고텍스트 [WS_HGT     ]  │  │
│  │ 제원텍스트   [WS_LBL     ]  │  │
│  │ 지시선       [WS_LEAD    ]  │  │
│  │ 불탐관로     [WS_BT      ]  │  │
│  └──────────────────────────────┘  │
│  ┌─ 하수 레이어 ───────────────┐  │
│  │ (동일 구조)                  │  │
│  └──────────────────────────────┘  │
│  ┌─ 전력통신 레이어 ───────────┐  │
│  │ (동일 구조 + 도형레이어)     │  │
│  └──────────────────────────────┘  │
│           ┌────────┐               │
│           │  저장   │               │
│           └────────┘               │
└─────────────────────────────────────┘
```

### 워크플로우

```
[팔레트에서 값 입력]
  → [신규입력] 버튼 클릭
  → 에디터: "관로 폴리라인을 선택하세요"  (EntitySelector.SelectOne)
  → (심도=자동이면) DepthCalculator.MeasureAtVertices()
  → (KEPCO면) 이미 [단면선택]으로 수집한 데이터 사용
  → 에디터: "지시선 시작점" → "끝점"  (or 평행이면 "기준점")
  → OdtManager: PIPE_COMMON + 도메인_EXT 기록
  → TextWriter + LeaderWriter: 라벨 생성
  → ColorHelper.SetColor(): 완료 표시
  → 팔레트로 포커스 복귀 → 다음 입력 대기
```

---

## 6. CLI 명령 설계

팔레트와 CLI가 **같은 ViewModel/Service 공유**.

| CLI 명령 | 기능 | 팔레트 대응 |
|----------|------|-----------|
| `GNTTOOLS_SHOW` | 팔레트 표시/토글 | — |
| `GNTTOOLS_WTL_ATT` | 상수 신규입력 | 상수 탭 [신규입력] |
| `GNTTOOLS_WTL_EDIT` | 상수 수정 | 상수 탭 [수정] |
| `GNTTOOLS_SWL_ATT` | 하수 신규입력 | 하수 탭 [신규입력] |
| `GNTTOOLS_SWL_EDIT` | 하수 수정 | 하수 탭 [수정] |
| `GNTTOOLS_KEPCO_ATT` | 전력 신규입력 | 전력 탭 [신규입력] |
| `GNTTOOLS_KEPCO_EDIT` | 전력 수정 | 전력 탭 [수정] |
| `GNTTOOLS_KEPCO_CHK` | 전력 오류검증 | 전력 탭 [오류검증] |
| `GNTTOOLS_SETTINGS` | 환경설정 탭 표시 | 설정 탭 활성화 |

CLI 실행 시: 팔레트가 열려있으면 팔레트 값 사용, 닫혀있으면 에디터 프롬프트로 값 입력.

---

## 7. 설정 관리

`My.Settings` (VB.NET) → **JSON 파일** 기반으로 전환.

저장 경로: `%AppData%/GntTools/settings.json`

```json
{
  "common": {
    "textStyle": "GHS",
    "shxFont": "ROMANS",
    "bigFont": "GHS",
    "textSize": 1.0,
    "lengthDecimals": 0,
    "depthDecimals": 1
  },
  "wtl": {
    "layers": {
      "depth": "WS_DEP",
      "groundHeight": "WS_HGT",
      "label": "WS_LBL",
      "leader": "WS_LEAD",
      "undetected": "WS_BT"
    },
    "defaults": {
      "year": "2024",
      "material": "PE",
      "diameter": "200"
    }
  },
  "swl": {
    "layers": {
      "depth": "SW_DEP",
      "groundHeight": "SW_HGT",
      "label": "SW_LBL",
      "leader": "SW_LEAD",
      "undetected": "SW_BT"
    },
    "defaults": {
      "year": "2024",
      "material": "HP",
      "diameter": "300",
      "useCode": "01"
    }
  },
  "kepco": {
    "layers": {
      "depth": "SC991",
      "drawing": "SC983",
      "label": "SC992",
      "leader": "SC982",
      "undetected": "SC999"
    },
    "defaults": {
      "year": "2024",
      "material": "ELP"
    },
    "diameters": [200, 175, 150, 125, 100, 80]
  }
}
```

---

## 8. 기존 VB.NET 버그 수정 목록

| 기존 버그 | 위치 | C#에서 해결 방법 |
|-----------|------|----------------|
| `ZoomClass.zoom2Obj`: `MaxPoint.X - MinPoint.Y` | 3개 프로젝트 | `ViewportManager`에서 `MinPoint.X` 사용 |
| `String.Replace()` 반환값 무시 | SWL, WTL | C# 불변 문자열 올바르게 `=` 할당 |
| `changColor` 파라미터 무시 (항상 2) | SWL, WTL | `ColorHelper.SetColor(id, colorIndex)` 파라미터 정확 적용 |
| `eXdataClass.removeXdata` Commit 누락 | Kepco | `XDataManager` Transaction 패턴 통일 |
| `ColorChange(ObjectId)` 변수 미할당 | Kepco | C# 컴파일러가 미할당 감지 |
| `userTextClass.rotTxt` 대소문자 오류 | SWL | C# 대소문자 구분으로 컴파일 시 잡힘 |
| Constructor-as-Action 안티패턴 | 3개 모두 | 메서드 기반 서비스 클래스로 전환 |
| `OptionStrict Off` 런타임 에러 | 3개 모두 | C# 정적 타입 시스템으로 컴파일 타임 검출 |
| `chkPipe` 누적 차감 로직 오류 | Kepco | `JunctionChecker`에서 원본 배열 보존 후 비교 |
| `dispObjProperty` 미선언 변수 참조 | Kepco | C# 컴파일 에러로 불가능 |

---

## 9. 구현 순서 (권장)

### Phase 1: Core 기반 (1주)
1. 솔루션 생성 + 프로젝트 구조 셋업
2. `GntTools.Core` 구현 (OdtManager, EntitySelector, DepthCalculator 등)
3. 단위 테스트 가능한 헬퍼 클래스 우선

### Phase 2: 첫 번째 도메인 — WTL 상수 (1주)
1. `GntTools.Wtl` 구현 (가장 단순한 도메인)
2. `GntTools.UI` 기본 틀 + WTL 탭 + 환경설정 탭
3. CLI 명령 동작 확인

### Phase 3: SWL 하수 (1주)
1. `GntTools.Swl` 구현 (관저고/관상고/구배 추가)
2. SWL 탭 UI

### Phase 4: KEPCO 전력통신 (1주)
1. `GntTools.Kepco` 구현 (단면카운팅, BxH, 오류검증)
2. KEPCO 탭 UI

### Phase 5: 통합 테스트 및 배포 (1주)
1. 전체 워크플로우 테스트
2. AutoLoading 레지스트리 설정
3. 문서 정리

---

## 10. 기술 결정 사항

| 항목 | 결정 | 이유 |
|------|------|------|
| 언어 | C# | 정적 타입, 풍부한 AutoCAD 예제 |
| 프레임워크 | .NET Framework 4.7 | AutoCAD Map 3D 2020 호환 (2021~2024도 하위호환) |
| UI | WPF (AddVisual) | 2020+ 네이티브 지원, 데이터 바인딩 |
| MVVM | 수동 구현 | 외부 의존성 최소화 (Prism/MVVM Light 불필요) |
| 설정 저장 | JSON 파일 | 이식성, 가독성, My.Settings 대체 |
| ODT 전략 | 공통 + 확장 (B안) | SHP 내보내기 호환, 필드명 비중복 조건 |
| KEPCO 구경 | JSON 문자열 | 스키마 변경 없이 구경 추가/변경 가능 |
| 불탐 구분 | BTCDE 필드 (Y/N) | Real 0.0과 미측정 구분 |
