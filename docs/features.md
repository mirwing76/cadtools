# GntTools 기능 목록

프로젝트에서 구현된 기능을 차곡차곡 기록합니다.

---

## 1. 블록 브라우저 (Block Browser)

- **명령어**: `GNTBLOCKS`
- **설명**: 현재 도면의 블록 정의를 탐색하고, 더블클릭으로 즉시 삽입
- **주요 기능**:
  - 블록 geometry를 WPF로 직접 렌더링한 썸네일
  - 그리드/리스트 뷰 전환
  - 수동 새로고침
  - 별도 PaletteSet (기존 GntTools 팔레트와 독립)
- **파일**: `src/GntTools.UI/BlockBrowser/`
- **구현일**: 2026-03-24
- **비고**: AutoCAD 내장 블록 팔레트 성능 저하 버그 우회용
