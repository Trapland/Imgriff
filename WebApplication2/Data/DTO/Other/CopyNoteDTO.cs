using Newtonsoft.Json;

namespace Imgriff.Data.DTO.Other
{
    public class CopyNoteDTO
    {
        [JsonProperty("email")]
        public string Email { get; set; }

        [JsonProperty("noteId")]
        public string NoteId { get; set; }

        [JsonProperty("newNoteId")]
        public string newNoteId { get; set; }
    }
}
