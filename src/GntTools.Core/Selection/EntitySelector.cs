using Autodesk.AutoCAD.ApplicationServices;
using Autodesk.AutoCAD.DatabaseServices;
using Autodesk.AutoCAD.EditorInput;

namespace GntTools.Core.Selection
{
    /// <summary>엔티티 선택 유틸리티 (사용자 대화형 + 프로그래밍 방식)</summary>
    public class EntitySelector
    {
        private Editor GetEditor()
        {
            return Application.DocumentManager.MdiActiveDocument.Editor;
        }

        /// <summary>단일 엔티티 선택 (사용자 클릭)</summary>
        public ObjectId SelectOne(SelectionFilter filter = null, string message = null)
        {
            var ed = GetEditor();
            var opts = new PromptEntityOptions(
                message ?? "\n엔티티를 선택하세요: ");
            opts.SetRejectMessage("\n유효하지 않은 객체입니다.");

            var result = ed.GetEntity(opts);
            if (result.Status != PromptStatus.OK)
                return ObjectId.Null;

            // 필터가 있으면 선택된 객체 검증
            if (filter != null)
            {
                var db = Application.DocumentManager.MdiActiveDocument.Database;
                using (var tr = db.TransactionManager.StartTransaction())
                {
                    var ent = tr.GetObject(result.ObjectId, OpenMode.ForRead) as Entity;
                    tr.Commit();

                    if (ent == null) return ObjectId.Null;
                    // DxfName 기반 간단 검증
                    string dxfName = ent.GetRXClass().DxfName.ToUpper();
                    // filter의 첫 번째 TypedValue에서 허용 타입 추출
                    foreach (TypedValue tv in filter.GetFilter())
                    {
                        if (tv.TypeCode == (int)DxfCode.Start)
                        {
                            string allowed = tv.Value.ToString().ToUpper();
                            if (!allowed.Contains(dxfName))
                                return ObjectId.Null;
                        }
                    }
                }
            }

            return result.ObjectId;
        }

        /// <summary>복수 엔티티 선택 (사용자 윈도우/크로싱)</summary>
        public ObjectId[] SelectMultiple(SelectionFilter filter = null,
            string message = null)
        {
            var ed = GetEditor();
            var opts = new PromptSelectionOptions();
            if (message != null)
                opts.MessageForAdding = message;

            PromptSelectionResult result;
            if (filter != null)
                result = ed.GetSelection(opts, filter);
            else
                result = ed.GetSelection(opts);

            if (result.Status != PromptStatus.OK)
                return new ObjectId[0];

            return result.Value.GetObjectIds();
        }

        /// <summary>도면 전체에서 필터 조건에 맞는 엔티티 선택</summary>
        public ObjectId[] SelectAll(SelectionFilter filter)
        {
            var ed = GetEditor();
            var result = ed.SelectAll(filter);
            if (result.Status != PromptStatus.OK)
                return new ObjectId[0];
            return result.Value.GetObjectIds();
        }

        /// <summary>폴리라인 전용 선택 필터</summary>
        public static SelectionFilter PolylineFilter()
        {
            return new SelectionFilter(new[]
            {
                new TypedValue((int)DxfCode.Start, "LWPOLYLINE,POLYLINE")
            });
        }

        /// <summary>특정 레이어의 텍스트 선택 필터</summary>
        public static SelectionFilter TextOnLayerFilter(string layerName)
        {
            return new SelectionFilter(new[]
            {
                new TypedValue((int)DxfCode.Start, "TEXT,MTEXT"),
                new TypedValue((int)DxfCode.LayerName, layerName)
            });
        }

        /// <summary>특정 레이어의 원(Circle) 선택 필터</summary>
        public static SelectionFilter CircleOnLayerFilter(string layerName)
        {
            return new SelectionFilter(new[]
            {
                new TypedValue((int)DxfCode.Start, "CIRCLE"),
                new TypedValue((int)DxfCode.LayerName, layerName)
            });
        }
    }
}
