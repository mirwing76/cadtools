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
            var db = Application.DocumentManager.MdiActiveDocument.Database;

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
            var db = Application.DocumentManager.MdiActiveDocument.Database;

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
