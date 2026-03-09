# AutoCAD 2020+ Entity Types - C# .NET API Reference

> **Namespace:** `Autodesk.AutoCAD.DatabaseServices`, `Autodesk.AutoCAD.Geometry`
> **Target:** AutoCAD 2020+ (.NET Framework 4.7.2 / .NET 8 for 2025+)

---

## 1. Entity Base Class

모든 도면 객체의 기본 클래스. `DBObject` -> `Entity` 상속 구조.

| Property | Type | Description |
|---|---|---|
| `Layer` | `string` | 레이어 이름 |
| `Color` | `Color` | 색상 (ByLayer, ByBlock, 직접 지정) |
| `ColorIndex` | `short` | ACI 색상 인덱스 (0-256) |
| `Linetype` | `string` | 선 종류 이름 |
| `LinetypeScale` | `double` | 선 종류 스케일 |
| `LineWeight` | `LineWeight` | 선 두께 |
| `Visible` | `bool` | 표시 여부 |

| Method | Description |
|---|---|
| `GetType()` | 런타임 엔티티 타입 반환 (`RXClass`) |
| `Clone()` | 엔티티 딥 카피 |
| `Erase()` | 데이터베이스에서 삭제 |
| `TransformBy(Matrix3d)` | 변환 매트릭스 적용 |
| `IntersectWith(Entity, Intersect)` | 교차점 계산 |

```csharp
// 모든 엔티티 추가의 기본 구조
using (Transaction tr = db.TransactionManager.StartTransaction())
{
    BlockTable bt = tr.GetObject(db.BlockTableId, OpenMode.ForRead) as BlockTable;
    BlockTableRecord btr = tr.GetObject(
        bt[BlockTableRecord.ModelSpace], OpenMode.ForWrite) as BlockTableRecord;

    Entity ent = new Line(Point3d.Origin, new Point3d(100, 100, 0));
    ent.Layer = "0";
    ent.ColorIndex = 1; // 빨간색

    btr.AppendEntity(ent);
    tr.AddNewlyCreatedDBObject(ent, true);
    tr.Commit();
}
```

---

## 2. Line

| Property | Type | Description |
|---|---|---|
| `StartPoint` | `Point3d` | 시작점 |
| `EndPoint` | `Point3d` | 끝점 |
| `Length` | `double` | 길이 (읽기 전용) |
| `Angle` | `double` | XY 평면 각도, 라디안 (읽기 전용) |
| `Delta` | `Vector3d` | Start→End 벡터 |

```csharp
// 시작점과 끝점으로 직선 생성
Line line = new Line(new Point3d(0, 0, 0), new Point3d(100, 50, 0));
line.Layer = "GRID";

btr.AppendEntity(line);
tr.AddNewlyCreatedDBObject(line, true);
```

---

## 3. Polyline (Lightweight)

2D 경량 폴리라인. `Polyline2d`/`Polyline3d`와 구분됨.

| Property | Type | Description |
|---|---|---|
| `NumberOfVertices` | `int` | 정점 개수 |
| `Closed` | `bool` | 닫힘 여부 |
| `Length` | `double` | 전체 길이 |
| `Area` | `double` | 닫힌 폴리라인의 면적 |
| `ConstantWidth` | `double` | 일정 폭 |

| Method | Description |
|---|---|
| `AddVertexAt(int, Point2d, bulge, startW, endW)` | 정점 추가 |
| `GetPoint2dAt(int)` / `GetPoint3dAt(int)` | 정점 좌표 반환 |
| `GetBulgeAt(int)` / `SetBulgeAt(int, double)` | 벌지값 (0=직선, 1=반원) |
| `SetPointAt(int, Point2d)` | 정점 좌표 변경 |
| `RemoveVertexAt(int)` | 정점 제거 |

```csharp
// 빈 폴리라인 생성 후 정점 추가
Polyline pline = new Polyline();
// AddVertexAt(index, point2d, bulge, startWidth, endWidth)
pline.AddVertexAt(0, new Point2d(0, 0), 0, 0, 0);
pline.AddVertexAt(1, new Point2d(100, 0), 0, 0, 0);
pline.AddVertexAt(2, new Point2d(100, 50), 0, 0, 0);
pline.AddVertexAt(3, new Point2d(0, 50), 0, 0, 0);
pline.Closed = true; // 닫힌 폴리라인

// 모서리가 둥근 직사각형 - bulge 활용
Polyline rpline = new Polyline();
double bulge = Math.Tan(Math.PI / 8); // 90도 호의 벌지값
rpline.AddVertexAt(0, new Point2d(10, 0), bulge, 0, 0);  // 호 세그먼트
rpline.AddVertexAt(1, new Point2d(90, 0), 0, 0, 0);      // 직선 세그먼트
rpline.Closed = true;
```

---

## 4. Circle

| Property | Type | Description |
|---|---|---|
| `Center` | `Point3d` | 중심점 |
| `Radius` | `double` | 반지름 |
| `Area` | `double` | 면적 (읽기 전용) |
| `Circumference` | `double` | 원주 (읽기 전용) |
| `Diameter` | `double` | 지름 |

```csharp
// Circle(center, normal, radius)
Circle circle = new Circle(new Point3d(50, 50, 0), Vector3d.ZAxis, 25.0);

// 원형 배열 예시: 8개 볼트 홀
for (int i = 0; i < 8; i++)
{
    double ang = 2 * Math.PI * i / 8;
    Point3d pt = new Point3d(200 + 50 * Math.Cos(ang), 200 + 50 * Math.Sin(ang), 0);
    btr.AppendEntity(new Circle(pt, Vector3d.ZAxis, 5.0));
}
```

---

## 5. Arc

| Property | Type | Description |
|---|---|---|
| `Center` | `Point3d` | 중심점 |
| `Radius` | `double` | 반지름 |
| `StartAngle` / `EndAngle` | `double` | 시작/끝 각도 (라디안) |
| `TotalAngle` | `double` | 전체 각도 (읽기 전용) |
| `Length` | `double` | 호 길이 (읽기 전용) |
| `StartPoint` / `EndPoint` | `Point3d` | 시작/끝점 (읽기 전용) |

```csharp
// Arc(center, radius, startAngle, endAngle) - 라디안
Arc arc = new Arc(new Point3d(0, 0, 0), 25.0, 0, Math.PI / 2);

// 법선 포함 오버로드: Arc(center, normal, radius, start, end)
// 각도 변환: double DegToRad(double deg) => deg * Math.PI / 180.0;
```

---

## 6. DBText / MText

### DBText (단일 행 문자)

| Property | Type | Description |
|---|---|---|
| `Position` | `Point3d` | 삽입점 |
| `TextString` | `string` | 문자열 내용 |
| `Height` | `double` | 문자 높이 |
| `Rotation` | `double` | 회전 (라디안) |
| `WidthFactor` | `double` | 폭 비율 (기본 1.0) |
| `HorizontalMode` | `TextHorizontalMode` | 수평 정렬 |
| `VerticalMode` | `TextVerticalMode` | 수직 정렬 |
| `AlignmentPoint` | `Point3d` | 정렬 기준점 |

```csharp
DBText text = new DBText();
text.Position = new Point3d(10, 10, 0);
text.TextString = "SECTION A-A";
text.Height = 2.5;
text.HorizontalMode = TextHorizontalMode.TextCenter;
text.VerticalMode = TextVerticalMode.TextVerticalMid;
text.AlignmentPoint = new Point3d(10, 10, 0); // 정렬 모드 변경 시 필요
```

### MText (다중 행 문자)

| Property | Type | Description |
|---|---|---|
| `Location` | `Point3d` | 삽입 위치 |
| `Contents` | `string` | 서식 코드 포함 내용 |
| `Text` | `string` | 순수 텍스트 (읽기 전용) |
| `TextHeight` | `double` | 문자 높이 |
| `Width` | `double` | 텍스트 박스 폭 |
| `Attachment` | `AttachmentPoint` | 부착점 (TopLeft, MiddleCenter 등) |

```csharp
MText mtext = new MText();
mtext.Location = new Point3d(10, 80, 0);
mtext.Contents = "첫째 줄\\P둘째 줄";  // \\P = 줄바꿈
mtext.TextHeight = 2.5;
mtext.Width = 100.0;
mtext.Attachment = AttachmentPoint.TopLeft;
```

서식: `\\P` 줄바꿈 | `\\L...\\l` 밑줄 | `{\\H3.5;text}` 높이 | `{\\C1;text}` 색상

---

## 7. BlockReference

| Property | Type | Description |
|---|---|---|
| `Position` | `Point3d` | 삽입점 |
| `Rotation` | `double` | 회전 (라디안) |
| `ScaleFactors` | `Scale3d` | X/Y/Z 스케일 |
| `BlockTableRecord` | `ObjectId` | 블록 정의 ID |
| `AttributeCollection` | `AttributeCollection` | 속성 참조 컬렉션 |
| `Name` | `string` | 블록 이름 (읽기 전용) |
| `IsDynamicBlock` | `bool` | 동적 블록 여부 |

```csharp
// 블록 참조 삽입 및 속성 설정
BlockTable bt = tr.GetObject(db.BlockTableId, OpenMode.ForRead) as BlockTable;
if (!bt.Has("VALVE")) return;

ObjectId blkId = bt["VALVE"];
BlockReference blkRef = new BlockReference(new Point3d(500, 300, 0), blkId);
blkRef.ScaleFactors = new Scale3d(1.0);
blkRef.Layer = "EQUIPMENT";
btr.AppendEntity(blkRef);
tr.AddNewlyCreatedDBObject(blkRef, true);

// 블록 정의에서 AttributeDefinition을 AttributeReference로 복사
BlockTableRecord blkDef = tr.GetObject(blkId, OpenMode.ForRead) as BlockTableRecord;
foreach (ObjectId entId in blkDef)
{
    AttributeDefinition attDef =
        tr.GetObject(entId, OpenMode.ForRead) as AttributeDefinition;
    if (attDef == null) continue;

    AttributeReference attRef = new AttributeReference();
    attRef.SetAttributeFromBlock(attDef, blkRef.BlockTransform);
    if (attDef.Tag == "TAG_NUMBER") attRef.TextString = "V-101";

    blkRef.AttributeCollection.AppendAttribute(attRef);
    tr.AddNewlyCreatedDBObject(attRef, true);
}

// 속성값 읽기: blkRef.AttributeCollection 순회
// attRef.Tag (태그이름), attRef.TextString (값)
```

---

## 8. Hatch

| Member | Description |
|---|---|
| `SetHatchPattern(type, name)` | 패턴 설정 (PreDefined, UserDefined, CustomDefined) |
| `AppendLoop(HatchLoopTypes, ObjectIdCollection)` | 경계 루프 추가 |
| `EvaluateHatch(bool)` | 해치 계산 (반드시 호출) |
| `PatternScale` / `PatternAngle` | 스케일 / 각도 |
| `HatchStyle` | Normal / Outer / Ignore |

```csharp
// 해치는 반드시 DB에 먼저 추가 후 설정
Hatch hatch = new Hatch();
btr.AppendEntity(hatch);
tr.AddNewlyCreatedDBObject(hatch, true);

hatch.SetHatchPattern(HatchPatternType.PreDefined, "ANSI31");
hatch.PatternScale = 1.0;
hatch.HatchStyle = HatchStyle.Normal;

ObjectIdCollection boundaryIds = new ObjectIdCollection();
boundaryIds.Add(closedPolylineId); // 닫힌 폴리라인 ObjectId
hatch.AppendLoop(HatchLoopTypes.Outermost, boundaryIds);
hatch.EvaluateHatch(true);

// 솔리드 채우기: "SOLID" 패턴 사용, ColorIndex로 색상 지정
```

---

## 9. Region

닫힌 커브로 구성된 2D 영역. Boolean 연산 지원.

| Member | Description |
|---|---|
| `Region.CreateFromCurves(DBObjectCollection)` | (static) 커브에서 Region 생성 |
| `BooleanOperation(type, region)` | BoolUnite / BoolIntersect / BoolSubtract |
| `Area` / `Perimeter` | 면적 / 둘레 |

```csharp
// 두 원으로 도넛 형태 Region 생성
DBObjectCollection curves = new DBObjectCollection();
curves.Add(new Circle(Point3d.Origin, Vector3d.ZAxis, 50));
Region outer = (Region)Region.CreateFromCurves(curves)[0];

curves.Clear();
curves.Add(new Circle(Point3d.Origin, Vector3d.ZAxis, 20));
Region inner = (Region)Region.CreateFromCurves(curves)[0];

// 차집합 연산
outer.BooleanOperation(BooleanOperationType.BoolSubtract, inner);
btr.AppendEntity(outer);
tr.AddNewlyCreatedDBObject(outer, true);
```

---

## 10. Common Entity Operations

### 복사

```csharp
Entity copy = ent.Clone() as Entity;
btr.AppendEntity(copy);
tr.AddNewlyCreatedDBObject(copy, true);
```

### 이동 (Displacement)

```csharp
Vector3d disp = new Point3d(0, 0, 0).GetVectorTo(new Point3d(100, 50, 0));
ent.TransformBy(Matrix3d.Displacement(disp));
```

### 회전 (Rotation)

```csharp
// 기준점 중심으로 45도 회전
ent.TransformBy(Matrix3d.Rotation(Math.PI / 4, Vector3d.ZAxis, new Point3d(50, 50, 0)));
```

### 스케일 (Scaling)

```csharp
ent.TransformBy(Matrix3d.Scaling(2.0, new Point3d(0, 0, 0)));
```

### 미러 (Mirroring)

```csharp
// Y축 기준 대칭 복사
Line3d axis = new Line3d(new Point3d(0, 0, 0), new Point3d(0, 100, 0));
Entity mirrorCopy = ent.Clone() as Entity;
mirrorCopy.TransformBy(Matrix3d.Mirroring(axis));
btr.AppendEntity(mirrorCopy);
tr.AddNewlyCreatedDBObject(mirrorCopy, true);
```

### 블록 분해 (Explode)

```csharp
BlockReference blkRef = tr.GetObject(blkRefId, OpenMode.ForWrite) as BlockReference;
DBObjectCollection exploded = new DBObjectCollection();
blkRef.Explode(exploded);
foreach (Entity e in exploded.Cast<Entity>())
{
    btr.AppendEntity(e);
    tr.AddNewlyCreatedDBObject(e, true);
}
blkRef.Erase(); // 원본 삭제 (선택 사항)
```

### 복합 변환

```csharp
// 스케일 + 회전 + 이동을 하나의 매트릭스로 결합
Point3d bp = new Point3d(50, 50, 0);
Matrix3d mat = Matrix3d.Identity
    .PostMultiplyBy(Matrix3d.Scaling(2.0, bp))
    .PostMultiplyBy(Matrix3d.Rotation(Math.PI / 6, Vector3d.ZAxis, bp))
    .PostMultiplyBy(Matrix3d.Displacement(new Vector3d(200, 100, 0)));
ent.TransformBy(mat);
```

---

## Quick Reference - Entity Type 판별

```csharp
foreach (ObjectId id in btr)
{
    Entity ent = tr.GetObject(id, OpenMode.ForRead) as Entity;
    switch (ent)
    {
        case Line line:         /* line.Length */          break;
        case Polyline pline:    /* pline.NumberOfVertices */ break;
        case Circle circle:     /* circle.Radius */       break;
        case Arc arc:           /* arc.StartAngle */      break;
        case DBText text:       /* text.TextString */     break;
        case MText mtext:       /* mtext.Contents */      break;
        case BlockReference br: /* br.Name */             break;
        case Hatch hatch:       /* hatch.PatternName */   break;
    }
}
```

> **참고:** 모든 엔티티 조작은 `Transaction` 내에서 수행. `OpenMode.ForWrite`로 열어야 수정 가능.
> `tr.Commit()` 없이 트랜잭션 종료 시 모든 변경 롤백.
