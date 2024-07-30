using Newtonsoft.Json;

namespace Imgriff.Data.DTO.Other
{
    public class GetTemplateDTO
    {
        [JsonProperty("email")]
        public string Email { get; set; }

        [JsonProperty("noteId")]
        public string NoteId { get; set; }

        [JsonProperty("templateName")]
        public string TemplateName { get; set; }

        [JsonProperty("currentLink")]
        public string currentLink { get; set; }
    }
}
