using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace WorkHelperGUI.Models
{
    class User
    {
        public int Id { get; set; }

        public string Username { get; set; }

        public string Password { get; set; }

        public string Operation { get; set; }

        public int Dotimes { get; set; }

        public int Duration { get; set; }

        public int RowNum { get; set; }
        
    }
}
