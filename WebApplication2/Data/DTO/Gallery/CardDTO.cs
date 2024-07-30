using Newtonsoft.Json;

namespace Imgriff.Data.DTO.Gallery
{
    public class CardDTO
    {
        [JsonProperty("id")]
        public string Id { get; set; }

        [JsonProperty("name")]
        public string? Name { get; set; }

        [JsonProperty("description")]
        public string? description { get; set; }

        public string? base64Image { get; set; } // This will be used when retrieving the image
    }
}
