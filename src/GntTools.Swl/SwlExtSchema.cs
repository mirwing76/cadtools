using System.Collections.Generic;
using Autodesk.Gis.Map.Constants;
using GntTools.Core.Odt;

namespace GntTools.Swl
{
    /// <summary>SWL_EXT 하수관로 확장 테이블 (12 fields)</summary>
    public class SwlExtSchema : IOdtSchema
    {
        public string TableName => "SWL_EXT";
        public string Description => "하수관로 확장 속성";

        public IReadOnlyList<OdtFieldDef> Fields { get; } = new List<OdtFieldDef>
        {
            new OdtFieldDef("PIPCDE", "용도코드",       DataType.Character),
            new OdtFieldDef("PIPHOL", "가로길이(m)",    DataType.Real),
            new OdtFieldDef("PIPVEL", "세로길이(m)",    DataType.Real),
            new OdtFieldDef("PIPLIN", "통로수",         DataType.Integer),
            new OdtFieldDef("SBKHLT", "시점지반고(m)",  DataType.Real),
            new OdtFieldDef("SBLHLT", "종점지반고(m)",  DataType.Real),
            new OdtFieldDef("SBKALT", "시점관저고(m)",  DataType.Real),
            new OdtFieldDef("SBLALT", "종점관저고(m)",  DataType.Real),
            new OdtFieldDef("ST_ALT", "시점관상고(m)",  DataType.Real),
            new OdtFieldDef("ED_ALT", "종점관상고(m)",  DataType.Real),
            new OdtFieldDef("PIPSLP", "평균구배(%)",    DataType.Real),
            new OdtFieldDef("TMPIDN", "그룹ID",        DataType.Character),
        }.AsReadOnly();
    }
}
