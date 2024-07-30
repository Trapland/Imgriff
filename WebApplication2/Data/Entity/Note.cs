using System.ComponentModel.DataAnnotations;

namespace Imgriff.Data.Entity
{
    public class Note
    {
        public Guid Id { get; set; }

        public string userId { get; set; }

        public Guid teamspaceId { get; set; }

        public String name { get; set; }

        [Required(ErrorMessage = "Note file name is required")]
        public String noteFileName { get; set; } = null!;

        public bool isFavorite { get; set; }

        public bool isDeleted { get; set; }

        public String iconPath { get; set; }

        public String routerLink { get; set;}
    }
}
