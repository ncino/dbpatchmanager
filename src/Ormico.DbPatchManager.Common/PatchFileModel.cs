using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using Newtonsoft.Json;
using Newtonsoft.Json.Converters;

namespace Ormico.DbPatchManager.Common
{
    public partial class PatchFile
    {
        [JsonProperty("DatabaseType")] public string DatabaseType { get; set; }

        [JsonProperty("ConnectionString")] public string ConnectionString { get; set; }

        [JsonProperty("CodeFolder")] public string CodeFolder { get; set; }

        [JsonProperty("CodeFiles")] public List<string> CodeFiles { get; set; }

        [JsonProperty("PatchFolder")] public string PatchFolder { get; set; }

        [JsonProperty("Options")] public Dictionary<string, string> Options { get; set; }

        [JsonProperty("patches")] public List<PatchFromFile> Patches { get; set; }

        public PatchFile()
        {
            Patches = new List<PatchFromFile>();
            Options = new Dictionary<string, string>();
            CodeFiles = new List<string>();
        }
    }

    public partial class PatchFromFile
    {
        [JsonProperty("id")] public string Id { get; set; }

        [JsonProperty("dependsOn")] public List<string> DependsOn { get; set; }

        public PatchFromFile()
        {
            DependsOn = new List<string>();
        }
    }

    public partial class PatchFile
    {
        public static PatchFile FromJson(string json) =>
            JsonConvert.DeserializeObject<PatchFile>(json, PatchFileConverter.Settings);
    }

    public static class PatchFileSerializer
    {
        public static string ToJson(this PatchFile self) =>
            JsonConvert.SerializeObject(self, PatchFileConverter.Settings);
    }

    internal static class PatchFileConverter
    {
        public static readonly JsonSerializerSettings Settings = new JsonSerializerSettings
        {
            MetadataPropertyHandling = MetadataPropertyHandling.Ignore,
            DateParseHandling = DateParseHandling.None,
            Converters =
            {
                new IsoDateTimeConverter { DateTimeStyles = DateTimeStyles.AssumeUniversal }
            },
            Formatting = Formatting.Indented
        };
    }
}