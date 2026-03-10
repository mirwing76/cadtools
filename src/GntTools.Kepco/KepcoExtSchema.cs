using System.Collections.Generic;
using Autodesk.Gis.Map.Constants;
using GntTools.Core.Odt;

namespace GntTools.Kepco
{
    /// <summary>KEPCO_EXT 전력통신 확장 테이블 (3 fields)</summary>
    public class KepcoExtSchema : IOdtSchema
    {
        public string TableName => "KEPCO_EXT";
        public string Description => "전력통신 확장 속성";

        public IReadOnlyList<OdtFieldDef> Fields { get; } = new List<OdtFieldDef>
        {
            new OdtFieldDef("PIPDAT", "구경별데이터(JSON)", DataType.Character),
            new OdtFieldDef("BXH",    "가로x세로",         DataType.Character),
            new OdtFieldDef("TMPIDN", "그룹ID",            DataType.Character),
        }.AsReadOnly();
    }
}
