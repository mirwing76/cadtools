namespace GntTools.Core.Geometry
{
    /// <summary>심도 측정 결과</summary>
    public class DepthResult
    {
        public double BeginDepth { get; set; }
        public double EndDepth { get; set; }
        public double AverageDepth { get; set; }
        public double MaxDepth { get; set; }
        public double MinDepth { get; set; }
        public bool IsUndetected { get; set; }
    }
}
