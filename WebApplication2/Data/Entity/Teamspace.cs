using System.ComponentModel.DataAnnotations;

namespace Imgriff.Data.Entity
{
    public class Teamspace
    {
        public Guid Id { get; set; }

        [Required(ErrorMessage = "Name is required")]
        public String Name { get; set; }
    }
}
