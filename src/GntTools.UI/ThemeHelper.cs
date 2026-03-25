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
                catch { return true; }
            }
        }

        // ─── 다크 테마 (AutoCAD Properties 팔레트 기준) ───
        private static readonly Color DarkBg        = Color.FromRgb(59, 68, 83);
        private static readonly Color DarkBgLight   = Color.FromRgb(69, 78, 93);
        private static readonly Color DarkText      = Color.FromRgb(220, 220, 220);
        private static readonly Color DarkGrayText  = Color.FromRgb(160, 160, 160);
        private static readonly Color DarkBorder    = Color.FromRgb(80, 90, 105);
        private static readonly Color DarkInputBg   = Color.FromRgb(50, 58, 72);

        // ─── 라이트 테마 ───
        private static readonly Color LightBg       = Color.FromRgb(222, 222, 222);
        private static readonly Color LightBgLight  = Color.FromRgb(240, 240, 240);
        private static readonly Color LightText     = Color.FromRgb(0, 0, 0);
        private static readonly Color LightGrayText = Color.FromRgb(109, 109, 109);
        private static readonly Color LightBorder   = Color.FromRgb(170, 170, 170);
        private static readonly Color LightInputBg  = Color.FromRgb(255, 255, 255);

        /// <summary>UserControl에 AutoCAD 테마 색상 적용</summary>
        public static void ApplyTheme(UserControl control)
        {
            bool dark = IsDarkTheme;

            var fg       = Freeze(dark ? DarkText     : LightText);
            var bg       = Freeze(dark ? DarkBg       : LightBg);
            var grayText = Freeze(dark ? DarkGrayText : LightGrayText);
            var border   = Freeze(dark ? DarkBorder   : LightBorder);
            var controlBg= Freeze(dark ? DarkBgLight  : LightBgLight);
            var inputBg  = Freeze(dark ? DarkInputBg  : LightInputBg);
            var inputFg  = Freeze(dark ? DarkText     : LightText);

            control.Foreground = fg;
            control.Background = new SolidColorBrush(dark ? DarkBg : LightBg);

            // SystemColors 오버라이드
            control.Resources[SystemColors.ControlTextBrushKey] = fg;
            control.Resources[SystemColors.WindowTextBrushKey] = fg;
            control.Resources[SystemColors.GrayTextBrushKey] = grayText;
            control.Resources[SystemColors.ActiveBorderBrushKey] = border;
            control.Resources[SystemColors.ControlBrushKey] = controlBg;

            // TextBox: 테마에 맞는 배경 + 글씨
            var textBoxStyle = new Style(typeof(TextBox));
            textBoxStyle.Setters.Add(new Setter(TextBox.ForegroundProperty, inputFg));
            textBoxStyle.Setters.Add(new Setter(TextBox.BackgroundProperty, inputBg));
            textBoxStyle.Setters.Add(new Setter(TextBox.BorderBrushProperty, border));
            textBoxStyle.Setters.Add(new Setter(TextBox.CaretBrushProperty, inputFg));
            control.Resources[typeof(TextBox)] = textBoxStyle;

            // ComboBox
            var comboStyle = new Style(typeof(ComboBox));
            comboStyle.Setters.Add(new Setter(ComboBox.ForegroundProperty, inputFg));
            comboStyle.Setters.Add(new Setter(ComboBox.BackgroundProperty, inputBg));
            comboStyle.Setters.Add(new Setter(ComboBox.BorderBrushProperty, border));
            control.Resources[typeof(ComboBox)] = comboStyle;

            // Button
            var btnStyle = new Style(typeof(Button));
            btnStyle.Setters.Add(new Setter(Button.ForegroundProperty, fg));
            btnStyle.Setters.Add(new Setter(Button.BackgroundProperty, controlBg));
            btnStyle.Setters.Add(new Setter(Button.BorderBrushProperty, border));
            control.Resources[typeof(Button)] = btnStyle;

            // ToggleButton
            var toggleStyle = new Style(typeof(System.Windows.Controls.Primitives.ToggleButton));
            toggleStyle.Setters.Add(new Setter(System.Windows.Controls.Primitives.ToggleButton.ForegroundProperty, fg));
            toggleStyle.Setters.Add(new Setter(System.Windows.Controls.Primitives.ToggleButton.BackgroundProperty, controlBg));
            toggleStyle.Setters.Add(new Setter(System.Windows.Controls.Primitives.ToggleButton.BorderBrushProperty, border));
            control.Resources[typeof(System.Windows.Controls.Primitives.ToggleButton)] = toggleStyle;

            // GroupBox
            var groupStyle = new Style(typeof(GroupBox));
            groupStyle.Setters.Add(new Setter(GroupBox.ForegroundProperty, fg));
            groupStyle.Setters.Add(new Setter(GroupBox.BorderBrushProperty, border));
            control.Resources[typeof(GroupBox)] = groupStyle;

            // CheckBox, RadioButton
            var checkStyle = new Style(typeof(CheckBox));
            checkStyle.Setters.Add(new Setter(CheckBox.ForegroundProperty, fg));
            control.Resources[typeof(CheckBox)] = checkStyle;

            var radioStyle = new Style(typeof(RadioButton));
            radioStyle.Setters.Add(new Setter(RadioButton.ForegroundProperty, fg));
            control.Resources[typeof(RadioButton)] = radioStyle;
        }

        /// <summary>썸네일 배경색</summary>
        public static Color ThumbnailBackgroundColor =>
            IsDarkTheme ? DarkBg : LightBg;

        /// <summary>썸네일 텍스트 브러시</summary>
        public static Brush ThumbnailTextBrush =>
            new SolidColorBrush(IsDarkTheme ? DarkGrayText : LightGrayText);

        private static SolidColorBrush Freeze(Color c)
        {
            var b = new SolidColorBrush(c);
            b.Freeze();
            return b;
        }
    }
}
