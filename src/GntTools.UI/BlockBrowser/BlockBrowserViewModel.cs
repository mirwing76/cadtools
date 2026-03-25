using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using System.Windows.Data;
using System.Windows.Input;
using Autodesk.AutoCAD.ApplicationServices;
using Autodesk.AutoCAD.DatabaseServices;
using Autodesk.AutoCAD.EditorInput;
using GntTools.UI.ViewModels;

namespace GntTools.UI.BlockBrowser
{
    public class BlockBrowserViewModel : ViewModelBase
    {
        private readonly BlockThumbnailRenderer _renderer = new BlockThumbnailRenderer();

        // 즐겨찾기 / 전체 분리 컬렉션
        public ObservableCollection<BlockItem> FavoriteBlocks { get; }
            = new ObservableCollection<BlockItem>();
        public ObservableCollection<BlockItem> AllBlocks { get; }
            = new ObservableCollection<BlockItem>();

        // 기존 호환용
        public ObservableCollection<BlockItem> Blocks { get; }
            = new ObservableCollection<BlockItem>();

        // 별칭/즐겨찾기 데이터
        private Dictionary<string, string> _aliases = new Dictionary<string, string>();
        private HashSet<string> _favorites = new HashSet<string>();

        // 선택 상태
        private BlockItem _selectedItem;
        public BlockItem SelectedItem
        {
            get => _selectedItem;
            set => SetProperty(ref _selectedItem, value);
        }

        // Insert state
        private bool _isInserting;
        private BlockItem _pendingInsert;

        // 뷰 상태
        private bool _isGridView = true;
        public bool IsGridView
        {
            get => _isGridView;
            set { SetProperty(ref _isGridView, value); }
        }

        private int _blockCount;
        public int BlockCount
        {
            get => _blockCount;
            set => SetProperty(ref _blockCount, value);
        }

        private int _favoriteCount;
        public int FavoriteCount
        {
            get => _favoriteCount;
            set => SetProperty(ref _favoriteCount, value);
        }

        // 정렬
        private int _sortModeIndex;
        public int SortModeIndex
        {
            get => _sortModeIndex;
            set { SetProperty(ref _sortModeIndex, value); ApplySort(); }
        }

        public string[] SortModeNames { get; } = { "이름순", "별칭순" };

        // 크기
        private static readonly double[] ThumbSizes = { 56, 36, 24 };
        private static readonly double[] TileSizes = { 64, 44, 32 };
        private static readonly string[] SizeLabels = { "크게", "중간", "작게" };
        private int _sizeIndex;

        public double ThumbWidth => ThumbSizes[_sizeIndex];
        public double TileWidth => TileSizes[_sizeIndex];
        public string SizeLabel => SizeLabels[_sizeIndex];

        // Insert options
        private double _insertScale = 1.0;
        public double InsertScale
        {
            get => _insertScale;
            set => SetProperty(ref _insertScale, value);
        }

        private double _insertRotation;
        public double InsertRotation
        {
            get => _insertRotation;
            set => SetProperty(ref _insertRotation, value);
        }

        private bool _explodeOnInsert;
        public bool ExplodeOnInsert
        {
            get => _explodeOnInsert;
            set => SetProperty(ref _explodeOnInsert, value);
        }

        // Commands
        public ICommand RefreshCommand { get; }
        public ICommand ToggleViewCommand { get; }
        public ICommand CycleSizeCommand { get; }

        public BlockBrowserViewModel()
        {
            RefreshCommand = new RelayCommand(Refresh);
            ToggleViewCommand = new RelayCommand(() => IsGridView = !IsGridView);
            CycleSizeCommand = new RelayCommand(CycleSize);
        }

        private void CycleSize()
        {
            _sizeIndex = (_sizeIndex + 1) % 3;
            OnPropertyChanged(nameof(ThumbWidth));
            OnPropertyChanged(nameof(TileWidth));
            OnPropertyChanged(nameof(SizeLabel));
            Refresh();
        }

        private void ApplySort()
        {
            SortBlocks(FavoriteBlocks);
            SortBlocks(AllBlocks);
        }

        private void SortBlocks(ObservableCollection<BlockItem> collection)
        {
            var view = CollectionViewSource.GetDefaultView(collection);
            view.SortDescriptions.Clear();
            switch (_sortModeIndex)
            {
                case 0: // 이름순
                    view.SortDescriptions.Add(
                        new SortDescription("Name", ListSortDirection.Ascending));
                    break;
                case 1: // 별칭순
                    view.SortDescriptions.Add(
                        new SortDescription("DisplayName", ListSortDirection.Ascending));
                    break;
            }
        }

        public void Refresh()
        {
            FavoriteBlocks.Clear();
            AllBlocks.Clear();
            Blocks.Clear();
            _renderer.ClearCache();

            var doc = Application.DocumentManager.MdiActiveDocument;
            if (doc == null) return;

            try
            {
                var db = doc.Database;

                _aliases = BlockDataStore.LoadAliases(db);
                _favorites = BlockDataStore.LoadFavorites(db);

                using (var tr = db.TransactionManager.StartTransaction())
                {
                    var bt = (BlockTable)tr.GetObject(db.BlockTableId, OpenMode.ForRead);

                    foreach (ObjectId btrId in bt)
                    {
                        var btr = (BlockTableRecord)tr.GetObject(btrId, OpenMode.ForRead);

                        if (btr.IsAnonymous) continue;
                        if (btr.IsLayout) continue;
                        if (btr.Name.StartsWith("*")) continue;

                        var thumbnail = _renderer.Render(btr, tr, ThumbWidth);

                        string alias = null;
                        _aliases.TryGetValue(btr.Name, out alias);

                        var item = new BlockItem
                        {
                            Name = btr.Name,
                            BlockId = btrId,
                            Thumbnail = thumbnail,
                            Alias = alias,
                            IsFavorite = _favorites.Contains(btr.Name)
                        };

                        Blocks.Add(item);
                        if (item.IsFavorite)
                            FavoriteBlocks.Add(item);
                        else
                            AllBlocks.Add(item);
                    }

                    tr.Commit();
                }
            }
            catch (Exception ex)
            {
                doc.Editor.WriteMessage($"\nBlock list load failed: {ex.Message}");
            }

            BlockCount = AllBlocks.Count;
            FavoriteCount = FavoriteBlocks.Count;
            ApplySort();
        }

        public void SelectBlock(BlockItem item)
        {
            SelectedItem = item;
        }

        public void SetAlias(BlockItem item, string alias)
        {
            var doc = Application.DocumentManager.MdiActiveDocument;
            if (doc == null) return;

            using (doc.LockDocument())
            {
                if (string.IsNullOrWhiteSpace(alias))
                    _aliases.Remove(item.Name);
                else
                    _aliases[item.Name] = alias.Trim();

                BlockDataStore.SaveAliases(doc.Database, _aliases);
            }

            item.Alias = string.IsNullOrWhiteSpace(alias) ? null : alias.Trim();
            Refresh();
        }

        public void ToggleFavorite(BlockItem item)
        {
            var doc = Application.DocumentManager.MdiActiveDocument;
            if (doc == null) return;

            using (doc.LockDocument())
            {
                if (_favorites.Contains(item.Name))
                    _favorites.Remove(item.Name);
                else
                    _favorites.Add(item.Name);

                BlockDataStore.SaveFavorites(doc.Database, _favorites);
            }

            Refresh();
        }

        /// <summary>블록 삽입 요청 (중복 방지, 새 선택 시 이전 취소)</summary>
        public void RequestInsert(BlockItem item)
        {
            if (_isInserting)
            {
                // 같은 블록이면 무시
                if (ReferenceEquals(item, SelectedItem)) return;

                // 다른 블록 → 이전 삽입 취소하고 새 블록 대기
                _pendingInsert = item;
                CancelCurrentInsert();
                return;
            }

            DoInsert(item);
        }

        private void CancelCurrentInsert()
        {
            try
            {
                var doc = Application.DocumentManager.MdiActiveDocument;
                if (doc != null)
                    doc.SendStringToExecute("\x03\x03", true, false, false); // ESC ESC
            }
            catch { }
        }

        private void DoInsert(BlockItem item)
        {
            _isInserting = true;
            _pendingInsert = null;

            try
            {
                var doc = Application.DocumentManager.MdiActiveDocument;
                if (doc == null) return;

                using (doc.LockDocument())
                {
                    var ed = doc.Editor;

                    // 1. Insertion point
                    var ptResult = ed.GetPoint($"\nInsert [{item.DisplayName}] - Specify point: ");
                    if (ptResult.Status != PromptStatus.OK) return;

                    // 2. Rotation
                    double rotation = InsertRotation;
                    if (rotation == 0)
                    {
                        var angOpt = new PromptAngleOptions("\nSpecify rotation angle <0>: ");
                        angOpt.DefaultValue = 0;
                        angOpt.UseDefaultValue = true;
                        angOpt.BasePoint = ptResult.Value;
                        angOpt.UseBasePoint = true;
                        var angResult = ed.GetAngle(angOpt);
                        if (angResult.Status == PromptStatus.Cancel) return; // ESC → cancel
                        if (angResult.Status == PromptStatus.OK)
                            rotation = angResult.Value;
                    }
                    else
                    {
                        rotation = InsertRotation * Math.PI / 180.0;
                    }

                    double scale = InsertScale;
                    if (scale <= 0) scale = 1.0;

                    var db = doc.Database;
                    using (var tr = db.TransactionManager.StartTransaction())
                    {
                        var bt = (BlockTable)tr.GetObject(db.BlockTableId, OpenMode.ForRead);
                        var ms = (BlockTableRecord)tr.GetObject(
                            bt[BlockTableRecord.ModelSpace], OpenMode.ForWrite);

                        var blkRef = new BlockReference(ptResult.Value, item.BlockId);
                        blkRef.ScaleFactors = new Autodesk.AutoCAD.Geometry.Scale3d(scale, scale, scale);
                        blkRef.Rotation = rotation;

                        ms.AppendEntity(blkRef);
                        tr.AddNewlyCreatedDBObject(blkRef, true);

                        if (ExplodeOnInsert)
                        {
                            var exploded = new DBObjectCollection();
                            blkRef.Explode(exploded);
                            foreach (DBObject obj in exploded)
                            {
                                var ent = obj as Entity;
                                if (ent != null)
                                {
                                    ms.AppendEntity(ent);
                                    tr.AddNewlyCreatedDBObject(ent, true);
                                }
                            }
                            blkRef.Erase();
                        }

                        tr.Commit();
                    }
                }
            }
            catch { }
            finally
            {
                _isInserting = false;

                // 대기 중인 블록이 있으면 이어서 삽입
                if (_pendingInsert != null)
                {
                    var next = _pendingInsert;
                    _pendingInsert = null;
                    SelectBlock(next);
                    DoInsert(next);
                }
            }
        }
    }
}
