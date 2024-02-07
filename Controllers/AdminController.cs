using DominoProject.Models;
using DominoProject.ViewModels;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace DominoProject.Controllers
{
    // Controller is applied only for users with admin role
    [Authorize(Roles = "admin")]
    public class AdminController : Controller
    {
        private UserContext database;
        public AdminController(UserContext userContext) // Setting db context object using dependency injection
        {
            database = userContext;
        }
        public IActionResult Index()
        {
            return View();
        }
        // Returning all users with their credentials
        public IActionResult UserList()
        {
            List<UserInfoViewModel> toShow = new List<UserInfoViewModel>();
            var data = database.Users.Include(u => u.Role);
            foreach (User user in data)
            {
                toShow.Add(new UserInfoViewModel
                {
                    Name = user.Name,
                    Surname = user.Surname,
                    Email = user.Email,
                    Group = user.Group,
                    RoleName = user.Role.Name,
                    FPlayerFilePath = user.FPlayerFilePath
                });
            }           
            return View(toShow);
        }
        public IActionResult DownloadAnyFile(string path) 
        {
            // TO DO async
            return PhysicalFile(path, "text/plain", path.Split('\\').Last());
        }
    }
}
