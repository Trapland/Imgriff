using Newtonsoft.Json;

namespace Imgriff.Data.DTO.Gallery
{
    public class GalleryDTO
    {
        [JsonProperty("email")]
        public string email { get; set; }

        [JsonProperty("noteId")]
        public string noteId { get; set; }

        [JsonProperty("title")]
        public string title { get; set; }

        [JsonProperty("content")]
        public List<CardDTO> content { get; set; } = new List<CardDTO>();

        [JsonProperty("currentLink")]
        public string currentLink { get; set; }

        [JsonProperty("iconPath")]
        public string iconPath { get; set; }

    }
}
