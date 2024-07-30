using Newtonsoft.Json;

namespace Imgriff.Data.DTO.Other
{
    public class GetAllNotes
    {
        [JsonProperty("email")]
        public string Email { get; set; }
    }
}
