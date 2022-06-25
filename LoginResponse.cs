using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Cards_against_humanity
{
    public class LoginResponse
    {
        public string token { get; set; } = "";
        public string message { get; set; } = "";
        public bool success { get; set; } = false; // DON'T CHANGE TO TRUE
    }
}
