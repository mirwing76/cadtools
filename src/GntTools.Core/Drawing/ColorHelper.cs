using Autodesk.AutoCAD.ApplicationServices;
using Autodesk.AutoCAD.Colors;
using Autodesk.AutoCAD.DatabaseServices;

namespace GntTools.Core.Drawing
{
    /// <summary>
    /// 엔티티 색상 변경.
    /// VB.NET 버그 수정: changColor 파라미터 무시(항상 2) → 정확 적용
    /// </summary>
    public static class ColorHelper
    {
        /// <summary>엔티티 색상을 ACI 인덱스로 변경</summary>
        public static void SetColor(ObjectId entityId, short colorIndex)
        {
            var db = Application.DocumentManager.MdiActiveDocument.Database;

            using (var tr = db.TransactionManager.StartTransaction())
            {
                var ent = tr.GetObject(entityId, OpenMode.ForWrite) as Entity;
                if (ent != null)
                {
                    ent.Color = Color.FromColorIndex(
                        ColorMethod.ByAci, colorIndex);
                }
                tr.Commit();
            }
        }
    }
}
