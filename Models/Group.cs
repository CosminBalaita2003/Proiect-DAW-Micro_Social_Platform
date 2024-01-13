using System.ComponentModel.DataAnnotations;

namespace Micro_Social_Platform.Models
{
    public class Group
    {
        [Key]
        public int Id { get; set; }
        public string GroupName { get; set; }
        public string? Description { get; set; }
        public string? UserId { get; set; }
        public virtual ICollection<UserGroup>? UserGroups { get; set; }
        public virtual ApplicationUser? User { get; set; }
        public virtual ICollection<Message>? Messages { get; set; }
    }
}