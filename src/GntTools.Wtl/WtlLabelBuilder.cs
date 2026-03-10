using GntTools.Core.Settings;

namespace GntTools.Wtl
{
    /// <summary>상수관로 라벨 포맷 빌더</summary>
    public static class WtlLabelBuilder
    {
        /// <summary>관라벨: "PE ∅200 L=12.3m"</summary>
        public static string BuildPipeLabel(string material, string diameter, double length)
        {
            var settings = AppSettings.Instance.Common;
            string lenStr = length.ToString($"F{settings.LengthDecimals}");
            return $"{material} ∅{diameter} L={lenStr}m";
        }

        /// <summary>심도 라벨: "1.2"</summary>
        public static string BuildDepthLabel(double depth)
        {
            var settings = AppSettings.Instance.Common;
            return depth.ToString($"F{settings.DepthDecimals}");
        }

        /// <summary>지반고 라벨: "EL.25.30"</summary>
        public static string BuildGroundHeightLabel(double height)
        {
            return $"EL.{height:F2}";
        }
    }
}
