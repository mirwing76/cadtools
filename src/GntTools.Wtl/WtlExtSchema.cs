using System.Collections.Generic;
using Autodesk.Gis.Map.Constants;
using GntTools.Core.Odt;

namespace GntTools.Wtl
{
    /// <summary>WTL_EXT 상수관로 확장 테이블 (2 fields)</summary>
    public class WtlExtSchema : IOdtSchema
    {
        public string TableName => "WTL_EXT";
        public string Description => "상수관로 확장 속성";

        public IReadOnlyList<OdtFieldDef> Fields { get; } = new List<OdtFieldDef>
        {
            new OdtFieldDef("SBKHLT", "시점지반고(m)", DataType.Real),
            new OdtFieldDef("SBLHLT", "종점지반고(m)", DataType.Real),
        }.AsReadOnly();
    }
}
