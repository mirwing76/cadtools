using Autodesk.Gis.Map.Constants;

namespace GntTools.Core.Odt
{
    /// <summary>ODT 테이블 필드 정의</summary>
    public class OdtFieldDef
    {
        public string Name { get; }
        public string Description { get; }
        public DataType DataType { get; }

        public OdtFieldDef(string name, string description, DataType dataType)
        {
            Name = name;
            Description = description;
            DataType = dataType;
        }
    }
}
