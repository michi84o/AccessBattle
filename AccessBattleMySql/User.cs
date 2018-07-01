namespace AccessBattle.MySqlProvider
{
    class User
    {
        public int IdUser;
        public string UserName;
        public string PasswordHash;
        public string PasswordSalt;
        public bool MustChangePassword;
    }
}
