using Newtonsoft.Json;

namespace Imgriff.Data.DTO.Other
{
    public class GetNoteDTO
    {
        [JsonProperty("email")]
        public string Email { get; set; }

        [JsonProperty("noteId")]
        public string NoteId { get; set; }
    }
}
