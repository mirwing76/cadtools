using System.Collections.Generic;
using System.Windows.Media;

namespace GntTools.UI.BlockBrowser
{
    /// <summary>AutoCAD ACI(0-255) → WPF Color 룩업 테이블</summary>
    public static class AciColorTable
    {
        private static readonly Dictionary<int, Color> _table = new Dictionary<int, Color>
        {
            { 0,   Colors.White },   // ByBlock
            { 1,   Color.FromRgb(255, 0,   0)   },  // Red
            { 2,   Color.FromRgb(255, 255, 0)   },  // Yellow
            { 3,   Color.FromRgb(0,   255, 0)   },  // Green
            { 4,   Color.FromRgb(0,   255, 255) },  // Cyan
            { 5,   Color.FromRgb(0,   0,   255) },  // Blue
            { 6,   Color.FromRgb(255, 0,   255) },  // Magenta
            { 7,   Colors.White },                    // White/Black
            { 8,   Color.FromRgb(128, 128, 128) },  // Dark gray
            { 9,   Color.FromRgb(192, 192, 192) },  // Light gray
            // 10-249: standard ACI palette (주요 색상만, 나머지는 White 폴백)
            { 10,  Color.FromRgb(255, 0,   0)   },
            { 30,  Color.FromRgb(255, 127, 0)   },
            { 40,  Color.FromRgb(255, 191, 0)   },
            { 50,  Color.FromRgb(255, 255, 0)   },
            { 70,  Color.FromRgb(127, 255, 0)   },
            { 90,  Color.FromRgb(0,   255, 0)   },
            { 110, Color.FromRgb(0,   255, 127) },
            { 130, Color.FromRgb(0,   255, 255) },
            { 150, Color.FromRgb(0,   127, 255) },
            { 170, Color.FromRgb(0,   0,   255) },
            { 190, Color.FromRgb(127, 0,   255) },
            { 210, Color.FromRgb(255, 0,   255) },
            { 230, Color.FromRgb(255, 0,   127) },
            { 250, Color.FromRgb(51,  51,  51)  },
            { 251, Color.FromRgb(80,  80,  80)  },
            { 252, Color.FromRgb(105, 105, 105) },
            { 253, Color.FromRgb(130, 130, 130) },
            { 254, Color.FromRgb(190, 190, 190) },
            { 255, Colors.White },
        };

        /// <summary>ACI → WPF Color. 미등록 인덱스는 White 반환.</summary>
        public static Color GetColor(int aci)
        {
            return _table.TryGetValue(aci, out var c) ? c : Colors.White;
        }

        /// <summary>AutoCAD Color → WPF Color (TrueColor, ACI 모두 처리)</summary>
        public static Color FromAcadColor(Autodesk.AutoCAD.Colors.Color acadColor)
        {
            if (acadColor == null) return Colors.White;
            if (acadColor.IsByLayer || acadColor.IsByBlock)
                return Colors.White;
            if (acadColor.ColorMethod == Autodesk.AutoCAD.Colors.ColorMethod.ByColor)
                return Color.FromRgb(acadColor.Red, acadColor.Green, acadColor.Blue);
            return GetColor(acadColor.ColorIndex);
        }
    }
}
