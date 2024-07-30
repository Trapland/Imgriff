using Imgriff.Data.DTO.List;
using Imgriff.Data.DTO.Other;
using Newtonsoft.Json;

namespace Imgriff.Data.DTO.Board
{
    public class CardBoard
    {
        [JsonProperty("id")]
        public string Id { get; set; }

        [JsonProperty("name")]
        public string Name { get; set; }

        [JsonProperty("description")]
        public string Description { get; set; }

        [JsonProperty("datetime")]
        public DateTime? Datetime { get; set; }

        [JsonProperty("properties")]
        public List<Property> Content { get; set; } = new List<Property>();
    }
}
