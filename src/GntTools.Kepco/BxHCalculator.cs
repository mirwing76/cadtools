using System;
using Autodesk.AutoCAD.ApplicationServices;
using Autodesk.AutoCAD.DatabaseServices;
using Autodesk.AutoCAD.EditorInput;
using GntTools.Core.Selection;

namespace GntTools.Kepco
{
    /// <summary>BxH (가로x세로) 측정</summary>
    public class BxHCalculator
    {
        /// <summary>사용자가 B(가로)와 H(세로) 객체를 선택하여 치수 측정</summary>
        public (double b, double h, string bxhStr) MeasureFromSelection()
        {
            var doc = Application.DocumentManager.MdiActiveDocument;
            var ed = doc.Editor;
            var db = doc.Database;

            // B(가로) 폴리라인 선택
            ed.WriteMessage("\nB(가로) 선을 선택하세요.");
            var selector = new EntitySelector();
            var bId = selector.SelectOne(null, "\nB(가로) 선택: ");
            double b = 0;

            if (!bId.IsNull)
            {
                using (var tr = db.TransactionManager.StartTransaction())
                {
                    var ent = tr.GetObject(bId, OpenMode.ForRead) as Curve;
                    if (ent != null)
                        b = Math.Round(ent.GetDistAtPoint(ent.EndPoint), 2);
                    tr.Commit();
                }
            }

            // H(세로) 폴리라인 선택
            ed.WriteMessage("\nH(세로) 선을 선택하세요.");
            var hId = selector.SelectOne(null, "\nH(세로) 선택: ");
            double h = 0;

            if (!hId.IsNull)
            {
                using (var tr = db.TransactionManager.StartTransaction())
                {
                    var ent = tr.GetObject(hId, OpenMode.ForRead) as Curve;
                    if (ent != null)
                        h = Math.Round(ent.GetDistAtPoint(ent.EndPoint), 2);
                    tr.Commit();
                }
            }

            string bxhStr = $"{b:F2}x{h:F2}";
            ed.WriteMessage($"\nBxH: {bxhStr}");
            return (b, h, bxhStr);
        }
    }
}
