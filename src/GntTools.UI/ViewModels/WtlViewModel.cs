using System.Windows.Input;
using Autodesk.AutoCAD.ApplicationServices;
using GntTools.Core.Selection;
using GntTools.Core.Settings;
using GntTools.Wtl;

namespace GntTools.UI.ViewModels
{
    public class WtlViewModel : ViewModelBase
    {
        private readonly WtlService _service = new WtlService();

        // 기본정보
        private string _installYear;
        public string InstallYear
        {
            get => _installYear;
            set => SetProperty(ref _installYear, value);
        }

        private string _material;
        public string Material
        {
            get => _material;
            set => SetProperty(ref _material, value);
        }

        private string _diameter;
        public string Diameter
        {
            get => _diameter;
            set => SetProperty(ref _diameter, value);
        }

        // 심도
        private bool _isAutoDepth;
        public bool IsAutoDepth
        {
            get => _isAutoDepth;
            set => SetProperty(ref _isAutoDepth, value);
        }

        private bool _isManualDepth = true;
        public bool IsManualDepth
        {
            get => _isManualDepth;
            set => SetProperty(ref _isManualDepth, value);
        }

        private double _beginDepth;
        public double BeginDepth
        {
            get => _beginDepth;
            set => SetProperty(ref _beginDepth, value);
        }

        private double _endDepth;
        public double EndDepth
        {
            get => _endDepth;
            set => SetProperty(ref _endDepth, value);
        }

        private bool _isUndetected;
        public bool IsUndetected
        {
            get => _isUndetected;
            set => SetProperty(ref _isUndetected, value);
        }

        // 지반고
        private double _beginGroundHeight;
        public double BeginGroundHeight
        {
            get => _beginGroundHeight;
            set => SetProperty(ref _beginGroundHeight, value);
        }

        private double _endGroundHeight;
        public double EndGroundHeight
        {
            get => _endGroundHeight;
            set => SetProperty(ref _endGroundHeight, value);
        }

        // 라벨 모드
        private bool _useLeader;
        public bool UseLeader
        {
            get => _useLeader;
            set => SetProperty(ref _useLeader, value);
        }

        // Commands
        public ICommand CreateCommand { get; }
        public ICommand ModifyCommand { get; }

        public WtlViewModel()
        {
            var d = AppSettings.Instance.Wtl.Defaults;
            InstallYear = d.Year;
            Material = d.Material;
            Diameter = d.Diameter;
            IsAutoDepth = d.IsAutoDepth;
            IsManualDepth = !d.IsAutoDepth;
            BeginDepth = d.BeginDepth;
            EndDepth = d.EndDepth;
            IsUndetected = d.IsUndetected;
            BeginGroundHeight = d.BeginGroundHeight;
            EndGroundHeight = d.EndGroundHeight;
            UseLeader = d.UseLeader;

            CreateCommand = new RelayCommand(ExecuteCreate);
            ModifyCommand = new RelayCommand(ExecuteModify);
        }

        /// <summary>현재 팔레트 값을 settings.json에 저장</summary>
        public void SaveState()
        {
            var d = AppSettings.Instance.Wtl.Defaults;
            d.Year = InstallYear;
            d.Material = Material;
            d.Diameter = Diameter;
            d.IsAutoDepth = IsAutoDepth;
            d.BeginDepth = BeginDepth;
            d.EndDepth = EndDepth;
            d.IsUndetected = IsUndetected;
            d.BeginGroundHeight = BeginGroundHeight;
            d.EndGroundHeight = EndGroundHeight;
            d.UseLeader = UseLeader;
        }

        /// <summary>팔레트 값 → WtlInputData 변환</summary>
        public WtlInputData ToInputData()
        {
            return new WtlInputData
            {
                InstallYear = InstallYear,
                Material = Material,
                Diameter = Diameter,
                IsAutoDepth = IsAutoDepth,
                ManualBeginDepth = BeginDepth,
                ManualEndDepth = EndDepth,
                IsUndetected = IsUndetected,
                BeginGroundHeight = BeginGroundHeight,
                EndGroundHeight = EndGroundHeight,
                UseLeader = UseLeader,
            };
        }

        private void ExecuteCreate()
        {
            var doc = Application.DocumentManager.MdiActiveDocument;
            using (doc.LockDocument())
            {
                _service.EnsureTables();

                var selector = new EntitySelector();
                var polyId = selector.SelectOne(
                    EntitySelector.PolylineFilter(),
                    "\n상수관로 폴리라인을 선택하세요: ");

                if (polyId.IsNull) return;
                _service.CreateAttribute(ToInputData(), polyId);
            }
        }

        private void ExecuteModify()
        {
            var doc = Application.DocumentManager.MdiActiveDocument;
            using (doc.LockDocument())
            {
                _service.EnsureTables();

                var selector = new EntitySelector();
                var polyId = selector.SelectOne(
                    EntitySelector.PolylineFilter(),
                    "\n수정할 상수관로를 선택하세요: ");

                if (polyId.IsNull) return;
                _service.ModifyAttribute(ToInputData(), polyId);
            }
        }
    }
}
