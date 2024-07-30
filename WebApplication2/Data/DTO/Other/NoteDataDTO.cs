using Imgriff.Data.DTO.Board;
using Newtonsoft.Json;

namespace Imgriff.Data.DTO.Other
{
    public class NoteDataDTO
    {
        [JsonProperty("title")]
        public string Title { get; set; }

        [JsonProperty("lists")]
        public List<ListBoard> Content { get; set; } = new List<ListBoard>();
    }
}
