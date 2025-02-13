using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace GameCenter.Helpers
{
    public class DLC
    {
        public int Id { get; set; }
        public int GameId { get; set; }
        public string Title { get; set; }
        public string ImageUrl { get; set; }
        public string Price { get; set; }
    }
}
