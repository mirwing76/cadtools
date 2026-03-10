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
            var db = Application.DocumentManager.MdiActiveDocument.Database;

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
            var db = Application.DocumentManager.MdiActiveDocument.Database;

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
            var db = Application.DocumentManager.MdiActiveDocument.Database;

            using (var tr = db.TransactionManager.StartTransaction())
            {
                var ent = tr.GetObject(textId, OpenMode.ForWrite);
                if (ent is DBText txt)
                    txt.Position = newPosition;
                tr.Commit();
            }
        }

        /// <summary>
        /// 폴리라인 세그먼트 각도에 맞춰 텍스트 회전각 계산.
        /// 항상 읽기 쉬운 방향 (0~180도)
        /// </summary>
        public static double CalcReadableAngle(Point3d start, Point3d end)
        {
            double angle = Math.Atan2(end.Y - start.Y, end.X - start.X);
            if (angle < 0) angle += Math.PI * 2;
            // 읽기 불편한 방향이면 180도 회전
            if (angle > Math.PI / 2 && angle < Math.PI * 3 / 2)
                angle += Math.PI;
            if (angle >= Math.PI * 2) angle -= Math.PI * 2;
            return angle;
        }
    }
}
