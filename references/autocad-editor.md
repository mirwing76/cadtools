# AutoCAD Editor / User Input API Reference (C# .NET, 2020+)

> Autodesk.AutoCAD.EditorInput 네임스페이스 기반 실무 레퍼런스

---

## 1. Editor Access

```csharp
using Autodesk.AutoCAD.ApplicationServices;
using Autodesk.AutoCAD.EditorInput;

// Editor 객체 가져오기
Document doc = Application.DocumentManager.MdiActiveDocument;
Editor ed = doc.Editor;

// 명령줄에 메시지 출력
ed.WriteMessage("\n선택된 객체 수: {0}", count);
ed.WriteMessage("\n작업 완료.");
```

| 멤버 | 설명 |
|---|---|
| `Editor` | 현재 문서의 편집기 객체. 모든 사용자 입력의 진입점 |
| `WriteMessage(string)` | 명령줄에 문자열 출력. `\n`으로 줄바꿈 |
| `WriteMessage(string, params object[])` | `string.Format` 스타일 서식 지원 |

---

## 2. Prompt for Values

### 2.1 GetString - 문자열 입력

```csharp
// 사용자에게 문자열 입력 요청
PromptStringOptions pso = new PromptStringOptions("\n이름 입력: ");
pso.AllowSpaces = true;          // 공백 포함 허용
pso.DefaultValue = "Default";    // 기본값 지정
pso.UseDefaultValue = true;      // 기본값 사용 여부

PromptResult pr = ed.GetString(pso);
if (pr.Status == PromptStatus.OK)
{
    string value = pr.StringResult;
}
```

### 2.2 GetInteger - 정수 입력

```csharp
// 정수 입력 요청
PromptIntegerOptions pio = new PromptIntegerOptions("\n층수 입력: ");
pio.AllowNegative = false;   // 음수 불허
pio.AllowZero = false;       // 0 불허
pio.AllowNone = true;        // Enter(빈 입력) 허용
pio.DefaultValue = 1;        // 기본값
pio.UseDefaultValue = true;

PromptIntegerResult pir = ed.GetInteger(pio);
if (pir.Status == PromptStatus.OK)
{
    int value = pir.Value;
}
```

### 2.3 GetDouble - 실수 입력

```csharp
// 실수 입력 요청
PromptDoubleOptions pdo = new PromptDoubleOptions("\n간격 입력: ");
pdo.AllowNegative = false;
pdo.AllowZero = false;
pdo.DefaultValue = 1.0;
pdo.UseDefaultValue = true;

PromptDoubleResult pdr = ed.GetDouble(pdo);
if (pdr.Status == PromptStatus.OK)
{
    double value = pdr.Value;
}
```

### 2.4 GetPoint - 좌표 입력

```csharp
using Autodesk.AutoCAD.Geometry;

// 단일 점 입력
PromptPointOptions ppo = new PromptPointOptions("\n시작점 선택: ");
ppo.AllowNone = false;

PromptPointResult ppr = ed.GetPoint(ppo);
if (ppr.Status == PromptStatus.OK)
{
    Point3d pt = ppr.Value;
}

// 기준점으로부터 러버밴드 라인 표시
PromptPointOptions ppo2 = new PromptPointOptions("\n끝점 선택: ");
ppo2.BasePoint = ppr.Value;       // 기준점 설정
ppo2.UseBasePoint = true;         // 러버밴드 활성화
ppo2.UseDashedLine = true;        // 점선 표시

PromptPointResult ppr2 = ed.GetPoint(ppo2);
```

### 2.5 GetDistance - 거리 입력

```csharp
// 두 점 사이 거리 또는 직접 숫자 입력
PromptDistanceOptions pdiso = new PromptDistanceOptions("\n거리 입력: ");
pdiso.AllowNegative = false;
pdiso.AllowZero = false;
pdiso.DefaultValue = 100.0;
pdiso.UseDefaultValue = true;

PromptDoubleResult pdisRes = ed.GetDistance(pdiso);
if (pdisRes.Status == PromptStatus.OK)
{
    double dist = pdisRes.Value;
}
```

### 2.6 GetAngle - 각도 입력

```csharp
// 각도 입력 (라디안 반환)
PromptAngleOptions pao = new PromptAngleOptions("\n각도 입력: ");
pao.AllowNone = false;
pao.DefaultValue = Math.PI / 2;   // 기본값: 90도
pao.UseDefaultValue = true;

PromptDoubleResult paRes = ed.GetAngle(pao);
if (paRes.Status == PromptStatus.OK)
{
    double radians = paRes.Value;
    double degrees = radians * (180.0 / Math.PI);  // 도 변환
}
```

### 2.7 GetKeywords - 키워드 선택

```csharp
// 키워드 옵션 제공
PromptKeywordOptions pko = new PromptKeywordOptions("\n옵션 선택 [예(Y)/아니오(N)]: ");
pko.Keywords.Add("Yes", "Y", "예(Y)");     // globalName, localName, displayName
pko.Keywords.Add("No", "N", "아니오(N)");
pko.Keywords.Default = "Yes";               // 기본 키워드
pko.AllowNone = true;                       // Enter로 기본값 선택

PromptResult pkr = ed.GetKeywords(pko);
if (pkr.Status == PromptStatus.OK)
{
    string keyword = pkr.StringResult;  // "Yes" 또는 "No"
}
```

### Common Options 요약

| 옵션 | 적용 대상 | 설명 |
|---|---|---|
| `AllowNone` | 전체 | Enter 키(빈 입력) 허용. true이면 `PromptStatus.None` 반환 |
| `AllowNegative` | Integer, Double, Distance | 음수 입력 허용 |
| `AllowZero` | Integer, Double, Distance | 0 입력 허용 |
| `DefaultValue` | 전체 | 기본값 지정 |
| `UseDefaultValue` | 전체 | true이면 프롬프트에 `<기본값>` 표시 |
| `Message` | 전체 | 프롬프트 메시지 (`\n` 접두어 권장) |
| `AllowSpaces` | String | 공백 포함 입력 허용 |
| `UseBasePoint` | Point | 러버밴드 기준점 사용 |
| `Keywords` | 전체 | 키워드 옵션 추가 가능 (`.Keywords.Add()`) |

---

## 3. Entity Selection

### 3.1 GetEntity - 단일 객체 선택

```csharp
// 단일 객체 선택 요청
PromptEntityOptions peo = new PromptEntityOptions("\n객체 선택: ");
peo.SetRejectMessage("\n폴리라인만 선택 가능합니다.");
peo.AddAllowedClass(typeof(Polyline), true);  // 특정 타입만 허용

PromptEntityResult per = ed.GetEntity(peo);
if (per.Status == PromptStatus.OK)
{
    ObjectId objId = per.ObjectId;         // 선택된 객체 ID
    Point3d pickPt = per.PickedPoint;      // 클릭한 좌표
}
```

| 멤버 | 설명 |
|---|---|
| `AddAllowedClass(Type, bool)` | 선택 가능 객체 타입 제한. bool은 정확히 일치(true) 또는 파생 포함(false) |
| `SetRejectMessage(string)` | 허용되지 않는 객체 선택 시 출력할 메시지 |
| `PromptEntityResult.ObjectId` | 선택된 객체의 ObjectId |
| `PromptEntityResult.PickedPoint` | 사용자가 클릭한 좌표 |

### 3.2 GetSelection - 다중 객체 선택

```csharp
// 다중 객체 선택 (사용자가 직접 선택 영역 지정)
PromptSelectionResult psr = ed.GetSelection();
if (psr.Status == PromptStatus.OK)
{
    SelectionSet ss = psr.Value;
    ed.WriteMessage("\n선택된 객체 수: {0}", ss.Count);

    // 선택된 객체 순회
    foreach (SelectedObject so in ss)
    {
        if (so != null)
        {
            ObjectId id = so.ObjectId;
            // Transaction 내에서 객체 열기
        }
    }
}
```

### 3.3 PromptSelectionOptions 활용

```csharp
// 선택 옵션 커스터마이징
PromptSelectionOptions pso = new PromptSelectionOptions();
pso.MessageForAdding = "\n객체를 선택하세요 (추가): ";
pso.MessageForRemoval = "\n제거할 객체 선택: ";
pso.AllowDuplicates = false;
pso.SingleOnly = false;            // true면 단일 선택만 허용
pso.SinglePickInSpace = false;     // true면 빈 공간 클릭 시 종료
pso.RejectObjectsOnLockedLayers = true;  // 잠긴 레이어 객체 거부

PromptSelectionResult psr = ed.GetSelection(pso);
```

---

## 4. Selection Filters

### 4.1 기본 필터 - TypedValue 배열

```csharp
using Autodesk.AutoCAD.DatabaseServices;

// 엔티티 타입으로 필터링 (폴리라인만)
TypedValue[] filter = new TypedValue[]
{
    new TypedValue((int)DxfCode.Start, "LWPOLYLINE")
};
SelectionFilter sf = new SelectionFilter(filter);

PromptSelectionResult psr = ed.GetSelection(sf);
```

### 4.2 레이어 필터

```csharp
// 특정 레이어의 객체만 선택
TypedValue[] filter = new TypedValue[]
{
    new TypedValue((int)DxfCode.LayerName, "도면층1")
};
SelectionFilter sf = new SelectionFilter(filter);

PromptSelectionResult psr = ed.GetSelection(sf);
```

### 4.3 논리 연산자

```csharp
// OR 조건: LINE 또는 CIRCLE
TypedValue[] filterOr = new TypedValue[]
{
    new TypedValue((int)DxfCode.Operator, "<OR"),
    new TypedValue((int)DxfCode.Start, "LINE"),
    new TypedValue((int)DxfCode.Start, "CIRCLE"),
    new TypedValue((int)DxfCode.Operator, "OR>")
};

// AND + NOT 조건: 폴리라인이면서 레이어가 "보조선"이 아닌 것
TypedValue[] filterComplex = new TypedValue[]
{
    new TypedValue((int)DxfCode.Operator, "<AND"),
    new TypedValue((int)DxfCode.Start, "LWPOLYLINE"),
    new TypedValue((int)DxfCode.Operator, "<NOT"),
    new TypedValue((int)DxfCode.LayerName, "보조선"),
    new TypedValue((int)DxfCode.Operator, "NOT>"),
    new TypedValue((int)DxfCode.Operator, "AND>")
};
```

### 4.4 복합 필터 예시

```csharp
// 실무 예시: 특정 레이어의 TEXT 또는 MTEXT만 선택
TypedValue[] filter = new TypedValue[]
{
    new TypedValue((int)DxfCode.Operator, "<AND"),
    new TypedValue((int)DxfCode.LayerName, "주석"),
    new TypedValue((int)DxfCode.Operator, "<OR"),
    new TypedValue((int)DxfCode.Start, "TEXT"),
    new TypedValue((int)DxfCode.Start, "MTEXT"),
    new TypedValue((int)DxfCode.Operator, "OR>"),
    new TypedValue((int)DxfCode.Operator, "AND>")
};
SelectionFilter sf = new SelectionFilter(filter);

PromptSelectionOptions pso = new PromptSelectionOptions();
pso.MessageForAdding = "\n주석 텍스트를 선택하세요: ";

PromptSelectionResult psr = ed.GetSelection(pso, sf);
```

| DxfCode 상수 | 값 | 설명 |
|---|---|---|
| `DxfCode.Start` | 0 | 엔티티 타입명 (LINE, CIRCLE, LWPOLYLINE 등) |
| `DxfCode.LayerName` | 8 | 레이어명 |
| `DxfCode.Color` | 62 | 색상 번호 |
| `DxfCode.LinetypeName` | 6 | 선종류명 |
| `DxfCode.Operator` | -4 | 논리 연산자 (`<AND`, `AND>`, `<OR`, `OR>`, `<NOT`, `NOT>`) |

---

## 5. PromptStatus

```csharp
PromptPointResult ppr = ed.GetPoint(new PromptPointOptions("\n점 선택: "));

switch (ppr.Status)
{
    case PromptStatus.OK:
        // 정상 입력 완료
        Point3d pt = ppr.Value;
        break;

    case PromptStatus.Cancel:
        // 사용자가 Esc 누름
        ed.WriteMessage("\n작업이 취소되었습니다.");
        return;

    case PromptStatus.None:
        // 사용자가 Enter만 누름 (AllowNone = true일 때)
        ed.WriteMessage("\n기본값을 사용합니다.");
        break;

    case PromptStatus.Keyword:
        // 키워드 입력됨
        string kw = ppr.StringResult;
        break;

    case PromptStatus.Error:
        // 입력 오류 발생
        ed.WriteMessage("\n입력 오류.");
        return;
}
```

| Status | 발생 조건 | 처리 방법 |
|---|---|---|
| `OK` | 유효한 값 입력 완료 | `.Value` 또는 `.StringResult`로 값 사용 |
| `Cancel` | Esc 키 입력 | 명령 종료 또는 이전 단계로 복귀 |
| `None` | Enter 키만 입력 (`AllowNone=true`) | 기본값 사용 또는 루프 종료 조건으로 활용 |
| `Keyword` | 등록된 키워드 입력 | `.StringResult`로 키워드 문자열 확인 |
| `Error` | 시스템 오류 | 명령 종료, 에러 로깅 |

---

## 6. Common Patterns

### 6.1 특정 타입만 선택 (폴리라인)

```csharp
[CommandMethod("SEL_PLINE")]
public void SelectPolylines()
{
    Editor ed = Application.DocumentManager.MdiActiveDocument.Editor;

    // 폴리라인 전용 필터
    TypedValue[] tv = { new TypedValue((int)DxfCode.Start, "LWPOLYLINE") };
    SelectionFilter sf = new SelectionFilter(tv);

    PromptSelectionResult psr = ed.GetSelection(sf);
    if (psr.Status != PromptStatus.OK) return;

    ed.WriteMessage("\n폴리라인 {0}개 선택됨.", psr.Value.Count);
}
```

### 6.2 특정 레이어 객체 선택

```csharp
[CommandMethod("SEL_LAYER")]
public void SelectByLayer()
{
    Editor ed = Application.DocumentManager.MdiActiveDocument.Editor;

    // 레이어명 입력 받기
    PromptStringOptions pso = new PromptStringOptions("\n레이어명 입력: ");
    pso.AllowSpaces = false;
    PromptResult pr = ed.GetString(pso);
    if (pr.Status != PromptStatus.OK) return;

    // 해당 레이어 객체만 필터링하여 선택
    TypedValue[] tv = { new TypedValue((int)DxfCode.LayerName, pr.StringResult) };
    SelectionFilter sf = new SelectionFilter(tv);

    PromptSelectionResult psr = ed.GetSelection(sf);
    if (psr.Status != PromptStatus.OK) return;

    ed.WriteMessage("\n[{0}] 레이어에서 {1}개 선택.", pr.StringResult, psr.Value.Count);
}
```

### 6.3 키워드 옵션 프롬프트 (Yes/No/Options)

```csharp
[CommandMethod("CONFIRM_ACTION")]
public void ConfirmAction()
{
    Editor ed = Application.DocumentManager.MdiActiveDocument.Editor;

    // 키워드 기반 옵션 선택
    PromptKeywordOptions pko = new PromptKeywordOptions(
        "\n작업 선택 [삭제(D)/이동(M)/복사(C)/취소(X)]: ");
    pko.Keywords.Add("Delete", "D", "삭제(D)");
    pko.Keywords.Add("Move", "M", "이동(M)");
    pko.Keywords.Add("Copy", "C", "복사(C)");
    pko.Keywords.Add("Cancel", "X", "취소(X)");
    pko.Keywords.Default = "Copy";
    pko.AllowNone = true;

    PromptResult pr = ed.GetKeywords(pko);
    if (pr.Status != PromptStatus.OK) return;

    switch (pr.StringResult)
    {
        case "Delete": /* 삭제 로직 */ break;
        case "Move":   /* 이동 로직 */ break;
        case "Copy":   /* 복사 로직 */ break;
        case "Cancel": return;
    }
}
```

### 6.4 기본값 포함 프롬프트

```csharp
[CommandMethod("SET_OFFSET")]
public void SetOffset()
{
    Editor ed = Application.DocumentManager.MdiActiveDocument.Editor;

    // 기본값이 표시되는 실수 입력 프롬프트
    PromptDoubleOptions pdo = new PromptDoubleOptions("\n옵셋 거리 입력: ");
    pdo.DefaultValue = 100.0;
    pdo.UseDefaultValue = true;     // 프롬프트에 <100.0> 표시
    pdo.AllowNegative = false;
    pdo.AllowZero = false;

    PromptDoubleResult pdr = ed.GetDouble(pdo);

    double offset;
    if (pdr.Status == PromptStatus.OK)
        offset = pdr.Value;
    else if (pdr.Status == PromptStatus.None)
        offset = pdo.DefaultValue;  // Enter만 눌렀을 때 기본값 사용
    else
        return;

    ed.WriteMessage("\n옵셋 거리: {0:F2}", offset);
}
```

### 6.5 반복 입력 루프 (유효 입력까지 반복)

```csharp
[CommandMethod("PICK_POINTS")]
public void PickPointsLoop()
{
    Editor ed = Application.DocumentManager.MdiActiveDocument.Editor;
    List<Point3d> points = new List<Point3d>();

    // Enter 또는 Esc까지 점을 반복 수집
    while (true)
    {
        PromptPointOptions ppo = new PromptPointOptions(
            "\n점 선택 (Enter=종료): ");
        ppo.AllowNone = true;   // Enter로 루프 종료 허용

        // 두 번째 점부터 러버밴드 표시
        if (points.Count > 0)
        {
            ppo.BasePoint = points[points.Count - 1];
            ppo.UseBasePoint = true;
            ppo.UseDashedLine = true;
        }

        PromptPointResult ppr = ed.GetPoint(ppo);

        if (ppr.Status == PromptStatus.None)
            break;   // Enter로 정상 종료
        if (ppr.Status != PromptStatus.OK)
            return;  // Esc 또는 에러로 취소

        points.Add(ppr.Value);
        ed.WriteMessage("\n  점 #{0}: ({1:F2}, {2:F2}, {3:F2})",
            points.Count, ppr.Value.X, ppr.Value.Y, ppr.Value.Z);
    }

    ed.WriteMessage("\n총 {0}개 점 수집 완료.", points.Count);
}
```

---

> **네임스페이스 요약**: `Autodesk.AutoCAD.ApplicationServices`, `Autodesk.AutoCAD.EditorInput`, `Autodesk.AutoCAD.DatabaseServices`, `Autodesk.AutoCAD.Geometry`
