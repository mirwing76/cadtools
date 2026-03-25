using System.Collections.Generic;
using System.Windows.Input;
using Autodesk.AutoCAD.ApplicationServices;
using GntTools.Core.Selection;
using GntTools.Core.Settings;
using GntTools.Kepco;

namespace GntTools.UI.ViewModels
{
    public class KepcoViewModel : ViewModelBase
    {
        private readonly KepcoService _service = new KepcoService();
        private readonly SectionCounter _counter = new SectionCounter();
        private readonly BxHCalculator _bxhCalc = new BxHCalculator();
        private readonly JunctionChecker _checker = new JunctionChecker();

        private string _installYear;
        public string InstallYear { get => _installYear; set => SetProperty(ref _installYear, value); }
        private string _material;
        public string Material { get => _material; set => SetProperty(ref _material, value); }

        // 단면 카운팅 결과 표시
        private string _sectionInfo = "";
        public string SectionInfo { get => _sectionInfo; set => SetProperty(ref _sectionInfo, value); }

        private string _bxh = "";
        public string BxH { get => _bxh; set => SetProperty(ref _bxh, value); }

        private bool _isUndetected;
        public bool IsUndetected { get => _isUndetected; set => SetProperty(ref _isUndetected, value); }

        private Dictionary<string, string> _pipeData = new Dictionary<string, string>();

        public ICommand SelectSectionCommand { get; }
        public ICommand SelectBCommand { get; }
        public ICommand CreateCommand { get; }
        public ICommand ModifyCommand { get; }
        public ICommand CheckCommand { get; }

        public KepcoViewModel()
        {
            var d = AppSettings.Instance.Kepco.Defaults;
            InstallYear = d.Year;
            Material = d.Material;
            IsUndetected = d.IsUndetected;

            SelectSectionCommand = new RelayCommand(DoSelectSection);
            SelectBCommand = new RelayCommand(DoSelectBH);
            CreateCommand = new RelayCommand(DoCreate);
            ModifyCommand = new RelayCommand(DoModify);
            CheckCommand = new RelayCommand(DoCheck);
        }

        public void SaveState()
        {
            var d = AppSettings.Instance.Kepco.Defaults;
            d.Year = InstallYear;
            d.Material = Material;
            d.IsUndetected = IsUndetected;
        }

        private void DoSelectSection()
        {
            var doc = Application.DocumentManager.MdiActiveDocument;
            using (doc.LockDocument())
            {
                var result = _counter.CountInSelection();
                _pipeData = result.ToPipeData();

                // 표시용 문자열
                var lines = new List<string>();
                var settings = AppSettings.Instance;
                foreach (int d in settings.Kepco.Diameters)
                {
                    string key = $"D{d}";
                    lines.Add($"{key}: {result.GetCountString(key)}");
                }
                SectionInfo = string.Join("  ", lines);
            }
        }

        private void DoSelectBH()
        {
            var doc = Application.DocumentManager.MdiActiveDocument;
            using (doc.LockDocument())
            {
                var (b, h, bxhStr) = _bxhCalc.MeasureFromSelection();
                BxH = bxhStr;
            }
        }

        private void DoCreate()
        {
            var doc = Application.DocumentManager.MdiActiveDocument;
            using (doc.LockDocument())
            {
                _service.EnsureTables();
                var selector = new EntitySelector();
                var polyId = selector.SelectOne(
                    EntitySelector.PolylineFilter(),
                    "\n전력관로 폴리라인을 선택하세요: ");
                if (polyId.IsNull) return;

                _service.CreateAttribute(new KepcoInputData
                {
                    InstallYear = InstallYear,
                    Material = Material,
                    PipeData = _pipeData,
                    BxH = BxH,
                    IsUndetected = IsUndetected,
                }, polyId);
            }
        }

        private void DoModify()
        {
            var doc = Application.DocumentManager.MdiActiveDocument;
            using (doc.LockDocument())
            {
                _service.EnsureTables();
                var selector = new EntitySelector();
                var polyId = selector.SelectOne(
                    EntitySelector.PolylineFilter(),
                    "\n수정할 전력관로를 선택하세요: ");
                if (polyId.IsNull) return;

                _service.ModifyAttribute(new KepcoInputData
                {
                    InstallYear = InstallYear,
                    Material = Material,
                    PipeData = _pipeData,
                    BxH = BxH,
                    IsUndetected = IsUndetected,
                }, polyId);
            }
        }

        private void DoCheck()
        {
            var doc = Application.DocumentManager.MdiActiveDocument;
            using (doc.LockDocument())
            {
                _checker.CheckSelected();
            }
        }
    }
}
