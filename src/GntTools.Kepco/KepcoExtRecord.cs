using System.Collections.Generic;
using System.Linq;

namespace GntTools.Kepco
{
    /// <summary>KEPCO_EXT 레코드 DTO</summary>
    public class KepcoExtRecord
    {
        /// <summary>구경별 데이터: key=D200, value="3(2)"</summary>
        public Dictionary<string, string> PipeData { get; set; }
            = new Dictionary<string, string>();

        public string BxH { get; set; } = "";   // "1.36x0.86"
        public string GroupId { get; set; } = "";

        /// <summary>PipeData → JSON 문자열</summary>
        public string PipeDataToJson()
        {
            if (PipeData == null || PipeData.Count == 0) return "{}";
            var parts = PipeData
                .Where(kv => kv.Value != "0(0)" && !string.IsNullOrEmpty(kv.Value))
                .Select(kv => $"\"{kv.Key}\":\"{kv.Value}\"");
            return "{" + string.Join(",", parts) + "}";
        }

        /// <summary>JSON → PipeData 역직렬화</summary>
        public static Dictionary<string, string> ParsePipeDataJson(string json)
        {
            var result = new Dictionary<string, string>();
            if (string.IsNullOrEmpty(json) || json == "{}") return result;

            json = json.Trim('{', '}');
            var pairs = json.Split(',');
            foreach (var pair in pairs)
            {
                var kv = pair.Split(':');
                if (kv.Length == 2)
                {
                    string key = kv[0].Trim().Trim('"');
                    string val = kv[1].Trim().Trim('"');
                    result[key] = val;
                }
            }
            return result;
        }

        public Dictionary<string, object> ToDictionary()
        {
            return new Dictionary<string, object>
            {
                ["PIPDAT"] = PipeDataToJson(),
                ["BXH"] = BxH,
                ["TMPIDN"] = GroupId,
            };
        }
    }
}
