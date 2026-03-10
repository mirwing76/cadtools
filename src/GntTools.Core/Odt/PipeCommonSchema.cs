using System.Collections.Generic;
using Autodesk.Gis.Map.Constants;

namespace GntTools.Core.Odt
{
    /// <summary>PIPE_COMMON 공통 테이블 스키마 (13 fields)</summary>
    public class PipeCommonSchema : IOdtSchema
    {
        public string TableName => "PIPE_COMMON";
        public string Description => "관로 공통 속성";

        public IReadOnlyList<OdtFieldDef> Fields { get; } = new List<OdtFieldDef>
        {
            new OdtFieldDef("ISTYMD", "설치일자",    DataType.Character),
            new OdtFieldDef("MOPCDE", "관재질",      DataType.Character),
            new OdtFieldDef("PIPDIP", "구경",        DataType.Character),
            new OdtFieldDef("PIPLEN", "연장(m)",     DataType.Real),
            new OdtFieldDef("BTCDE",  "불탐여부",    DataType.Character),
            new OdtFieldDef("BEGDEP", "시점심도(m)", DataType.Real),
            new OdtFieldDef("ENDDEP", "종점심도(m)", DataType.Real),
            new OdtFieldDef("AVEDEP", "평균심도(m)", DataType.Real),
            new OdtFieldDef("HGHDEP", "최고심도(m)", DataType.Real),
            new OdtFieldDef("LOWDEP", "최저심도(m)", DataType.Real),
            new OdtFieldDef("PIPLBL", "관라벨",      DataType.Character),
            new OdtFieldDef("WRKDTE", "준공일자",    DataType.Character),
            new OdtFieldDef("REMARK", "비고",        DataType.Character),
        }.AsReadOnly();
    }
}
