using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace GameCenter.Helpers.Models
{
    public class GamePath
    {
        public string Id { get; set; } = Guid.NewGuid().ToString();
        public string Path { get; set; }
        public string Name { get; set; }
        public string LauncherType { get; set; } // "Steam", "Xbox", "Epic", "Other"
        public DateTime DateAdded { get; set; } = DateTime.Now;
        public bool IsActive { get; set; } = true;
    }
}
