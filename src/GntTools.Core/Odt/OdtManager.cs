using System;
using System.Collections.Generic;
using Autodesk.AutoCAD.ApplicationServices;
using Autodesk.AutoCAD.DatabaseServices;
using Autodesk.AutoCAD.EditorInput;
using Autodesk.Gis.Map;
using Autodesk.Gis.Map.ObjectData;
using Autodesk.Gis.Map.Constants;
using Autodesk.Gis.Map.Project;
using MapDataType = Autodesk.Gis.Map.Constants.DataType;
using MapTable = Autodesk.Gis.Map.ObjectData.Table;
using MapOpenMode = Autodesk.Gis.Map.Constants.OpenMode;
using Autodesk.Gis.Map.Utilities;

namespace GntTools.Core.Odt
{
    /// <summary>
    /// ODT(Object Data Table) CRUD 통합 관리자.
    /// autocad-map-odt 스킬 에러 방지 체크리스트 10항목 준수.
    /// </summary>
    public class OdtManager
    {
        private Tables GetOdtTables()
        {
            MapApplication mapApp = HostMapApplicationServices.Application;
            ProjectModel proj = mapApp.ActiveProject;
            return proj.ODTables;
        }

        private ProjectModel GetProject()
        {
            return HostMapApplicationServices.Application.ActiveProject;
        }

        // ─── 테이블 관리 ───

        /// <summary>테이블이 없으면 생성, 있으면 true 반환</summary>
        public bool EnsureTable(IOdtSchema schema)
        {
            try
            {
                Tables tables = GetOdtTables();

                // 체크리스트 #7: 테이블 접근 전에 존재 확인
                if (tables.IsTableDefined(schema.TableName))
                    return true;

                // 공식 API: NewODFieldDefinitions()로 생성
                ProjectModel proj = GetProject();
                FieldDefinitions fieldDefs = proj.MapUtility.NewODFieldDefinitions();

                foreach (var field in schema.Fields)
                {
                    fieldDefs.Add(
                        field.Name, field.Description, field.DataType, 0);
                }

                tables.Add(schema.TableName, fieldDefs, schema.Description, true);
                return true;
            }
            catch (MapException mapEx)
            {
                WriteError($"테이블 생성 실패: {mapEx.Message} (코드: {mapEx.ErrorCode})");
                return false;
            }
        }

        /// <summary>테이블 존재 여부</summary>
        public bool TableExists(string tableName)
        {
            try
            {
                return GetOdtTables().IsTableDefined(tableName);
            }
            catch
            {
                return false;
            }
        }

        /// <summary>테이블 제거</summary>
        public bool RemoveTable(string tableName)
        {
            if (!TableExists(tableName)) return false;
            try
            {
                GetOdtTables().RemoveTable(tableName);
                return true;
            }
            catch (MapException mapEx)
            {
                WriteError($"테이블 제거 실패: {mapEx.Message}");
                return false;
            }
        }

        // ─── 레코드 CRUD ───

        /// <summary>엔티티에 빈 레코드 부착</summary>
        public bool AttachRecord(string tableName, ObjectId entityId)
        {
            // 체크리스트 #7: 존재 확인
            if (!TableExists(tableName)) return false;

            // 이미 레코드가 있으면 스킵
            if (RecordExists(tableName, entityId)) return true;

            try
            {
                Tables tables = GetOdtTables();
                MapTable table = tables[tableName];
                Database db = Application.DocumentManager.MdiActiveDocument.Database;

                // 체크리스트 #2: Entity를 ForWrite로 열어야 함
                // 체크리스트 #4: Transaction.Commit() 필수
                using (Transaction tr = db.TransactionManager.StartTransaction())
                {
                    Entity ent = (Entity)tr.GetObject(entityId, OpenMode.ForWrite);

                    // 체크리스트 #3: Record.Create() 후 InitRecord() 필수
                    Record rec = Record.Create();
                    table.InitRecord(rec);
                    table.AddRecord(rec, entityId);

                    tr.Commit();
                }
                return true;
            }
            catch (Exception ex)
            {
                WriteError($"레코드 부착 실패: {ex.Message}");
                return false;
            }
        }

        /// <summary>레코드 값 업데이트</summary>
        public bool UpdateRecord(string tableName, ObjectId entityId,
            Dictionary<string, object> values)
        {
            if (!TableExists(tableName)) return false;

            try
            {
                Tables tables = GetOdtTables();
                MapTable table = tables[tableName];

                // 체크리스트 #9: Write용 Records는 OpenForWrite로 열기
                // 체크리스트 #1: Records는 반드시 using 블록
                using (Records recs = table.GetObjectTableRecords(
                    0, entityId,
                    MapOpenMode.OpenForWrite, true))
                {
                    if (recs.Count == 0) return false;

                    foreach (Record rec in recs)
                    {
                        foreach (var kvp in values)
                        {
                            // 공식 API: GetColumnIndex로 인덱스 조회
                            int idx = table.FieldDefinitions.GetColumnIndex(kvp.Key);
                            if (idx < 0) continue;

                            // 체크리스트 #8: Assign 타입과 DataType 일치
                            MapValue val = rec[idx];
                            if (kvp.Value is string s)
                                val.Assign(s);
                            else if (kvp.Value is double d)
                                val.Assign(d);
                            else if (kvp.Value is int n)
                                val.Assign(n);
                        }

                        // 체크리스트 #5: UpdateRecord() 호출 필수
                        recs.UpdateRecord(rec);
                        break; // 첫 번째 레코드만 수정
                    }
                }
                return true;
            }
            catch (Exception ex)
            {
                WriteError($"레코드 업데이트 실패: {ex.Message}");
                return false;
            }
        }

        /// <summary>레코드 값 읽기 (string 배열로 반환)</summary>
        public string[] ReadRecord(string tableName, ObjectId entityId)
        {
            if (!TableExists(tableName)) return null;

            try
            {
                Tables tables = GetOdtTables();
                MapTable table = tables[tableName];

                // 체크리스트 #1: using 블록
                // 체크리스트 #9: 읽기는 OpenForRead
                using (Records recs = table.GetObjectTableRecords(
                    0, entityId,
                    MapOpenMode.OpenForRead, true))
                {
                    if (recs.Count == 0) return null;

                    FieldDefinitions fieldDefs = table.FieldDefinitions;
                    foreach (Record rec in recs)
                    {
                        var result = new string[rec.Count];
                        for (int i = 0; i < rec.Count; i++)
                        {
                            MapValue val = rec[i];
                            switch (val.Type)
                            {
                                case MapDataType.Character:
                                    result[i] = val.StrValue ?? "";
                                    break;
                                case MapDataType.Integer:
                                    result[i] = val.Int32Value.ToString();
                                    break;
                                case MapDataType.Real:
                                    result[i] = val.DoubleValue.ToString();
                                    break;
                                default:
                                    result[i] = val.StrValue ?? "";
                                    break;
                            }
                        }
                        return result; // 첫 번째 레코드만
                    }
                }
                return null;
            }
            catch (Exception ex)
            {
                WriteError($"레코드 읽기 실패: {ex.Message}");
                return null;
            }
        }

        /// <summary>레코드 존재 여부</summary>
        public bool RecordExists(string tableName, ObjectId entityId)
        {
            if (!TableExists(tableName)) return false;

            try
            {
                Tables tables = GetOdtTables();
                MapTable table = tables[tableName];

                using (Records recs = table.GetObjectTableRecords(
                    0, entityId,
                    MapOpenMode.OpenForRead, true))
                {
                    return recs.Count > 0;
                }
            }
            catch
            {
                return false;
            }
        }

        /// <summary>레코드 제거</summary>
        public bool RemoveRecord(string tableName, ObjectId entityId)
        {
            if (!TableExists(tableName)) return false;

            try
            {
                Tables tables = GetOdtTables();
                MapTable table = tables[tableName];

                // 체크리스트 #9: 삭제는 OpenForWrite
                using (Records recs = table.GetObjectTableRecords(
                    0, entityId,
                    MapOpenMode.OpenForWrite, true))
                {
                    if (recs.Count == 0) return false;

                    System.Collections.IEnumerator ie = recs.GetEnumerator();
                    while (ie.MoveNext())
                    {
                        recs.RemoveRecord();
                    }
                }
                return true;
            }
            catch (Exception ex)
            {
                WriteError($"레코드 제거 실패: {ex.Message}");
                return false;
            }
        }

        // ─── 내부 헬퍼 ───

        private void WriteError(string msg)
        {
            try
            {
                var ed = Application.DocumentManager.MdiActiveDocument?.Editor;
                ed?.WriteMessage($"\n[OdtManager] {msg}");
            }
            catch { }
        }
    }
}
