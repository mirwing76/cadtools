using System;
using System.Collections.Generic;
using System.IO;
using System.Runtime.Serialization.Json;
using System.Text;
using Autodesk.AutoCAD.DatabaseServices;

namespace GntTools.UI.BlockBrowser
{
    /// <summary>DWG NOD에 블록 별칭/즐겨찾기를 도면별 저장</summary>
    public static class BlockDataStore
    {
        private const string AliasKey = "GntTools_BlockAliases";
        private const string FavoriteKey = "GntTools_BlockFavorites";

        public static Dictionary<string, string> LoadAliases(Database db)
        {
            string json = ReadNodString(db, AliasKey);
            if (string.IsNullOrEmpty(json)) return new Dictionary<string, string>();
            try
            {
                var ser = new DataContractJsonSerializer(typeof(Dictionary<string, string>));
                using (var ms = new MemoryStream(Encoding.UTF8.GetBytes(json)))
                    return (Dictionary<string, string>)ser.ReadObject(ms);
            }
            catch { return new Dictionary<string, string>(); }
        }

        public static void SaveAliases(Database db, Dictionary<string, string> aliases)
        {
            string json = SerializeDict(aliases);
            WriteNodString(db, AliasKey, json);
        }

        public static HashSet<string> LoadFavorites(Database db)
        {
            string json = ReadNodString(db, FavoriteKey);
            if (string.IsNullOrEmpty(json)) return new HashSet<string>();
            try
            {
                var ser = new DataContractJsonSerializer(typeof(List<string>));
                using (var ms = new MemoryStream(Encoding.UTF8.GetBytes(json)))
                {
                    var list = (List<string>)ser.ReadObject(ms);
                    return new HashSet<string>(list);
                }
            }
            catch { return new HashSet<string>(); }
        }

        public static void SaveFavorites(Database db, HashSet<string> favorites)
        {
            var list = new List<string>(favorites);
            var ser = new DataContractJsonSerializer(typeof(List<string>));
            using (var ms = new MemoryStream())
            {
                ser.WriteObject(ms, list);
                string json = Encoding.UTF8.GetString(ms.ToArray());
                WriteNodString(db, FavoriteKey, json);
            }
        }

        private static string ReadNodString(Database db, string key)
        {
            using (var tr = db.TransactionManager.StartTransaction())
            {
                var nod = (DBDictionary)tr.GetObject(db.NamedObjectsDictionaryId, OpenMode.ForRead);
                if (!nod.Contains(key))
                {
                    tr.Commit();
                    return null;
                }
                var xrec = (Xrecord)tr.GetObject(nod.GetAt(key), OpenMode.ForRead);
                var data = xrec.Data;
                if (data == null)
                {
                    tr.Commit();
                    return null;
                }
                var values = data.AsArray();
                tr.Commit();
                if (values.Length > 0 && values[0].Value is string s)
                    return s;
                return null;
            }
        }

        private static void WriteNodString(Database db, string key, string value)
        {
            using (var tr = db.TransactionManager.StartTransaction())
            {
                var nod = (DBDictionary)tr.GetObject(db.NamedObjectsDictionaryId, OpenMode.ForWrite);
                var xrec = new Xrecord();
                xrec.Data = new ResultBuffer(
                    new TypedValue((int)DxfCode.Text, value));

                if (nod.Contains(key))
                {
                    var existing = (Xrecord)tr.GetObject(nod.GetAt(key), OpenMode.ForWrite);
                    existing.Data = xrec.Data;
                }
                else
                {
                    nod.SetAt(key, xrec);
                    tr.AddNewlyCreatedDBObject(xrec, true);
                }
                tr.Commit();
            }
        }

        private static string SerializeDict(Dictionary<string, string> dict)
        {
            var ser = new DataContractJsonSerializer(typeof(Dictionary<string, string>));
            using (var ms = new MemoryStream())
            {
                ser.WriteObject(ms, dict);
                return Encoding.UTF8.GetString(ms.ToArray());
            }
        }
    }
}
