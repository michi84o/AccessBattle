namespace AccessBattle.MySqlProvider
{
    class User
    {
        public int IdUser;
        public string UserName;
        public int ELO;
        public string PasswordHash;
        public string PasswordSalt;
        public bool MustChangePassword;
    }
}
