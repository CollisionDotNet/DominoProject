using Microsoft.AspNetCore.Http;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;
using System.Threading.Tasks;

namespace DominoProject.Models
{
    public class User
    {
        // Represents user
        public int Id { get; set; } // User's ID, PK in DB     
        public string Name { get; set; }
        public string Surname { get; set; }      
        public string Email { get; set; }
        public string Group { get; set; }
        [JsonIgnore]
        public string Password { get; set; }       
        public UserRole Role { get; set; }
        public int? RoleId { get; set; } // FK to UserRole table
        public string FPlayerFilePath { get; set; } // Path to user's FPlayer.cs
        public string SPlayerFilePath { get; set; } // Path to user's SPlayer.cs
        public int QualifyingStageScores { get; set; }
        public string GetFullName()
        {
            return $"{Name} {Surname}";
        }
        // Dummy player for Qualifying stage
        public static readonly User Dummy = new User { 
            Name = "Dummy", 
            FPlayerFilePath = Helper.WebRootFullPath("DummyFPlayer.cs"), 
            SPlayerFilePath = Helper.WebRootFullPath("DummySPlayer.cs") 
        };
    }
}
