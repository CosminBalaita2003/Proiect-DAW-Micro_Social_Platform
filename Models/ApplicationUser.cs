using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc.Rendering;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Micro_Social_Platform.Models
{
    //PASUL 1 user si roluri - adaugare clasa pentru user care mosteneste IdentityUser
    public class ApplicationUser : IdentityUser
    {
        public string FirstName { get; set; }
        public string LastName { get; set; }
        public string? Pronouns { get; set; }
        public string? Bio { get; set; }
        
        //un user poate avea cont privat sau public
        public bool IsPrivate { get; set; }

        //un user poate avea mai multe comentarii
        public virtual ICollection<Comment>? Comments { get; set; }

        //un user poate posta mai multe postari
        public virtual ICollection<Post>? Posts { get; set; }

        //un user poate avea mai multe mesaje intr-un grup
        public virtual ICollection<Message>? Messages { get; set; }

        public virtual ICollection<UserGroup>? UserGroups { get; set; }

        public virtual ICollection<FollowRequest>? ReceivedFollowRequests { get; set; }
        public virtual ICollection<FollowRequest>? SentFollowRequests { get; set; }

        [NotMapped]
        public IEnumerable<SelectListItem>? AllRoles { get; set; }
    }
}