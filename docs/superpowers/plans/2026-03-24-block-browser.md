# Block Browser Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** AutoCAD 내장 블록 팔레트의 성능 저하를 우회하는 커스텀 블록 브라우저 — 현재 도면 블록을 WPF로 직접 렌더링하여 썸네일 표시, 더블클릭으로 삽입.

**Architecture:** GntTools.UI 프로젝트 내 `BlockBrowser/` 폴더에 MVVM 패턴으로 구현. 기존 ViewModelBase, RelayCommand 재활용. 별도 PaletteSet으로 독립 운용.

**Tech Stack:** C# .NET Framework 4.7, WPF (DrawingContext/StreamGeometry), AutoCAD Map 3D 2020+ Managed API

**Spec:** `docs/superpowers/specs/2026-03-24-block-browser-design.md`

---

## File Map

| Action | Path | Responsibility |
|--------|------|----------------|
| Create | `src/GntTools.UI/BlockBrowser/BlockItem.cs` | 블록 DTO (Name, ObjectId, Thumbnail) |
| Create | `src/GntTools.UI/BlockBrowser/AciColorTable.cs` | ACI(0-255) → RGB 룩업 테이블 |
| Create | `src/GntTools.UI/BlockBrowser/BlockThumbnailRenderer.cs` | BlockTableRecord geometry → DrawingImage |
| Create | `src/GntTools.UI/BlockBrowser/BlockBrowserViewModel.cs` | 목록 로드, 뷰 전환, 삽입 로직 |
| Create | `src/GntTools.UI/BlockBrowser/BlockBrowserPanel.xaml` | WPF UI — 툴바, 그리드/리스트, 상태바 |
| Create | `src/GntTools.UI/BlockBrowser/BlockBrowserPanel.xaml.cs` | Code-behind (더블클릭 이벤트) |
| Create | `src/GntTools.UI/BlockBrowser/BlockBrowserPaletteManager.cs` | PaletteSet 싱글톤 |
| Modify | `src/GntTools.UI/Commands/PaletteCommands.cs` | GNTBLOCKS 명령 추가 |
| Modify | `src/GntTools.UI/GntTools.UI.csproj` | 새 파일 Compile/Page 항목 |
| Create | `docs/features.md` | 기능 추적 마크다운 |

---

### Task 1: BlockItem DTO

**Files:**
- Create: `src/GntTools.UI/BlockBrowser/BlockItem.cs`

- [ ] **Step 1: Create BlockItem.cs**

```csharp
using System.Windows.Media;
using Autodesk.AutoCAD.DatabaseServices;

namespace GntTools.UI.BlockBrowser
{
    public class BlockItem
    {
        public string Name { get; set; }
        public ObjectId BlockId { get; set; }
        public ImageSource Thumbnail { get; set; }
    }
}
```

- [ ] **Step 2: Add to csproj**

`GntTools.UI.csproj`의 Compile ItemGroup에 추가:
```xml
<Compile Include="BlockBrowser\BlockItem.cs" />
```

- [ ] **Step 3: Commit**

```bash
git add src/GntTools.UI/BlockBrowser/BlockItem.cs src/GntTools.UI/GntTools.UI.csproj
git commit -m "feat(block-browser): add BlockItem DTO"
```

---

### Task 2: ACI Color Lookup Table

**Files:**
- Create: `src/GntTools.UI/BlockBrowser/AciColorTable.cs`

- [ ] **Step 1: Create AciColorTable.cs**

AutoCAD Color Index (0-255) → System.Windows.Media.Color 매핑 테이블.

```csharp
using System.Collections.Generic;
using System.Windows.Media;

namespace GntTools.UI.BlockBrowser
{
    /// <summary>AutoCAD ACI(0-255) → WPF Color 룩업 테이블</summary>
    public static class AciColorTable
    {
        private static readonly Dictionary<int, Color> _table = new Dictionary<int, Color>
        {
            { 0,   Colors.White },   // ByBlock
            { 1,   Color.FromRgb(255, 0,   0)   },  // Red
            { 2,   Color.FromRgb(255, 255, 0)   },  // Yellow
            { 3,   Color.FromRgb(0,   255, 0)   },  // Green
            { 4,   Color.FromRgb(0,   255, 255) },  // Cyan
            { 5,   Color.FromRgb(0,   0,   255) },  // Blue
            { 6,   Color.FromRgb(255, 0,   255) },  // Magenta
            { 7,   Colors.White },                    // White/Black
            { 8,   Color.FromRgb(128, 128, 128) },  // Dark gray
            { 9,   Color.FromRgb(192, 192, 192) },  // Light gray
            // 10-249: standard ACI palette (주요 색상만, 나머지는 White 폴백)
            { 10,  Color.FromRgb(255, 0,   0)   },
            { 30,  Color.FromRgb(255, 127, 0)   },
            { 40,  Color.FromRgb(255, 191, 0)   },
            { 50,  Color.FromRgb(255, 255, 0)   },
            { 70,  Color.FromRgb(127, 255, 0)   },
            { 90,  Color.FromRgb(0,   255, 0)   },
            { 110, Color.FromRgb(0,   255, 127) },
            { 130, Color.FromRgb(0,   255, 255) },
            { 150, Color.FromRgb(0,   127, 255) },
            { 170, Color.FromRgb(0,   0,   255) },
            { 190, Color.FromRgb(127, 0,   255) },
            { 210, Color.FromRgb(255, 0,   255) },
            { 230, Color.FromRgb(255, 0,   127) },
            { 250, Color.FromRgb(51,  51,  51)  },
            { 251, Color.FromRgb(80,  80,  80)  },
            { 252, Color.FromRgb(105, 105, 105) },
            { 253, Color.FromRgb(130, 130, 130) },
            { 254, Color.FromRgb(190, 190, 190) },
            { 255, Colors.White },
        };

        /// <summary>ACI → WPF Color. 미등록 인덱스는 White 반환.</summary>
        public static Color GetColor(int aci)
        {
            return _table.TryGetValue(aci, out var c) ? c : Colors.White;
        }

        /// <summary>AutoCAD Color → WPF Color (TrueColor, ACI 모두 처리)</summary>
        public static Color FromAcadColor(Autodesk.AutoCAD.Colors.Color acadColor)
        {
            if (acadColor == null) return Colors.White;
            if (acadColor.IsByLayer || acadColor.IsByBlock)
                return Colors.White;
            if (acadColor.ColorMethod == Autodesk.AutoCAD.Colors.ColorMethod.ByColor)
                return Color.FromRgb(acadColor.Red, acadColor.Green, acadColor.Blue);
            return GetColor(acadColor.ColorIndex);
        }
    }
}
```

- [ ] **Step 2: Add to csproj**

```xml
<Compile Include="BlockBrowser\AciColorTable.cs" />
```

- [ ] **Step 3: Commit**

```bash
git add src/GntTools.UI/BlockBrowser/AciColorTable.cs src/GntTools.UI/GntTools.UI.csproj
git commit -m "feat(block-browser): add ACI color lookup table"
```

---

### Task 3: BlockThumbnailRenderer

**Files:**
- Create: `src/GntTools.UI/BlockBrowser/BlockThumbnailRenderer.cs`

- [ ] **Step 1: Create BlockThumbnailRenderer.cs**

BlockTableRecord의 엔티티를 순회하여 WPF DrawingImage로 변환하는 렌더러.

핵심 구조:

```csharp
using System;
using System.Collections.Generic;
using System.Windows;
using System.Windows.Media;
using Autodesk.AutoCAD.DatabaseServices;
using Autodesk.AutoCAD.Geometry;

namespace GntTools.UI.BlockBrowser
{
    public class BlockThumbnailRenderer
    {
        private const double ThumbSize = 80.0;
        private const double Padding = 4.0;
        private readonly Dictionary<ObjectId, DrawingImage> _cache
            = new Dictionary<ObjectId, DrawingImage>();

        public void ClearCache() => _cache.Clear();

        /// <summary>블록의 썸네일 DrawingImage 생성 (캐시)</summary>
        public DrawingImage Render(BlockTableRecord btr, Transaction tr)
        {
            if (_cache.TryGetValue(btr.ObjectId, out var cached))
                return cached;

            DrawingImage result;
            try
            {
                result = RenderInternal(btr, tr);
            }
            catch
            {
                result = CreatePlaceholder();
            }
            _cache[btr.ObjectId] = result;
            return result;
        }

        private DrawingImage RenderInternal(BlockTableRecord btr, Transaction tr)
        {
            // 1. 엔티티에서 geometry 수집
            var geometries = new List<GeometryData>();
            CollectGeometries(btr, tr, Matrix3d.Identity, geometries);

            if (geometries.Count == 0)
                return CreatePlaceholder();

            // 2. 바운딩박스 계산
            double minX = double.MaxValue, minY = double.MaxValue;
            double maxX = double.MinValue, maxY = double.MinValue;
            foreach (var g in geometries)
            {
                foreach (var pt in g.Points)
                {
                    double r = (g.Type == GeomType.Circle || g.Type == GeomType.Ellipse || g.Type == GeomType.Arc)
                        ? g.Radius : 0;
                    if (pt.X - r < minX) minX = pt.X - r;
                    if (pt.Y - r < minY) minY = pt.Y - r;
                    if (pt.X + r > maxX) maxX = pt.X + r;
                    if (pt.Y + r > maxY) maxY = pt.Y + r;
                }
            }

            double w = maxX - minX;
            double h = maxY - minY;
            if (w < 1e-6 && h < 1e-6) return CreatePlaceholder();

            // 3. 스케일/센터 계산
            double drawArea = ThumbSize - Padding * 2;
            double scale = Math.Min(drawArea / Math.Max(w, 1e-6), drawArea / Math.Max(h, 1e-6));
            double cx = (minX + maxX) / 2.0;
            double cy = (minY + maxY) / 2.0;

            // 4. DrawingVisual로 렌더
            var dv = new DrawingVisual();
            using (var dc = dv.RenderOpen())
            {
                // 배경
                dc.DrawRectangle(new SolidColorBrush(Color.FromRgb(30, 30, 30)),
                    null, new Rect(0, 0, ThumbSize, ThumbSize));

                foreach (var g in geometries)
                {
                    var pen = new Pen(new SolidColorBrush(g.Color), 1.0);
                    pen.Freeze();

                    switch (g.Type)
                    {
                        case GeomType.Line:
                            dc.DrawLine(pen, ToThumb(g.Points[0], cx, cy, scale),
                                             ToThumb(g.Points[1], cx, cy, scale));
                            break;
                        case GeomType.Circle:
                            var center = ToThumb(g.Points[0], cx, cy, scale);
                            double r = g.Radius * scale;
                            dc.DrawEllipse(null, pen, center, r, r);
                            break;
                        case GeomType.Ellipse:
                            var eCenter = ToThumb(g.Points[0], cx, cy, scale);
                            double majorR = g.Radius * scale;
                            double minorR = g.MinorRadius * scale;
                            dc.DrawEllipse(null, pen, eCenter, majorR, minorR);
                            break;
                        case GeomType.Arc:
                            DrawArcGeometry(dc, pen, g, cx, cy, scale);
                            break;
                    }
                }
            }

            var img = new DrawingImage(dv.Drawing);
            img.Freeze();
            return img;
        }

        /// <summary>블록 내 엔티티 재귀 수집 (중첩 블록 포함)</summary>
        private void CollectGeometries(BlockTableRecord btr, Transaction tr,
            Matrix3d transform, List<GeometryData> result)
        {
            foreach (ObjectId entId in btr)
            {
                try
                {
                    var ent = tr.GetObject(entId, OpenMode.ForRead) as Entity;
                    if (ent == null) continue;

                    var color = ResolveColor(ent, tr);

                    if (ent is Line line)
                    {
                        var p1 = line.StartPoint.TransformBy(transform);
                        var p2 = line.EndPoint.TransformBy(transform);
                        result.Add(new GeometryData(GeomType.Line, color,
                            new[] { new Point2(p1.X, p1.Y), new Point2(p2.X, p2.Y) }));
                    }
                    else if (ent is Circle circle)
                    {
                        var c = circle.Center.TransformBy(transform);
                        result.Add(new GeometryData(GeomType.Circle, color,
                            new[] { new Point2(c.X, c.Y) }, circle.Radius));
                    }
                    else if (ent is Arc arc)
                    {
                        var c = arc.Center.TransformBy(transform);
                        result.Add(new GeometryData(GeomType.Arc, color,
                            new[] { new Point2(c.X, c.Y) }, arc.Radius,
                            arc.StartAngle, arc.EndAngle));
                    }
                    else if (ent is Polyline pl)
                    {
                        CollectPolylineSegments(pl, transform, color, result);
                    }
                    else if (ent is Polyline2d pl2d)
                    {
                        CollectPolyline2dSegments(pl2d, tr, transform, color, result);
                    }
                    else if (ent is Polyline3d pl3d)
                    {
                        CollectPolyline3dSegments(pl3d, tr, transform, color, result);
                    }
                    else if (ent is Ellipse ellipse)
                    {
                        var c = ellipse.Center.TransformBy(transform);
                        double majorR = ellipse.MajorRadius;
                        double minorR = majorR * ellipse.RadiusRatio;
                        result.Add(new GeometryData(GeomType.Ellipse, color,
                            new[] { new Point2(c.X, c.Y) }, majorR, minorR));
                    }
                    else if (ent is BlockReference blkRef)
                    {
                        var nestedBtr = tr.GetObject(blkRef.BlockTableRecord, OpenMode.ForRead)
                            as BlockTableRecord;
                        if (nestedBtr != null)
                            CollectGeometries(nestedBtr, tr, transform * blkRef.BlockTransform, result);
                    }
                }
                catch (Autodesk.AutoCAD.Runtime.Exception)
                {
                    continue; // 문제 있는 엔티티 건너뛰기
                }
            }
        }

        private void CollectPolylineSegments(Polyline pl, Matrix3d transform,
            Color color, List<GeometryData> result)
        {
            for (int i = 0; i < pl.NumberOfVertices - 1; i++)
            {
                var p1 = pl.GetPoint3dAt(i).TransformBy(transform);
                var p2 = pl.GetPoint3dAt(i + 1).TransformBy(transform);
                result.Add(new GeometryData(GeomType.Line, color,
                    new[] { new Point2(p1.X, p1.Y), new Point2(p2.X, p2.Y) }));
            }
            if (pl.Closed && pl.NumberOfVertices > 1)
            {
                var pLast = pl.GetPoint3dAt(pl.NumberOfVertices - 1).TransformBy(transform);
                var pFirst = pl.GetPoint3dAt(0).TransformBy(transform);
                result.Add(new GeometryData(GeomType.Line, color,
                    new[] { new Point2(pLast.X, pLast.Y), new Point2(pFirst.X, pFirst.Y) }));
            }
        }

        private void CollectPolyline2dSegments(Polyline2d pl2d, Transaction tr,
            Matrix3d transform, Color color, List<GeometryData> result)
        {
            var verts = new List<Point3d>();
            foreach (ObjectId vId in pl2d)
            {
                var v = tr.GetObject(vId, OpenMode.ForRead) as Vertex2d;
                if (v != null) verts.Add(v.Position.TransformBy(transform));
            }
            for (int i = 0; i < verts.Count - 1; i++)
            {
                result.Add(new GeometryData(GeomType.Line, color,
                    new[] { new Point2(verts[i].X, verts[i].Y),
                            new Point2(verts[i+1].X, verts[i+1].Y) }));
            }
            if (pl2d.Closed && verts.Count > 1)
            {
                result.Add(new GeometryData(GeomType.Line, color,
                    new[] { new Point2(verts[verts.Count-1].X, verts[verts.Count-1].Y),
                            new Point2(verts[0].X, verts[0].Y) }));
            }
        }

        private void CollectPolyline3dSegments(Polyline3d pl3d, Transaction tr,
            Matrix3d transform, Color color, List<GeometryData> result)
        {
            var verts = new List<Point3d>();
            foreach (ObjectId vId in pl3d)
            {
                var v = tr.GetObject(vId, OpenMode.ForRead) as PolylineVertex3d;
                if (v != null) verts.Add(v.Position.TransformBy(transform));
            }
            for (int i = 0; i < verts.Count - 1; i++)
            {
                result.Add(new GeometryData(GeomType.Line, color,
                    new[] { new Point2(verts[i].X, verts[i].Y),
                            new Point2(verts[i+1].X, verts[i+1].Y) }));
            }
            if (pl3d.Closed && verts.Count > 1)
            {
                result.Add(new GeometryData(GeomType.Line, color,
                    new[] { new Point2(verts[verts.Count-1].X, verts[verts.Count-1].Y),
                            new Point2(verts[0].X, verts[0].Y) }));
            }
        }

        private Color ResolveColor(Entity ent, Transaction tr)
        {
            var acadColor = ent.Color;
            if (acadColor.IsByLayer)
            {
                var ltr = tr.GetObject(ent.LayerId, OpenMode.ForRead) as LayerTableRecord;
                if (ltr != null)
                    return AciColorTable.FromAcadColor(ltr.Color);
            }
            return AciColorTable.FromAcadColor(acadColor);
        }

        private Point ToThumb(Point2 pt, double cx, double cy, double scale)
        {
            double x = (pt.X - cx) * scale + ThumbSize / 2.0;
            double y = -(pt.Y - cy) * scale + ThumbSize / 2.0;  // Y축 반전
            return new Point(x, y);
        }

        private void DrawArcGeometry(DrawingContext dc, Pen pen,
            GeometryData g, double cx, double cy, double scale)
        {
            double r = g.Radius * scale;
            double startAngle = g.StartAngle;
            double endAngle = g.EndAngle;

            // Arc → 시작점/끝점 계산
            var center = g.Points[0];
            double sx = center.X + g.Radius * Math.Cos(startAngle);
            double sy = center.Y + g.Radius * Math.Sin(startAngle);
            double ex = center.X + g.Radius * Math.Cos(endAngle);
            double ey = center.Y + g.Radius * Math.Sin(endAngle);

            var startPt = ToThumb(new Point2(sx, sy), cx, cy, scale);
            var endPt = ToThumb(new Point2(ex, ey), cx, cy, scale);

            double sweep = endAngle - startAngle;
            if (sweep < 0) sweep += 2 * Math.PI;
            bool isLargeArc = sweep > Math.PI;

            var sg = new StreamGeometry();
            using (var ctx = sg.Open())
            {
                ctx.BeginFigure(startPt, false, false);
                // Y축 반전이므로 SweepDirection 반전
                ctx.ArcTo(endPt, new Size(r, r), 0, isLargeArc,
                    SweepDirection.Counterclockwise, true, false);
            }
            sg.Freeze();
            dc.DrawGeometry(null, pen, sg);
        }

        private DrawingImage CreatePlaceholder()
        {
            var dv = new DrawingVisual();
            using (var dc = dv.RenderOpen())
            {
                dc.DrawRectangle(new SolidColorBrush(Color.FromRgb(30, 30, 30)),
                    null, new Rect(0, 0, ThumbSize, ThumbSize));
                var ft = new FormattedText("No Preview",
                    System.Globalization.CultureInfo.CurrentCulture,
                    FlowDirection.LeftToRight,
                    new Typeface("Segoe UI"), 10, Brushes.Gray,
                    1.0); // pixelsPerDip — 논리 단위 렌더링이므로 1.0 사용
                dc.DrawText(ft, new Point(
                    (ThumbSize - ft.Width) / 2, (ThumbSize - ft.Height) / 2));
            }
            var img = new DrawingImage(dv.Drawing);
            img.Freeze();
            return img;
        }

        // 내부 타입
        private enum GeomType { Line, Circle, Arc, Ellipse }

        private struct Point2
        {
            public double X, Y;
            public Point2(double x, double y) { X = x; Y = y; }
        }

        private class GeometryData
        {
            public GeomType Type;
            public Color Color;
            public Point2[] Points;
            public double Radius;
            public double MinorRadius; // Ellipse용
            public double StartAngle, EndAngle;

            public GeometryData(GeomType type, Color color, Point2[] points,
                double radius = 0, double startAngleOrMinorR = 0, double endAngle = 0)
            {
                Type = type; Color = color; Points = points;
                Radius = radius;
                if (type == GeomType.Ellipse)
                    MinorRadius = startAngleOrMinorR;
                else
                {
                    StartAngle = startAngleOrMinorR;
                    EndAngle = endAngle;
                }
            }
        }
    }
}
```

- [ ] **Step 2: Add to csproj**

```xml
<Compile Include="BlockBrowser\BlockThumbnailRenderer.cs" />
```

- [ ] **Step 3: Commit**

```bash
git add src/GntTools.UI/BlockBrowser/BlockThumbnailRenderer.cs src/GntTools.UI/GntTools.UI.csproj
git commit -m "feat(block-browser): add thumbnail renderer with geometry-to-WPF conversion"
```

---

### Task 4: BlockBrowserViewModel

**Files:**
- Create: `src/GntTools.UI/BlockBrowser/BlockBrowserViewModel.cs`

- [ ] **Step 1: Create BlockBrowserViewModel.cs**

```csharp
using System.Collections.ObjectModel;
using System.Windows.Input;
using Autodesk.AutoCAD.ApplicationServices;
using Autodesk.AutoCAD.DatabaseServices;
using Autodesk.AutoCAD.EditorInput;
using Autodesk.AutoCAD.Geometry;
using GntTools.UI.ViewModels;

namespace GntTools.UI.BlockBrowser
{
    public class BlockBrowserViewModel : ViewModelBase
    {
        private readonly BlockThumbnailRenderer _renderer = new BlockThumbnailRenderer();

        public ObservableCollection<BlockItem> Blocks { get; }
            = new ObservableCollection<BlockItem>();

        private bool _isGridView = true;
        public bool IsGridView
        {
            get => _isGridView;
            set => SetProperty(ref _isGridView, value);
        }

        private int _blockCount;
        public int BlockCount
        {
            get => _blockCount;
            set => SetProperty(ref _blockCount, value);
        }

        public ICommand RefreshCommand { get; }
        public ICommand ToggleViewCommand { get; }

        public BlockBrowserViewModel()
        {
            RefreshCommand = new RelayCommand(Refresh);
            ToggleViewCommand = new RelayCommand(() => IsGridView = !IsGridView);
        }

        public void Refresh()
        {
            Blocks.Clear();
            _renderer.ClearCache();

            var doc = Application.DocumentManager.MdiActiveDocument;
            if (doc == null) return;

            try
            {
                var db = doc.Database;
                using (var tr = db.TransactionManager.StartTransaction())
                {
                    var bt = (BlockTable)tr.GetObject(db.BlockTableId, OpenMode.ForRead);

                    foreach (ObjectId btrId in bt)
                    {
                        var btr = (BlockTableRecord)tr.GetObject(btrId, OpenMode.ForRead);

                        // 익명 블록, 레이아웃 블록, ModelSpace, PaperSpace 제외
                        if (btr.IsAnonymous) continue;
                        if (btr.IsLayout) continue;
                        if (btr.Name.StartsWith("*")) continue;

                        var thumbnail = _renderer.Render(btr, tr);

                        Blocks.Add(new BlockItem
                        {
                            Name = btr.Name,
                            BlockId = btrId,
                            Thumbnail = thumbnail
                        });
                    }

                    tr.Commit();
                }
            }
            catch (System.Exception ex)
            {
                doc.Editor.WriteMessage($"\n블록 목록 로드 실패: {ex.Message}");
            }

            BlockCount = Blocks.Count;
        }

        /// <summary>블록 삽입 — PaletteSet에서 호출되므로 LockDocument 필수</summary>
        public void InsertBlock(BlockItem item)
        {
            var doc = Application.DocumentManager.MdiActiveDocument;
            if (doc == null) return;

            using (doc.LockDocument())
            {
                var ed = doc.Editor;
                var pr = ed.GetPoint("\n블록 삽입점을 지정하세요: ");
                if (pr.Status != PromptStatus.OK) return;

                var db = doc.Database;
                using (var tr = db.TransactionManager.StartTransaction())
                {
                    var bt = (BlockTable)tr.GetObject(db.BlockTableId, OpenMode.ForRead);
                    var ms = (BlockTableRecord)tr.GetObject(
                        bt[BlockTableRecord.ModelSpace], OpenMode.ForWrite);

                    var blkRef = new BlockReference(pr.Value, item.BlockId);
                    ms.AppendEntity(blkRef);
                    tr.AddNewlyCreatedDBObject(blkRef, true);

                    tr.Commit();
                }
            }
        }
    }
}
```

- [ ] **Step 2: Add to csproj**

```xml
<Compile Include="BlockBrowser\BlockBrowserViewModel.cs" />
```

- [ ] **Step 3: Commit**

```bash
git add src/GntTools.UI/BlockBrowser/BlockBrowserViewModel.cs src/GntTools.UI/GntTools.UI.csproj
git commit -m "feat(block-browser): add ViewModel with refresh, view toggle, and insert logic"
```

---

### Task 5: BlockBrowserPanel XAML + Code-Behind

**Files:**
- Create: `src/GntTools.UI/BlockBrowser/BlockBrowserPanel.xaml`
- Create: `src/GntTools.UI/BlockBrowser/BlockBrowserPanel.xaml.cs`

- [ ] **Step 1: Create BlockBrowserPanel.xaml**

```xml
<UserControl x:Class="GntTools.UI.BlockBrowser.BlockBrowserPanel"
             xmlns="http://schemas.microsoft.com/winfx/2006/xaml/presentation"
             xmlns:x="http://schemas.microsoft.com/winfx/2006/xaml"
             Padding="4">
    <UserControl.Resources>
        <BooleanToVisibilityConverter x:Key="BoolToVis"/>
    </UserControl.Resources>
    <DockPanel>
        <!-- 툴바 -->
        <StackPanel DockPanel.Dock="Top" Orientation="Horizontal" Margin="0,0,0,4">
            <Button Content="새로고침" Command="{Binding RefreshCommand}"
                    Padding="8,4" Margin="0,0,4,0"/>
            <ToggleButton Content="그리드" IsChecked="{Binding IsGridView}"
                          Padding="8,4"/>
        </StackPanel>

        <!-- 상태바 -->
        <TextBlock DockPanel.Dock="Bottom" Margin="0,4,0,0"
                   Foreground="Gray" FontSize="11">
            <Run Text="블록 수: "/><Run Text="{Binding BlockCount, Mode=OneWay}"/><Run Text="개"/>
        </TextBlock>

        <!-- 그리드 뷰 -->
        <ScrollViewer VerticalScrollBarVisibility="Auto"
                      Visibility="{Binding IsGridView, Converter={StaticResource BoolToVis}}">
            <ItemsControl ItemsSource="{Binding Blocks}">
                <ItemsControl.ItemsPanel>
                    <ItemsPanelTemplate>
                        <WrapPanel/>
                    </ItemsPanelTemplate>
                </ItemsControl.ItemsPanel>
                <ItemsControl.ItemTemplate>
                    <DataTemplate>
                        <Border Margin="2" Padding="2" BorderBrush="#444" BorderThickness="1"
                                CornerRadius="2" Background="#1E1E1E" Width="88"
                                MouseLeftButtonDown="OnBlockDoubleClick" Cursor="Hand">
                            <StackPanel HorizontalAlignment="Center">
                                <Image Source="{Binding Thumbnail}" Width="80" Height="80"
                                       Stretch="Uniform" RenderOptions.BitmapScalingMode="HighQuality"/>
                                <TextBlock Text="{Binding Name}" Foreground="White"
                                           FontSize="10" TextTrimming="CharacterEllipsis"
                                           HorizontalAlignment="Center" Margin="0,2,0,0"
                                           ToolTip="{Binding Name}"/>
                            </StackPanel>
                        </Border>
                    </DataTemplate>
                </ItemsControl.ItemTemplate>
            </ItemsControl>
        </ScrollViewer>

        <!-- 리스트 뷰 (IsGridView == false) -->
        <ScrollViewer VerticalScrollBarVisibility="Auto">
            <ScrollViewer.Style>
                <Style TargetType="ScrollViewer">
                    <Setter Property="Visibility" Value="Collapsed"/>
                    <Style.Triggers>
                        <DataTrigger Binding="{Binding IsGridView}" Value="False">
                            <Setter Property="Visibility" Value="Visible"/>
                        </DataTrigger>
                    </Style.Triggers>
                </Style>
            </ScrollViewer.Style>
            <ItemsControl ItemsSource="{Binding Blocks}">
                <ItemsControl.ItemTemplate>
                    <DataTemplate>
                        <Border Margin="1" Padding="4,2" BorderBrush="#444" BorderThickness="0,0,0,1"
                                Background="#1E1E1E"
                                MouseLeftButtonDown="OnBlockDoubleClick" Cursor="Hand">
                            <StackPanel Orientation="Horizontal">
                                <Image Source="{Binding Thumbnail}" Width="40" Height="40"
                                       Stretch="Uniform" Margin="0,0,8,0"/>
                                <TextBlock Text="{Binding Name}" Foreground="White"
                                           VerticalAlignment="Center" FontSize="12"
                                           ToolTip="{Binding Name}"/>
                            </StackPanel>
                        </Border>
                    </DataTemplate>
                </ItemsControl.ItemTemplate>
            </ItemsControl>
        </ScrollViewer>
    </DockPanel>
</UserControl>
```

- [ ] **Step 2: Create BlockBrowserPanel.xaml.cs**

```csharp
using System.Windows.Controls;
using System.Windows.Input;

namespace GntTools.UI.BlockBrowser
{
    public partial class BlockBrowserPanel : UserControl
    {
        public BlockBrowserPanel()
        {
            InitializeComponent();
        }

        private void OnBlockDoubleClick(object sender, MouseButtonEventArgs e)
        {
            if (e.ClickCount < 2) return;

            var fe = sender as System.Windows.FrameworkElement;
            var item = fe?.DataContext as BlockItem;
            if (item == null) return;

            var vm = DataContext as BlockBrowserViewModel;
            vm?.InsertBlock(item);
        }
    }
}
```

- [ ] **Step 3: Add to csproj**

Compile 섹션에 추가:
```xml
<Compile Include="BlockBrowser\BlockBrowserPanel.xaml.cs">
  <DependentUpon>BlockBrowserPanel.xaml</DependentUpon>
</Compile>
```

Page 섹션에 추가:
```xml
<Page Include="BlockBrowser\BlockBrowserPanel.xaml">
  <Generator>MSBuild:Compile</Generator>
  <SubType>Designer</SubType>
</Page>
```

- [ ] **Step 4: Commit**

```bash
git add src/GntTools.UI/BlockBrowser/BlockBrowserPanel.xaml src/GntTools.UI/BlockBrowser/BlockBrowserPanel.xaml.cs src/GntTools.UI/GntTools.UI.csproj
git commit -m "feat(block-browser): add WPF panel with grid/list toggle views"
```

---

### Task 6: BlockBrowserPaletteManager

**Files:**
- Create: `src/GntTools.UI/BlockBrowser/BlockBrowserPaletteManager.cs`

- [ ] **Step 1: Create BlockBrowserPaletteManager.cs**

기존 `PaletteManager.cs` 패턴을 따르되 별도 GUID/인스턴스 사용.

```csharp
using System;
using Autodesk.AutoCAD.Windows;

namespace GntTools.UI.BlockBrowser
{
    public static class BlockBrowserPaletteManager
    {
        private static readonly Guid PaletteGuid =
            new Guid("C8F4A2B3-5E6D-7F80-9012-BCDE01234567");

        private static PaletteSet _ps;
        private static BlockBrowserViewModel _vm;

        public static void Toggle()
        {
            Initialize();
            _ps.Visible = !_ps.Visible;
            if (_ps.Visible)
                _vm.Refresh();
        }

        private static void Initialize()
        {
            if (_ps != null) return;

            _ps = new PaletteSet("블록 브라우저", PaletteGuid);
            _ps.Style = PaletteSetStyles.ShowAutoHideButton
                      | PaletteSetStyles.ShowCloseButton;
            _ps.MinimumSize = new System.Drawing.Size(250, 300);
            _ps.DockEnabled = DockSides.Left | DockSides.Right;

            _vm = new BlockBrowserViewModel();
            _ps.AddVisual("블록", new BlockBrowserPanel { DataContext = _vm });
        }
    }
}
```

- [ ] **Step 2: Add to csproj**

```xml
<Compile Include="BlockBrowser\BlockBrowserPaletteManager.cs" />
```

- [ ] **Step 3: Commit**

```bash
git add src/GntTools.UI/BlockBrowser/BlockBrowserPaletteManager.cs src/GntTools.UI/GntTools.UI.csproj
git commit -m "feat(block-browser): add PaletteManager singleton for block browser"
```

---

### Task 7: GNTBLOCKS Command + csproj Finalize

**Files:**
- Modify: `src/GntTools.UI/Commands/PaletteCommands.cs:6` (add method)
- Modify: `src/GntTools.UI/GntTools.UI.csproj` (verify all entries)

- [ ] **Step 1: Add GNTBLOCKS command**

`PaletteCommands.cs`에 메서드 추가:

```csharp
[CommandMethod("GNTBLOCKS")]
public void ShowBlockBrowser()
{
    BlockBrowser.BlockBrowserPaletteManager.Toggle();
}
```

파일 상단에 using 추가 불필요 (같은 네임스페이스 하위이므로 풀네임 사용).

- [ ] **Step 2: Verify csproj has all entries**

Compile ItemGroup에 5개 항목:
```
BlockBrowser\BlockItem.cs
BlockBrowser\AciColorTable.cs
BlockBrowser\BlockThumbnailRenderer.cs
BlockBrowser\BlockBrowserViewModel.cs
BlockBrowser\BlockBrowserPaletteManager.cs
BlockBrowser\BlockBrowserPanel.xaml.cs (DependentUpon)
```

Page ItemGroup에 1개:
```
BlockBrowser\BlockBrowserPanel.xaml
```

- [ ] **Step 3: Commit**

```bash
git add src/GntTools.UI/Commands/PaletteCommands.cs src/GntTools.UI/GntTools.UI.csproj
git commit -m "feat(block-browser): add GNTBLOCKS command to palette commands"
```

---

### Task 8: Features Tracking Markdown + Final Commit

**Files:**
- Create: `docs/features.md`

- [ ] **Step 1: Create features.md**

사용자가 요청한 기능 추적 마크다운 파일.

```markdown
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
```

- [ ] **Step 2: Commit**

```bash
git add docs/features.md
git commit -m "docs: add features tracking markdown with block browser entry"
```
