using System.Windows.Input;
using GntTools.Core.Settings;

namespace GntTools.UI.ViewModels
{
    public class SettingsViewModel : ViewModelBase
    {
        private AppSettings Settings => AppSettings.Instance;

        // 공통
        public string TextStyle
        {
            get => Settings.Common.TextStyle;
            set { Settings.Common.TextStyle = value; OnPropertyChanged(); }
        }
        public double TextSize
        {
            get => Settings.Common.TextSize;
            set { Settings.Common.TextSize = value; OnPropertyChanged(); }
        }
        public int LengthDecimals
        {
            get => Settings.Common.LengthDecimals;
            set { Settings.Common.LengthDecimals = value; OnPropertyChanged(); }
        }
        public int DepthDecimals
        {
            get => Settings.Common.DepthDecimals;
            set { Settings.Common.DepthDecimals = value; OnPropertyChanged(); }
        }

        // WTL 레이어
        public string WtlDepthLayer
        {
            get => Settings.Wtl.Layers.Depth;
            set { Settings.Wtl.Layers.Depth = value; OnPropertyChanged(); }
        }
        public string WtlHeightLayer
        {
            get => Settings.Wtl.Layers.GroundHeight;
            set { Settings.Wtl.Layers.GroundHeight = value; OnPropertyChanged(); }
        }
        public string WtlLabelLayer
        {
            get => Settings.Wtl.Layers.Label;
            set { Settings.Wtl.Layers.Label = value; OnPropertyChanged(); }
        }
        public string WtlLeaderLayer
        {
            get => Settings.Wtl.Layers.Leader;
            set { Settings.Wtl.Layers.Leader = value; OnPropertyChanged(); }
        }
        public string WtlUndetectedLayer
        {
            get => Settings.Wtl.Layers.Undetected;
            set { Settings.Wtl.Layers.Undetected = value; OnPropertyChanged(); }
        }

        // SWL 레이어
        public string SwlDepthLayer
        {
            get => Settings.Swl.Layers.Depth;
            set { Settings.Swl.Layers.Depth = value; OnPropertyChanged(); }
        }
        public string SwlLabelLayer
        {
            get => Settings.Swl.Layers.Label;
            set { Settings.Swl.Layers.Label = value; OnPropertyChanged(); }
        }

        // KEPCO 레이어
        public string KepcoDepthLayer
        {
            get => Settings.Kepco.Layers.Depth;
            set { Settings.Kepco.Layers.Depth = value; OnPropertyChanged(); }
        }
        public string KepcoDrawingLayer
        {
            get => Settings.Kepco.Layers.Drawing;
            set { Settings.Kepco.Layers.Drawing = value; OnPropertyChanged(); }
        }

        // 저장 명령
        public ICommand SaveCommand { get; }

        public SettingsViewModel()
        {
            SaveCommand = new RelayCommand(() =>
            {
                Settings.Save();
                Autodesk.AutoCAD.ApplicationServices.Application
                    .DocumentManager.MdiActiveDocument.Editor
                    .WriteMessage("\n환경설정이 저장되었습니다.");
            });
        }
    }
}
