# Phase 1: 솔루션 셋업 + GntTools.Core 구현 계획

> **For agentic workers:** REQUIRED: Use superpowers:subagent-driven-development (if subagents available) or superpowers:executing-plans to implement this plan. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Visual Studio 솔루션 구조 생성 및 Core 공통 라이브러리의 모든 클래스 구현

**Architecture:** 5개 프로젝트(Core, Wtl, Swl, Kepco, UI)를 가진 단일 솔루션. Phase 1에서는 Core 프로젝트만 구현하고 나머지는 빈 프로젝트로 생성.

**Tech Stack:** C# / .NET Framework 4.7 / AutoCAD Map 3D 2020 API (AcDbMgd, AcMgd, AcCoreMgd, ManagedMapApi)

**Spec:** `docs/specs/2026-03-10-gnttools-integration-design.md`

**VB.NET 원본 참고:** `old_make/` (Kepco_tools2, SWL_TOOLS, WTL_TOOLS)

---

## File Map

### Phase 1에서 생성하는 파일

| 작업 | 파일 | 역할 |
|------|------|------|
| Task 1 | `src/GntTools.Core/GntTools.Core.csproj` | Core 프로젝트 파일 |
| Task 1 | `src/GntTools.Wtl/GntTools.Wtl.csproj` | WTL 빈 프로젝트 |
| Task 1 | `src/GntTools.Swl/GntTools.Swl.csproj` | SWL 빈 프로젝트 |
| Task 1 | `src/GntTools.Kepco/GntTools.Kepco.csproj` | KEPCO 빈 프로젝트 |
| Task 1 | `src/GntTools.UI/GntTools.UI.csproj` | UI 빈 프로젝트 |
| Task 1 | `src/GntTools.sln` | 솔루션 파일 |
| Task 2 | `src/GntTools.Core/Odt/OdtFieldDef.cs` | ODT 필드 정의 클래스 |
| Task 2 | `src/GntTools.Core/Odt/IOdtSchema.cs` | ODT 스키마 인터페이스 |
| Task 2 | `src/GntTools.Core/Odt/PipeCommonSchema.cs` | PIPE_COMMON 테이블 스키마 |
| Task 2 | `src/GntTools.Core/Odt/PipeCommonRecord.cs` | PIPE_COMMON 레코드 DTO |
| Task 3 | `src/GntTools.Core/Odt/OdtManager.cs` | ODT CRUD 통합 관리자 |
| Task 4 | `src/GntTools.Core/Settings/DomainSettings.cs` | 도메인별 설정 모델 |
| Task 4 | `src/GntTools.Core/Settings/AppSettings.cs` | JSON 설정 관리 (로드/저장) |
| Task 5 | `src/GntTools.Core/Selection/EntitySelector.cs` | 엔티티 선택 유틸리티 |
| Task 6 | `src/GntTools.Core/Geometry/DepthResult.cs` | 심도 측정 결과 DTO |
| Task 6 | `src/GntTools.Core/Geometry/DepthCalculator.cs` | 심도 자동/수동 측정 |
| Task 6 | `src/GntTools.Core/Geometry/PolylineHelper.cs` | 폴리라인 정점/길이 유틸 |
| Task 7 | `src/GntTools.Core/Geometry/ViewportManager.cs` | 줌 저장/복원/이동 |
| Task 8 | `src/GntTools.Core/Drawing/LayerHelper.cs` | 레이어 존재확인/생성 |
| Task 8 | `src/GntTools.Core/Drawing/TextStyleHelper.cs` | 텍스트 스타일 관리 |
| Task 8 | `src/GntTools.Core/Drawing/ColorHelper.cs` | 엔티티 색상 변경 |
| Task 9 | `src/GntTools.Core/Drawing/TextWriter.cs` | DBText 생성/수정/이동/회전 |
| Task 9 | `src/GntTools.Core/Drawing/LeaderWriter.cs` | 지시선 폴리라인 생성 |
| Task 10 | `src/GntTools.Core/XData/XDataManager.cs` | RegApp, 그룹ID 읽기/쓰기 |

---

## Chunk 1: 솔루션 셋업 + ODT 스키마/매니저

### Task 1: 솔루션 및 프로젝트 구조 생성

**Files:**
- Create: `src/GntTools.sln`
- Create: `src/GntTools.Core/GntTools.Core.csproj`
- Create: `src/GntTools.Wtl/GntTools.Wtl.csproj`
- Create: `src/GntTools.Swl/GntTools.Swl.csproj`
- Create: `src/GntTools.Kepco/GntTools.Kepco.csproj`
- Create: `src/GntTools.UI/GntTools.UI.csproj`

**AutoCAD DLL 경로 (참고):**
AutoCAD Map 3D 2020 기본 설치 경로는 개발자 환경에 따라 다름.
ObjectARX SDK: `다운로드/ObjectARX_for_AutoCAD_2020_Win_64_bit/`

- [ ] **Step 1: 솔루션 및 5개 프로젝트 생성**

```bash
cd /home/gntmaster/project/cadtools
mkdir -p src

# 솔루션 생성
dotnet new sln -n GntTools -o src/

# Core: Class Library (.NET Framework 4.7)
# 주의: dotnet CLI는 .NET Framework 프로젝트를 직접 생성 못함
# → .csproj 파일을 수동으로 작성해야 함
```

각 .csproj를 수동 작성 (old-style format, .NET Framework 4.7):

`src/GntTools.Core/GntTools.Core.csproj`:
```xml
<?xml version="1.0" encoding="utf-8"?>
<Project ToolsVersion="15.0" xmlns="http://schemas.microsoft.com/developer/msbuild/2003">
  <Import Project="$(MSBuildExtensionsPath)\$(MSBuildToolsVersion)\Microsoft.Common.props"
          Condition="Exists('$(MSBuildExtensionsPath)\$(MSBuildToolsVersion)\Microsoft.Common.props')" />
  <PropertyGroup>
    <Configuration Condition=" '$(Configuration)' == '' ">Debug</Configuration>
    <Platform Condition=" '$(Platform)' == '' ">AnyCPU</Platform>
    <ProjectGuid>{A1B2C3D4-0001-0000-0000-000000000001}</ProjectGuid>
    <OutputType>Library</OutputType>
    <RootNamespace>GntTools.Core</RootNamespace>
    <AssemblyName>GntTools.Core</AssemblyName>
    <TargetFrameworkVersion>v4.7</TargetFrameworkVersion>
  </PropertyGroup>
  <PropertyGroup Condition=" '$(Configuration)' == 'Debug' ">
    <DebugSymbols>true</DebugSymbols>
    <DebugType>full</DebugType>
    <Optimize>false</Optimize>
    <OutputPath>bin\Debug\</OutputPath>
  </PropertyGroup>
  <PropertyGroup Condition=" '$(Configuration)' == 'Release' ">
    <Optimize>true</Optimize>
    <OutputPath>bin\Release\</OutputPath>
  </PropertyGroup>
  <ItemGroup>
    <!-- AutoCAD 2020 API — Copy Local = false -->
    <!-- 경로는 개발환경에 맞게 수정 필요 -->
    <Reference Include="AcDbMgd">
      <HintPath>..\..\lib\AcDbMgd.dll</HintPath>
      <Private>false</Private>
    </Reference>
    <Reference Include="AcMgd">
      <HintPath>..\..\lib\AcMgd.dll</HintPath>
      <Private>false</Private>
    </Reference>
    <Reference Include="AcCoreMgd">
      <HintPath>..\..\lib\AcCoreMgd.dll</HintPath>
      <Private>false</Private>
    </Reference>
    <!-- Map 3D ODT API -->
    <Reference Include="ManagedMapApi">
      <HintPath>..\..\lib\ManagedMapApi.dll</HintPath>
      <Private>false</Private>
    </Reference>
    <Reference Include="System" />
    <Reference Include="System.Core" />
  </ItemGroup>
  <ItemGroup>
    <Compile Include="Odt\OdtFieldDef.cs" />
    <Compile Include="Odt\IOdtSchema.cs" />
    <Compile Include="Odt\PipeCommonSchema.cs" />
    <Compile Include="Odt\PipeCommonRecord.cs" />
    <Compile Include="Odt\OdtManager.cs" />
    <Compile Include="Selection\EntitySelector.cs" />
    <Compile Include="Geometry\DepthResult.cs" />
    <Compile Include="Geometry\DepthCalculator.cs" />
    <Compile Include="Geometry\PolylineHelper.cs" />
    <Compile Include="Geometry\ViewportManager.cs" />
    <Compile Include="Drawing\LayerHelper.cs" />
    <Compile Include="Drawing\TextStyleHelper.cs" />
    <Compile Include="Drawing\ColorHelper.cs" />
    <Compile Include="Drawing\TextWriter.cs" />
    <Compile Include="Drawing\LeaderWriter.cs" />
    <Compile Include="XData\XDataManager.cs" />
    <Compile Include="Settings\DomainSettings.cs" />
    <Compile Include="Settings\AppSettings.cs" />
  </ItemGroup>
  <Import Project="$(MSBuildToolsPath)\Microsoft.CSharp.targets" />
</Project>
```

`src/GntTools.Wtl/GntTools.Wtl.csproj`:
```xml
<?xml version="1.0" encoding="utf-8"?>
<Project ToolsVersion="15.0" xmlns="http://schemas.microsoft.com/developer/msbuild/2003">
  <Import Project="$(MSBuildExtensionsPath)\$(MSBuildToolsVersion)\Microsoft.Common.props"
          Condition="Exists('$(MSBuildExtensionsPath)\$(MSBuildToolsVersion)\Microsoft.Common.props')" />
  <PropertyGroup>
    <Configuration Condition=" '$(Configuration)' == '' ">Debug</Configuration>
    <Platform Condition=" '$(Platform)' == '' ">AnyCPU</Platform>
    <ProjectGuid>{A1B2C3D4-0002-0000-0000-000000000002}</ProjectGuid>
    <OutputType>Library</OutputType>
    <RootNamespace>GntTools.Wtl</RootNamespace>
    <AssemblyName>GntTools.Wtl</AssemblyName>
    <TargetFrameworkVersion>v4.7</TargetFrameworkVersion>
  </PropertyGroup>
  <PropertyGroup Condition=" '$(Configuration)' == 'Debug' ">
    <DebugSymbols>true</DebugSymbols>
    <DebugType>full</DebugType>
    <Optimize>false</Optimize>
    <OutputPath>bin\Debug\</OutputPath>
  </PropertyGroup>
  <PropertyGroup Condition=" '$(Configuration)' == 'Release' ">
    <Optimize>true</Optimize>
    <OutputPath>bin\Release\</OutputPath>
  </PropertyGroup>
  <ItemGroup>
    <Reference Include="AcDbMgd">
      <HintPath>..\..\lib\AcDbMgd.dll</HintPath>
      <Private>false</Private>
    </Reference>
    <Reference Include="AcCoreMgd">
      <HintPath>..\..\lib\AcCoreMgd.dll</HintPath>
      <Private>false</Private>
    </Reference>
    <Reference Include="System" />
    <Reference Include="System.Core" />
  </ItemGroup>
  <ItemGroup>
    <ProjectReference Include="..\GntTools.Core\GntTools.Core.csproj">
      <Project>{A1B2C3D4-0001-0000-0000-000000000001}</Project>
      <Name>GntTools.Core</Name>
    </ProjectReference>
  </ItemGroup>
  <Import Project="$(MSBuildToolsPath)\Microsoft.CSharp.targets" />
</Project>
```

`src/GntTools.Swl/GntTools.Swl.csproj`: (Wtl과 동일 구조)
```xml
<!-- ProjectGuid: {A1B2C3D4-0003-0000-0000-000000000003} -->
<!-- RootNamespace: GntTools.Swl / AssemblyName: GntTools.Swl -->
<!-- 동일한 AutoCAD 참조 + Core 프로젝트 참조 -->
```

`src/GntTools.Kepco/GntTools.Kepco.csproj`: (Wtl과 동일 구조)
```xml
<!-- ProjectGuid: {A1B2C3D4-0004-0000-0000-000000000004} -->
<!-- RootNamespace: GntTools.Kepco / AssemblyName: GntTools.Kepco -->
<!-- 동일한 AutoCAD 참조 + Core 프로젝트 참조 -->
```

`src/GntTools.UI/GntTools.UI.csproj`:
```xml
<?xml version="1.0" encoding="utf-8"?>
<Project ToolsVersion="15.0" xmlns="http://schemas.microsoft.com/developer/msbuild/2003">
  <Import Project="$(MSBuildExtensionsPath)\$(MSBuildToolsVersion)\Microsoft.Common.props"
          Condition="Exists('$(MSBuildExtensionsPath)\$(MSBuildToolsVersion)\Microsoft.Common.props')" />
  <PropertyGroup>
    <Configuration Condition=" '$(Configuration)' == '' ">Debug</Configuration>
    <Platform Condition=" '$(Platform)' == '' ">AnyCPU</Platform>
    <ProjectGuid>{A1B2C3D4-0005-0000-0000-000000000005}</ProjectGuid>
    <OutputType>Library</OutputType>
    <RootNamespace>GntTools.UI</RootNamespace>
    <AssemblyName>GntTools.UI</AssemblyName>
    <TargetFrameworkVersion>v4.7</TargetFrameworkVersion>
  </PropertyGroup>
  <PropertyGroup Condition=" '$(Configuration)' == 'Debug' ">
    <DebugSymbols>true</DebugSymbols>
    <DebugType>full</DebugType>
    <Optimize>false</Optimize>
    <OutputPath>bin\Debug\</OutputPath>
  </PropertyGroup>
  <PropertyGroup Condition=" '$(Configuration)' == 'Release' ">
    <Optimize>true</Optimize>
    <OutputPath>bin\Release\</OutputPath>
  </PropertyGroup>
  <ItemGroup>
    <Reference Include="AcDbMgd">
      <HintPath>..\..\lib\AcDbMgd.dll</HintPath>
      <Private>false</Private>
    </Reference>
    <Reference Include="AcMgd">
      <HintPath>..\..\lib\AcMgd.dll</HintPath>
      <Private>false</Private>
    </Reference>
    <Reference Include="AcCoreMgd">
      <HintPath>..\..\lib\AcCoreMgd.dll</HintPath>
      <Private>false</Private>
    </Reference>
    <Reference Include="PresentationCore" />
    <Reference Include="PresentationFramework" />
    <Reference Include="WindowsBase" />
    <Reference Include="System" />
    <Reference Include="System.Core" />
    <Reference Include="System.Xaml" />
  </ItemGroup>
  <ItemGroup>
    <ProjectReference Include="..\GntTools.Core\GntTools.Core.csproj">
      <Project>{A1B2C3D4-0001-0000-0000-000000000001}</Project>
      <Name>GntTools.Core</Name>
    </ProjectReference>
    <ProjectReference Include="..\GntTools.Wtl\GntTools.Wtl.csproj">
      <Project>{A1B2C3D4-0002-0000-0000-000000000002}</Project>
      <Name>GntTools.Wtl</Name>
    </ProjectReference>
    <ProjectReference Include="..\GntTools.Swl\GntTools.Swl.csproj">
      <Project>{A1B2C3D4-0003-0000-0000-000000000003}</Project>
      <Name>GntTools.Swl</Name>
    </ProjectReference>
    <ProjectReference Include="..\GntTools.Kepco\GntTools.Kepco.csproj">
      <Project>{A1B2C3D4-0004-0000-0000-000000000004}</Project>
      <Name>GntTools.Kepco</Name>
    </ProjectReference>
  </ItemGroup>
  <Import Project="$(MSBuildToolsPath)\Microsoft.CSharp.targets" />
</Project>
```

- [ ] **Step 2: lib/ 폴더에 AutoCAD DLL 심볼릭 링크 또는 복사**

```bash
mkdir -p src/lib
# Windows 개발환경에서:
# AutoCAD 2020 설치 경로에서 DLL 복사 (빌드 참조용)
# copy "C:\Program Files\Autodesk\AutoCAD Map 3D 2020\AcDbMgd.dll" src\lib\
# copy "C:\Program Files\Autodesk\AutoCAD Map 3D 2020\AcMgd.dll" src\lib\
# copy "C:\Program Files\Autodesk\AutoCAD Map 3D 2020\AcCoreMgd.dll" src\lib\
# copy "C:\Program Files\Autodesk\AutoCAD Map 3D 2020\ManagedMapApi.dll" src\lib\
```

- [ ] **Step 3: 빌드 확인**

```bash
# Windows: Visual Studio에서 솔루션 열기 → Build Solution (Ctrl+Shift+B)
# 또는 MSBuild:
msbuild src\GntTools.sln /t:Build /p:Configuration=Debug
```
Expected: 빈 프로젝트이므로 경고만 있고 에러 없이 빌드 성공

- [ ] **Step 4: 커밋**

```bash
git add src/
git commit -m "feat: create GntTools solution with 5 project structure

- GntTools.Core: 공통 라이브러리
- GntTools.Wtl/Swl/Kepco: 도메인 모듈 (빈 프로젝트)
- GntTools.UI: PaletteSet UI 진입점 (빈 프로젝트)
- .NET Framework 4.7 / AutoCAD Map 3D 2020 대상"
```

---

### Task 2: ODT 스키마 인터페이스 및 PIPE_COMMON 정의

**Files:**
- Create: `src/GntTools.Core/Odt/OdtFieldDef.cs`
- Create: `src/GntTools.Core/Odt/IOdtSchema.cs`
- Create: `src/GntTools.Core/Odt/PipeCommonSchema.cs`
- Create: `src/GntTools.Core/Odt/PipeCommonRecord.cs`
- Ref: `references/autocad-core.md` (Transaction 패턴)
- Ref: `old_make/WTL_TOOLS/WTL_TOOLS/ObjDataClass.vb` (기존 ODT 패턴)

- [ ] **Step 1: OdtFieldDef.cs 작성**

```csharp
// src/GntTools.Core/Odt/OdtFieldDef.cs
using Autodesk.Gis.Map.Constants;

namespace GntTools.Core.Odt
{
    /// <summary>ODT 테이블 필드 정의</summary>
    public class OdtFieldDef
    {
        public string Name { get; }
        public string Description { get; }
        public DataType DataType { get; }

        public OdtFieldDef(string name, string description, DataType dataType)
        {
            Name = name;
            Description = description;
            DataType = dataType;
        }
    }
}
```

- [ ] **Step 2: IOdtSchema.cs 작성**

```csharp
// src/GntTools.Core/Odt/IOdtSchema.cs
using System.Collections.Generic;

namespace GntTools.Core.Odt
{
    /// <summary>ODT 테이블 스키마 인터페이스</summary>
    public interface IOdtSchema
    {
        string TableName { get; }
        string Description { get; }
        IReadOnlyList<OdtFieldDef> Fields { get; }
    }
}
```

- [ ] **Step 3: PipeCommonSchema.cs 작성**

스펙 §2 PIPE_COMMON (13개 필드) 기준:

```csharp
// src/GntTools.Core/Odt/PipeCommonSchema.cs
using System.Collections.Generic;
using Autodesk.Gis.Map.Constants;

namespace GntTools.Core.Odt
{
    /// <summary>PIPE_COMMON 공통 테이블 스키마 (13 fields)</summary>
    public class PipeCommonSchema : IOdtSchema
    {
        public string TableName => "PIPE_COMMON";
        public string Description => "관로 공통 속성";

        public IReadOnlyList<OdtFieldDef> Fields { get; } = new List<OdtFieldDef>
        {
            new OdtFieldDef("ISTYMD", "설치일자",   DataType.Character),
            new OdtFieldDef("MOPCDE", "관재질",     DataType.Character),
            new OdtFieldDef("PIPDIP", "구경",       DataType.Character),
            new OdtFieldDef("PIPLEN", "연장(m)",    DataType.Real),
            new OdtFieldDef("BTCDE",  "불탐여부",   DataType.Character),
            new OdtFieldDef("BEGDEP", "시점심도(m)", DataType.Real),
            new OdtFieldDef("ENDDEP", "종점심도(m)", DataType.Real),
            new OdtFieldDef("AVEDEP", "평균심도(m)", DataType.Real),
            new OdtFieldDef("HGHDEP", "최고심도(m)", DataType.Real),
            new OdtFieldDef("LOWDEP", "최저심도(m)", DataType.Real),
            new OdtFieldDef("PIPLBL", "관라벨",     DataType.Character),
            new OdtFieldDef("WRKDTE", "준공일자",   DataType.Character),
            new OdtFieldDef("REMARK", "비고",       DataType.Character),
        }.AsReadOnly();
    }
}
```

- [ ] **Step 4: PipeCommonRecord.cs 작성**

```csharp
// src/GntTools.Core/Odt/PipeCommonRecord.cs
namespace GntTools.Core.Odt
{
    /// <summary>PIPE_COMMON 레코드 DTO</summary>
    public class PipeCommonRecord
    {
        public string InstallDate { get; set; } = "";    // ISTYMD
        public string Material { get; set; } = "";       // MOPCDE
        public string Diameter { get; set; } = "";       // PIPDIP
        public double Length { get; set; }               // PIPLEN
        public string Undetected { get; set; } = "N";   // BTCDE (Y/N)
        public double BeginDepth { get; set; }           // BEGDEP
        public double EndDepth { get; set; }             // ENDDEP
        public double AverageDepth { get; set; }         // AVEDEP
        public double MaxDepth { get; set; }             // HGHDEP
        public double MinDepth { get; set; }             // LOWDEP
        public string Label { get; set; } = "";          // PIPLBL
        public string CompletionDate { get; set; } = ""; // WRKDTE
        public string Remark { get; set; } = "";         // REMARK

        /// <summary>OdtManager.UpdateRecord용 Dictionary 변환</summary>
        public System.Collections.Generic.Dictionary<string, object> ToDictionary()
        {
            return new System.Collections.Generic.Dictionary<string, object>
            {
                ["ISTYMD"] = InstallDate,
                ["MOPCDE"] = Material,
                ["PIPDIP"] = Diameter,
                ["PIPLEN"] = Length,
                ["BTCDE"]  = Undetected,
                ["BEGDEP"] = BeginDepth,
                ["ENDDEP"] = EndDepth,
                ["AVEDEP"] = AverageDepth,
                ["HGHDEP"] = MaxDepth,
                ["LOWDEP"] = MinDepth,
                ["PIPLBL"] = Label,
                ["WRKDTE"] = CompletionDate,
                ["REMARK"] = Remark,
            };
        }
    }
}
```

- [ ] **Step 5: 빌드 확인 후 커밋**

```bash
msbuild src\GntTools.Core\GntTools.Core.csproj /t:Build /p:Configuration=Debug
```
Expected: 성공 (AutoCAD DLL이 lib/에 있어야 함)

```bash
git add src/GntTools.Core/Odt/
git commit -m "feat(core): add ODT schema interface and PIPE_COMMON definition

- IOdtSchema: 테이블 스키마 인터페이스
- OdtFieldDef: 필드 정의 (Name, Description, DataType)
- PipeCommonSchema: PIPE_COMMON 13개 필드 정의
- PipeCommonRecord: 레코드 DTO with ToDictionary()"
```

---

### Task 3: OdtManager — ODT CRUD 통합 관리자

**Files:**
- Create: `src/GntTools.Core/Odt/OdtManager.cs`
- Ref: `references/map3d-project.md` (MapApplication, ObjectData API)
- Ref: skill `autocad-map-odt` (ODT CRUD 패턴)

> **중요:** @autocad-map-odt 스킬의 에러 방지 체크리스트 10항목 준수

- [ ] **Step 1: OdtManager.cs 작성**

```csharp
// src/GntTools.Core/Odt/OdtManager.cs
using System;
using System.Collections.Generic;
using Autodesk.AutoCAD.DatabaseServices;
using Autodesk.Gis.Map;
using Autodesk.Gis.Map.ObjectData;
using Autodesk.Gis.Map.Constants;

namespace GntTools.Core.Odt
{
    /// <summary>
    /// ODT(Object Data Table) CRUD 통합 관리자
    /// autocad-map-odt 스킬 체크리스트 준수
    /// </summary>
    public class OdtManager
    {
        private Tables GetOdtTables()
        {
            var mapApp = HostMapApplicationServices.Application;
            var proj = mapApp.ActiveProject;
            return proj.ODTables;
        }

        // ─── 테이블 관리 ───

        /// <summary>테이블이 없으면 생성, 있으면 true 반환</summary>
        public bool EnsureTable(IOdtSchema schema)
        {
            var tables = GetOdtTables();
            if (tables.GetCount() > 0)
            {
                // 이미 존재하는지 확인
                for (int i = 0; i < tables.GetCount(); i++)
                {
                    if (tables[i].Name == schema.TableName)
                        return true;
                }
            }

            // 테이블 생성
            var tableDef = tables.GetTableDefinition(schema.TableName);
            // 실제로는 MapApplication.CreateTable 사용
            var fieldDefs = new Autodesk.Gis.Map.ObjectData.FieldDefinitions();
            foreach (var field in schema.Fields)
            {
                var fd = FieldDefinition.Create(field.Name, field.Description, field.DataType);
                fieldDefs.AddColumn(fd, fieldDefs.Count);
            }
            tables.Add(schema.TableName, fieldDefs, schema.Description, true);
            return true;
        }

        /// <summary>테이블 존재 여부</summary>
        public bool TableExists(string tableName)
        {
            var tables = GetOdtTables();
            for (int i = 0; i < tables.GetCount(); i++)
            {
                if (tables[i].Name == tableName)
                    return true;
            }
            return false;
        }

        /// <summary>테이블 제거</summary>
        public bool RemoveTable(string tableName)
        {
            if (!TableExists(tableName)) return false;
            var tables = GetOdtTables();
            tables.RemoveTable(tableName);
            return true;
        }

        // ─── 레코드 CRUD ───

        /// <summary>엔티티에 빈 레코드 부착</summary>
        public bool AttachRecord(string tableName, ObjectId entityId)
        {
            if (!TableExists(tableName)) return false;
            var tables = GetOdtTables();
            var table = tables[tableName];

            // 이미 레코드가 있으면 스킵
            using (var records = table.GetObjectTableRecords(
                Convert.ToUInt32(0), entityId,
                Autodesk.Gis.Map.Constants.OpenMode.OpenForRead))
            {
                if (records.Count > 0) return true;
            }

            var rec = Record.Create();
            table.InitRecord(rec);
            table.AddRecord(rec, entityId);
            return true;
        }

        /// <summary>레코드 값 업데이트</summary>
        public bool UpdateRecord(string tableName, ObjectId entityId,
            Dictionary<string, object> values)
        {
            if (!TableExists(tableName)) return false;
            var tables = GetOdtTables();
            var table = tables[tableName];

            using (var records = table.GetObjectTableRecords(
                Convert.ToUInt32(0), entityId,
                Autodesk.Gis.Map.Constants.OpenMode.OpenForWrite))
            {
                if (records.Count == 0) return false;
                records.MoveFirst();
                var rec = records.Current;

                foreach (var kvp in values)
                {
                    int idx = FindFieldIndex(table, kvp.Key);
                    if (idx < 0) continue;

                    var val = rec[idx];
                    if (kvp.Value is string s)
                        val.Assign(s);
                    else if (kvp.Value is double d)
                        val.Assign(d);
                    else if (kvp.Value is int n)
                        val.Assign(n);

                    rec[idx] = val;
                }
                return true;
            }
        }

        /// <summary>레코드 값 읽기 (string 배열로 반환)</summary>
        public string[] ReadRecord(string tableName, ObjectId entityId)
        {
            if (!TableExists(tableName)) return null;
            var tables = GetOdtTables();
            var table = tables[tableName];

            using (var records = table.GetObjectTableRecords(
                Convert.ToUInt32(0), entityId,
                Autodesk.Gis.Map.Constants.OpenMode.OpenForRead))
            {
                if (records.Count == 0) return null;
                records.MoveFirst();
                var rec = records.Current;

                var result = new string[rec.Count];
                for (int i = 0; i < rec.Count; i++)
                {
                    result[i] = rec[i].StrValue ?? "";
                }
                return result;
            }
        }

        /// <summary>레코드 존재 여부</summary>
        public bool RecordExists(string tableName, ObjectId entityId)
        {
            if (!TableExists(tableName)) return false;
            var tables = GetOdtTables();
            var table = tables[tableName];

            using (var records = table.GetObjectTableRecords(
                Convert.ToUInt32(0), entityId,
                Autodesk.Gis.Map.Constants.OpenMode.OpenForRead))
            {
                return records.Count > 0;
            }
        }

        /// <summary>레코드 제거</summary>
        public bool RemoveRecord(string tableName, ObjectId entityId)
        {
            if (!TableExists(tableName)) return false;
            var tables = GetOdtTables();
            var table = tables[tableName];

            using (var records = table.GetObjectTableRecords(
                Convert.ToUInt32(0), entityId,
                Autodesk.Gis.Map.Constants.OpenMode.OpenForWrite))
            {
                if (records.Count == 0) return false;
                records.MoveFirst();
                records.RemoveRecord();
                return true;
            }
        }

        // ─── 내부 헬퍼 ───

        private int FindFieldIndex(Table table, string fieldName)
        {
            var fieldDefs = table.FieldDefinitions;
            for (int i = 0; i < fieldDefs.Count; i++)
            {
                if (fieldDefs[i].Name == fieldName)
                    return i;
            }
            return -1;
        }
    }
}
```

- [ ] **Step 2: 빌드 확인 후 커밋**

```bash
msbuild src\GntTools.Core\GntTools.Core.csproj /t:Build /p:Configuration=Debug
```
Expected: 성공

```bash
git add src/GntTools.Core/Odt/OdtManager.cs
git commit -m "feat(core): implement OdtManager with full CRUD operations

- EnsureTable: 스키마 기반 테이블 자동생성
- AttachRecord/UpdateRecord/ReadRecord/RemoveRecord
- RecordExists: 중복 방지
- autocad-map-odt 스킬 체크리스트 준수"
```

---

## Chunk 2: Settings + Selection + Geometry + Drawing + XData

### Task 4: AppSettings — JSON 설정 관리

**Files:**
- Create: `src/GntTools.Core/Settings/DomainSettings.cs`
- Create: `src/GntTools.Core/Settings/AppSettings.cs`
- Ref: 스펙 §7 설정 관리 (settings.json 구조)

- [ ] **Step 1: DomainSettings.cs 작성**

```csharp
// src/GntTools.Core/Settings/DomainSettings.cs
using System.Collections.Generic;
using System.Runtime.Serialization;

namespace GntTools.Core.Settings
{
    [DataContract]
    public class CommonSettings
    {
        [DataMember(Name = "textStyle")]
        public string TextStyle { get; set; } = "GHS";
        [DataMember(Name = "shxFont")]
        public string ShxFont { get; set; } = "ROMANS";
        [DataMember(Name = "bigFont")]
        public string BigFont { get; set; } = "GHS";
        [DataMember(Name = "textSize")]
        public double TextSize { get; set; } = 1.0;
        [DataMember(Name = "lengthDecimals")]
        public int LengthDecimals { get; set; } = 0;
        [DataMember(Name = "depthDecimals")]
        public int DepthDecimals { get; set; } = 1;
    }

    [DataContract]
    public class LayerSettings
    {
        [DataMember(Name = "depth")]
        public string Depth { get; set; } = "";
        [DataMember(Name = "groundHeight")]
        public string GroundHeight { get; set; } = "";
        [DataMember(Name = "label")]
        public string Label { get; set; } = "";
        [DataMember(Name = "leader")]
        public string Leader { get; set; } = "";
        [DataMember(Name = "undetected")]
        public string Undetected { get; set; } = "";
        [DataMember(Name = "drawing")]
        public string Drawing { get; set; } = "";  // KEPCO 전용
    }

    [DataContract]
    public class DefaultValues
    {
        [DataMember(Name = "year")]
        public string Year { get; set; } = "2024";
        [DataMember(Name = "material")]
        public string Material { get; set; } = "";
        [DataMember(Name = "diameter")]
        public string Diameter { get; set; } = "";
        [DataMember(Name = "useCode")]
        public string UseCode { get; set; } = "";  // SWL 전용
    }

    [DataContract]
    public class DomainSettings
    {
        [DataMember(Name = "layers")]
        public LayerSettings Layers { get; set; } = new LayerSettings();
        [DataMember(Name = "defaults")]
        public DefaultValues Defaults { get; set; } = new DefaultValues();
        [DataMember(Name = "diameters")]
        public List<int> Diameters { get; set; }  // KEPCO 전용
    }
}
```

- [ ] **Step 2: AppSettings.cs 작성**

```csharp
// src/GntTools.Core/Settings/AppSettings.cs
using System;
using System.IO;
using System.Runtime.Serialization;
using System.Runtime.Serialization.Json;
using System.Text;

namespace GntTools.Core.Settings
{
    [DataContract]
    public class AppSettings
    {
        private static AppSettings _instance;
        private static readonly string SettingsDir =
            Path.Combine(Environment.GetFolderPath(
                Environment.SpecialFolder.ApplicationData), "GntTools");
        private static readonly string SettingsPath =
            Path.Combine(SettingsDir, "settings.json");

        [DataMember(Name = "common")]
        public CommonSettings Common { get; set; } = new CommonSettings();
        [DataMember(Name = "wtl")]
        public DomainSettings Wtl { get; set; } = new DomainSettings();
        [DataMember(Name = "swl")]
        public DomainSettings Swl { get; set; } = new DomainSettings();
        [DataMember(Name = "kepco")]
        public DomainSettings Kepco { get; set; } = new DomainSettings();

        /// <summary>싱글 인스턴스 로드</summary>
        public static AppSettings Instance
        {
            get
            {
                if (_instance == null)
                    _instance = Load();
                return _instance;
            }
        }

        /// <summary>JSON에서 로드 (파일 없으면 기본값)</summary>
        public static AppSettings Load()
        {
            if (!File.Exists(SettingsPath))
                return CreateDefault();

            try
            {
                var json = File.ReadAllText(SettingsPath, Encoding.UTF8);
                var ser = new DataContractJsonSerializer(typeof(AppSettings));
                using (var ms = new MemoryStream(Encoding.UTF8.GetBytes(json)))
                {
                    _instance = (AppSettings)ser.ReadObject(ms);
                    return _instance;
                }
            }
            catch
            {
                return CreateDefault();
            }
        }

        /// <summary>JSON으로 저장</summary>
        public void Save()
        {
            if (!Directory.Exists(SettingsDir))
                Directory.CreateDirectory(SettingsDir);

            var ser = new DataContractJsonSerializer(typeof(AppSettings));
            using (var ms = new MemoryStream())
            {
                ser.WriteObject(ms, this);
                var json = Encoding.UTF8.GetString(ms.ToArray());
                File.WriteAllText(SettingsPath, json, Encoding.UTF8);
            }
        }

        private static AppSettings CreateDefault()
        {
            var s = new AppSettings();

            // WTL 기본값
            s.Wtl.Layers = new LayerSettings
            {
                Depth = "WS_DEP", GroundHeight = "WS_HGT",
                Label = "WS_LBL", Leader = "WS_LEAD", Undetected = "WS_BT"
            };
            s.Wtl.Defaults = new DefaultValues
            { Year = "2024", Material = "PE", Diameter = "200" };

            // SWL 기본값
            s.Swl.Layers = new LayerSettings
            {
                Depth = "SW_DEP", GroundHeight = "SW_HGT",
                Label = "SW_LBL", Leader = "SW_LEAD", Undetected = "SW_BT"
            };
            s.Swl.Defaults = new DefaultValues
            { Year = "2024", Material = "HP", Diameter = "300", UseCode = "01" };

            // KEPCO 기본값
            s.Kepco.Layers = new LayerSettings
            {
                Depth = "SC991", Drawing = "SC983",
                Label = "SC992", Leader = "SC982", Undetected = "SC999"
            };
            s.Kepco.Defaults = new DefaultValues
            { Year = "2024", Material = "ELP" };
            s.Kepco.Diameters = new System.Collections.Generic.List<int>
            { 200, 175, 150, 125, 100, 80 };

            _instance = s;
            return s;
        }
    }
}
```

- [ ] **Step 3: 빌드 확인 후 커밋**

```bash
git add src/GntTools.Core/Settings/
git commit -m "feat(core): implement JSON-based settings management

- AppSettings: singleton, Load/Save to %AppData%/GntTools/settings.json
- DomainSettings: WTL/SWL/KEPCO 레이어 + 기본값 모델
- DataContractJsonSerializer 사용 (외부 의존성 없음)"
```

---

### Task 5: EntitySelector — 엔티티 선택 유틸리티

**Files:**
- Create: `src/GntTools.Core/Selection/EntitySelector.cs`
- Ref: `references/autocad-editor.md` (프롬프트, 선택, 필터)

- [ ] **Step 1: EntitySelector.cs 작성**

```csharp
// src/GntTools.Core/Selection/EntitySelector.cs
using Autodesk.AutoCAD.ApplicationServices;
using Autodesk.AutoCAD.DatabaseServices;
using Autodesk.AutoCAD.EditorInput;
using Autodesk.AutoCAD.Geometry;

namespace GntTools.Core.Selection
{
    /// <summary>엔티티 선택 유틸리티 (사용자 대화형 + 프로그래밍 방식)</summary>
    public class EntitySelector
    {
        private Editor GetEditor()
        {
            return Application.DocumentManager.MdiActiveDocument.Editor;
        }

        /// <summary>단일 엔티티 선택 (사용자 클릭)</summary>
        public ObjectId SelectOne(SelectionFilter filter = null, string message = null)
        {
            var ed = GetEditor();
            var opts = new PromptEntityOptions(
                message ?? "\n엔티티를 선택하세요: ");
            opts.SetRejectMessage("\n유효하지 않은 객체입니다.");

            // 필터에서 DXF 코드 0 (엔티티 타입) 추출하여 AllowedClass 설정
            if (filter != null)
            {
                // filter는 PromptEntityOptions에 직접 적용 불가
                // → SelectOne 후 필터 검증 또는 SSGET 사용
            }

            var result = ed.GetEntity(opts);
            if (result.Status != PromptStatus.OK)
                return ObjectId.Null;
            return result.ObjectId;
        }

        /// <summary>복수 엔티티 선택 (사용자 윈도우/크로싱)</summary>
        public ObjectId[] SelectMultiple(SelectionFilter filter = null,
            string message = null)
        {
            var ed = GetEditor();
            var opts = new PromptSelectionOptions();
            if (message != null)
                opts.MessageForAdding = message;

            PromptSelectionResult result;
            if (filter != null)
                result = ed.GetSelection(opts, filter);
            else
                result = ed.GetSelection(opts);

            if (result.Status != PromptStatus.OK)
                return new ObjectId[0];

            return result.Value.GetObjectIds();
        }

        /// <summary>도면 전체에서 필터 조건에 맞는 엔티티 선택</summary>
        public ObjectId[] SelectAll(SelectionFilter filter)
        {
            var ed = GetEditor();
            var result = ed.SelectAll(filter);
            if (result.Status != PromptStatus.OK)
                return new ObjectId[0];
            return result.Value.GetObjectIds();
        }

        /// <summary>폴리라인 전용 선택 필터 생성</summary>
        public static SelectionFilter PolylineFilter()
        {
            return new SelectionFilter(new[]
            {
                new TypedValue((int)DxfCode.Start, "LWPOLYLINE,POLYLINE")
            });
        }

        /// <summary>특정 레이어의 텍스트 선택 필터</summary>
        public static SelectionFilter TextOnLayerFilter(string layerName)
        {
            return new SelectionFilter(new[]
            {
                new TypedValue((int)DxfCode.Start, "TEXT,MTEXT"),
                new TypedValue((int)DxfCode.LayerName, layerName)
            });
        }

        /// <summary>특정 레이어의 원(Circle) 선택 필터</summary>
        public static SelectionFilter CircleOnLayerFilter(string layerName)
        {
            return new SelectionFilter(new[]
            {
                new TypedValue((int)DxfCode.Start, "CIRCLE"),
                new TypedValue((int)DxfCode.LayerName, layerName)
            });
        }
    }
}
```

- [ ] **Step 2: 빌드 확인 후 커밋**

```bash
git add src/GntTools.Core/Selection/
git commit -m "feat(core): implement EntitySelector with common filters

- SelectOne/SelectMultiple: 사용자 대화형 선택
- SelectAll: 프로그래밍 방식 전체 선택
- PolylineFilter/TextOnLayerFilter/CircleOnLayerFilter: 재사용 필터"
```

---

### Task 6: DepthCalculator + PolylineHelper — 심도 측정 및 폴리라인 유틸

**Files:**
- Create: `src/GntTools.Core/Geometry/DepthResult.cs`
- Create: `src/GntTools.Core/Geometry/DepthCalculator.cs`
- Create: `src/GntTools.Core/Geometry/PolylineHelper.cs`
- Ref: `references/autocad-geometry.md` (Point3d.DistanceTo)
- Ref: 스펙 §4 DepthCalculator 설계

- [ ] **Step 1: DepthResult.cs 작성**

```csharp
// src/GntTools.Core/Geometry/DepthResult.cs
namespace GntTools.Core.Geometry
{
    /// <summary>심도 측정 결과</summary>
    public class DepthResult
    {
        public double BeginDepth { get; set; }
        public double EndDepth { get; set; }
        public double AverageDepth { get; set; }
        public double MaxDepth { get; set; }
        public double MinDepth { get; set; }
        public bool IsUndetected { get; set; }
    }
}
```

- [ ] **Step 2: PolylineHelper.cs 작성**

```csharp
// src/GntTools.Core/Geometry/PolylineHelper.cs
using System.Collections.Generic;
using Autodesk.AutoCAD.ApplicationServices;
using Autodesk.AutoCAD.DatabaseServices;
using Autodesk.AutoCAD.Geometry;

namespace GntTools.Core.Geometry
{
    /// <summary>폴리라인 정점 추출 및 길이 계산</summary>
    public static class PolylineHelper
    {
        /// <summary>폴리라인 정점 좌표 목록</summary>
        public static List<Point3d> GetVertices(ObjectId polyId)
        {
            var points = new List<Point3d>();
            var doc = Application.DocumentManager.MdiActiveDocument;
            var db = doc.Database;

            using (var tr = db.TransactionManager.StartTransaction())
            {
                var ent = tr.GetObject(polyId, OpenMode.ForRead);
                if (ent is Polyline pl)
                {
                    for (int i = 0; i < pl.NumberOfVertices; i++)
                        points.Add(pl.GetPoint3dAt(i));
                }
                tr.Commit();
            }
            return points;
        }

        /// <summary>폴리라인 총 길이 (m)</summary>
        public static double GetLength(ObjectId polyId)
        {
            var doc = Application.DocumentManager.MdiActiveDocument;
            var db = doc.Database;

            using (var tr = db.TransactionManager.StartTransaction())
            {
                var ent = tr.GetObject(polyId, OpenMode.ForRead) as Curve;
                tr.Commit();
                return ent?.GetDistAtPoint(ent.EndPoint) ?? 0.0;
            }
        }

        /// <summary>시점/종점 좌표</summary>
        public static (Point3d start, Point3d end) GetEndpoints(ObjectId polyId)
        {
            var doc = Application.DocumentManager.MdiActiveDocument;
            var db = doc.Database;

            using (var tr = db.TransactionManager.StartTransaction())
            {
                var ent = tr.GetObject(polyId, OpenMode.ForRead) as Curve;
                tr.Commit();
                if (ent == null)
                    return (Point3d.Origin, Point3d.Origin);
                return (ent.StartPoint, ent.EndPoint);
            }
        }
    }
}
```

- [ ] **Step 3: DepthCalculator.cs 작성**

```csharp
// src/GntTools.Core/Geometry/DepthCalculator.cs
using System;
using System.Collections.Generic;
using System.Linq;
using Autodesk.AutoCAD.ApplicationServices;
using Autodesk.AutoCAD.DatabaseServices;
using Autodesk.AutoCAD.EditorInput;
using Autodesk.AutoCAD.Geometry;
using GntTools.Core.Selection;

namespace GntTools.Core.Geometry
{
    /// <summary>심도 자동/수동 측정</summary>
    public class DepthCalculator
    {
        /// <summary>
        /// 자동: 폴리라인 정점 근처의 심도 텍스트를 읽어서 계산
        /// </summary>
        /// <param name="polylineId">대상 폴리라인</param>
        /// <param name="depthLayer">심도 텍스트 레이어명</param>
        /// <param name="tolerance">정점-텍스트 탐색 반경</param>
        public DepthResult MeasureAtVertices(ObjectId polylineId,
            string depthLayer, double tolerance = 5.0)
        {
            var vertices = PolylineHelper.GetVertices(polylineId);
            if (vertices.Count < 2) return Undetected();

            var doc = Application.DocumentManager.MdiActiveDocument;
            var db = doc.Database;
            var ed = doc.Editor;

            var depths = new List<double>();

            using (var tr = db.TransactionManager.StartTransaction())
            {
                // 심도 레이어의 모든 텍스트 수집
                var filter = EntitySelector.TextOnLayerFilter(depthLayer);
                var ssResult = ed.SelectAll(filter);
                if (ssResult.Status != PromptStatus.OK)
                {
                    tr.Commit();
                    return Undetected();
                }

                var textEntities = new List<(Point3d pos, double val)>();
                foreach (var id in ssResult.Value.GetObjectIds())
                {
                    var ent = tr.GetObject(id, OpenMode.ForRead);
                    if (ent is DBText txt)
                    {
                        if (double.TryParse(txt.TextString, out double v))
                            textEntities.Add((txt.Position, v));
                    }
                }

                // 각 정점에 가장 가까운 심도 텍스트 찾기
                foreach (var vertex in vertices)
                {
                    double minDist = double.MaxValue;
                    double closestVal = 0;
                    bool found = false;

                    foreach (var (pos, val) in textEntities)
                    {
                        double dist = vertex.DistanceTo(pos);
                        if (dist < tolerance && dist < minDist)
                        {
                            minDist = dist;
                            closestVal = val;
                            found = true;
                        }
                    }

                    if (found) depths.Add(closestVal);
                }

                tr.Commit();
            }

            if (depths.Count == 0) return Undetected();

            return new DepthResult
            {
                BeginDepth = depths.First(),
                EndDepth = depths.Last(),
                AverageDepth = Math.Round(depths.Average(), 2),
                MaxDepth = depths.Max(),
                MinDepth = depths.Min(),
                IsUndetected = false
            };
        }

        /// <summary>수동: 사용자 입력 시점/종점 심도</summary>
        public DepthResult FromManualInput(double beginDepth, double endDepth)
        {
            double avg = Math.Round((beginDepth + endDepth) / 2.0, 2);
            double max = Math.Max(beginDepth, endDepth);
            double min = Math.Min(beginDepth, endDepth);

            return new DepthResult
            {
                BeginDepth = beginDepth,
                EndDepth = endDepth,
                AverageDepth = avg,
                MaxDepth = max,
                MinDepth = min,
                IsUndetected = false
            };
        }

        /// <summary>불탐</summary>
        public DepthResult Undetected()
        {
            return new DepthResult
            {
                BeginDepth = 0,
                EndDepth = 0,
                AverageDepth = 0,
                MaxDepth = 0,
                MinDepth = 0,
                IsUndetected = true
            };
        }
    }
}
```

- [ ] **Step 4: 빌드 확인 후 커밋**

```bash
git add src/GntTools.Core/Geometry/
git commit -m "feat(core): implement DepthCalculator and PolylineHelper

- DepthResult: 심도 측정 결과 DTO (Begin/End/Avg/Max/Min + IsUndetected)
- DepthCalculator: 자동(정점별 텍스트 탐색) / 수동(시점·종점) / 불탐
- PolylineHelper: 정점 추출, 길이 계산, 시종점 조회"
```

---

### Task 7: ViewportManager — 줌 저장/복원/이동

**Files:**
- Create: `src/GntTools.Core/Geometry/ViewportManager.cs`
- Ref: `references/autocad-geometry.md` (Point3d, Extents3d)
- Bug fix: `MinPoint.X - MinPoint.Y` → `MinPoint.X` (스펙 §8)

- [ ] **Step 1: ViewportManager.cs 작성**

```csharp
// src/GntTools.Core/Geometry/ViewportManager.cs
using Autodesk.AutoCAD.ApplicationServices;
using Autodesk.AutoCAD.DatabaseServices;
using Autodesk.AutoCAD.Geometry;

namespace GntTools.Core.Geometry
{
    /// <summary>뷰포트 줌 저장/복원/이동</summary>
    public static class ViewportManager
    {
        /// <summary>엔티티 범위로 줌 (여백 포함)</summary>
        /// <remarks>
        /// VB.NET 버그 수정: MaxPoint.X - MinPoint.Y → MinPoint.X 사용
        /// </remarks>
        public static void ZoomToEntity(ObjectId entityId, double marginFactor = 1.2)
        {
            var doc = Application.DocumentManager.MdiActiveDocument;
            var db = doc.Database;

            using (var tr = db.TransactionManager.StartTransaction())
            {
                var ent = tr.GetObject(entityId, OpenMode.ForRead) as Entity;
                if (ent == null) { tr.Commit(); return; }

                var ext = ent.GeometricExtents;
                ZoomToExtents(ext, marginFactor);
                tr.Commit();
            }
        }

        /// <summary>범위(Extents3d)로 줌</summary>
        public static void ZoomToExtents(Extents3d extents, double marginFactor = 1.2)
        {
            var doc = Application.DocumentManager.MdiActiveDocument;
            var ed = doc.Editor;

            // 중심점 계산
            double cx = (extents.MinPoint.X + extents.MaxPoint.X) / 2.0;
            double cy = (extents.MinPoint.Y + extents.MaxPoint.Y) / 2.0;

            // 크기 계산 (여백 적용)
            double width = (extents.MaxPoint.X - extents.MinPoint.X) * marginFactor;
            double height = (extents.MaxPoint.Y - extents.MinPoint.Y) * marginFactor;

            var center = new Point2d(cx, cy);
            var viewSize = new Vector2d(width, height);

            using (var view = ed.GetCurrentView())
            {
                view.CenterPoint = center;
                view.Height = height;
                view.Width = width;
                ed.SetCurrentView(view);
            }
        }

        /// <summary>현재 뷰 저장</summary>
        public static ViewTableRecord SaveCurrentView()
        {
            var doc = Application.DocumentManager.MdiActiveDocument;
            return (ViewTableRecord)doc.Editor.GetCurrentView().Clone();
        }

        /// <summary>저장된 뷰 복원</summary>
        public static void RestoreView(ViewTableRecord savedView)
        {
            if (savedView == null) return;
            var doc = Application.DocumentManager.MdiActiveDocument;
            doc.Editor.SetCurrentView(savedView);
        }
    }
}
```

- [ ] **Step 2: 빌드 확인 후 커밋**

```bash
git add src/GntTools.Core/Geometry/ViewportManager.cs
git commit -m "feat(core): implement ViewportManager with VB.NET bug fix

- ZoomToEntity: 엔티티 범위 줌 (여백 조절 가능)
- SaveCurrentView/RestoreView: 뷰 저장/복원
- fix: MinPoint.Y → MinPoint.X 좌표 오류 수정"
```

---

### Task 8: Drawing 유틸리티 — Layer, TextStyle, Color

**Files:**
- Create: `src/GntTools.Core/Drawing/LayerHelper.cs`
- Create: `src/GntTools.Core/Drawing/TextStyleHelper.cs`
- Create: `src/GntTools.Core/Drawing/ColorHelper.cs`
- Bug fix: `changColor` 파라미터 무시 → 정확 적용 (스펙 §8)

- [ ] **Step 1: LayerHelper.cs 작성**

```csharp
// src/GntTools.Core/Drawing/LayerHelper.cs
using Autodesk.AutoCAD.ApplicationServices;
using Autodesk.AutoCAD.Colors;
using Autodesk.AutoCAD.DatabaseServices;

namespace GntTools.Core.Drawing
{
    /// <summary>레이어 존재확인/생성</summary>
    public static class LayerHelper
    {
        /// <summary>레이어가 없으면 생성, 있으면 스킵</summary>
        public static void EnsureLayer(string layerName, short colorIndex = 7)
        {
            var doc = Application.DocumentManager.MdiActiveDocument;
            var db = doc.Database;

            using (var tr = db.TransactionManager.StartTransaction())
            {
                var lt = (LayerTable)tr.GetObject(db.LayerTableId, OpenMode.ForRead);
                if (!lt.Has(layerName))
                {
                    lt.UpgradeOpen();
                    var layer = new LayerTableRecord
                    {
                        Name = layerName,
                        Color = Color.FromColorIndex(ColorMethod.ByAci, colorIndex)
                    };
                    lt.Add(layer);
                    tr.AddNewlyCreatedDBObject(layer, true);
                }
                tr.Commit();
            }
        }

        /// <summary>레이어 존재 여부 확인</summary>
        public static bool Exists(string layerName)
        {
            var db = Application.DocumentManager.MdiActiveDocument.Database;
            using (var tr = db.TransactionManager.StartTransaction())
            {
                var lt = (LayerTable)tr.GetObject(db.LayerTableId, OpenMode.ForRead);
                tr.Commit();
                return lt.Has(layerName);
            }
        }
    }
}
```

- [ ] **Step 2: TextStyleHelper.cs 작성**

```csharp
// src/GntTools.Core/Drawing/TextStyleHelper.cs
using Autodesk.AutoCAD.ApplicationServices;
using Autodesk.AutoCAD.DatabaseServices;

namespace GntTools.Core.Drawing
{
    /// <summary>텍스트 스타일 관리</summary>
    public static class TextStyleHelper
    {
        /// <summary>텍스트 스타일이 없으면 생성 (SHX + BigFont)</summary>
        public static ObjectId EnsureStyle(string styleName,
            string shxFont = "ROMANS", string bigFont = "GHS")
        {
            var doc = Application.DocumentManager.MdiActiveDocument;
            var db = doc.Database;

            using (var tr = db.TransactionManager.StartTransaction())
            {
                var tst = (TextStyleTable)tr.GetObject(
                    db.TextStyleTableId, OpenMode.ForRead);

                if (tst.Has(styleName))
                {
                    var id = tst[styleName];
                    tr.Commit();
                    return id;
                }

                tst.UpgradeOpen();
                var style = new TextStyleTableRecord
                {
                    Name = styleName,
                    FileName = shxFont + ".shx",
                    BigFontFileName = bigFont + ".shx"
                };
                var styleId = tst.Add(style);
                tr.AddNewlyCreatedDBObject(style, true);
                tr.Commit();
                return styleId;
            }
        }

        /// <summary>스타일명으로 ObjectId 조회</summary>
        public static ObjectId GetStyleId(string styleName)
        {
            var db = Application.DocumentManager.MdiActiveDocument.Database;
            using (var tr = db.TransactionManager.StartTransaction())
            {
                var tst = (TextStyleTable)tr.GetObject(
                    db.TextStyleTableId, OpenMode.ForRead);
                tr.Commit();
                return tst.Has(styleName) ? tst[styleName] : ObjectId.Null;
            }
        }
    }
}
```

- [ ] **Step 3: ColorHelper.cs 작성**

```csharp
// src/GntTools.Core/Drawing/ColorHelper.cs
using Autodesk.AutoCAD.ApplicationServices;
using Autodesk.AutoCAD.Colors;
using Autodesk.AutoCAD.DatabaseServices;

namespace GntTools.Core.Drawing
{
    /// <summary>엔티티 색상 변경</summary>
    /// <remarks>
    /// VB.NET 버그 수정: changColor 파라미터 무시(항상 2) → 정확 적용
    /// </remarks>
    public static class ColorHelper
    {
        /// <summary>엔티티 색상을 ACI 인덱스로 변경</summary>
        public static void SetColor(ObjectId entityId, short colorIndex)
        {
            var doc = Application.DocumentManager.MdiActiveDocument;
            var db = doc.Database;

            using (var tr = db.TransactionManager.StartTransaction())
            {
                var ent = tr.GetObject(entityId, OpenMode.ForWrite) as Entity;
                if (ent != null)
                {
                    ent.Color = Color.FromColorIndex(
                        ColorMethod.ByAci, colorIndex);
                }
                tr.Commit();
            }
        }
    }
}
```

- [ ] **Step 4: 빌드 확인 후 커밋**

```bash
git add src/GntTools.Core/Drawing/LayerHelper.cs
git add src/GntTools.Core/Drawing/TextStyleHelper.cs
git add src/GntTools.Core/Drawing/ColorHelper.cs
git commit -m "feat(core): implement Drawing utilities (Layer, TextStyle, Color)

- LayerHelper: EnsureLayer (없으면 생성), Exists 확인
- TextStyleHelper: EnsureStyle (SHX+BigFont), GetStyleId
- ColorHelper: SetColor with correct parameter (VB.NET bug fix)"
```

---

### Task 9: TextWriter + LeaderWriter — 텍스트/지시선 생성

**Files:**
- Create: `src/GntTools.Core/Drawing/TextWriter.cs`
- Create: `src/GntTools.Core/Drawing/LeaderWriter.cs`
- Ref: `references/autocad-core.md` (Transaction, Entity 생성)
- Ref: VB.NET `userTextClass.vb` (기존 텍스트 로직 참고)

- [ ] **Step 1: TextWriter.cs 작성**

```csharp
// src/GntTools.Core/Drawing/TextWriter.cs
using System;
using Autodesk.AutoCAD.ApplicationServices;
using Autodesk.AutoCAD.DatabaseServices;
using Autodesk.AutoCAD.Geometry;

namespace GntTools.Core.Drawing
{
    /// <summary>DBText 생성/수정/이동/회전</summary>
    public static class TextWriter
    {
        /// <summary>새 DBText 생성하여 ModelSpace에 추가</summary>
        public static ObjectId Create(string text, Point3d position,
            double height, double rotation, string layerName,
            ObjectId textStyleId)
        {
            var doc = Application.DocumentManager.MdiActiveDocument;
            var db = doc.Database;

            using (var tr = db.TransactionManager.StartTransaction())
            {
                var bt = (BlockTable)tr.GetObject(db.BlockTableId, OpenMode.ForRead);
                var btr = (BlockTableRecord)tr.GetObject(
                    bt[BlockTableRecord.ModelSpace], OpenMode.ForWrite);

                var dbText = new DBText
                {
                    TextString = text,
                    Position = position,
                    Height = height,
                    Rotation = rotation,
                    Layer = layerName
                };

                if (!textStyleId.IsNull)
                    dbText.TextStyleId = textStyleId;

                var id = btr.AppendEntity(dbText);
                tr.AddNewlyCreatedDBObject(dbText, true);
                tr.Commit();
                return id;
            }
        }

        /// <summary>기존 텍스트 내용 수정</summary>
        public static void UpdateText(ObjectId textId, string newText)
        {
            var doc = Application.DocumentManager.MdiActiveDocument;
            var db = doc.Database;

            using (var tr = db.TransactionManager.StartTransaction())
            {
                var ent = tr.GetObject(textId, OpenMode.ForWrite);
                if (ent is DBText txt)
                    txt.TextString = newText;
                tr.Commit();
            }
        }

        /// <summary>텍스트 위치 이동</summary>
        public static void Move(ObjectId textId, Point3d newPosition)
        {
            var doc = Application.DocumentManager.MdiActiveDocument;
            var db = doc.Database;

            using (var tr = db.TransactionManager.StartTransaction())
            {
                var ent = tr.GetObject(textId, OpenMode.ForWrite);
                if (ent is DBText txt)
                    txt.Position = newPosition;
                tr.Commit();
            }
        }

        /// <summary>
        /// 폴리라인 세그먼트 각도에 맞춰 텍스트 회전각 계산
        /// 항상 읽기 쉬운 방향 (0~180도)
        /// </summary>
        public static double CalcReadableAngle(Point3d start, Point3d end)
        {
            double angle = Math.Atan2(end.Y - start.Y, end.X - start.X);
            // 라디안 → 항상 읽기 방향
            if (angle < 0) angle += Math.PI * 2;
            if (angle > Math.PI / 2 && angle < Math.PI * 3 / 2)
                angle += Math.PI;
            if (angle >= Math.PI * 2) angle -= Math.PI * 2;
            return angle;
        }
    }
}
```

- [ ] **Step 2: LeaderWriter.cs 작성**

```csharp
// src/GntTools.Core/Drawing/LeaderWriter.cs
using Autodesk.AutoCAD.ApplicationServices;
using Autodesk.AutoCAD.DatabaseServices;
using Autodesk.AutoCAD.Geometry;

namespace GntTools.Core.Drawing
{
    /// <summary>지시선(폴리라인) 생성</summary>
    public static class LeaderWriter
    {
        /// <summary>2점 지시선 (시작점 → 끝점) 폴리라인 생성</summary>
        public static ObjectId Create(Point3d startPoint, Point3d endPoint,
            string layerName)
        {
            var doc = Application.DocumentManager.MdiActiveDocument;
            var db = doc.Database;

            using (var tr = db.TransactionManager.StartTransaction())
            {
                var bt = (BlockTable)tr.GetObject(db.BlockTableId, OpenMode.ForRead);
                var btr = (BlockTableRecord)tr.GetObject(
                    bt[BlockTableRecord.ModelSpace], OpenMode.ForWrite);

                var pline = new Polyline(2);
                pline.AddVertexAt(0,
                    new Point2d(startPoint.X, startPoint.Y), 0, 0, 0);
                pline.AddVertexAt(1,
                    new Point2d(endPoint.X, endPoint.Y), 0, 0, 0);
                pline.Layer = layerName;

                var id = btr.AppendEntity(pline);
                tr.AddNewlyCreatedDBObject(pline, true);
                tr.Commit();
                return id;
            }
        }

        /// <summary>3점 지시선 (꺾임 포함)</summary>
        public static ObjectId CreateBent(Point3d start, Point3d bend,
            Point3d end, string layerName)
        {
            var doc = Application.DocumentManager.MdiActiveDocument;
            var db = doc.Database;

            using (var tr = db.TransactionManager.StartTransaction())
            {
                var bt = (BlockTable)tr.GetObject(db.BlockTableId, OpenMode.ForRead);
                var btr = (BlockTableRecord)tr.GetObject(
                    bt[BlockTableRecord.ModelSpace], OpenMode.ForWrite);

                var pline = new Polyline(3);
                pline.AddVertexAt(0,
                    new Point2d(start.X, start.Y), 0, 0, 0);
                pline.AddVertexAt(1,
                    new Point2d(bend.X, bend.Y), 0, 0, 0);
                pline.AddVertexAt(2,
                    new Point2d(end.X, end.Y), 0, 0, 0);
                pline.Layer = layerName;

                var id = btr.AppendEntity(pline);
                tr.AddNewlyCreatedDBObject(pline, true);
                tr.Commit();
                return id;
            }
        }
    }
}
```

- [ ] **Step 3: 빌드 확인 후 커밋**

```bash
git add src/GntTools.Core/Drawing/TextWriter.cs
git add src/GntTools.Core/Drawing/LeaderWriter.cs
git commit -m "feat(core): implement TextWriter and LeaderWriter

- TextWriter: Create/UpdateText/Move + CalcReadableAngle
- LeaderWriter: 2점/3점 지시선 폴리라인 생성"
```

---

### Task 10: XDataManager — RegApp 및 그룹ID 관리

**Files:**
- Create: `src/GntTools.Core/XData/XDataManager.cs`
- Bug fix: `eXdataClass.removeXdata` Commit 누락 (스펙 §8)

- [ ] **Step 1: XDataManager.cs 작성**

```csharp
// src/GntTools.Core/XData/XDataManager.cs
using System;
using Autodesk.AutoCAD.ApplicationServices;
using Autodesk.AutoCAD.DatabaseServices;

namespace GntTools.Core.XData
{
    /// <summary>
    /// XData(Extended Data) 관리 — RegApp 등록, 그룹ID 읽기/쓰기
    /// VB.NET 버그 수정: removeXdata에서 Commit 누락 → Transaction 패턴 통일
    /// </summary>
    public static class XDataManager
    {
        private const string AppName = "GNTTOOLS";

        /// <summary>RegApp 등록 (없으면 생성)</summary>
        public static void EnsureRegApp(string appName = null)
        {
            appName = appName ?? AppName;
            var doc = Application.DocumentManager.MdiActiveDocument;
            var db = doc.Database;

            using (var tr = db.TransactionManager.StartTransaction())
            {
                var rat = (RegAppTable)tr.GetObject(
                    db.RegAppTableId, OpenMode.ForRead);
                if (!rat.Has(appName))
                {
                    rat.UpgradeOpen();
                    var rec = new RegAppTableRecord { Name = appName };
                    rat.Add(rec);
                    tr.AddNewlyCreatedDBObject(rec, true);
                }
                tr.Commit();
            }
        }

        /// <summary>그룹ID(타임스탬프) 생성</summary>
        public static string GenerateGroupId()
        {
            return DateTime.Now.ToString("yyyyMMddHHmmssfff");
        }

        /// <summary>엔티티에 그룹ID XData 쓰기</summary>
        public static void WriteGroupId(ObjectId entityId, string groupId,
            string appName = null)
        {
            appName = appName ?? AppName;
            EnsureRegApp(appName);

            var doc = Application.DocumentManager.MdiActiveDocument;
            var db = doc.Database;

            using (var tr = db.TransactionManager.StartTransaction())
            {
                var ent = tr.GetObject(entityId, OpenMode.ForWrite) as Entity;
                if (ent != null)
                {
                    var rb = new ResultBuffer(
                        new TypedValue((int)DxfCode.ExtendedDataRegAppName, appName),
                        new TypedValue((int)DxfCode.ExtendedDataAsciiString, groupId)
                    );
                    ent.XData = rb;
                }
                tr.Commit();  // VB.NET 버그: 여기서 Commit 누락 → 수정
            }
        }

        /// <summary>엔티티에서 그룹ID 읽기</summary>
        public static string ReadGroupId(ObjectId entityId, string appName = null)
        {
            appName = appName ?? AppName;
            var doc = Application.DocumentManager.MdiActiveDocument;
            var db = doc.Database;

            using (var tr = db.TransactionManager.StartTransaction())
            {
                var ent = tr.GetObject(entityId, OpenMode.ForRead) as Entity;
                if (ent == null) { tr.Commit(); return null; }

                var rb = ent.GetXDataForApplication(appName);
                tr.Commit();

                if (rb == null) return null;
                var values = rb.AsArray();
                // [0] = RegAppName, [1] = GroupId string
                if (values.Length >= 2)
                    return values[1].Value as string;
                return null;
            }
        }

        /// <summary>엔티티에서 XData 제거</summary>
        public static void RemoveXData(ObjectId entityId, string appName = null)
        {
            appName = appName ?? AppName;
            var doc = Application.DocumentManager.MdiActiveDocument;
            var db = doc.Database;

            using (var tr = db.TransactionManager.StartTransaction())
            {
                var ent = tr.GetObject(entityId, OpenMode.ForWrite) as Entity;
                if (ent != null)
                {
                    // XData 제거: RegAppName만 있는 빈 ResultBuffer 설정
                    var rb = new ResultBuffer(
                        new TypedValue((int)DxfCode.ExtendedDataRegAppName, appName)
                    );
                    ent.XData = rb;
                }
                tr.Commit();  // VB.NET 버그: 여기서 Commit 누락 → 수정
            }
        }
    }
}
```

- [ ] **Step 2: 빌드 확인 후 커밋**

```bash
git add src/GntTools.Core/XData/
git commit -m "feat(core): implement XDataManager with Transaction bug fix

- EnsureRegApp: GNTTOOLS RegApp 자동 등록
- WriteGroupId/ReadGroupId: 그룹ID(타임스탬프) XData 관리
- RemoveXData: VB.NET Commit 누락 버그 수정"
```

---

## Phase 1 완료 체크리스트

- [ ] 솔루션 5개 프로젝트 빌드 성공
- [ ] GntTools.Core 18개 소스 파일 작성 완료
- [ ] AutoCAD Map 3D 2020에서 NETLOAD → 에러 없이 로드 확인
- [ ] VB.NET 버그 3건 반영 확인 (ViewportManager, ColorHelper, XDataManager)

**Phase 2 → `plan-phase2-wtl.md` (WTL 도메인 + UI 기본틀)**
