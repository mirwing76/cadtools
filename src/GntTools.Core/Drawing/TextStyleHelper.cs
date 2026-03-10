using Autodesk.AutoCAD.ApplicationServices;
using Autodesk.AutoCAD.DatabaseServices;

namespace GntTools.Core.Drawing
{
    /// <summary>텍스트 스타일 관리</summary>
    public static class TextStyleHelper
    {
        /// <summary>텍스트 스타일이 없으면 생성 (SHX + BigFont)</summary>
        public static ObjectId EnsureStyle(string styleName,
            string shxFont = "ROMANS", string bigFont = "GHS")
        {
            var db = Application.DocumentManager.MdiActiveDocument.Database;

            using (var tr = db.TransactionManager.StartTransaction())
            {
                var tst = (TextStyleTable)tr.GetObject(
                    db.TextStyleTableId, OpenMode.ForRead);

                if (tst.Has(styleName))
                {
                    var id = tst[styleName];
                    tr.Commit();
                    return id;
                }

                tst.UpgradeOpen();
                var style = new TextStyleTableRecord
                {
                    Name = styleName,
                    FileName = shxFont + ".shx",
                    BigFontFileName = bigFont + ".shx"
                };
                var styleId = tst.Add(style);
                tr.AddNewlyCreatedDBObject(style, true);
                tr.Commit();
                return styleId;
            }
        }

        /// <summary>스타일명으로 ObjectId 조회</summary>
        public static ObjectId GetStyleId(string styleName)
        {
            var db = Application.DocumentManager.MdiActiveDocument.Database;
            using (var tr = db.TransactionManager.StartTransaction())
            {
                var tst = (TextStyleTable)tr.GetObject(
                    db.TextStyleTableId, OpenMode.ForRead);
                tr.Commit();
                return tst.Has(styleName) ? tst[styleName] : ObjectId.Null;
            }
        }
    }
}
