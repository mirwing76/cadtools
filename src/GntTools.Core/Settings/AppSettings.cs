using System;
using System.Collections.Generic;
using System.IO;
using System.Runtime.Serialization;
using System.Runtime.Serialization.Json;
using System.Text;

namespace GntTools.Core.Settings
{
    [DataContract]
    public class AppSettings
    {
        private static AppSettings _instance;
        private static readonly string SettingsDir =
            Path.Combine(Environment.GetFolderPath(
                Environment.SpecialFolder.ApplicationData), "GntTools");
        private static readonly string SettingsPath =
            Path.Combine(SettingsDir, "settings.json");

        [DataMember(Name = "common")]
        public CommonSettings Common { get; set; } = new CommonSettings();
        [DataMember(Name = "wtl")]
        public DomainSettings Wtl { get; set; } = new DomainSettings();
        [DataMember(Name = "swl")]
        public DomainSettings Swl { get; set; } = new DomainSettings();
        [DataMember(Name = "kepco")]
        public DomainSettings Kepco { get; set; } = new DomainSettings();

        /// <summary>싱글 인스턴스</summary>
        public static AppSettings Instance
        {
            get
            {
                if (_instance == null)
                    _instance = Load();
                return _instance;
            }
        }

        /// <summary>JSON에서 로드 (파일 없으면 기본값)</summary>
        public static AppSettings Load()
        {
            if (!File.Exists(SettingsPath))
                return CreateDefault();

            try
            {
                var json = File.ReadAllText(SettingsPath, Encoding.UTF8);
                var ser = new DataContractJsonSerializer(typeof(AppSettings));
                using (var ms = new MemoryStream(Encoding.UTF8.GetBytes(json)))
                {
                    _instance = (AppSettings)ser.ReadObject(ms);
                    return _instance;
                }
            }
            catch
            {
                return CreateDefault();
            }
        }

        /// <summary>JSON으로 저장</summary>
        public void Save()
        {
            try
            {
                if (!Directory.Exists(SettingsDir))
                    Directory.CreateDirectory(SettingsDir);

                var settings = new DataContractJsonSerializerSettings
                {
                    UseSimpleDictionaryFormat = true
                };
                var ser = new DataContractJsonSerializer(typeof(AppSettings), settings);
                using (var ms = new MemoryStream())
                {
                    ser.WriteObject(ms, this);
                    var json = Encoding.UTF8.GetString(ms.ToArray());
                    File.WriteAllText(SettingsPath, json, Encoding.UTF8);
                }
            }
            catch { }
        }

        private static AppSettings CreateDefault()
        {
            var s = new AppSettings();

            s.Wtl.Layers = new LayerSettings
            {
                Depth = "WS_DEP", GroundHeight = "WS_HGT",
                Label = "WS_LBL", Leader = "WS_LEAD", Undetected = "WS_BT"
            };
            s.Wtl.Defaults = new DefaultValues
            { Year = "2024", Material = "PE", Diameter = "200" };

            s.Swl.Layers = new LayerSettings
            {
                Depth = "SW_DEP", GroundHeight = "SW_HGT",
                Label = "SW_LBL", Leader = "SW_LEAD", Undetected = "SW_BT"
            };
            s.Swl.Defaults = new DefaultValues
            { Year = "2024", Material = "HP", Diameter = "300", UseCode = "01" };

            s.Kepco.Layers = new LayerSettings
            {
                Depth = "SC991", Drawing = "SC983",
                Label = "SC992", Leader = "SC982", Undetected = "SC999"
            };
            s.Kepco.Defaults = new DefaultValues
            { Year = "2024", Material = "ELP" };
            s.Kepco.Diameters = new List<int> { 200, 175, 150, 125, 100, 80 };

            _instance = s;
            return s;
        }
    }
}
