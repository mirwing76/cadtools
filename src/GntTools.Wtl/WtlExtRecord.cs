using System.Collections.Generic;

namespace GntTools.Wtl
{
    /// <summary>WTL_EXT 레코드 DTO</summary>
    public class WtlExtRecord
    {
        public double BeginGroundHeight { get; set; }  // SBKHLT
        public double EndGroundHeight { get; set; }    // SBLHLT

        public Dictionary<string, object> ToDictionary()
        {
            return new Dictionary<string, object>
            {
                ["SBKHLT"] = BeginGroundHeight,
                ["SBLHLT"] = EndGroundHeight,
            };
        }
    }
}
