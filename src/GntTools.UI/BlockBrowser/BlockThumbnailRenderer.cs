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
        private const double Padding = 4.0;
        private readonly Dictionary<string, DrawingImage> _cache
            = new Dictionary<string, DrawingImage>();

        public void ClearCache() => _cache.Clear();

        /// <summary>블록의 썸네일 DrawingImage 생성 (캐시)</summary>
        public DrawingImage Render(BlockTableRecord btr, Transaction tr, double thumbSize = 56.0)
        {
            string key = $"{btr.ObjectId}_{thumbSize}";
            if (_cache.TryGetValue(key, out var cached))
                return cached;

            DrawingImage result;
            try
            {
                result = RenderInternal(btr, tr, thumbSize);
            }
            catch
            {
                result = CreatePlaceholder(thumbSize);
            }
            _cache[key] = result;
            return result;
        }

        private DrawingImage RenderInternal(BlockTableRecord btr, Transaction tr, double thumbSize)
        {
            // 1. 엔티티에서 geometry 수집
            var geometries = new List<GeometryData>();
            CollectGeometries(btr, tr, Matrix3d.Identity, geometries, new HashSet<ObjectId>());

            if (geometries.Count == 0)
                return CreatePlaceholder(thumbSize);

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
            if (w < 1e-6 && h < 1e-6) return CreatePlaceholder(thumbSize);

            // 3. 스케일/센터 계산
            double drawArea = thumbSize - Padding * 2;
            double scale = Math.Min(drawArea / Math.Max(w, 1e-6), drawArea / Math.Max(h, 1e-6));
            double cx = (minX + maxX) / 2.0;
            double cy = (minY + maxY) / 2.0;

            // 4. DrawingVisual로 렌더
            var dv = new DrawingVisual();
            using (var dc = dv.RenderOpen())
            {
                // 배경
                dc.DrawRectangle(new SolidColorBrush(SystemColors.ControlColor),
                    null, new Rect(0, 0, thumbSize, thumbSize));

                foreach (var g in geometries)
                {
                    var pen = new Pen(new SolidColorBrush(g.Color), 1.0);
                    pen.Freeze();

                    switch (g.Type)
                    {
                        case GeomType.Line:
                            dc.DrawLine(pen, ToThumb(g.Points[0], cx, cy, scale, thumbSize),
                                             ToThumb(g.Points[1], cx, cy, scale, thumbSize));
                            break;
                        case GeomType.Circle:
                            var center = ToThumb(g.Points[0], cx, cy, scale, thumbSize);
                            double r = g.Radius * scale;
                            dc.DrawEllipse(null, pen, center, r, r);
                            break;
                        case GeomType.Ellipse:
                            var eCenter = ToThumb(g.Points[0], cx, cy, scale, thumbSize);
                            double majorR = g.Radius * scale;
                            double minorR = g.MinorRadius * scale;
                            dc.DrawEllipse(null, pen, eCenter, majorR, minorR);
                            break;
                        case GeomType.Arc:
                            DrawArcGeometry(dc, pen, g, cx, cy, scale, thumbSize);
                            break;
                    }
                }
            }

            var img = new DrawingImage(dv.Drawing);
            img.Freeze();
            return img;
        }

        /// <summary>블록 내 엔티티 재귀 수집 (중첩 블록 포함, 순환 참조 방지)</summary>
        private void CollectGeometries(BlockTableRecord btr, Transaction tr,
            Matrix3d transform, List<GeometryData> result, HashSet<ObjectId> visited)
        {
            if (!visited.Add(btr.ObjectId)) return; // 순환 참조 방지
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
                            CollectGeometries(nestedBtr, tr, transform * blkRef.BlockTransform, result, visited);
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

        private Point ToThumb(Point2 pt, double cx, double cy, double scale, double thumbSize)
        {
            double x = (pt.X - cx) * scale + thumbSize / 2.0;
            double y = -(pt.Y - cy) * scale + thumbSize / 2.0;  // Y축 반전
            return new Point(x, y);
        }

        private void DrawArcGeometry(DrawingContext dc, Pen pen,
            GeometryData g, double cx, double cy, double scale, double thumbSize = 56.0)
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

            var startPt = ToThumb(new Point2(sx, sy), cx, cy, scale, thumbSize);
            var endPt = ToThumb(new Point2(ex, ey), cx, cy, scale, thumbSize);

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

        private DrawingImage CreatePlaceholder(double thumbSize)
        {
            var dv = new DrawingVisual();
            using (var dc = dv.RenderOpen())
            {
                dc.DrawRectangle(new SolidColorBrush(SystemColors.ControlColor),
                    null, new Rect(0, 0, thumbSize, thumbSize));
                var ft = new FormattedText("No Preview",
                    System.Globalization.CultureInfo.CurrentCulture,
                    System.Windows.FlowDirection.LeftToRight,
                    new Typeface("Segoe UI"), 10, SystemColors.GrayTextBrush,
                    1.0); // pixelsPerDip — 논리 단위 렌더링이므로 1.0 사용
                dc.DrawText(ft, new Point(
                    (thumbSize - ft.Width) / 2, (thumbSize - ft.Height) / 2));
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
