using System.Collections.Generic;

namespace GntTools.Core.Odt
{
    /// <summary>ODT 테이블 스키마 인터페이스</summary>
    public interface IOdtSchema
    {
        string TableName { get; }
        string Description { get; }
        IReadOnlyList<OdtFieldDef> Fields { get; }
    }
}
