using GntTools.Core.Settings;

namespace GntTools.Swl
{
    /// <summary>하수관로 라벨 포맷 빌더</summary>
    public static class SwlLabelBuilder
    {
        /// <summary>관라벨: "HP ∅300 L=15.2m"</summary>
        public static string BuildPipeLabel(string material, string diameter,
            double length)
        {
            var s = AppSettings.Instance.Common;
            return $"{material} ∅{diameter} L={length.ToString($"F{s.LengthDecimals}")}m";
        }

        /// <summary>박스관 라벨: "HP □1200x800 L=15.2m"</summary>
        public static string BuildBoxLabel(string material,
            double width, double height, double length)
        {
            var s = AppSettings.Instance.Common;
            return $"{material} □{width:F0}x{height:F0} L={length.ToString($"F{s.LengthDecimals}")}m";
        }

        /// <summary>심도 라벨: "1.2"</summary>
        public static string BuildDepthLabel(double depth)
        {
            var s = AppSettings.Instance.Common;
            return depth.ToString($"F{s.DepthDecimals}");
        }

        /// <summary>관저고 라벨: "IL=25.300"</summary>
        public static string BuildInvertLabel(double invertLevel)
        {
            return $"IL={invertLevel:F3}";
        }

        /// <summary>구배 라벨: "S=0.5%"</summary>
        public static string BuildSlopeLabel(double slope)
        {
            return $"S={slope:F3}%";
        }
    }
}
