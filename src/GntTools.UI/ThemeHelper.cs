using System;
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
        private static readonly Color DarkBg          = Color.FromRgb(59, 68, 83);
        private static readonly Color DarkHeaderBg    = Color.FromRgb(69, 78, 93);
        private static readonly Color DarkText        = Color.FromRgb(220, 220, 220);
        private static readonly Color DarkValueText   = Color.FromRgb(200, 200, 200);
        private static readonly Color DarkGrayText    = Color.FromRgb(160, 160, 160);
        private static readonly Color DarkBorder      = Color.FromRgb(80, 90, 105);
        private static readonly Color DarkRowHover    = Color.FromRgb(70, 80, 100);
        private static readonly Color DarkSelectionBg = Color.FromRgb(51, 102, 204);
        private static readonly Color DarkSelectionBd = Color.FromRgb(70, 130, 230);

        // ─── 라이트 테마 ───
        private static readonly Color LightBg          = Color.FromRgb(240, 240, 240);
        private static readonly Color LightHeaderBg    = Color.FromRgb(222, 222, 222);
        private static readonly Color LightText        = Color.FromRgb(0, 0, 0);
        private static readonly Color LightValueText   = Color.FromRgb(30, 30, 30);
        private static readonly Color LightGrayText    = Color.FromRgb(109, 109, 109);
        private static readonly Color LightBorder      = Color.FromRgb(190, 190, 190);
        private static readonly Color LightRowHover    = Color.FromRgb(210, 220, 240);
        private static readonly Color LightSelectionBg = Color.FromRgb(51, 102, 204);
        private static readonly Color LightSelectionBd = Color.FromRgb(70, 130, 230);

        /// <summary>UserControl에 AutoCAD 테마 색상 적용 (PropertyGrid 스타일 포함)</summary>
        public static void ApplyTheme(UserControl control)
        {
            bool dark = IsDarkTheme;

            // PropertyGridStyles.xaml 로드
            try
            {
                var styleDict = new ResourceDictionary
                {
                    Source = new Uri("/GntTools.UI;component/Styles/PropertyGridStyles.xaml", UriKind.Relative)
                };
                control.Resources.MergedDictionaries.Add(styleDict);
            }
            catch { /* 스타일 파일 없어도 동작 */ }

            // 테마 색상 DynamicResource 키 등록
            control.Resources["PanelBg"]        = Freeze(dark ? DarkBg          : LightBg);
            control.Resources["SectionHeaderBg"]= Freeze(dark ? DarkHeaderBg    : LightHeaderBg);
            control.Resources["TextFg"]         = Freeze(dark ? DarkText        : LightText);
            control.Resources["ValueFg"]        = Freeze(dark ? DarkValueText   : LightValueText);
            control.Resources["GrayTextFg"]     = Freeze(dark ? DarkGrayText    : LightGrayText);
            control.Resources["GridBorder"]     = Freeze(dark ? DarkBorder      : LightBorder);
            control.Resources["RowHoverBg"]     = Freeze(dark ? DarkRowHover    : LightRowHover);
            control.Resources["SelectionBg"]    = Freeze(dark ? DarkSelectionBg : LightSelectionBg);
            control.Resources["SelectionBorder"]= Freeze(dark ? DarkSelectionBd : LightSelectionBd);

            control.Foreground = Freeze(dark ? DarkText : LightText);
            control.Background = Freeze(dark ? DarkBg   : LightBg);

            // 기본 컨트롤 스타일
            var checkStyle = new Style(typeof(CheckBox));
            checkStyle.Setters.Add(new Setter(CheckBox.ForegroundProperty, Freeze(dark ? DarkText : LightText)));
            control.Resources[typeof(CheckBox)] = checkStyle;

            var radioStyle = new Style(typeof(RadioButton));
            radioStyle.Setters.Add(new Setter(RadioButton.ForegroundProperty, Freeze(dark ? DarkText : LightText)));
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
