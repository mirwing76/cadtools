using Autodesk.AutoCAD.ApplicationServices;
using Autodesk.AutoCAD.DatabaseServices;
using Autodesk.AutoCAD.Geometry;

namespace GntTools.Core.Geometry
{
    /// <summary>
    /// 뷰포트 줌 저장/복원/이동.
    /// VB.NET 버그 수정: MaxPoint.X - MinPoint.Y → MinPoint.X 사용
    /// </summary>
    public static class ViewportManager
    {
        /// <summary>엔티티 범위로 줌 (여백 포함)</summary>
        public static void ZoomToEntity(ObjectId entityId, double marginFactor = 1.2)
        {
            var db = Application.DocumentManager.MdiActiveDocument.Database;

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
            var ed = Application.DocumentManager.MdiActiveDocument.Editor;

            // 버그 수정: MinPoint.X 정확 사용
            double cx = (extents.MinPoint.X + extents.MaxPoint.X) / 2.0;
            double cy = (extents.MinPoint.Y + extents.MaxPoint.Y) / 2.0;

            double width = (extents.MaxPoint.X - extents.MinPoint.X) * marginFactor;
            double height = (extents.MaxPoint.Y - extents.MinPoint.Y) * marginFactor;

            using (var view = ed.GetCurrentView())
            {
                view.CenterPoint = new Point2d(cx, cy);
                view.Height = height;
                view.Width = width;
                ed.SetCurrentView(view);
            }
        }

        /// <summary>현재 뷰 저장</summary>
        public static ViewTableRecord SaveCurrentView()
        {
            var ed = Application.DocumentManager.MdiActiveDocument.Editor;
            return (ViewTableRecord)ed.GetCurrentView().Clone();
        }

        /// <summary>저장된 뷰 복원</summary>
        public static void RestoreView(ViewTableRecord savedView)
        {
            if (savedView == null) return;
            var ed = Application.DocumentManager.MdiActiveDocument.Editor;
            ed.SetCurrentView(savedView);
        }
    }
}
