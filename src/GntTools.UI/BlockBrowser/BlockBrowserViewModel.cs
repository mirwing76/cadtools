using System.Collections.ObjectModel;
using System.Windows.Input;
using Autodesk.AutoCAD.ApplicationServices;
using Autodesk.AutoCAD.DatabaseServices;
using Autodesk.AutoCAD.EditorInput;
using Autodesk.AutoCAD.Geometry;
using GntTools.UI.ViewModels;

namespace GntTools.UI.BlockBrowser
{
    public class BlockBrowserViewModel : ViewModelBase
    {
        private readonly BlockThumbnailRenderer _renderer = new BlockThumbnailRenderer();

        public ObservableCollection<BlockItem> Blocks { get; }
            = new ObservableCollection<BlockItem>();

        private bool _isGridView = true;
        public bool IsGridView
        {
            get => _isGridView;
            set => SetProperty(ref _isGridView, value);
        }

        private int _blockCount;
        public int BlockCount
        {
            get => _blockCount;
            set => SetProperty(ref _blockCount, value);
        }

        public ICommand RefreshCommand { get; }
        public ICommand ToggleViewCommand { get; }

        public BlockBrowserViewModel()
        {
            RefreshCommand = new RelayCommand(Refresh);
            ToggleViewCommand = new RelayCommand(() => IsGridView = !IsGridView);
        }

        public void Refresh()
        {
            Blocks.Clear();
            _renderer.ClearCache();

            var doc = Application.DocumentManager.MdiActiveDocument;
            if (doc == null) return;

            try
            {
                var db = doc.Database;
                using (var tr = db.TransactionManager.StartTransaction())
                {
                    var bt = (BlockTable)tr.GetObject(db.BlockTableId, OpenMode.ForRead);

                    foreach (ObjectId btrId in bt)
                    {
                        var btr = (BlockTableRecord)tr.GetObject(btrId, OpenMode.ForRead);

                        // 익명 블록, 레이아웃 블록, ModelSpace, PaperSpace 제외
                        if (btr.IsAnonymous) continue;
                        if (btr.IsLayout) continue;
                        if (btr.Name.StartsWith("*")) continue;

                        var thumbnail = _renderer.Render(btr, tr);

                        Blocks.Add(new BlockItem
                        {
                            Name = btr.Name,
                            BlockId = btrId,
                            Thumbnail = thumbnail
                        });
                    }

                    tr.Commit();
                }
            }
            catch (System.Exception ex)
            {
                doc.Editor.WriteMessage($"\n블록 목록 로드 실패: {ex.Message}");
            }

            BlockCount = Blocks.Count;
        }

        /// <summary>블록 삽입 — PaletteSet에서 호출되므로 LockDocument 필수</summary>
        public void InsertBlock(BlockItem item)
        {
            var doc = Application.DocumentManager.MdiActiveDocument;
            if (doc == null) return;

            using (doc.LockDocument())
            {
                var ed = doc.Editor;
                var pr = ed.GetPoint("\n블록 삽입점을 지정하세요: ");
                if (pr.Status != PromptStatus.OK) return;

                var db = doc.Database;
                using (var tr = db.TransactionManager.StartTransaction())
                {
                    var bt = (BlockTable)tr.GetObject(db.BlockTableId, OpenMode.ForRead);
                    var ms = (BlockTableRecord)tr.GetObject(
                        bt[BlockTableRecord.ModelSpace], OpenMode.ForWrite);

                    var blkRef = new BlockReference(pr.Value, item.BlockId);
                    ms.AppendEntity(blkRef);
                    tr.AddNewlyCreatedDBObject(blkRef, true);

                    tr.Commit();
                }
            }
        }
    }
}
