# AutoCAD 2020+ PaletteSet - C# API Quick Reference

> **Namespace:** `Autodesk.AutoCAD.Windows`
> **Assembly:** `AcMgd.dll` (Copy Local = false)
> **원본 참고:** AU 2007 CP205-2 "Creating a Docking Palette" + ObjectARX 2020 CHM

---

## 1. 핵심 클래스

```csharp
// PaletteSet : Window, ICollection
// AduiPaletteSet ObjectARX 래퍼
// AutoCAD 2009+ 부터 sealed 해제 → 상속 가능
using Autodesk.AutoCAD.Windows;
```

| 클래스 | 설명 |
|--------|------|
| `PaletteSet` | 도킹 가능한 팔레트 컨테이너 (탭으로 여러 팔레트 보유) |
| `Palette` | 개별 팔레트 (PaletteSet.Add()의 반환값) |
| `PaletteSetStyles` | Style 플래그 (열거형, 비트 OR 결합) |
| `DockSides` | 도킹 위치 (None, Left, Top, Right, Bottom) |
| `PaletteSetTitleBarLocation` | 타이틀바 위치 (Left, Right) |

---

## 2. 기본 생성 패턴

```csharp
// === 싱글 인스턴스 패턴 (필수) ===
private static PaletteSet s_ps = null;

[CommandMethod("SHOW_PALETTE")]
public void ShowPalette()
{
    if (s_ps == null)
    {
        // GUID로 사용자 설정 영구 저장 (위치, 크기, 도킹 상태)
        s_ps = new PaletteSet("My Tools",
            new Guid("ECBFEC73-9FE4-4aa2-8E4B-3068E94A2BFA"));

        // WinForms UserControl 추가 (탭)
        var uc1 = new MyUserControl1();
        s_ps.Add("일반", uc1);

        // WPF UserControl 추가 (2020+)
        var wpf1 = new MyWpfControl();
        s_ps.AddVisual("설정", wpf1);

        // 기본 설정
        s_ps.MinimumSize = new System.Drawing.Size(250, 200);
        s_ps.DockEnabled = DockSides.Left | DockSides.Right;
        s_ps.Style = PaletteSetStyles.ShowAutoHideButton
                   | PaletteSetStyles.ShowCloseButton
                   | PaletteSetStyles.ShowPropertiesMenu;
    }
    s_ps.Visible = true;
}
```

---

## 3. 생성자

```csharp
// 이름만 (설정 저장 안 됨)
new PaletteSet("My Palette");
// 이름 + GUID (사용자 설정 영구 저장 — 실무에서 반드시 사용)
new PaletteSet("My Palette", new Guid("{ECBFEC73-9FE4-4aa2-8E4B-3068E94A2BFA}"));
```

---

## 4. 주요 프로퍼티

### 핵심

| 프로퍼티 | 타입 | R/W | 설명 |
|----------|------|-----|------|
| `Visible` | bool | R/W | 표시/숨김 |
| `Name` | string | R/W | 팔레트셋 이름 |
| `Style` | PaletteSetStyles | R/W | 스타일 플래그 (비트 OR) |
| `Count` | int | R/O | 팔레트 개수 |

### 도킹

| 프로퍼티 | 타입 | R/W | 설명 |
|----------|------|-----|------|
| `Dock` | DockSides | **R/O** | 현재 도킹 위치 (읽기 전용) |
| `DockEnabled` | DockSides | R/W | 허용되는 도킹 위치 (플래그 결합) |
| `Anchored` | DockSides | R/O | 앵커 상태 |

### 크기/위치

| 프로퍼티 | 타입 | R/W | 설명 |
|----------|------|-----|------|
| `Size` | System.Drawing.Size | R/W | 실제 크기 |
| `MinimumSize` | System.Drawing.Size | R/W | 최소 크기 |
| `Location` | System.Drawing.Point | R/W | 위치 |
| `DeviceIndependentSize` | — | R/W | DPI 인식 크기 (고해상도용) |

### 동작

| 프로퍼티 | 타입 | R/W | 설명 |
|----------|------|-----|------|
| `Opacity` | int | R/W | 투명도 (0~100) |
| `AutoRollUp` | bool | R/W | 자동 접기 활성화 |
| `RolledUp` | bool | R/W | 현재 접힌 상태 |
| `KeepFocus` | bool | R/W | 포커스 유지 (ComboBox 워크어라운드) |
| `TitleBarLocation` | PaletteSetTitleBarLocation | R/W | 타이틀바 위치 (Left/Right) |

### 아이콘

| 프로퍼티 | 타입 | 설명 |
|----------|------|------|
| `Icon` | System.Drawing.Icon | 기본 아이콘 |
| `DarkThemedIcon` | System.Drawing.Icon | 다크 테마 16×16 |
| `LightThemedIcon` | System.Drawing.Icon | 라이트 테마 16×16 |
| `LargeDarkThemedIcon` | System.Drawing.Icon | 다크 테마 32×32 |
| `LargeLightThemedIcon` | System.Drawing.Icon | 라이트 테마 32×32 |

---

## 5. 주요 메서드

```csharp
// 팔레트 추가
Palette p1 = ps.Add("탭이름", winFormsControl);     // WinForms
Palette p2 = ps.AddVisual("탭이름", wpfVisual);      // WPF (2020+)
Palette p3 = ps.AddVisual("탭이름", wpfVisual, true); // WPF + 투명 배경

// 팔레트 제거 / 활성화
ps.Remove(index);
ps.Activate(index);         // 탭 전환

// 투명도
ps.EnableTransparency(true);

// 테마 아이콘
ps.SetThemedIcon(icon, ColorThemeEnum.Dark);
Icon themed = ps.GetThemedIcon(false); // false=16x16, true=32x32

// 컬렉션
ps.GetEnumerator();
ps.CopyTo(paletteArray, 0);
```

---

## 6. 이벤트

| 이벤트 | 설명 |
|--------|------|
| `Load` | XML에서 설정 로드 시 (GUID 필요) |
| `Save` | XML에 설정 저장 시 (GUID 필요) |
| `PaletteActivated` | 탭 전환 시 |
| `StateChanged` | 상태 변경 (도킹, 크기 등) |
| `SizeChanged` | 크기 변경 시 |
| `PaletteSetHostMoved` | 위치 이동 후 |
| `PaletteSetEnterSizeMove` | 리사이즈 시작 시 |
| `PaletteAddContextMenu` | 컨텍스트 메뉴 요청 시 |
| `Focused` | 포커스 수신 시 |

### 설정 저장/복원 (Load/Save)

```csharp
s_ps = new PaletteSet("My Tools", new Guid("{...}"));
s_ps.Load += OnPaletteLoad;
s_ps.Save += OnPaletteSave;

private void OnPaletteLoad(object sender, PalettePersistEventArgs e)
{
    // 저장된 사용자 데이터 읽기
    double val = (double)e.ConfigurationSection.ReadProperty("MyKey", 0.0);
}

private void OnPaletteSave(object sender, PalettePersistEventArgs e)
{
    // 사용자 데이터 저장
    e.ConfigurationSection.WriteProperty("MyKey", 42.0);
}
```

---

## 7. 열거형

### DockSides

```csharp
public enum DockSides {
    None   = 0x0000,   // 플로팅
    Left   = 0x1000,
    Top    = 0x2000,
    Right  = 0x4000,
    Bottom = 0x8000
}
// 결합 가능: DockSides.Left | DockSides.Right
```

### PaletteSetStyles

```csharp
public enum PaletteSetStyles {
    ShowAutoHideButton  = 0x0002,   // 자동 숨김 버튼
    ShowPropertiesMenu  = 0x0004,   // 속성 메뉴
    ShowCloseButton     = 0x0008,   // 닫기 버튼
    NameEditable        = 0x0010,   // 이름 편집 가능
    Snappable           = 0x0020,   // 다른 팔레트셋에 스냅
    ShowTabForSingle    = 0x0040,   // 단일 탭도 표시
    UsePaletteNameAsTitleForSingle = 0x0080,
    SingleRowDock       = 0x0200,   // 단일 행 도킹
    Notify              = 0x0400,
    SingleRowNoVertResize = 0x0800,
    SingleColDock       = 0x1000,   // 단일 열 도킹
    NoTitleBar          = 0x8000,   // 타이틀바 숨김
    PauseAutoRollupForChildModalDialog = 0x10000
}
```

---

## 8. 실무 패턴

### Cross-Communication (팔레트 간 통신)

```csharp
// UserControl 생성자에 PaletteSet 참조 전달
public class MyUserControl : UserControl
{
    private PaletteSet _host;

    public MyUserControl(PaletteSet host)
    {
        InitializeComponent();
        _host = host;
    }

    // 다른 팔레트 접근: _host[index] 또는 직접 참조
}
```

### ComboBox 포커스 문제 해결 (도킹 상태)

```csharp
// ComboBox가 도킹 상태에서 드롭다운이 안 되는 문제
private void ComboBox_DropDown(object sender, EventArgs e)
{
    if (s_ps.Dock != DockSides.None)
        s_ps.KeepFocus = true;  // 포커스 강제 유지
}

private void ComboBox_DropDownClosed(object sender, EventArgs e)
{
    if (s_ps.Dock != DockSides.None)
        s_ps.KeepFocus = false; // 원복
}
```

### AutoRollUp (자동 접기) 패턴

```csharp
// 도킹 상태에서는 AutoRollUp 불가 → Undock 후 설정
private void DoAutoRollUp()
{
    if (s_ps.Dock == DockSides.None)
    {
        // 이미 플로팅 — 직접 설정
        s_ps.AutoRollUp = true;
        s_ps.Visible = false;
        s_ps.Visible = true;  // 토글로 리프레시
    }
    else
    {
        // 도킹 상태 — undock 후 설정
        s_ps.Dock = DockSides.None;
        s_ps.AutoRollUp = true;
        s_ps.Visible = false;
        s_ps.Visible = true;
    }
}
```

### 버튼 클릭 → 도면 상호작용 (플로팅 시 AutoRollUp)

```csharp
private void btnSelect_Click(object sender, EventArgs e)
{
    bool rolledUp = false;
    // 플로팅 팔레트를 접어서 도면 영역 확보
    if (s_ps.Dock == DockSides.None)
    {
        s_ps.AutoRollUp = true;
        s_ps.Visible = false;
        s_ps.Visible = true;
        rolledUp = true;
    }

    // Editor 상호작용
    Editor ed = Application.DocumentManager.MdiActiveDocument.Editor;
    PromptPointResult ppr = ed.GetPoint("\n점 선택: ");
    if (ppr.Status == PromptStatus.OK)
    {
        txtX.Text = ppr.Value.X.ToString("F3");
        txtY.Text = ppr.Value.Y.ToString("F3");
    }

    // 복원
    if (rolledUp)
    {
        s_ps.AutoRollUp = false;
        s_ps.Visible = false;
        s_ps.Visible = true;
    }
}
```

### Drag-and-Drop (팔레트 → 도면)

```csharp
// 1. DropTarget 상속 클래스 생성
public class MyDropTarget : Autodesk.AutoCAD.Windows.DropTarget
{
    private static string _droppedData;

    public override void OnDrop(DragEventArgs e)
    {
        _droppedData = (string)e.Data.GetData(typeof(string));
        // 직접 도면 수정 금지 → 명령으로 위임
        Application.DocumentManager.MdiActiveDocument
            .SendStringToExecute("MYDROP\n", false, false, false);
    }

    [CommandMethod("MYDROP")]
    public static void MyDropCmd()
    {
        if (_droppedData != null)
        {
            // 여기서 도면 수정 (Transaction 내에서)
            _droppedData = null;
        }
    }
}

// 2. UserControl에서 DoDragDrop 호출
private void txtSource_MouseMove(object sender, MouseEventArgs e)
{
    if (e.Button == MouseButtons.Left)
    {
        using (DocumentLock dl =
            Application.DocumentManager.MdiActiveDocument.LockDocument())
        {
            Application.DoDragDrop(this, txtSource.Text,
                DragDropEffects.All, new MyDropTarget());
        }
    }
}
```

### Active Drawing 추적

```csharp
// 도면 전환 시 팔레트 데이터 동기화
Application.DocumentManager.DocumentActivated += (s, e) =>
{
    // 활성 도면 변경됨 → UI 업데이트
    UpdatePaletteForDocument(e.Document);
};

Application.DocumentManager.DocumentToBeDeactivated += (s, e) =>
{
    // 현재 도면 비활성화 전 → 데이터 저장
    SavePaletteDataForDocument(e.Document);
};
```

### AutoLoading (레지스트리)

```
; AutoCAD 2020 레지스트리 자동 로딩
[HKEY_LOCAL_MACHINE\SOFTWARE\Autodesk\AutoCAD\R23.1\ACAD-2001:409\Applications\MyApp]
"DESCRIPTION"="My Tool Palette App"
"LOADER"="C:\\Path\\To\\MyApp.dll"
"LOADCTRLS"=dword:00000002   ; 2 = AutoCAD 시작 시 자동 로드
"MANAGED"=dword:00000001     ; 1 = .NET 관리 어셈블리
```

---

## 9. WPF 팔레트 (2020+)

```csharp
// WPF UserControl을 PaletteSet에 추가
var wpfControl = new MyWpfUserControl();
s_ps.AddVisual("WPF Tab", wpfControl);        // 기본
s_ps.AddVisual("WPF Tab", wpfControl, true);   // 투명 배경

// WPF UserControl 내에서 AutoCAD 접근
// Dispatcher 주의: AutoCAD API는 메인 스레드에서만 호출
Application.Current.Dispatcher.Invoke(() =>
{
    // AutoCAD API 호출
});
```

---

**참고 리소스**:
[PaletteSet Class (2022 API)](https://help.autodesk.com/cloudhelp/2022/ENU/OARX-ManagedRefGuide/files/OARX-ManagedRefGuide-Autodesk_AutoCAD_Windows_PaletteSet.html) |
[Pluggable PaletteSet](https://drive-cad-with-code.blogspot.com/2010/09/pluggable-paletteset.html) |
[AutoRollUp DevNote](https://adndevblog.typepad.com/autocad/2017/01/rollup-of-a-custom-docked-paletteset-using-net-api-.html) |
[AU 2007 원본 PDF](https://forums.autodesk.com/autodesk/attachments/autodesk/net-forum-en/9131/1/CP205-2_Mike_Tuersley.pdf)
