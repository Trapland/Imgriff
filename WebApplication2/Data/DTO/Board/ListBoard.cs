using Newtonsoft.Json;

namespace Imgriff.Data.DTO.Board
{
    public class ListBoard
    {
        [JsonProperty("id")]
        public string Id { get; set; }


        [JsonProperty("name")]
        public string Name { get; set; }

        [JsonProperty("cards")]
        public List<CardBoard> Cards { get; set; } = new List<CardBoard>();
    }
}
