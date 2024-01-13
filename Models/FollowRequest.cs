using System.ComponentModel.DataAnnotations.Schema;

namespace Micro_Social_Platform.Models
{
    public class FollowRequest
    {
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public int Id { get; set; }
        public string SenderId { get; set; }
        public ApplicationUser Sender { get; set; }
        public string ReceiverId { get; set; }
        public ApplicationUser Receiver { get; set; }
        public bool IsAccepted { get; set; }
    }
}
