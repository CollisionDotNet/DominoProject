using DominoProject.Models;
using DominoProject.ViewModels;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Collections.Generic;
using System.Security.Claims;
using System.Threading.Tasks;

namespace DominoProject.Controllers
{
    public class AccountController : Controller
    {
        private UserContext database;
        public AccountController(UserContext userContext) // Setting db context object using dependency injection
        {
            database = userContext;
        }
        // Controller actions for url queries binding
        [HttpGet]
        public IActionResult Login()
        {
            if (User.Identity.IsAuthenticated)
                return RedirectToAction("Index", "Home");
            return View();
        }

        [HttpGet]
        public IActionResult Registration()
        {
            if (User.Identity.IsAuthenticated)
                return RedirectToAction("Index", "Home");
            return View();
        }

        [HttpPost]
        [ValidateAntiForgeryToken] // Create anti-forgery token. Nessesary for security
        public async Task<IActionResult> Login(LoginViewModel userModel) // Pass login view model with params from Views.Account.Login
        {
            if (User.Identity.IsAuthenticated)
                return RedirectToAction("Index", "Home");
            if (ModelState.IsValid) // ViewModel validation. Validation conditions are in ViewModels.LoginViewModel
            {
                User user = await database.Users.Include(u => u.Role).FirstOrDefaultAsync(u => u.Email == userModel.Email); // Async user search with specific email
                if (user != null)
                {
                    if(user.Password == userModel.Password)
                    {
                        await Authenticate(user); // Successfull authentication
                        return RedirectToAction("Index", "Home");
                    }
                    else // Wrong password
                    {
                        ModelState.AddModelError("Password", "Неверный пароль");
                    }
                }
                else // No such email
                {
                    ModelState.AddModelError("Email", "Пользователя с таким Email не существует");
                }
            }
            return View(userModel); // Show view with view model of a user, who wasn't logged in
        }

        [HttpPost] 
        [ValidateAntiForgeryToken] // Create anti-forgery token. Nessesary for security
        public async Task<IActionResult> Registration(RegistrationViewModel userModel) // Pass registration view model with params from Views.Account.Registration
        {
            if (User.Identity.IsAuthenticated)
                return RedirectToAction("Index", "Home");
            if (ModelState.IsValid) // ViewModel validation. Validation conditions are in ViewModels.RegistrationViewModel
            {
                User user = await database.Users.FirstOrDefaultAsync(u => u.Email == userModel.Email); // Async user search with specific email
                if (user == null) // No such user (email is vacant)
                {
                    user = new User // Creating new user and adding him to DB
                    { 
                        Email = userModel.Email, 
                        Password = userModel.Password, 
                        Name = userModel.Name, 
                        Surname = userModel.Surname,
                        Group = userModel.Group,
                        FPlayerFilePath = "", 
                        SPlayerFilePath = "" 
                    };
                    UserRole userRole = await database.UserRoles.FirstOrDefaultAsync(r => r.Name == "user");
                    if (userRole != null)
                        user.Role = userRole;
                    database.Users.Add(user);
                    await database.SaveChangesAsync();
                    await Authenticate(user); // Then authenticate him
                    return RedirectToAction("Index", "Home");  
                }
                else // Email is occupied
                {
                    ModelState.AddModelError("Email", "Пользователь с таким Email уже зарегистрирован");
                }
            }
            return View(userModel); // Show view with view model of a user, who wasn't registered
        }

        private async Task Authenticate(User user) // Authenticates user
        {          
            var claims = new List<Claim> // Claims are used for user credentials storing
            {
                new Claim(ClaimsIdentity.DefaultNameClaimType, user.Email), 
                new Claim("name", user.Name),
                new Claim("surname", user.Surname),
                new Claim(ClaimsIdentity.DefaultRoleClaimType, user.Role.Name),
                new Claim("group", user.Group),
                new Claim("fcsfile", user.FPlayerFilePath),
                new Claim("scsfile", user.SPlayerFilePath)
            };
            ClaimsIdentity id = new ClaimsIdentity(claims, "ApplicationCookie", ClaimsIdentity.DefaultNameClaimType, ClaimsIdentity.DefaultRoleClaimType); // Creating ClaimsIdentity          
            await HttpContext.SignInAsync(CookieAuthenticationDefaults.AuthenticationScheme, new ClaimsPrincipal(id));  // Then setting auth cookies
        }
        
        public IActionResult Denied()
        {
            if (User.Identity.IsAuthenticated)
                return RedirectToAction("Index", "Home");
            else
                return RedirectToAction("Login", "Account");
        }
        public async Task<IActionResult> Logout() // Logouth method
        {
            await HttpContext.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);
            return RedirectToAction("Login", "Account"); 
        }
    }
}
