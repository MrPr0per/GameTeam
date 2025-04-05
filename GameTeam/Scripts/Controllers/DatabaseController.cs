namespace GameTeam.Scripts.Controllers
{
    public static class DatabaseController
    {
        private static Dictionary<int, string[]> dataBase;
        private static int lasId;
        static DatabaseController() 
        { 
            dataBase = new Dictionary<int, string[]>();
            lasId = 0;
        }

        public static void Register(string username, string email, string password)
        {
            try
            {
                //Сейчас уникальность username и email не проверяется, но в бд по идее можно настроить уникальность
                dataBase[lasId] = new string[3] { username, email, password };
                lasId++;
            }
            catch (Exception e)
            { 
                Console.WriteLine($"Не получилось зарегистрировать пользователя {e}");
            }
        }

        public static bool Login(string? username, string? email, string password, out string? outUsername)
        {
            if (username != null)
            {
                foreach (var key in dataBase.Keys)
                {
                    if (dataBase[key][0] == username && dataBase[key][2] == password)
                    {
                        outUsername = username;
                        return true;
                    }
                }
            }
            else if (email != null)
            {
                foreach (var key in dataBase.Keys)
                {
                    if (dataBase[key][1] == email && dataBase[key][2] == password)
                    {
                        outUsername = dataBase[key][0];
                        return true;
                    }
                }
            }

            outUsername = null;
            return false;
        }
    }
}
