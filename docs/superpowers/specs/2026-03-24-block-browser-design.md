# Block Browser Design Spec

## 개요

AutoCAD 내장 블록 팔레트의 성능 저하 버그를 우회하기 위해, 현재 도면의 블록을 탐색하고 삽입할 수 있는 커스텀 블록 브라우저를 구현한다.

## 요구사항

- 현재 도면의 블록 정의를 목록으로 표시
- 블록 geometry를 WPF Canvas에 직접 렌더링하여 썸네일 생성
- 더블클릭으로 즉시 삽입점 지정 모드 진입
- 그리드/리스트 뷰 전환
- 수동 새로고침으로 목록 갱신
- 별도 PaletteSet (기존 GntTools 팔레트와 독립)

## 접근 방식

**A안 채택**: GntTools.UI 프로젝트에 직접 추가. 기존 MVVM 인프라(ViewModelBase, RelayCommand) 재활용.

## 파일 구성

GntTools.UI 프로젝트 내 `BlockBrowser/` 폴더 (네임스페이스: `GntTools.UI.BlockBrowser`):

| 파일 | 역할 |
|------|------|
| `BlockBrowserPaletteManager.cs` | 별도 PaletteSet 싱글톤 관리 |
| `BlockBrowserPanel.xaml(.cs)` | WPF UserControl — 툴바, 블록 목록 |
| `BlockBrowserViewModel.cs` | 블록 목록 로드, 뷰 전환, 삽입 명령 |
| `BlockThumbnailRenderer.cs` | BlockTableRecord geometry → WPF DrawingImage 변환 |
| `BlockItem.cs` | DTO — 블록 이름, ObjectId, 썸네일(ImageSource) |

기존 파일 수정:
- `PaletteCommands.cs` — `GNTBLOCKS` 명령 추가
- `GntTools.UI.csproj` — 새 파일들의 Compile/Page 항목 추가

## 썸네일 렌더링

`BlockThumbnailRenderer`가 BlockTableRecord의 엔티티를 순회하여 WPF DrawingImage로 변환.

### 지원 엔티티
- Line → DrawingContext.DrawLine
- Polyline/Polyline2d/Polyline3d → DrawLine (직선 세그먼트), bulge 있는 세그먼트는 chord(현)으로 근사
- Circle → DrawingContext.DrawEllipse
- Arc → StreamGeometry + ArcSegment (center/radius/angles → startPoint/endpoint/size/sweepDirection 변환)
- Ellipse → DrawEllipse (축 정렬만, 회전/부분 타원은 근사)
- BlockReference (중첩 블록) → 재귀 확장하여 렌더링

### 변환 로직
1. 블록 내 모든 엔티티의 GeometricExtents로 바운딩박스 계산 (eInvalidExtents 예외 시 플레이스홀더 이미지 반환)
2. 썸네일 크기(80×80 WPF DIU, DPI 독립)에 맞게 uniform scale + center 변환
3. AutoCAD Color → WPF Brush: ACI(0-255) → 고정 RGB 룩업테이블, TrueColor → 직접 RGB 변환
4. ByLayer → LayerTableRecord에서 색상 조회, ByBlock → 흰색 폴백
5. 렌더링 가능한 엔티티가 없는 블록 → "No Preview" 플레이스홀더 이미지 표시

### 캐싱
- `Dictionary<ObjectId, DrawingImage>` — 한 번 렌더링 후 재사용
- 새로고침 시 캐시 전체 클리어

### 미지원 엔티티
- Hatch, Solid, Region 등은 무시 (외곽선만으로 식별 충분)

## UI 레이아웃

```
┌─ 블록 브라우저 ──────────────┐
│ [새로고침] [그리드|리스트]      │
├──────────────────────────────┤
│                              │
│  (블록 썸네일 + 이름 목록)     │
│                              │
├──────────────────────────────┤
│ 블록 수: N개                  │
└──────────────────────────────┘
```

### 그리드 모드
- WrapPanel 안에 80×100 크기 아이템 (80×80 썸네일 + 이름)
- ScrollViewer로 스크롤

### 리스트 모드
- StackPanel에 40×40 썸네일 + 블록 이름 가로 배치
- ScrollViewer로 스크롤

### 인터랙션
- **더블클릭**: 해당 블록 삽입점 지정 모드 진입 (Editor.GetPoint → BlockReference 생성)
- **필터링**: 익명 블록(`*` prefix), 레이아웃 블록(`*Model_Space`, `*Paper_Space`) 자동 제외

### PaletteSet
- 별도 GUID
- 최소 크기: 250×300
- 스타일: ShowAutoHideButton | ShowCloseButton
- 도킹: Left/Right

## 삽입 로직

1. 사용자가 블록 더블클릭
2. `doc.LockDocument()` 로 문서 잠금 (PaletteSet은 Application 컨텍스트에서 실행되므로 필수)
3. `Editor.GetPoint()` 로 삽입점 요청
4. `PromptStatus.OK` 확인 (Cancel/Escape 시 중단)
5. Transaction 내에서 `BlockReference` 생성 (Scale 1:1:1, Rotation 0, 현재 레이어)
6. ModelSpace에 AppendEntity
7. Transaction Commit

### 스레드 제약
- 블록 목록 로드/썸네일 렌더링은 AutoCAD 메인 스레드에서 실행 (Application.DocumentManager.MdiActiveDocument 접근)
- 백그라운드 스레드 사용 금지

### PaletteSet 수명주기
- `GNTBLOCKS` 첫 호출: PaletteSet 초기화 + 표시
- 이후 호출: Visible 토글
- 도면 전환 시: 블록 목록 자동 갱신 안 함 (수동 새로고침 필요)

## 명령

| 명령 | 설명 |
|------|------|
| `GNTBLOCKS` | 블록 브라우저 PaletteSet 토글 (표시/숨김) |

## 제약사항

- .NET Framework 4.7
- AutoCAD Map 3D 2020+ API
- 외부 DWG 파일 참조 미지원 (현재 도면만)
- 블록 속성(Attribute) 편집 미지원 (삽입만)
