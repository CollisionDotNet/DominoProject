using DominoProject.Models;
using DominoProject.ViewModels;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Security.Claims;
using System.Threading.Tasks;

namespace DominoProject.Controllers
{
    [Authorize]
    public class HomeController : Controller
    {
        private UserContext database;
        public HomeController(UserContext userContext) // Setting db context object using dependency injection
        {
            database = userContext;
        }

        public IActionResult Index()
        {
            return View();
        }

        public IActionResult Info() // View with current user credentials info
        {
            return View(new UserInfoViewModel
            {
                Email = User.Identity.Name,
                RoleName = User.FindFirstValue(ClaimsIdentity.DefaultRoleClaimType),
                Name = User.FindFirstValue("name"),
                Surname = User.FindFirstValue("surname"),
                Group = User.FindFirstValue("group"),
            });
        }
        public IActionResult File() // View with file uploading (shows user's loaded file at the moment)
        {
            return View(new FileViewModel
            {
                Path = User.FindFirstValue("fcsfile")
            });
        }
        [NonAction]
        public async Task UpdateClaims(List<Claim> toUpdate)
        {
            await HttpContext.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);
            var claims = new List<Claim> // Immutable claims
            {
                new Claim(ClaimsIdentity.DefaultNameClaimType, User.Identity.Name),
                new Claim("name", User.FindFirstValue("name")),
                new Claim("surname", User.FindFirstValue("surname")),
                new Claim(ClaimsIdentity.DefaultRoleClaimType, User.FindFirstValue(ClaimsIdentity.DefaultRoleClaimType)),               
            };
            claims.AddRange(toUpdate);
            ClaimsIdentity id = new ClaimsIdentity(claims, "ApplicationCookie", ClaimsIdentity.DefaultNameClaimType, ClaimsIdentity.DefaultRoleClaimType); // Creating ClaimsIdentity             
            await HttpContext.SignInAsync(CookieAuthenticationDefaults.AuthenticationScheme, new ClaimsPrincipal(id));  // Then setting auth cookies
        }
        public async Task<IActionResult> UploadFile(IFormFile file) // Action on file uploading
        {
            if (file != null)
            {
                
                string fpath = Path.Combine(Helper.FPlayerFilesDirectoryPath, file.FileName);
                string spath = Path.Combine(Helper.SPlayerFilesDirectoryPath, file.FileName);
                try
                {
                    string validation = new StreamReader(file.OpenReadStream()).ReadToEnd();
                    validation = validation.Replace("\n", "").Replace("\r", "").Replace(" ", "");
                    // File content validation
                    if (!validation.Contains("namespaceDominoC"))
                    {
                        ModelState.AddModelError("Path", "Модуль имеет неверное пространство имен");
                        return View("File", new FileViewModel { Path = User.FindFirstValue(Helper.FPlayerFilePathType) });
                    }
                    if (!validation.Contains("classMFPlayer"))
                    {
                        ModelState.AddModelError("Path", "Модуль имеет неверное название класса");
                        return View("File", new FileViewModel { Path = User.FindFirstValue(Helper.FPlayerFilePathType) });
                    }
                    if (!validation.Contains("List<MTable.SBone>lHand;"))
                    {
                        ModelState.AddModelError("Path", "В модуле отсутствует поле lHand типа List<MTable.SBone>");
                        return View("File", new FileViewModel { Path = User.FindFirstValue(Helper.FPlayerFilePathType) });
                    }
                    if (!validation.Contains("voidInitialize(){lHand=newList<MTable.SBone>();}"))
                    {
                        ModelState.AddModelError("Path", "В модуле отсутствует метод Initialize() необходимой сигнатуры либо его содержимое было изменено");
                        return View("File", new FileViewModel { Path = User.FindFirstValue(Helper.FPlayerFilePathType) });
                    }
                    if (!validation.Contains("voidPrintAll(){MTable.PrintAll(lHand);}"))
                    {
                        ModelState.AddModelError("Path", "В модуле отсутствует метод PrintAll() необходимой сигнатуры либо его содержимое было изменено");
                        return View("File", new FileViewModel { Path = User.FindFirstValue(Helper.FPlayerFilePathType) });
                    }
                    if (!validation.Contains("intGetCount(){returnlHand.Count;}"))
                    {
                        ModelState.AddModelError("Path", "В модуле отсутствует метод GetCount() необходимой сигнатуры либо его содержимое было изменено");
                        return View("File", new FileViewModel { Path = User.FindFirstValue(Helper.FPlayerFilePathType) });
                    }
                    if (!validation.Contains("voidAddItem(MTable.SBonesb){"))
                    {
                        ModelState.AddModelError("Path", "В модуле отсутствует метод AddItem() необходимой сигнатуры ");
                        return View("File", new FileViewModel { Path = User.FindFirstValue(Helper.FPlayerFilePathType) });
                    }
                    if (!validation.Contains("intGetScore(){"))
                    {
                        ModelState.AddModelError("Path", "В модуле отсутствует метод GetScore() необходимой сигнатуры");
                        return View("File", new FileViewModel { Path = User.FindFirstValue(Helper.FPlayerFilePathType) });
                    }
                    if (!validation.Contains("boolMakeStep(outMTable.SBonesb,outboolEnd){"))
                    {
                        ModelState.AddModelError("Path", "В модуле отсутствует метод MakeStep() необходимой сигнатуры");
                        return View("File", new FileViewModel { Path = User.FindFirstValue(Helper.FPlayerFilePathType) });
                    }
                    // Async content saving
                    FileStream stream = new FileStream(fpath, FileMode.CreateNew);
                    await file.CopyToAsync(stream);
                    stream.Close();

                    stream = new FileStream(spath, FileMode.CreateNew);
                    await file.CopyToAsync(stream);
                    stream.Close();
                    // Changing second file content
                    string text = await System.IO.File.ReadAllTextAsync(spath);
                    text = text.Replace("MFPlayer", "MSPlayer");
                    await System.IO.File.WriteAllTextAsync(spath, text);
                    // Updating uploader's file credentials and claims
                    User user = await database.Users.FirstOrDefaultAsync(u => u.Email == User.Identity.Name);
                    user.FPlayerFilePath = fpath;
                    user.SPlayerFilePath = spath;
                    await database.SaveChangesAsync();

                    List<Claim> updatePaths = new List<Claim> { new Claim(Helper.FPlayerFilePathType, fpath), new Claim(Helper.SPlayerFilePathType, spath) };
                    await UpdateClaims(updatePaths);
                }
                catch (IOException)
                {
                    // This file is already exists on server
                    ModelState.AddModelError("Path", "Файл с таким названием уже загружен. Переименуйте файл.");       
                    return View("File", new FileViewModel { Path = User.FindFirstValue(Helper.FPlayerFilePathType) });
                }                                    
            }
            return RedirectToAction("File");
        }
        public IActionResult DownloadFile() // Downloading file from server
        {
            string path = User.FindFirstValue(Helper.FPlayerFilePathType);
            return PhysicalFile(path, "text/plain", path.Split('\\').Last());
        }      
        [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
        public IActionResult Error()
        {
            return View(new ErrorViewModel { RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier });
        }
    }
}
