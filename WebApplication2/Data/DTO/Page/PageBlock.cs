using Newtonsoft.Json;

namespace Imgriff.Data.DTO.Page
{
    public class PageBlock
    {
        [JsonProperty("id")]
        public string Id { get; set; }

        [JsonProperty("name")]
        public string Name { get; set; }

        [JsonProperty("description")]
        public string Description { get; set; }
    }
}
