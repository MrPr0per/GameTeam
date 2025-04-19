using System.Security.Cryptography;
using System.Text;

namespace GameTeam.Scripts
{
    public class HashOperator
    {
        public static string GenerateSalt()
        {
            using (var rng = new RNGCryptoServiceProvider())
            {
                var saltBytes = new byte[16]; // 16 байт для соли
                rng.GetBytes(saltBytes);
                return Convert.ToBase64String(saltBytes);
            }
        }

        public static string HashPassword(string password, string salt)
        {
            using (var sha256 = SHA256.Create())
            {
                // Преобразуем пароль и соль в байты
                var passwordBytes = Encoding.UTF8.GetBytes(password);
                var saltBytes = Convert.FromBase64String(salt);

                // Соединяем соль и пароль
                var saltedPassword = new byte[passwordBytes.Length + saltBytes.Length];
                Buffer.BlockCopy(passwordBytes, 0, saltedPassword, 0, passwordBytes.Length);
                Buffer.BlockCopy(saltBytes, 0, saltedPassword, passwordBytes.Length, saltBytes.Length);

                // Хэшируем соединённые данные
                var hashBytes = sha256.ComputeHash(saltedPassword);

                // Возвращаем хэш в виде строки
                return Convert.ToBase64String(hashBytes);
            }
        }

    }
}
