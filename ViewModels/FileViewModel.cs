using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DominoProject.ViewModels
{
    // Represents file loaded on the server
    public class FileViewModel
    {
        public string Path { get; set; }
        public string Name 
        { 
            get 
            {
                try 
                {
                    return System.IO.Path.GetFileName(Path);
                }
                catch
                {
                    return "";
                }
            } 
        }
        public string Content
        {
            get 
            {
                try
                {
                    return System.IO.File.ReadAllText(Path);
                }               
                catch
                {
                    return "";
                }
            }
        }
    }
}
