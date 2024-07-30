using Microsoft.AspNetCore.Identity;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Imgriff.Data.Entity
{
    public class User
    {
        public String Id { get; set; }

        public String Email { get; set; } = null!;

        public String? EmailCode { get; set; }

        public String? Salt { get; set; }

        public String? imgPath { get; set; }

        public bool isDeleted { get; set; }
    }
}
