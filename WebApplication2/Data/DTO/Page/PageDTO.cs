using Newtonsoft.Json;

namespace Imgriff.Data.DTO.Page
{
    public class PageDTO
    {
        [JsonProperty("email")]
        public string Email { get; set; }

        [JsonProperty("noteId")]
        public string NoteId { get; set; }

        [JsonProperty("title")]
        public string Title { get; set; }

        [JsonProperty("mainText")]
        public string Content { get; set; }

        [JsonProperty("currentLink")]
        public string currentLink { get; set; }

        [JsonProperty("iconPath")]
        public string iconPath { get; set; }
    }
}
