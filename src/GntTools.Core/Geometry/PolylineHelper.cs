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
            var db = Application.DocumentManager.MdiActiveDocument.Database;

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
            var db = Application.DocumentManager.MdiActiveDocument.Database;

            using (var tr = db.TransactionManager.StartTransaction())
            {
                var ent = tr.GetObject(polyId, OpenMode.ForRead) as Curve;
                tr.Commit();
                if (ent == null) return 0.0;
                return ent.GetDistAtPoint(ent.EndPoint);
            }
        }

        /// <summary>시점/종점 좌표</summary>
        public static (Point3d start, Point3d end) GetEndpoints(ObjectId polyId)
        {
            var db = Application.DocumentManager.MdiActiveDocument.Database;

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
