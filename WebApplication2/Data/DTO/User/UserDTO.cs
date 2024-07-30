using Newtonsoft.Json;
using System.ComponentModel.DataAnnotations;

namespace Imgriff.Data.DTO.User
{
    public class UserDTO
    {
        [Required]
        [EmailAddress]
        [DataType(DataType.EmailAddress)]
        [JsonProperty("email")]
        public string Email { get; set; }
    }
}
