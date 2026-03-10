using GntTools.Core.Settings;

namespace GntTools.Kepco
{
    /// <summary>전력통신 라벨 포맷</summary>
    public static class KepcoLabelBuilder
    {
        /// <summary>관라벨: "ELP L=12.3m"</summary>
        public static string BuildPipeLabel(string material, double length)
        {
            var s = AppSettings.Instance.Common;
            return $"{material} L={length.ToString($"F{s.LengthDecimals}")}m";
        }

        /// <summary>심도 라벨: "1.2"</summary>
        public static string BuildDepthLabel(double depth)
        {
            var s = AppSettings.Instance.Common;
            return depth.ToString($"F{s.DepthDecimals}");
        }

        /// <summary>BxH 라벨: "1.36×0.86"</summary>
        public static string BuildBxHLabel(string bxh)
        {
            return bxh.Replace("x", "×");
        }
    }
}
