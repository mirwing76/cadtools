using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using AcApp = Autodesk.AutoCAD.ApplicationServices.Application;

namespace GntTools.UI
{
    /// <summary>AutoCAD COLORTHEME에 맞춰 WPF 컨트롤 색상 적용</summary>
    public static class ThemeHelper
    {
        /// <summary>AutoCAD 다크 테마 여부 (COLORTHEME: 0=dark, 1=light)</summary>
        public static bool IsDarkTheme
        {
            get
            {
                try
                {
                    var val = AcApp.GetSystemVariable("COLORTHEME");
                    return val != null && val.ToString() == "0";
                }
                catch { return true; } // 기본값: 다크
            }
        }

        /// <summary>UserControl에 AutoCAD 테마 색상 적용</summary>
        public static void ApplyTheme(UserControl control)
        {
            bool dark = IsDarkTheme;

            var fg = dark ? Brushes.White : Brushes.Black;
            var bg = Brushes.Transparent;
            var grayText = dark
                ? new SolidColorBrush(Color.FromRgb(170, 170, 170))
                : new SolidColorBrush(Color.FromRgb(109, 109, 109));
            var border = dark
                ? new SolidColorBrush(Color.FromRgb(80, 80, 80))
                : new SolidColorBrush(Color.FromRgb(170, 170, 170));
            var controlBg = dark
                ? new SolidColorBrush(Color.FromRgb(45, 45, 48))
                : new SolidColorBrush(Color.FromRgb(240, 240, 240));

            grayText.Freeze();
            border.Freeze();
            controlBg.Freeze();

            control.Foreground = fg;
            control.Background = bg;

            // SystemColors 오버라이드
            control.Resources[SystemColors.ControlTextBrushKey] = fg;
            control.Resources[SystemColors.WindowTextBrushKey] = fg;
            control.Resources[SystemColors.GrayTextBrushKey] = grayText;
            control.Resources[SystemColors.ActiveBorderBrushKey] = border;
            control.Resources[SystemColors.ControlBrushKey] = controlBg;

            // TextBox: 밝은 배경 + 검은 글씨 유지 (Foreground 상속 방지)
            var textBoxStyle = new Style(typeof(TextBox));
            textBoxStyle.Setters.Add(new Setter(TextBox.ForegroundProperty, Brushes.Black));
            textBoxStyle.Setters.Add(new Setter(TextBox.BackgroundProperty, Brushes.White));
            control.Resources[typeof(TextBox)] = textBoxStyle;

            // ComboBox: 동일하게 검은 글씨
            var comboStyle = new Style(typeof(ComboBox));
            comboStyle.Setters.Add(new Setter(ComboBox.ForegroundProperty, Brushes.Black));
            comboStyle.Setters.Add(new Setter(ComboBox.BackgroundProperty, Brushes.White));
            control.Resources[typeof(ComboBox)] = comboStyle;
        }

        /// <summary>썸네일 배경색 반환</summary>
        public static Color ThumbnailBackgroundColor
        {
            get => IsDarkTheme
                ? Color.FromRgb(45, 45, 48)
                : Color.FromRgb(240, 240, 240);
        }

        /// <summary>썸네일 텍스트 브러시</summary>
        public static Brush ThumbnailTextBrush
        {
            get => IsDarkTheme
                ? new SolidColorBrush(Color.FromRgb(170, 170, 170))
                : new SolidColorBrush(Color.FromRgb(109, 109, 109));
        }
    }
}
