# AutoCAD 2020+ Geometry Types - C# API Reference

> **Namespace:** `Autodesk.AutoCAD.Geometry`
> **Assembly:** `acdbmgd.dll` / `accoremgd.dll`

---

## 1. Point2d

```csharp
// 생성자
Point2d pt = new Point2d(10.0, 20.0);
// 속성
double x = pt.X;  double y = pt.Y;
// 두 점 사이 거리
double dist = pt1.GetDistanceTo(pt2);
// pt1에서 pt2로 향하는 벡터
Vector2d vec = pt1.GetVectorTo(pt2);
// 변환 행렬 적용
Point2d transformed = pt.TransformBy(Matrix2d.Identity);
// 연산자
Point2d sum = pt + new Vector2d(5.0, 5.0);   // 점 + 벡터 = 점
Vector2d diff = pt1 - pt2;                     // 점 - 점 = 벡터
bool equal = (pt1 == pt2);
bool notEqual = (pt1 != pt2);
```

---

## 2. Point3d

```csharp
// 생성자 및 원점 상수
Point3d pt = new Point3d(10.0, 20.0, 30.0);
Point3d origin = Point3d.Origin; // (0, 0, 0)
// 속성
double x = pt.X;  double y = pt.Y;  double z = pt.Z;
// 두 점 사이 거리 / 벡터
double dist = pt1.DistanceTo(pt2);
Vector3d vec = pt1.GetVectorTo(pt2);
// 변환 행렬 적용 (이동, 회전, 스케일 등)
Point3d moved = pt.TransformBy(Matrix3d.Displacement(new Vector3d(5, 0, 0)));
// 연산자
Point3d sum = pt + new Vector3d(1.0, 2.0, 3.0);  // 점 + 벡터 = 점
Vector3d diff = pt1 - pt2;                          // 점 - 점 = 벡터
bool equal = (pt1 == pt2);
bool notEqual = (pt1 != pt2);
```

### Point3d <-> Point2d 변환

```csharp
// Point3d -> Point2d (Z값 버림)
Point2d pt2 = new Point2d(pt3.X, pt3.Y);
// Point2d -> Point3d (Z=0으로 설정)
Point3d pt3 = new Point3d(pt2.X, pt2.Y, 0.0);

// Polyline은 Point2d, Entity 위치는 Point3d
Polyline pline = new Polyline();
pline.AddVertexAt(0, new Point2d(0, 0), 0, 0, 0);
pline.AddVertexAt(1, new Point2d(100, 0), 0, 0, 0);
Line line = new Line(new Point3d(0, 0, 0), new Point3d(100, 0, 0));
```

---

## 3. Vector2d / Vector3d

```csharp
// Vector2d
Vector2d v2 = new Vector2d(3.0, 4.0);
double len2 = v2.Length;             // 5.0
Vector2d unit2 = v2.GetNormal();     // 단위벡터 (0.6, 0.8)
double angle2 = v2a.GetAngleTo(v2b); // 두 벡터 사이 각도 (라디안)
double dot2 = v2a.DotProduct(v2b);   // 내적

// Vector3d 생성자 및 속성
Vector3d v = new Vector3d(1.0, 2.0, 3.0);
double x = v.X;  double y = v.Y;  double z = v.Z;
double len = v.Length;

// 주요 메서드
Vector3d unit = v.GetNormal();                       // 단위벡터 (길이=1)
double angle = v1.GetAngleTo(v2);                    // 0 ~ PI 범위
double angleSigned = v1.GetAngleTo(v2, Vector3d.ZAxis); // 방향 구분 가능
Vector3d cross = v1.CrossProduct(v2);                // 외적
double dot = v1.DotProduct(v2);                      // 내적

// 축 벡터 상수
Vector3d.XAxis  // (1, 0, 0)
Vector3d.YAxis  // (0, 1, 0)
Vector3d.ZAxis  // (0, 0, 1)

// 연산자
Vector3d sum  = v1 + v2;     // 벡터 덧셈
Vector3d diff = v1 - v2;     // 벡터 뺄셈
Vector3d neg  = -v1;         // 반대 방향
Vector3d scaled = v * 2.5;   // 스칼라 곱
Vector3d half = v / 2.0;     // 스칼라 나눗셈
```

---

## 4. Matrix3d

### 기본 변환 행렬

```csharp
// 항등 행렬 (변환 없음)
Matrix3d identity = Matrix3d.Identity;

// 이동 행렬
Matrix3d displacement = Matrix3d.Displacement(new Vector3d(100.0, 50.0, 0.0));

// 회전 행렬 (angle: 라디안, axis: 회전축, center: 회전 중심점)
Matrix3d rotation = Matrix3d.Rotation(Math.PI / 4, Vector3d.ZAxis, Point3d.Origin);

// 스케일 행렬 (factor: 배율, center: 기준점)
Matrix3d scaling = Matrix3d.Scaling(2.0, Point3d.Origin);

// 미러 행렬 -- 평면, 점, 선 기준 모두 가능
Plane mirrorPlane = new Plane(Point3d.Origin, Vector3d.XAxis);
Matrix3d mirrorByPlane = Matrix3d.Mirroring(mirrorPlane);
Matrix3d mirrorByPoint = Matrix3d.Mirroring(new Point3d(50, 50, 0));
Line3d mirrorLine = new Line3d(Point3d.Origin, new Point3d(100, 100, 0));
Matrix3d mirrorByLine = Matrix3d.Mirroring(mirrorLine);
```

### 행렬 곱 (변환 합성) 및 Entity 적용

```csharp
// 이동 후 회전 (오른쪽부터 적용됨에 주의)
Matrix3d combined = rotation * displacement;

// Entity에 변환 적용
using (Transaction tr = db.TransactionManager.StartTransaction())
{
    Entity ent = tr.GetObject(entId, OpenMode.ForWrite) as Entity;
    ent.TransformBy(Matrix3d.Displacement(new Vector3d(100, 50, 0)));
    tr.Commit();
}

// 실전: 특정 점 기준으로 45도 회전
Point3d basePoint = new Point3d(50, 50, 0);
double rotAngle = 45.0 * Math.PI / 180.0;
entity.TransformBy(Matrix3d.Rotation(rotAngle, Vector3d.ZAxis, basePoint));
```

---

## 5. Plane

```csharp
// 생성자: 원점 + 법선벡터
Plane plane = new Plane(Point3d.Origin, Vector3d.ZAxis);
// 세 점으로 평면 정의
Plane plane3pt = new Plane(pt1, pt2, pt3);
// 원점 + 두 축 벡터로 정의
Plane planeAxes = new Plane(Point3d.Origin, Vector3d.XAxis, Vector3d.YAxis);

// 자주 쓰는 평면
Plane xyPlane = new Plane(Point3d.Origin, Vector3d.ZAxis); // XY 평면 (Z=0)
Plane xzPlane = new Plane(Point3d.Origin, Vector3d.YAxis); // XZ 평면 (Y=0)
Plane yzPlane = new Plane(Point3d.Origin, Vector3d.XAxis); // YZ 평면 (X=0)

// Circle 생성 시 법선벡터로 평면 지정
Circle circle = new Circle(new Point3d(0, 0, 0), Vector3d.ZAxis, 50.0);
```

---

## 6. Extents3d

```csharp
// Entity에서 BoundingBox 가져오기
Entity ent = tr.GetObject(entId, OpenMode.ForRead) as Entity;
Extents3d ext = ent.GeometricExtents;
Point3d minPt = ext.MinPoint;
Point3d maxPt = ext.MaxPoint;
double width  = maxPt.X - minPt.X;
double height = maxPt.Y - minPt.Y;
```

### 여러 Entity의 통합 BoundingBox

```csharp
Extents3d totalExt = new Extents3d();
totalExt.AddPoint(new Point3d(0, 0, 0));      // 점 추가로 범위 확장
totalExt.AddPoint(new Point3d(100, 200, 0));

// 여러 Entity 합산
foreach (ObjectId id in selectedIds)
{
    Entity ent = tr.GetObject(id, OpenMode.ForRead) as Entity;
    if (totalExt.MinPoint == totalExt.MaxPoint)
        totalExt = ent.GeometricExtents;       // 첫 번째는 직접 할당
    else
        totalExt.AddExtents(ent.GeometricExtents);
}
// 중심점 계산
Point3d center = new Point3d(
    (totalExt.MinPoint.X + totalExt.MaxPoint.X) / 2.0,
    (totalExt.MinPoint.Y + totalExt.MaxPoint.Y) / 2.0,
    (totalExt.MinPoint.Z + totalExt.MaxPoint.Z) / 2.0);
```

---

## 7. Tolerance

```csharp
// 전역 Tolerance (AutoCAD 기본 허용 오차)
Tolerance tol = Tolerance.Global;

// 사용자 정의 Tolerance (equalPoint, equalVector)
Tolerance customTol = new Tolerance(1e-6, 1e-6);

// Tolerance를 사용한 비교
bool ptSame  = pt1.IsEqualTo(pt2, customTol);  // 점 비교
bool vecSame = v1.IsEqualTo(v2, customTol);     // 벡터 비교
```

---

## 8. Angle/Radian Helpers

```csharp
// 도 -> 라디안 변환
double DegToRad(double deg) => deg * Math.PI / 180.0;
// 라디안 -> 도 변환
double RadToDeg(double rad) => rad * 180.0 / Math.PI;

// 자주 쓰는 각도 상수
double deg45  = Math.PI / 4.0;   // 0.7854
double deg90  = Math.PI / 2.0;   // 1.5708
double deg180 = Math.PI;         // 3.1416
double deg270 = Math.PI * 1.5;   // 4.7124
double deg360 = Math.PI * 2.0;   // 6.2832

// 두 점 사이의 X축 기준 각도 (라디안)
Vector3d dir = pt1.GetVectorTo(pt2);
double angle = Math.Atan2(dir.Y, dir.X);
if (angle < 0) angle += Math.PI * 2.0; // 0~2PI 정규화
```

---

## 9. Common Geometric Operations

### 거리 / 중점 / Offset

```csharp
// 두 점 사이 거리 (Point3d는 DistanceTo, Point2d는 GetDistanceTo)
double distance = pt1.DistanceTo(pt2);

// 두 점의 중점
Point3d MidPoint(Point3d p1, Point3d p2) => new Point3d(
    (p1.X + p2.X) / 2.0, (p1.Y + p2.Y) / 2.0, (p1.Z + p2.Z) / 2.0);
// 벡터 연산 활용 버전
Point3d mid = p1 + (p1.GetVectorTo(p2) * 0.5);

// 선분의 수직 Offset 방향
Vector3d lineDir = pt1.GetVectorTo(pt2).GetNormal();
Vector3d offsetLeft = lineDir.CrossProduct(Vector3d.ZAxis).GetNormal();
Vector3d offsetRight = -offsetLeft;
Point3d offsetPt = pt1 + offsetLeft * 10.0; // 10만큼 왼쪽으로 Offset
```

### 회전 각도 계산

```csharp
// 기준점에서 대상점까지의 각도 (라디안)
double GetAngle(Point3d basePt, Point3d targetPt)
{
    Vector3d v = basePt.GetVectorTo(targetPt);
    return Math.Atan2(v.Y, v.X);
}

// 두 선분 사이 끼인각
Vector3d v1 = ptA.GetVectorTo(ptB).GetNormal();
Vector3d v2 = ptA.GetVectorTo(ptC).GetNormal();
double included = v1.GetAngleTo(v2); // 항상 0 ~ PI 범위
```

### Selection의 BoundingBox

```csharp
Extents3d GetSelectionExtents(SelectionSet ss, Transaction tr)
{
    Extents3d totalExt = new Extents3d();
    bool first = true;
    foreach (SelectedObject so in ss)
    {
        Entity ent = tr.GetObject(so.ObjectId, OpenMode.ForRead) as Entity;
        if (ent == null) continue;
        if (first) { totalExt = ent.GeometricExtents; first = false; }
        else       { totalExt.AddExtents(ent.GeometricExtents); }
    }
    return totalExt;
}
```

### 유틸리티: 범위 판정 / 좌표 반올림

```csharp
// 점이 Extents 범위 안에 있는지 확인
bool IsInsideExtents(Point3d pt, Extents3d ext) =>
    pt.X >= ext.MinPoint.X && pt.X <= ext.MaxPoint.X &&
    pt.Y >= ext.MinPoint.Y && pt.Y <= ext.MaxPoint.Y &&
    pt.Z >= ext.MinPoint.Z && pt.Z <= ext.MaxPoint.Z;

// 좌표를 소수점 n자리로 반올림 (비교 시 유용)
Point3d RoundPoint(Point3d pt, int decimals) => new Point3d(
    Math.Round(pt.X, decimals),
    Math.Round(pt.Y, decimals),
    Math.Round(pt.Z, decimals));
```

---

> **참고:** 모든 각도 파라미터는 라디안 단위이다. `Math.PI / 180.0`을 곱하여 도(degree)를 변환할 것.
> **필수 using:** `using Autodesk.AutoCAD.Geometry;`
