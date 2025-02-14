using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

// To learn more about WinUI, the WinUI project structure,
// and more about our project templates, see: http://aka.ms/winui-project-info.

namespace GameCenter.Commun
{
    public class UserData
    {
        public string SteamId { get; set; }
        public string PersonaName { get; set; }
        public string ProfileUrl { get; set; }
        public string AvatarUrl { get; set; }
        public string AccessToken { get; set; }
        public string RefreshToken { get; set; }
        public DateTime TokenExpirationTime { get; set; }
    }
}
