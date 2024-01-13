using System.ComponentModel.DataAnnotations;
using System.Data.SqlTypes;

namespace Micro_Social_Platform.Models
{
    public class Post
    {
        [Key]
        public int Id { get; set; }

        [Required(ErrorMessage = "Postarea trebuia sa aiba continut")]
        public string Content { get; set; }
        public DateTime Date { get; set; }
        public string? UserId { get; set; }

        public virtual ApplicationUser? User { get; set; }
        public virtual ICollection<Comment>? Comments { get; set; }

    }
}
