using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace DominoProject.ViewModels
{
    // Used for user's credentials showcase on Views.Home.Info
    public class UserInfoViewModel
    {
        public string Name { get; set; }
        public string Surname { get; set; }
        public string Email { get; set; }
        public string Group { get; set; }
        public string RoleName { get; set; }
        public string FPlayerFilePath { get; set; }
    }
}
