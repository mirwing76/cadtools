using System;
using Autodesk.AutoCAD.ApplicationServices;
using Autodesk.AutoCAD.DatabaseServices;

namespace GntTools.Core.XData
{
    /// <summary>
    /// XData(Extended Data) 관리 — RegApp 등록, 그룹ID 읽기/쓰기.
    /// VB.NET 버그 수정: removeXdata에서 Commit 누락 → Transaction 패턴 통일
    /// </summary>
    public static class XDataManager
    {
        private const string AppName = "GNTTOOLS";

        /// <summary>RegApp 등록 (없으면 생성)</summary>
        public static void EnsureRegApp(string appName = null)
        {
            appName = appName ?? AppName;
            var db = Application.DocumentManager.MdiActiveDocument.Database;

            using (var tr = db.TransactionManager.StartTransaction())
            {
                var rat = (RegAppTable)tr.GetObject(
                    db.RegAppTableId, OpenMode.ForRead);
                if (!rat.Has(appName))
                {
                    rat.UpgradeOpen();
                    var rec = new RegAppTableRecord { Name = appName };
                    rat.Add(rec);
                    tr.AddNewlyCreatedDBObject(rec, true);
                }
                tr.Commit();
            }
        }

        /// <summary>그룹ID(타임스탬프) 생성</summary>
        public static string GenerateGroupId()
        {
            return DateTime.Now.ToString("yyyyMMddHHmmssfff");
        }

        /// <summary>엔티티에 그룹ID XData 쓰기</summary>
        public static void WriteGroupId(ObjectId entityId, string groupId,
            string appName = null)
        {
            appName = appName ?? AppName;
            EnsureRegApp(appName);

            var db = Application.DocumentManager.MdiActiveDocument.Database;

            using (var tr = db.TransactionManager.StartTransaction())
            {
                var ent = tr.GetObject(entityId, OpenMode.ForWrite) as Entity;
                if (ent != null)
                {
                    var rb = new ResultBuffer(
                        new TypedValue((int)DxfCode.ExtendedDataRegAppName, appName),
                        new TypedValue((int)DxfCode.ExtendedDataAsciiString, groupId)
                    );
                    ent.XData = rb;
                }
                tr.Commit(); // VB.NET 버그 수정: Commit 누락 → 추가
            }
        }

        /// <summary>엔티티에서 그룹ID 읽기</summary>
        public static string ReadGroupId(ObjectId entityId, string appName = null)
        {
            appName = appName ?? AppName;
            var db = Application.DocumentManager.MdiActiveDocument.Database;

            using (var tr = db.TransactionManager.StartTransaction())
            {
                var ent = tr.GetObject(entityId, OpenMode.ForRead) as Entity;
                if (ent == null) { tr.Commit(); return null; }

                var rb = ent.GetXDataForApplication(appName);
                tr.Commit();

                if (rb == null) return null;
                var values = rb.AsArray();
                if (values.Length >= 2)
                    return values[1].Value as string;
                return null;
            }
        }

        /// <summary>엔티티에서 XData 제거</summary>
        public static void RemoveXData(ObjectId entityId, string appName = null)
        {
            appName = appName ?? AppName;
            var db = Application.DocumentManager.MdiActiveDocument.Database;

            using (var tr = db.TransactionManager.StartTransaction())
            {
                var ent = tr.GetObject(entityId, OpenMode.ForWrite) as Entity;
                if (ent != null)
                {
                    var rb = new ResultBuffer(
                        new TypedValue((int)DxfCode.ExtendedDataRegAppName, appName)
                    );
                    ent.XData = rb;
                }
                tr.Commit(); // VB.NET 버그 수정: Commit 누락 → 추가
            }
        }
    }
}
