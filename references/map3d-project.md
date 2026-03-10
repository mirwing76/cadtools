# AutoCAD Map 3D 2020+ .NET API Quick Reference (C#)

> Project/Map 기능 중심의 C# 개발자용 실무 레퍼런스

---

## 1. Required Assemblies & Namespaces

```csharp
// 어셈블리 참조 (Copy Local = false)
// ManagedMapApi.dll          — Map 3D 핵심 (Object Data, Query, Topology, Classification)
// Autodesk.Map.Platform.dll  — Geospatial Platform (FDO, Feature Service)
// acmgd.dll / acdbmgd.dll / accoremgd.dll — AutoCAD 기본

using Autodesk.AutoCAD.Runtime;
using Autodesk.AutoCAD.ApplicationServices;
using Autodesk.AutoCAD.DatabaseServices;
using Autodesk.AutoCAD.EditorInput;
using Autodesk.Gis.Map;                    // MapApplication, ProjectModel
using Autodesk.Gis.Map.ObjectData;         // Tables, Table, Record, FieldDefinitions
using Autodesk.Gis.Map.Query;             // QueryBranch, DataCondition, LocationCondition
using Autodesk.Gis.Map.Topology;          // TopologyModel
using Autodesk.Gis.Map.Classification;    // ObjectClassification
using Autodesk.Gis.Map.Utilities;         // MapUtility
using Autodesk.Gis.Map.Platform;          // AcMapFeatureService, AcMapLayer (FDO)
using OSGeo.MapGuide;                     // MgFeatureService
```

## 2. MapApplication Entry Point

```csharp
// Map 3D 최상위 진입점
MapApplication mapApp = HostMapApplicationServices.Application;
ProjectModel project  = mapApp.ActiveProject;   // 현재 도면 프로젝트
bool isMap            = mapApp.IsMapProduct;     // Map 3D 제품 여부
mapApp.DisplayMessage("상태 메시지");             // 상태 바 출력
```

## 3. ProjectModel

```csharp
ProjectModel project = HostMapApplicationServices.Application.ActiveProject;

Tables odTables       = project.ODTables;       // Object Data 테이블 컬렉션
MapUtility utility    = project.MapUtility;     // 유틸리티 팩토리
DrawingSet drawingSet = project.DrawingSet;     // 첨부 도면 관리
QueryBranch query     = project.CreateQuery();  // 새 쿼리 생성
string path           = project.ProjectPath;    // 프로젝트 경로
```

## 4. Drawing Management

```csharp
DrawingSet dwgSet = HostMapApplicationServices.Application.ActiveProject.DrawingSet;

// 도면 첨부
AttachedDrawing attached = dwgSet.AttachDrawing(@"C:\GIS\parcel.dwg");

// 첨부 도면 순회
for (int i = 0; i < dwgSet.DirectDrawingsCount; i++)
{
    AttachedDrawing dwg = dwgSet[i];
    string name = dwg.ActualName;   // 파일명
}

// 도면 분리 (인덱스 기반)
dwgSet.DetachDrawing(0);

// 전체 분리
while (dwgSet.DirectDrawingsCount > 0)
    dwgSet.DetachDrawing(0);
```

> 소스 도면은 읽기 전용. Query-In으로 엔티티를 활성 도면에 복사.

## 5. Map Queries

쿼리는 트리 구조: `QueryBranch`(분기) + 조건(`PropertyCondition`, `LocationCondition`, `DataCondition`, `SqlCondition`)

```csharp
ProjectModel project = HostMapApplicationServices.Application.ActiveProject;

// Property Query — AutoCAD 속성(레이어, 색상 등) 기반
PropertyCondition propCond = new PropertyCondition(
    PropertyType.Color, ConditionOperator.Equal, "1");

// Location Query — 공간 영역(Window, Crossing, Fence 등) 기반
LocationCondition locCond = new LocationCondition();
locCond.LocationType = LocationType.Window;

// Data Query — OD 테이블 필드 값 기반
DataCondition dataCond = new DataCondition(
    "PARCEL", "AREA", ConditionOperator.GreaterThan, "1000");

// SQL Query — OD 테이블에 SQL 구문 적용
SqlCondition sqlCond = new SqlCondition(
    "SELECT * FROM PARCEL WHERE OWNER LIKE 'Kim%'");

// 복합 쿼리 (AND/OR 결합)
QueryBranch combined = project.CreateQuery();
combined.BranchOperator = QueryOperator.And;
combined.AppendOperand(propCond);
combined.AppendOperand(dataCond);

// 쿼리 실행 (첨부 도면에서 조건 맞는 엔티티를 활성 도면으로)
project.MapUtility.ExecuteQuery(combined);
```

## 6. Object Data Tables (ODTables)

```csharp
ProjectModel project = HostMapApplicationServices.Application.ActiveProject;
Tables odTables = project.ODTables;

// === OD 테이블 생성 ===
FieldDefinitions fieldDefs = project.MapUtility.NewODFieldDefinitions();
fieldDefs.AddColumn(FieldDefinition.Create("OWNER", "소유자", Constants.DataType.Character), 0);
fieldDefs.AddColumn(FieldDefinition.Create("AREA", "면적", Constants.DataType.Real), 1);
fieldDefs.AddColumn(FieldDefinition.Create("ZONE_CODE", "용도코드", Constants.DataType.Integer), 2);
odTables.Add("PARCEL", fieldDefs, "필지 속성", true);

// === 레코드 추가 (엔티티에 OD 연결) ===
using (Table table = odTables["PARCEL"])
{
    using (Record record = Record.Create())
    {
        table.InitRecord(record);            // 스키마 기반 초기화
        record[0].Assign("홍길동");           // OWNER (Character)
        record[1].Assign(1500.5);            // AREA (Double)
        record[2].Assign(101);               // ZONE_CODE (Integer)
        table.AddRecord(record, entityId);   // DBObject Write 모드 필요
    }
}

// === 레코드 읽기 ===
using (Table table = odTables["PARCEL"])
{
    using (Records records = table.GetObjectTableRecords(
        0, entityId, Autodesk.Gis.Map.Constants.OpenMode.OpenForRead))
    {
        foreach (Record rec in records)
        {
            string owner = rec[0].StrValue;
            double area  = rec[1].DoubleValue;
            int code     = rec[2].Int32Value;
        }
    } // Records는 반드시 Dispose 필요
}
```

## 7. Classification / Object Class

```csharp
// Map 3D 분류 — OD 테이블 기반 분류 체계가 실무에서 유연함
// 내장 ObjectClassification (Autodesk.Gis.Map.Classification)도 존재

// 분류용 OD 테이블 생성
FieldDefinitions classDefs = project.MapUtility.NewODFieldDefinitions();
classDefs.AddColumn(FieldDefinition.Create("CLASS_NAME", "분류명", Constants.DataType.Character), 0);
classDefs.AddColumn(FieldDefinition.Create("CLASS_CODE", "분류코드", Constants.DataType.Integer), 1);
odTables.Add("CLASSIFICATION", classDefs, "분류 테이블", true);

// 엔티티에 분류 할당
using (Table ct = odTables["CLASSIFICATION"])
{
    using (Record rec = Record.Create())
    {
        ct.InitRecord(rec);
        rec[0].Assign("도로");
        rec[1].Assign(200);
        ct.AddRecord(rec, entityId);
    }
}

// 분류 기반 쿼리
DataCondition classCond = new DataCondition(
    "CLASSIFICATION", "CLASS_CODE", ConditionOperator.Equal, "200");
```

## 8. Topology

```csharp
// Autodesk.Gis.Map.Topology — 엔티티 간 공간 관계 정의
// Network Topology : Link + Node (도로, 배관, 전선)
// Polygon Topology : Link + Node + Centroid (필지, 행정구역)
// Node Topology    : Node (교차점)

// 생성: 주로 MAPTOPOCREATE 명령 사용, .NET에서는 조회/분석 중심
// TopologyClean: 중복선, 미처리 교차, 갭 자동 보정
// 주의: SHP/SDF는 토폴로지 미지원 (Oracle, SQL Server는 지원)
```

## 9. FDO / Feature Data Objects

```csharp
// FDO — 외부 GIS 소스(SHP, SDF, Oracle Spatial, PostGIS) 통합 접근
AcMapFeatureService featureService =
    AcMapServiceFactory.GetService(MgServiceType.FeatureService)
    as AcMapFeatureService;

// FDO Provider: OSGeo.SHP, OSGeo.SDF, OSGeo.SQLServerSpatial 등
// AcMapLayer: FDO 기반 피처 레이어 (Display Manager에서 관리)
// FeatureService 이벤트: Insert, Update, Delete 구독 가능

// === FDO vs Object Data 선택 기준 ===
// Object Data: DWG 내부 저장, 단일 도면 작업, API 단순
// FDO:         외부 데이터소스, 다중 소스 통합, DB 확장성, API 복잡
```

## 10. MapUtility

```csharp
MapUtility utility = HostMapApplicationServices.Application.ActiveProject.MapUtility;

FieldDefinitions fieldDefs = utility.NewODFieldDefinitions(); // OD 필드 정의 생성
Record record = Record.Create();                               // 레코드 생성 (정적)
utility.ExecuteQuery(queryBranch);                             // 쿼리 실행 (Query-In)
// MapValue: Record[index].Assign() 내부의 타입 안전 값 컨테이너
```

## 11. Common Patterns

```csharp
// === Map 3D 환경 확인 ===
public static bool IsMap3DAvailable()
{
    try
    {
        var app = HostMapApplicationServices.Application;
        return app != null && app.IsMapProduct;
    }
    catch { return false; } // ManagedMapApi.dll 미로드 시
}

// === Safe Project 접근 ===
public static ProjectModel GetActiveProject()
{
    if (!IsMap3DAvailable()) return null;
    return HostMapApplicationServices.Application.ActiveProject
        ?? throw new InvalidOperationException("활성 프로젝트 없음");
}

// === OD 테이블 존재 확인 후 생성 ===
public static void EnsureODTable(string tableName, FieldDefinitions defs, string desc)
{
    Tables odTables = HostMapApplicationServices.Application.ActiveProject.ODTables;
    foreach (string name in odTables.GetTableNames())
        if (name.Equals(tableName, StringComparison.OrdinalIgnoreCase)) return;
    odTables.Add(tableName, defs, desc, true);
}

// === 엔티티 OD 읽기 유틸리티 ===
public static Dictionary<string, object> ReadOD(ObjectId eid, string tableName)
{
    var result = new Dictionary<string, object>();
    Tables odTables = HostMapApplicationServices.Application.ActiveProject.ODTables;
    using (Table table = odTables[tableName])
    using (Records records = table.GetObjectTableRecords(
        0, eid, Autodesk.Gis.Map.Constants.OpenMode.OpenForRead))
    {
        if (records.Count == 0) return result;
        records.MoveFirst();
        Record rec = records.CurrentRecord;
        for (int i = 0; i < table.FieldDefinitions.Count; i++)
        {
            var fd = table.FieldDefinitions[i];
            result[fd.Name] = fd.Type switch
            {
                Constants.DataType.Character => rec[i].StrValue,
                Constants.DataType.Integer   => rec[i].Int32Value,
                Constants.DataType.Real    => rec[i].DoubleValue,
                Constants.DataType.Real      => rec[i].DoubleValue,
                _                   => rec[i].StrValue
            };
        }
    }
    return result;
}

// === OD 기반 쿼리 실행 커맨드 ===
[CommandMethod("MAP_QUERY_OD")]
public void QueryByObjectData()
{
    var project = HostMapApplicationServices.Application.ActiveProject;
    var ed = Application.DocumentManager.MdiActiveDocument.Editor;
    var cond = new DataCondition("PARCEL", "AREA", ConditionOperator.GreaterThan, "500");
    QueryBranch query = project.CreateQuery();
    query.AppendOperand(cond);
    try
    {
        project.MapUtility.ExecuteQuery(query);
        ed.WriteMessage("\n쿼리 완료.");
    }
    catch (MapException ex) { ed.WriteMessage($"\n오류: {ex.Message}"); }
}

// === 첨부 도면 목록 출력 ===
[CommandMethod("MAP_LIST_DWG")]
public void ListAttachedDrawings()
{
    var dwgSet = HostMapApplicationServices.Application.ActiveProject.DrawingSet;
    var ed = Application.DocumentManager.MdiActiveDocument.Editor;
    for (int i = 0; i < dwgSet.DirectDrawingsCount; i++)
        ed.WriteMessage($"\n[{i}] {dwgSet[i].ActualName}");
}
```

---

**참고 리소스**:
[Map 3D .NET API Docs](https://documentation.help/AutoCAD-Map-3D-2008-.NET-API/WS73099cc142f487551d92abb10dc573c45d-7f95.htm) |
[ObjectARX SDK](https://aps.autodesk.com/developer/overview/autocad-map-3d-objectarx-sdk) |
[DevBlog](https://adndevblog.typepad.com/infrastructure/) |
[OD Records Example](https://adndevblog.typepad.com/infrastructure/2012/05/adding-object-data-records-to-entity-using-map-3d-api.html) |
[Attach/Detach DWG](https://adndevblog.typepad.com/infrastructure/2012/03/programmatically-attach-and-detach-dwg-files-in-autocad-map-3d.html)
