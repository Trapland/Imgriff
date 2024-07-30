using Newtonsoft.Json;

namespace Imgriff.Data.DTO.Table
{
    public class TableDTO
    {
        [JsonProperty("email")]
        public string Email { get; set; }

        [JsonProperty("noteId")]
        public string NoteId { get; set; }

        [JsonProperty("title")]
        public string Title { get; set; }

        [JsonProperty("tableData")]
        public List<List<string>> Content { get; set; } = new List<List<string>>();

        [JsonProperty("currentLink")]
        public string currentLink { get; set; }

        [JsonProperty("iconPath")]
        public string iconPath { get; set; }
    }
}
