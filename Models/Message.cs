using System.ComponentModel.DataAnnotations;

namespace Micro_Social_Platform.Models
{
    public class Message
    {
        public int Id { get; set; }
        [Required(ErrorMessage = "Mesajul trebuia sa aiba continut")]
        public string Content { get; set; }
        public DateTime Date { get; set; }
        public int? GroupId { get; set; }
        public string? UserId { get; set; }
        public virtual Group? Group { get; set; }


        public virtual ApplicationUser? User { get; set; }
       
    }
}
