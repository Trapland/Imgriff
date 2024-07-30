using Newtonsoft.Json;

namespace Imgriff.Data.DTO.Gallery
{
    public class Card
    {
        [JsonProperty("id")]
        public string Id { get; set; }

        [JsonProperty("name")]
        public string? Name { get; set; }

        [JsonProperty("description")]
        public string? description { get; set; }

        [JsonProperty("file")]
        public IFormFile? file { get; set; }

        [JsonProperty("fileName")]
        public string? fileName { get; set; }

        [JsonIgnore]
        public string? base64Image { get; set; } // This will be used when retrieving the image
    }
}
