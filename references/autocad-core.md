# AutoCAD 2020+ .NET C# API Quick Reference

> AutoCAD .NET API (ObjectARX Managed Wrapper) 핵심 레퍼런스.
> 대상: AutoCAD 2020 ~ 2025, .NET Framework 4.8 / .NET 8 (AutoCAD 2025+)

---

## 1. Required Assemblies & Namespaces

| DLL | 용도 |
|-----|------|
| `AcDbMgd.dll` | 데이터베이스 객체 (Entity, Table, Record 등) |
| `AcMgd.dll` | 애플리케이션 UI (Document, Editor, Ribbon 등) |
| `AcCoreMgd.dll` | 코어 런타임 (CommandLine, DocumentLock 등) |

> **주의:** Copy Local = `false` 설정 필수. AutoCAD 프로세스 내에서 이미 로드됨.

```csharp
using Autodesk.AutoCAD.ApplicationServices;       // 애플리케이션 레벨
using Autodesk.AutoCAD.ApplicationServices.Core;
using Autodesk.AutoCAD.DatabaseServices;           // 데이터베이스 레벨
using Autodesk.AutoCAD.EditorInput;                // 에디터 (사용자 입력, 선택)
using Autodesk.AutoCAD.Geometry;                   // 지오메트리
using Autodesk.AutoCAD.Runtime;                    // 런타임 (CommandClass 등)
using Autodesk.AutoCAD.Colors;                     // 색상
```

```csharp
// 어셈블리 속성 — 프로젝트에 반드시 한 번 선언 (AssemblyInfo.cs 또는 소스 파일 최상단)
[assembly: ExtensionApplication(typeof(MyNamespace.MyPlugin))]   // 초기화 클래스 지정 (null 가능)
[assembly: CommandClass(typeof(MyNamespace.MyCommands))]         // 명령 클래스 지정 (생략 시 전체 검색)

// 플러그인 진입점 — AutoCAD 로드 시 자동 초기화
public class MyPlugin : IExtensionApplication
{
    public void Initialize() { /* 초기화 */ }
    public void Terminate()  { /* 정리 (거의 호출 안 됨) */ }
}
```

---

## 2. Application & Document

```csharp
// 가장 기본적인 접근 패턴
Document doc = Application.DocumentManager.MdiActiveDocument;
Database db = doc.Database;
Editor ed = doc.Editor;
```

> **주의:** `MdiActiveDocument`는 모달 대화상자 실행 중 `null`이 될 수 있음.

```csharp
// Document 주요 멤버
doc.Name;              // string - 파일 전체 경로
doc.Database;          // Database - 도면 데이터베이스
doc.Editor;            // Editor - 명령행 입출력
doc.IsReadOnly;        // bool - 읽기 전용 여부
doc.CommandInProgress; // string - 현재 실행 중인 명령
```

### DocumentLock

```csharp
// Session 커맨드 또는 모들리스 대화상자에서 도큐먼트 수정 시 필수
using (DocumentLock docLock = doc.LockDocument())
{
    using (Transaction tr = db.TransactionManager.StartTransaction())
    {
        // 도면 수정 작업
        tr.Commit();
    }
}
```

> - `[CommandMethod("CMD", CommandFlags.Session)]` → `LockDocument()` **필수**
> - `[CommandMethod("CMD")]` 일반 명령 → Lock **불필요** (자동 잠금)
> - 모들리스 폼/팔레트에서 도면 수정 → `LockDocument()` **필수**

### CommandMethod 어트리뷰트

```csharp
[CommandMethod("MYCMD")]                              // 기본 문서 컨텍스트
[CommandMethod("MYCMD", CommandFlags.Session)]         // 세션 컨텍스트 (문서 간 작업)
[CommandMethod("MYCMD", CommandFlags.Transparent)]     // 투명 명령 (실행 중 호출)
```

---

## 3. Transaction

### 기본 패턴

```csharp
using (Transaction tr = db.TransactionManager.StartTransaction())
{
    BlockTable bt = tr.GetObject(db.BlockTableId, OpenMode.ForRead) as BlockTable;
    tr.Commit(); // 생략 시 자동 Abort → 모든 변경 롤백
}
```

### GetObject — 객체 열기

```csharp
// 시그니처 (오버로드 3개)
DBObject GetObject(ObjectId id, OpenMode mode);
DBObject GetObject(ObjectId id, OpenMode mode, bool openErased);
DBObject GetObject(ObjectId id, OpenMode mode, bool openErased, bool forceOpenOnLockedLayer);

BlockTable bt = tr.GetObject(db.BlockTableId, OpenMode.ForRead) as BlockTable;
BlockTableRecord btr = tr.GetObject(
    bt[BlockTableRecord.ModelSpace], OpenMode.ForWrite) as BlockTableRecord;

// 새 엔티티 등록 — 누락 시 메모리 누수 또는 크래시
Line line = new Line(new Point3d(0, 0, 0), new Point3d(100, 100, 0));
btr.AppendEntity(line);
tr.AddNewlyCreatedDBObject(line, true); // true = 트랜잭션 소유권 위임
```

### 트랜잭션 중첩

```csharp
using (Transaction trOuter = db.TransactionManager.StartTransaction())
{
    using (Transaction trInner = db.TransactionManager.StartTransaction())
    {
        trInner.Commit();
    }
    trOuter.Commit(); // 외부까지 커밋해야 전체 반영
}
// 외부 Abort 시 내부 커밋도 롤백됨
```

---

## 4. Database

### 심볼 테이블 ObjectId 프로퍼티

```csharp
db.BlockTableId;     // 블록 정의 (ModelSpace, PaperSpace 포함)
db.LayerTableId;     // 레이어
db.LinetypeTableId;  // 선종류
db.DimStyleTableId;  // 치수 스타일
db.TextStyleTableId; // 문자 스타일
db.RegAppTableId;    // 등록 애플리케이션 (XData용)
db.UcsTableId;       // 사용자 좌표계
db.ViewportTableId;  // 뷰포트
```

### ModelSpace / PaperSpace 엔티티 추가

```csharp
BlockTable bt = tr.GetObject(db.BlockTableId, OpenMode.ForRead) as BlockTable;

// ModelSpace (ForWrite — 엔티티 추가하려면 쓰기 모드)
BlockTableRecord ms = tr.GetObject(
    bt[BlockTableRecord.ModelSpace], OpenMode.ForWrite) as BlockTableRecord;
// PaperSpace도 동일 패턴
BlockTableRecord ps = tr.GetObject(
    bt[BlockTableRecord.PaperSpace], OpenMode.ForWrite) as BlockTableRecord;

Circle circle = new Circle(new Point3d(0, 0, 0), Vector3d.ZAxis, 50.0);
ms.AppendEntity(circle);
tr.AddNewlyCreatedDBObject(circle, true);
```

### 현재 레이어 / 선종류 / 색상 설정

```csharp
db.Clayer = layerTableRecordId;    // 현재 레이어 (ObjectId)
db.Celtype = linetypeTableRecordId; // 현재 선종류 (ObjectId)
db.Cecolor = Color.FromColorIndex(ColorMethod.ByAci, 1); // 현재 색상 (Red)
```

---

## 5. OpenMode

| 모드 | 용도 | 잠금 | 수정 가능 |
|------|------|------|-----------|
| `ForRead` | 프로퍼티 읽기 전용 | 공유 | X |
| `ForWrite` | 프로퍼티 수정 | 배타 | O |
| `ForNotify` | 알림 수신만 (드물게 사용) | 없음 | X |

> **성능 팁:** 읽기만 할 객체는 반드시 `ForRead`. `ForWrite`는 Undo 기록 생성으로 느림.

### UpgradeOpen / DowngradeOpen

```csharp
// ForRead로 열었다가 조건부로 쓰기 전환
Entity ent = tr.GetObject(entId, OpenMode.ForRead) as Entity;
if (ent.Layer == "OLD_LAYER")
{
    ent.UpgradeOpen();       // ForRead → ForWrite 승격
    ent.Layer = "NEW_LAYER";
}
```

---

## 6. ObjectId

| 항목 | ObjectId | Handle |
|------|----------|--------|
| 수명 | 세션 내 유효 | 도면 파일에 영구 저장 |
| 용도 | 런타임 객체 접근 | 도면 간 / 세션 간 참조 |

```csharp
// ObjectId ↔ Handle 변환
Handle handle = someObjectId.Handle;           // ObjectId → Handle
string handleStr = handle.ToString();          // "1A3F" 16진수 문자열
ObjectId id = db.GetObjectId(false, new Handle(0x1A3F), 0); // Handle → ObjectId

// 유효성 검사
id.IsNull;    // 할당되지 않은 ID (ObjectId.Null)
id.IsValid;   // 유효한 데이터베이스 객체 참조
id.IsErased;  // 삭제된 객체 (Undo 복구 가능)

if (!id.IsNull && id.IsValid && !id.IsErased)
    DBObject obj = tr.GetObject(id, OpenMode.ForRead);

// GetObject 없이 타입 확인 (성능 우수)
RXClass lineClass = RXObject.GetClass(typeof(Line));
if (id.ObjectClass.IsDerivedFrom(lineClass))
    Line line = tr.GetObject(id, OpenMode.ForRead) as Line;
```

---

## 7. Common Patterns

### 패턴 1: ModelSpace에 엔티티 추가 (전체 흐름)

```csharp
[CommandMethod("ADD_LINE")]
public void AddLine()
{
    Document doc = Application.DocumentManager.MdiActiveDocument;
    Database db = doc.Database;

    using (Transaction tr = db.TransactionManager.StartTransaction())
    {
        BlockTable bt = tr.GetObject(db.BlockTableId, OpenMode.ForRead) as BlockTable;
        BlockTableRecord ms = tr.GetObject(
            bt[BlockTableRecord.ModelSpace], OpenMode.ForWrite
        ) as BlockTableRecord;

        Line line = new Line(new Point3d(0, 0, 0), new Point3d(100, 50, 0));
        line.Layer = "0";
        line.ColorIndex = 256; // ByLayer
        ms.AppendEntity(line);
        tr.AddNewlyCreatedDBObject(line, true);
        tr.Commit();
    }
}
```

### 패턴 2: ModelSpace 엔티티 순회

```csharp
using (Transaction tr = db.TransactionManager.StartTransaction())
{
    BlockTable bt = tr.GetObject(db.BlockTableId, OpenMode.ForRead) as BlockTable;
    BlockTableRecord ms = tr.GetObject(
        bt[BlockTableRecord.ModelSpace], OpenMode.ForRead
    ) as BlockTableRecord;

    foreach (ObjectId id in ms) // BlockTableRecord = IEnumerable<ObjectId>
    {
        Entity ent = tr.GetObject(id, OpenMode.ForRead) as Entity;
        if (ent == null) continue;
        ed.WriteMessage($"\n{ent.GetType().Name}, Layer={ent.Layer}");
    }
    tr.Commit();
}
```

### 패턴 3: 레이어 생성 + 현재 레이어 설정

```csharp
public ObjectId CreateLayer(Database db, string layerName, short colorIndex)
{
    ObjectId layerId = ObjectId.Null;
    using (Transaction tr = db.TransactionManager.StartTransaction())
    {
        LayerTable lt = tr.GetObject(db.LayerTableId, OpenMode.ForRead) as LayerTable;
        if (lt.Has(layerName))
        {
            layerId = lt[layerName];
        }
        else
        {
            LayerTableRecord ltr = new LayerTableRecord();
            ltr.Name = layerName;
            ltr.Color = Color.FromColorIndex(ColorMethod.ByAci, colorIndex);
            ltr.LineWeight = LineWeight.LineWeight025;
            lt.UpgradeOpen();
            layerId = lt.Add(ltr);
            tr.AddNewlyCreatedDBObject(ltr, true);
        }
        // 현재 레이어로 설정하려면:
        // db.Clayer = layerId;
        tr.Commit();
    }
    return layerId;
}
```

### 패턴 4: 에러 핸들링

```csharp
Document doc = Application.DocumentManager.MdiActiveDocument;
if (doc == null) return; // 모달 대화상자 중일 수 있음

try
{
    using (Transaction tr = db.TransactionManager.StartTransaction())
    {
        // 잠긴 레이어 위 엔티티 수정 시 forceOpenOnLockedLayer = true
        Entity ent = tr.GetObject(someId, OpenMode.ForWrite, false, true) as Entity;
        tr.Commit();
    }
}
catch (Autodesk.AutoCAD.Runtime.Exception ex)
{   // AutoCAD 고유 예외 (eOnLockedLayer, eWasErased 등)
    ed.WriteMessage($"\nAutoCAD 오류: {ex.ErrorStatus} - {ex.Message}");
}
catch (System.Exception ex)
{
    ed.WriteMessage($"\n오류: {ex.Message}");
}
```

### 패턴 5: Editor 사용자 입력

```csharp
// 점 선택
PromptPointResult ppr = ed.GetPoint("\n점을 선택하세요: ");
if (ppr.Status != PromptStatus.OK) return;
Point3d pt = ppr.Value;

// 엔티티 선택
PromptEntityResult per = ed.GetEntity("\n엔티티를 선택하세요: ");
if (per.Status != PromptStatus.OK) return;
ObjectId selectedId = per.ObjectId;

// 문자열 입력 (AllowSpaces로 공백 포함 허용)
PromptStringOptions pso = new PromptStringOptions("\n이름 입력: ") { AllowSpaces = true };
PromptResult pr = ed.GetString(pso);

// 키워드 선택
PromptKeywordOptions pko = new PromptKeywordOptions("\n옵션 [예(Y)/아니오(N)]: ");
pko.Keywords.Add("Y"); pko.Keywords.Add("N"); pko.Keywords.Default = "Y";
PromptResult pkr = ed.GetKeywords(pko); // pkr.StringResult = "Y" or "N"
```

### 패턴 6: SelectionFilter

```csharp
// 라인만 선택
TypedValue[] filterList = { new TypedValue((int)DxfCode.Start, "LINE") };
SelectionFilter filter = new SelectionFilter(filterList);
PromptSelectionResult selRes = ed.GetSelection(filter);
if (selRes.Status != PromptStatus.OK) return;
SelectionSet ss = selRes.Value;

// 복합 필터: 레이어 "WALL"의 라인 또는 폴리라인
TypedValue[] complexFilter = {
    new TypedValue((int)DxfCode.Operator, "<OR"),
    new TypedValue((int)DxfCode.Start, "LINE"),
    new TypedValue((int)DxfCode.Start, "LWPOLYLINE"),
    new TypedValue((int)DxfCode.Operator, "OR>"),
    new TypedValue((int)DxfCode.LayerName, "WALL")
};
```

### 패턴 7: XData 읽기/쓰기

```csharp
// 1) 앱 등록 (XData 쓰기 전 필수)
RegAppTable rat = tr.GetObject(db.RegAppTableId, OpenMode.ForRead) as RegAppTable;
if (!rat.Has("MY_APP"))
{
    rat.UpgradeOpen();
    RegAppTableRecord ratr = new RegAppTableRecord { Name = "MY_APP" };
    rat.Add(ratr);
    tr.AddNewlyCreatedDBObject(ratr, true);
}

// 2) XData 쓰기
Entity ent = tr.GetObject(entId, OpenMode.ForWrite) as Entity;
ent.XData = new ResultBuffer(
    new TypedValue((int)DxfCode.ExtendedDataRegAppName, "MY_APP"),
    new TypedValue((int)DxfCode.ExtendedDataAsciiString, "some_value"),
    new TypedValue((int)DxfCode.ExtendedDataReal, 3.14));

// 3) XData 읽기
ResultBuffer xdata = ent.GetXDataForApplication("MY_APP");
if (xdata != null)
{
    TypedValue[] values = xdata.AsArray();
    // values[0]=AppName, values[1]="some_value", values[2]=3.14
    xdata.Dispose();
}
```

---

## 부록: 자주 발생하는 오류

| ErrorStatus | 원인 | 해결 |
|-------------|------|------|
| `eNotOpenForWrite` | `ForRead`로 열고 수정 시도 | `ForWrite` 또는 `UpgradeOpen()` |
| `eOnLockedLayer` | 잠긴 레이어 엔티티 수정 | `GetObject(..., false, true)` |
| `eWasErased` | 삭제된 객체 접근 | `id.IsErased` 사전 검사 |
| `eNoDocument` | `MdiActiveDocument`가 null | null 체크 |
| `eLockViolation` | Session 명령에서 Lock 없이 수정 | `doc.LockDocument()` |
| `eWasNotifying` | 이벤트 핸들러 내 트랜잭션 중첩 | 이벤트에서 직접 DB 수정 회피 |
