using Newtonsoft.Json;

namespace Imgriff.Data.DTO.List
{
    public class ListDTO
    {
        [JsonProperty("email")]
        public string Email { get; set; }

        [JsonProperty("noteId")]
        public string NoteId { get; set; }

        [JsonProperty("title")]
        public string Title { get; set; }

        [JsonProperty("lists")]
        public List<ListCard> Content { get; set; } = new List<ListCard>();

        [JsonProperty("currentLink")]
        public string currentLink { get; set; }

        [JsonProperty("iconPath")]
        public string iconPath { get; set; }
    }
}
