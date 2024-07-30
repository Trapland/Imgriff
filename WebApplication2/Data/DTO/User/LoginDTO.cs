using Newtonsoft.Json;
using System.ComponentModel.DataAnnotations;

namespace Imgriff.Data.DTO.User
{
    public class LoginDTO
    {
        private string Id { get; set; }

        [Required]
        [EmailAddress]
        [DataType(DataType.EmailAddress)]
        [JsonProperty("email")]
        public string Email { get; set; }

        [Required]
        [JsonProperty("emailCode")]
        public string EmailCode { get; set; }
    }
}
