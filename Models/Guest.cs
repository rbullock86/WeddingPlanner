using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Collections.Generic;

namespace WeddingPlanner.Models
{
    public class Guest
    {
        [Key]
        public int GuestId { get; set; }

        [MinLength(2)]
        [MaxLength(45)]
        public string FirstName { get; set; }

        [MinLength(2)]
        [MaxLength(45)]
        public string LastName { get; set; }

        [Required]
        [EmailAddress]
        public string Email { get; set; }

        public Boolean ReceivedInvite { get; set; }
        public Boolean HasRSVP { get; set; }

        public DateTime CreatedAt{ get; set; } = DateTime.Now;
        public DateTime UpdatedAt{ get; set; } = DateTime.Now;

        [Required]
        public int WeddingId { get; set; }

        [Required]
        public int UserId { get; set; }

        [ForeignKey("WeddingId")]
        public Wedding ThisWedding { get; set; }

        [ForeignKey("UserId")]
        public User ThisUser { get; set; }
    }
}