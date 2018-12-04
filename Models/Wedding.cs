using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Collections.Generic;

namespace WeddingPlanner.Models
{
    public class Wedding
    {
        [Key]
        public int WeddingId { get; set; }

        [Required]
        [MinLength(5)]
        [MaxLength(45)]
        public string WeddingName { get; set; }

        [Required]
        [EmailAddress]
        public string WedderOneEmail { get; set; }

        [EmailAddress]
        public string WedderTwoEmail { get; set; }

        [Required]
        [DataType(DataType.DateTime)]
        public DateTime Date { get; set; }

        [MaxLength(255)]
        public string InviteMessage { get; set; }

        [Required]
        [MaxLength(45)]
        public string Location { get; set; }

        [Required]
        [MaxLength(45)]
        public string City { get; set; }

        [Required]
        [MaxLength(45)]
        public string State { get; set; }

        public DateTime CreatedAt {get; set; } = DateTime.Now;
        public DateTime UpdatedAt { get; set; } = DateTime.Now;

        public string GooglePlaceId { get; set; }

        [Required]
        public int WedderOneId { get; set; }

        public int WedderTwoId { get; set; }

        [ForeignKey("WedderOneId")]
        public User WedderOne { get; set; }

        [ForeignKey("WedderTwoId")]
        public User WedderTwo { get; set; }

        List<Guest> Guests { get; set; }

    }
}