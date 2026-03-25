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

        public ObservableCollection<BlockItem> Blocks { get; }
            = new ObservableCollection<BlockItem>();

        private ICollectionView _sortedBlocks;
        public ICollectionView SortedBlocks
        {
            get
            {
                if (_sortedBlocks == null)
                {
                    _sortedBlocks = CollectionViewSource.GetDefaultView(Blocks);
                    _sortedBlocks.SortDescriptions.Add(
                        new SortDescription("DisplayName", ListSortDirection.Ascending));
                }
                return _sortedBlocks;
            }
        }

        // 별칭/즐겨찾기 데이터
        private Dictionary<string, string> _aliases = new Dictionary<string, string>();
        private HashSet<string> _favorites = new HashSet<string>();

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

        // 정렬
        private int _sortModeIndex;
        public int SortModeIndex
        {
            get => _sortModeIndex;
            set { SetProperty(ref _sortModeIndex, value); ApplySort(); }
        }

        public string[] SortModeNames { get; } = { "이름순", "별칭순", "즐겨찾기 우선" };

        // 크기
        private static readonly double[] ThumbSizes = { 56, 36, 24 };
        private static readonly double[] TileSizes = { 64, 44, 32 };
        private static readonly string[] SizeLabels = { "크게", "중간", "작게" };
        private int _sizeIndex;

        public double ThumbWidth => ThumbSizes[_sizeIndex];
        public double TileWidth => TileSizes[_sizeIndex];
        public string SizeLabel => SizeLabels[_sizeIndex];

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
            SortedBlocks.SortDescriptions.Clear();
            switch (_sortModeIndex)
            {
                case 0: // 이름순
                    SortedBlocks.SortDescriptions.Add(
                        new SortDescription("Name", ListSortDirection.Ascending));
                    break;
                case 1: // 별칭순
                    SortedBlocks.SortDescriptions.Add(
                        new SortDescription("DisplayName", ListSortDirection.Ascending));
                    break;
                case 2: // 즐겨찾기 우선
                    SortedBlocks.SortDescriptions.Add(
                        new SortDescription("IsFavorite", ListSortDirection.Descending));
                    SortedBlocks.SortDescriptions.Add(
                        new SortDescription("DisplayName", ListSortDirection.Ascending));
                    break;
            }
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

                // NOD에서 별칭/즐겨찾기 로드
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

                        Blocks.Add(new BlockItem
                        {
                            Name = btr.Name,
                            BlockId = btrId,
                            Thumbnail = thumbnail,
                            Alias = alias,
                            IsFavorite = _favorites.Contains(btr.Name)
                        });
                    }

                    tr.Commit();
                }
            }
            catch (Exception ex)
            {
                doc.Editor.WriteMessage($"\n블록 목록 로드 실패: {ex.Message}");
            }

            BlockCount = Blocks.Count;
            ApplySort();
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
            SortedBlocks.Refresh();
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

            item.IsFavorite = _favorites.Contains(item.Name);
            SortedBlocks.Refresh();
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
