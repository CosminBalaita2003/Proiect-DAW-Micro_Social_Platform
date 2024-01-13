using System.ComponentModel.DataAnnotations;

namespace Micro_Social_Platform.Models
{
    public class Comment
    {
        public int Id { get; set; }
        [Required(ErrorMessage = "Comentariul nu poate fi gol")]
        public string Content { get; set; }
        
        public DateTime Date { get; set; }
        public int? PostId { get; set; }
        public string? UserId { get; set; }

        public virtual ApplicationUser? User { get; set; }
        
        public virtual Post? Post { get; set; }
    }
}
