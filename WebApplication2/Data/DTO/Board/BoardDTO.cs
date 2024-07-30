using Newtonsoft.Json;

namespace Imgriff.Data.DTO.Board
{
    public class BoardDTO
    {
        [JsonProperty("email")]
        public string Email { get; set; }

        [JsonProperty("noteId")]
        public string NoteId { get; set; }

        [JsonProperty("title")]
        public string Title { get; set; }

        [JsonProperty("lists")]
        public List<ListBoard> Content { get; set; } = new List<ListBoard>();

        [JsonProperty("currentLink")]
        public string currentLink { get; set; }

        [JsonProperty("iconPath")]
        public string iconPath { get; set; }
    }
}
