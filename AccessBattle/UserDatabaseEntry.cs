using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AccessBattle
{
    public class UserDatabaseEntry
    {
        /// <summary>Unique ID for use with databases that require it. This value is optional.</summary>
        public int IdUser = 0;
        public string UserName;
        public int ELO = 1000;
        public string PasswordHash;
        public string PasswordSalt;
        public bool MustChangePassword = true;
        public bool IsAccountEnabled = false;
    }
}
