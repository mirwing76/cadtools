using System.Windows.Input;
using Autodesk.AutoCAD.ApplicationServices;
using GntTools.Core.Selection;
using GntTools.Core.Settings;
using GntTools.Swl;

namespace GntTools.UI.ViewModels
{
    public class SwlViewModel : ViewModelBase
    {
        private readonly SwlService _service = new SwlService();

        // 기본정보
        private string _installYear;
        public string InstallYear { get => _installYear; set => SetProperty(ref _installYear, value); }
        private string _material;
        public string Material { get => _material; set => SetProperty(ref _material, value); }
        private string _diameter;
        public string Diameter { get => _diameter; set => SetProperty(ref _diameter, value); }
        private string _useCode;
        public string UseCode { get => _useCode; set => SetProperty(ref _useCode, value); }

        // 박스관
        private bool _isBoxPipe;
        public bool IsBoxPipe { get => _isBoxPipe; set => SetProperty(ref _isBoxPipe, value); }
        private double _boxWidth;
        public double BoxWidth { get => _boxWidth; set => SetProperty(ref _boxWidth, value); }
        private double _boxHeight;
        public double BoxHeight { get => _boxHeight; set => SetProperty(ref _boxHeight, value); }
        private int _lineCount = 1;
        public int LineCount { get => _lineCount; set => SetProperty(ref _lineCount, value); }

        // 심도
        private bool _isAutoDepth;
        public bool IsAutoDepth { get => _isAutoDepth; set => SetProperty(ref _isAutoDepth, value); }
        private bool _isManualDepth = true;
        public bool IsManualDepth { get => _isManualDepth; set => SetProperty(ref _isManualDepth, value); }
        private double _beginDepth;
        public double BeginDepth { get => _beginDepth; set => SetProperty(ref _beginDepth, value); }
        private double _endDepth;
        public double EndDepth { get => _endDepth; set => SetProperty(ref _endDepth, value); }
        private bool _isUndetected;
        public bool IsUndetected { get => _isUndetected; set => SetProperty(ref _isUndetected, value); }

        // 지반고
        private double _beginGroundHeight;
        public double BeginGroundHeight { get => _beginGroundHeight; set => SetProperty(ref _beginGroundHeight, value); }
        private double _endGroundHeight;
        public double EndGroundHeight { get => _endGroundHeight; set => SetProperty(ref _endGroundHeight, value); }

        // 계산결과 (읽기전용 표시)
        private string _elevationInfo = "";
        public string ElevationInfo { get => _elevationInfo; set => SetProperty(ref _elevationInfo, value); }

        private bool _useLeader;
        public bool UseLeader { get => _useLeader; set => SetProperty(ref _useLeader, value); }

        public ICommand CreateCommand { get; }
        public ICommand ModifyCommand { get; }
        public ICommand CalcElevationCommand { get; }

        public SwlViewModel()
        {
            var s = AppSettings.Instance;
            InstallYear = s.Swl.Defaults.Year;
            Material = s.Swl.Defaults.Material;
            Diameter = s.Swl.Defaults.Diameter;
            UseCode = s.Swl.Defaults.UseCode;

            CreateCommand = new RelayCommand(ExecuteCreate);
            ModifyCommand = new RelayCommand(ExecuteModify);
            CalcElevationCommand = new RelayCommand(CalcPreview);
        }

        /// <summary>표고 미리보기 계산</summary>
        private void CalcPreview()
        {
            double diamM = ElevationCalculator.DiameterToMeter(Diameter);
            var elev = ElevationCalculator.Calculate(
                BeginGroundHeight, EndGroundHeight,
                BeginDepth, EndDepth, diamM, 1.0);
            ElevationInfo = $"관저고: {elev.BeginInvertLevel:F3}→{elev.EndInvertLevel:F3}  구배: {elev.Slope:F3}%";
        }

        public SwlInputData ToInputData()
        {
            return new SwlInputData
            {
                InstallYear = InstallYear,
                Material = Material,
                Diameter = Diameter,
                UseCode = UseCode,
                IsBoxPipe = IsBoxPipe,
                BoxWidth = BoxWidth,
                BoxHeight = BoxHeight,
                LineCount = LineCount,
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
                    "\n하수관로 폴리라인을 선택하세요: ");
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
                    "\n수정할 하수관로를 선택하세요: ");
                if (polyId.IsNull) return;
                _service.ModifyAttribute(ToInputData(), polyId);
            }
        }
    }
}
