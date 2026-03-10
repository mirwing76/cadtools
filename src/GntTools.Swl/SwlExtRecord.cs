using System.Collections.Generic;

namespace GntTools.Swl
{
    /// <summary>SWL_EXT 레코드 DTO</summary>
    public class SwlExtRecord
    {
        public string UseCode { get; set; } = "";         // PIPCDE
        public double BoxWidth { get; set; }              // PIPHOL (박스관 가로)
        public double BoxHeight { get; set; }             // PIPVEL (박스관 세로)
        public int LineCount { get; set; } = 1;           // PIPLIN (통로수)
        public double BeginGroundHeight { get; set; }     // SBKHLT
        public double EndGroundHeight { get; set; }       // SBLHLT
        public double BeginInvertLevel { get; set; }      // SBKALT (시점관저고)
        public double EndInvertLevel { get; set; }        // SBLALT (종점관저고)
        public double BeginCrownLevel { get; set; }       // ST_ALT (시점관상고)
        public double EndCrownLevel { get; set; }         // ED_ALT (종점관상고)
        public double Slope { get; set; }                 // PIPSLP (구배%)
        public string GroupId { get; set; } = "";         // TMPIDN

        public Dictionary<string, object> ToDictionary()
        {
            return new Dictionary<string, object>
            {
                ["PIPCDE"] = UseCode,
                ["PIPHOL"] = BoxWidth,
                ["PIPVEL"] = BoxHeight,
                ["PIPLIN"] = LineCount,
                ["SBKHLT"] = BeginGroundHeight,
                ["SBLHLT"] = EndGroundHeight,
                ["SBKALT"] = BeginInvertLevel,
                ["SBLALT"] = EndInvertLevel,
                ["ST_ALT"] = BeginCrownLevel,
                ["ED_ALT"] = EndCrownLevel,
                ["PIPSLP"] = Slope,
                ["TMPIDN"] = GroupId,
            };
        }
    }
}
