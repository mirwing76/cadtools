# Phase 5: 통합 테스트 및 배포 계획

> **For agentic workers:** REQUIRED: Use superpowers:subagent-driven-development (if subagents available) or superpowers:executing-plans to implement this plan. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** 전체 워크플로우 통합 테스트, AutoLoading 설정, 최종 배포 패키지 생성

**선행 조건:** Phase 1~4 모두 완료

**Spec:** `docs/specs/2026-03-10-gnttools-integration-design.md` §8~§9

---

## File Map

| Task | 파일 | 역할 |
|------|------|------|
| Task 1 | (빌드 확인) | 전체 솔루션 Release 빌드 |
| Task 2 | (수동 테스트) | AutoCAD에서 전체 기능 검증 |
| Task 3 | `src/GntTools.UI/Plugin.cs` | 버전 정보 + AutoLoading |
| Task 3 | `install/install.reg` | 레지스트리 자동로딩 설정 |
| Task 4 | `install/deploy.bat` | 배포 스크립트 |
| Task 5 | (코드 정리) | 미사용 코드 제거, 경고 해결 |

---

### Task 1: Release 빌드

- [ ] **Step 1: 전체 Release 빌드**

```bash
msbuild src\GntTools.sln /t:Rebuild /p:Configuration=Release
```
Expected: 5개 프로젝트 전부 Build succeeded, 0 Errors

- [ ] **Step 2: 출력 DLL 확인**

```bash
dir src\GntTools.UI\bin\Release\
```
Expected 파일:
- GntTools.Core.dll
- GntTools.Wtl.dll
- GntTools.Swl.dll
- GntTools.Kepco.dll
- GntTools.UI.dll

- [ ] **Step 3: 커밋**

```bash
git commit --allow-empty -m "build: verify Release build for all 5 projects"
```

---

### Task 2: 통합 수동 테스트

AutoCAD Map 3D 2020에서 수행.

- [ ] **Step 1: NETLOAD 및 팔레트 기본 동작**

1. AutoCAD Map 3D 2020 실행
2. NETLOAD → `GntTools.UI.dll` (Release 빌드)
3. 확인: "GntTools v1.0 로드됨" 메시지 출력
4. `GNTTOOLS_SHOW` → 팔레트 표시
5. 확인: 4탭 (상수, 하수, 전력통신, 환경설정)
6. 팔레트 도킹 (왼쪽/오른쪽) → 위치 저장
7. AutoCAD 재시작 → NETLOAD → `GNTTOOLS_SHOW` → 이전 위치 복원 확인

- [ ] **Step 2: 환경설정 테스트**

1. 환경설정 탭 → 텍스트 크기 2.0으로 변경 → [저장]
2. `%AppData%\GntTools\settings.json` 파일 생성 확인
3. AutoCAD 재시작 → 설정값 유지 확인

- [ ] **Step 3: WTL 상수 워크플로우**

테스트 도면에 폴리라인 그리기 (WS_DEP 레이어에 심도 텍스트 배치)

1. 상수 탭 → 설치년도: 2024, 관재질: PE, 구경: 200
2. 수동입력 → 시점심도 1.2, 종점심도 0.9
3. [신규입력] → 폴리라인 선택
4. 지시선 끝점 지정 → 라벨 생성 확인
5. 확인: PIPE_COMMON 레코드 부착 (MAPQDL로 확인)
6. 확인: WTL_EXT 레코드 부착
7. 확인: 심도 텍스트 생성 (시점 1.2, 종점 0.9)
8. 확인: 폴리라인 색상 변경 (노란색)

9. CLI: `GNTTOOLS_WTL_ATT` → 동일 동작 확인
10. CLI: `GNTTOOLS_WTL_EDIT` → 수정 동작 확인

- [ ] **Step 4: SWL 하수 워크플로우**

1. 하수 탭 → 용도코드: 01, HP, 300
2. 수동입력 → 시점심도 2.0, 종점심도 1.5
3. 시점지반고 30.0, 종점지반고 29.5
4. [표고 미리보기] → 관저고/구배 표시 확인
5. [신규입력] → 폴리라인 선택 → 라벨 생성
6. 확인: SWL_EXT 레코드 (관저고, 관상고, 구배 자동계산)
7. CLI: `GNTTOOLS_SWL_ATT` → 동일 동작

8. 박스관 테스트:
   - 박스관 체크 → 가로 1200, 세로 800
   - [신규입력] → 라벨 "HP □1200x800 L=..." 확인

- [ ] **Step 5: KEPCO 전력통신 워크플로우**

테스트 도면에 단면도 (원, 해치) 준비

1. 전력 탭 → [단면선택] → 크로싱 윈도우로 단면 선택
2. 확인: D200, D150 등 카운팅 결과 표시
3. [BxH 선택] → B선, H선 선택 → BxH 값 표시
4. [신규입력] → 관로 폴리라인 선택 → 라벨 생성
5. 확인: KEPCO_EXT.PIPDAT JSON 형식 (MAPQDL)
6. 확인: KEPCO_EXT.BXH 값

7. 오류검증:
   - [오류검증] → 관로 여러 개 선택
   - 확인: 분기점 불일치 에러 정확 표시
   - CLI: `GNTTOOLS_KEPCO_CHK` → 동일 동작

- [ ] **Step 6: 불탐 테스트 (전체 도메인)**

1. 각 도메인에서 불탐 체크 후 [신규입력]
2. 확인: BTCDE = "Y", 심도값 = 0.0
3. 확인: 심도 텍스트 미생성

- [ ] **Step 7: SHP 내보내기 테스트**

1. Map → 내보내기 → SHP
2. PIPE_COMMON + WTL_EXT 두 테이블 선택
3. 확인: 필드명 중복 없이 합산된 SHP 생성
4. PIPE_COMMON + SWL_EXT → 동일 확인
5. PIPE_COMMON + KEPCO_EXT → 동일 확인

---

### Task 3: AutoLoading 및 버전 정보

**Files:**
- Modify: `src/GntTools.UI/Plugin.cs` (버전 정보 추가)
- Create: `install/install.reg` (레지스트리)

- [ ] **Step 1: Plugin.cs 버전 정보**

```csharp
// Plugin.cs Initialize() 수정:
var ver = System.Reflection.Assembly.GetExecutingAssembly().GetName().Version;
ed.WriteMessage($"\nGntTools v{ver.Major}.{ver.Minor}.{ver.Build} 로드됨. " +
    "GNTTOOLS_SHOW로 팔레트를 엽니다.\n");
```

`AssemblyInfo.cs`에 버전:
```csharp
[assembly: AssemblyVersion("1.0.0.0")]
[assembly: AssemblyFileVersion("1.0.0.0")]
[assembly: AssemblyProduct("GntTools")]
[assembly: AssemblyDescription("AutoCAD Map 3D 관로 속성입력 도구")]
```

- [ ] **Step 2: install.reg 작성**

```reg
; install/install.reg
; AutoCAD Map 3D 2020 자동 로딩 레지스트리
Windows Registry Editor Version 5.00

[HKEY_LOCAL_MACHINE\SOFTWARE\Autodesk\AutoCAD\R23.1\ACAD-2001:409\Applications\GntTools]
"DESCRIPTION"="GntTools - AutoCAD Map 3D 관로 속성입력 도구"
"LOADER"="C:\\GntTools\\GntTools.UI.dll"
"LOADCTRLS"=dword:00000002
"MANAGED"=dword:00000001
```

- [ ] **Step 3: 커밋**

```bash
mkdir -p install
git add src/GntTools.UI/Plugin.cs install/install.reg
git commit -m "feat: add version info and AutoLoading registry setup

- Plugin.cs: 어셈블리 버전 표시
- install.reg: AutoCAD 2020 시작 시 자동 로드"
```

---

### Task 4: 배포 스크립트

**Files:**
- Create: `install/deploy.bat`

- [ ] **Step 1: deploy.bat 작성**

```bat
@echo off
REM install/deploy.bat
REM GntTools 배포 스크립트

set TARGET=C:\GntTools
set SOURCE=..\src\GntTools.UI\bin\Release

echo === GntTools 배포 ===
echo 대상 폴더: %TARGET%

if not exist %TARGET% mkdir %TARGET%

echo DLL 복사 중...
copy /Y %SOURCE%\GntTools.Core.dll %TARGET%\
copy /Y %SOURCE%\GntTools.Wtl.dll %TARGET%\
copy /Y %SOURCE%\GntTools.Swl.dll %TARGET%\
copy /Y %SOURCE%\GntTools.Kepco.dll %TARGET%\
copy /Y %SOURCE%\GntTools.UI.dll %TARGET%\

echo.
echo === 배포 완료 ===
echo.
echo 자동 로딩 설정:
echo   1. install.reg를 관리자 권한으로 실행
echo   2. 또는 AutoCAD에서 NETLOAD → %TARGET%\GntTools.UI.dll
echo.
pause
```

- [ ] **Step 2: 커밋**

```bash
git add install/deploy.bat
git commit -m "feat: add deployment script for GntTools"
```

---

### Task 5: 코드 정리 및 최종 커밋

- [ ] **Step 1: 컴파일 경고 확인**

```bash
msbuild src\GntTools.sln /t:Build /p:Configuration=Release /p:TreatWarningsAsErrors=false
```
경고가 있으면 수정.

- [ ] **Step 2: .gitignore 정리**

```gitignore
# src/.gitignore (추가)
bin/
obj/
*.user
*.suo
.vs/
```

- [ ] **Step 3: 최종 커밋**

```bash
git add -A
git commit -m "chore: final cleanup - resolve warnings, add .gitignore

GntTools v1.0 구현 완료:
- GntTools.Core: ODT, 선택, 심도, 도면 유틸리티 (18개 클래스)
- GntTools.Wtl: 상수관로 도메인 (5개 클래스)
- GntTools.Swl: 하수관로 도메인 + 표고계산 (6개 클래스)
- GntTools.Kepco: 전력관로 + 단면카운팅 + 오류검증 (8개 클래스)
- GntTools.UI: PaletteSet 4탭 + MVVM (12개 파일)
- CLI 명령 9개 병행 지원
- VB.NET 버그 10건 수정"
```

---

## Phase 5 완료 체크리스트

- [ ] Release 빌드 성공 (5 DLL)
- [ ] 전체 수동 테스트 통과
  - [ ] NETLOAD + 팔레트 4탭
  - [ ] 환경설정 저장/복원
  - [ ] WTL: 신규입력/수정, CLI, 자동/수동/불탐
  - [ ] SWL: 원형관/박스관, 관저고/구배, CLI
  - [ ] KEPCO: 단면카운팅, BxH, 오류검증, CLI
  - [ ] SHP 내보내기 (3개 도메인)
- [ ] AutoLoading 레지스트리 설정
- [ ] 배포 스크립트 동작
- [ ] 컴파일 경고 0건

---

## 전체 프로젝트 요약

| 항목 | 수량 |
|------|------|
| 프로젝트 | 5개 (Core, Wtl, Swl, Kepco, UI) |
| C# 소스 파일 | ~45개 |
| XAML 파일 | 4개 (WtlPanel, SwlPanel, KepcoPanel, SettingsPanel) |
| ODT 테이블 | 4개 (PIPE_COMMON, WTL_EXT, SWL_EXT, KEPCO_EXT) |
| CLI 명령 | 9개 |
| VB.NET 버그 수정 | 10건 |
| PaletteSet 탭 | 4개 (상수, 하수, 전력통신, 환경설정) |
