using Newtonsoft.Json;

namespace Imgriff.Data.DTO.List
{
    public class ListCard
    {
        [JsonProperty("id")]
        public string Id { get; set; }

        [JsonProperty("name")]
        public string Name { get; set; }

        [JsonProperty("description")]
        public string Description { get; set; }

        [JsonProperty("placeholder")]
        public string Placeholder { get; set; }

        [JsonProperty("icon")]
        public string Icon { get; set; }
    }
}

