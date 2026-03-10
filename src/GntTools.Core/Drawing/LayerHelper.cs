using Autodesk.AutoCAD.ApplicationServices;
using Autodesk.AutoCAD.Colors;
using Autodesk.AutoCAD.DatabaseServices;

namespace GntTools.Core.Drawing
{
    /// <summary>레이어 존재확인/생성</summary>
    public static class LayerHelper
    {
        /// <summary>레이어가 없으면 생성, 있으면 스킵</summary>
        public static void EnsureLayer(string layerName, short colorIndex = 7)
        {
            var db = Application.DocumentManager.MdiActiveDocument.Database;

            using (var tr = db.TransactionManager.StartTransaction())
            {
                var lt = (LayerTable)tr.GetObject(db.LayerTableId, OpenMode.ForRead);
                if (!lt.Has(layerName))
                {
                    lt.UpgradeOpen();
                    var layer = new LayerTableRecord
                    {
                        Name = layerName,
                        Color = Color.FromColorIndex(ColorMethod.ByAci, colorIndex)
                    };
                    lt.Add(layer);
                    tr.AddNewlyCreatedDBObject(layer, true);
                }
                tr.Commit();
            }
        }

        /// <summary>레이어 존재 여부 확인</summary>
        public static bool Exists(string layerName)
        {
            var db = Application.DocumentManager.MdiActiveDocument.Database;
            using (var tr = db.TransactionManager.StartTransaction())
            {
                var lt = (LayerTable)tr.GetObject(db.LayerTableId, OpenMode.ForRead);
                tr.Commit();
                return lt.Has(layerName);
            }
        }
    }
}
