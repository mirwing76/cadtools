using System.Collections.Generic;
using System.Runtime.Serialization;

namespace GntTools.Core.Settings
{
    [DataContract]
    public class CommonSettings
    {
        [DataMember(Name = "textStyle")]
        public string TextStyle { get; set; } = "GHS";
        [DataMember(Name = "shxFont")]
        public string ShxFont { get; set; } = "ROMANS";
        [DataMember(Name = "bigFont")]
        public string BigFont { get; set; } = "GHS";
        [DataMember(Name = "textSize")]
        public double TextSize { get; set; } = 1.0;
        [DataMember(Name = "lengthDecimals")]
        public int LengthDecimals { get; set; } = 0;
        [DataMember(Name = "depthDecimals")]
        public int DepthDecimals { get; set; } = 1;
    }

    [DataContract]
    public class LayerSettings
    {
        [DataMember(Name = "depth")]
        public string Depth { get; set; } = "";
        [DataMember(Name = "groundHeight")]
        public string GroundHeight { get; set; } = "";
        [DataMember(Name = "label")]
        public string Label { get; set; } = "";
        [DataMember(Name = "leader")]
        public string Leader { get; set; } = "";
        [DataMember(Name = "undetected")]
        public string Undetected { get; set; } = "";
        [DataMember(Name = "drawing")]
        public string Drawing { get; set; } = "";
    }

    [DataContract]
    public class DefaultValues
    {
        [DataMember(Name = "year")]
        public string Year { get; set; } = "2024";
        [DataMember(Name = "material")]
        public string Material { get; set; } = "";
        [DataMember(Name = "diameter")]
        public string Diameter { get; set; } = "";
        [DataMember(Name = "useCode")]
        public string UseCode { get; set; } = "";

        // Palette state persistence
        [DataMember(Name = "isAutoDepth")]
        public bool IsAutoDepth { get; set; }
        [DataMember(Name = "beginDepth")]
        public double BeginDepth { get; set; }
        [DataMember(Name = "endDepth")]
        public double EndDepth { get; set; }
        [DataMember(Name = "isUndetected")]
        public bool IsUndetected { get; set; }
        [DataMember(Name = "beginGroundHeight")]
        public double BeginGroundHeight { get; set; }
        [DataMember(Name = "endGroundHeight")]
        public double EndGroundHeight { get; set; }
        [DataMember(Name = "useLeader")]
        public bool UseLeader { get; set; }
        [DataMember(Name = "isBoxPipe")]
        public bool IsBoxPipe { get; set; }
        [DataMember(Name = "boxWidth")]
        public double BoxWidth { get; set; }
        [DataMember(Name = "boxHeight")]
        public double BoxHeight { get; set; }
        [DataMember(Name = "lineCount")]
        public int LineCount { get; set; } = 1;
    }

    [DataContract]
    public class DomainSettings
    {
        [DataMember(Name = "layers")]
        public LayerSettings Layers { get; set; } = new LayerSettings();
        [DataMember(Name = "defaults")]
        public DefaultValues Defaults { get; set; } = new DefaultValues();
        [DataMember(Name = "diameters")]
        public List<int> Diameters { get; set; }
    }
}
