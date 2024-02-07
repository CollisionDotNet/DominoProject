using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace DominoProject.Models
{
    // User's data context for DB
    public class UserContext : DbContext
    {
        public DbSet<User> Users { get; set; } // Matches table "User"
        public DbSet<UserRole> UserRoles { get; set; }
        public UserContext(DbContextOptions<UserContext> options) : base(options)
        {
            Database.EnsureCreated(); // Create DB in case of a first call
        }
        // Predefines admin credentials
        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            
            string adminRoleName = "admin";
            string userRoleName = "user";

            string adminEmail = "admin@mail.ru";
            string adminPassword = "123456";
            string adminName = "Admin";
            string adminSurname = "";
            string adminGroup = "";
            string adminFPlayerFilePath = "";
            string adminSPlayerFilePath = "";

            // Adding roles
            UserRole adminRole = new UserRole { Id = 1, Name = adminRoleName };
            UserRole userRole = new UserRole { Id = 2, Name = userRoleName };
            User adminUser = new User
            {
                Id = 1,
                Email = adminEmail,
                Password = adminPassword,
                Name = adminName,
                Group = adminGroup,
                Surname = adminSurname, 
                FPlayerFilePath = adminFPlayerFilePath, 
                SPlayerFilePath = adminSPlayerFilePath,
                RoleId = adminRole.Id 
            };
            modelBuilder.Entity<UserRole>().HasData(new UserRole[] { adminRole, userRole });
            modelBuilder.Entity<User>().HasData(new User[] { adminUser });
            base.OnModelCreating(modelBuilder);
        }
    }
}
