using System.Collections.Generic;

namespace GntTools.Core.Odt
{
    /// <summary>PIPE_COMMON 레코드 DTO</summary>
    public class PipeCommonRecord
    {
        public string InstallDate { get; set; } = "";    // ISTYMD
        public string Material { get; set; } = "";       // MOPCDE
        public string Diameter { get; set; } = "";       // PIPDIP
        public double Length { get; set; }               // PIPLEN
        public string Undetected { get; set; } = "N";   // BTCDE (Y/N)
        public double BeginDepth { get; set; }           // BEGDEP
        public double EndDepth { get; set; }             // ENDDEP
        public double AverageDepth { get; set; }         // AVEDEP
        public double MaxDepth { get; set; }             // HGHDEP
        public double MinDepth { get; set; }             // LOWDEP
        public string Label { get; set; } = "";          // PIPLBL
        public string CompletionDate { get; set; } = ""; // WRKDTE
        public string Remark { get; set; } = "";         // REMARK

        /// <summary>OdtManager.UpdateRecord용 Dictionary 변환</summary>
        public Dictionary<string, object> ToDictionary()
        {
            return new Dictionary<string, object>
            {
                ["ISTYMD"] = InstallDate,
                ["MOPCDE"] = Material,
                ["PIPDIP"] = Diameter,
                ["PIPLEN"] = Length,
                ["BTCDE"]  = Undetected,
                ["BEGDEP"] = BeginDepth,
                ["ENDDEP"] = EndDepth,
                ["AVEDEP"] = AverageDepth,
                ["HGHDEP"] = MaxDepth,
                ["LOWDEP"] = MinDepth,
                ["PIPLBL"] = Label,
                ["WRKDTE"] = CompletionDate,
                ["REMARK"] = Remark,
            };
        }
    }
}
