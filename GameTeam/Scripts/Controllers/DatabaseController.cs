using Npgsql;
using GameTeam.Classes.Data;
using NodaTime;
using NpgsqlTypes;
using System.Security.Cryptography;
using System.Text;
using GameTeam.Classes.Exceptions;
using static Microsoft.ApplicationInsights.MetricDimensionNames.TelemetryContext;

namespace GameTeam.Scripts.Controllers
{
    public static class DatabaseController
    {
        public static readonly string ConnectionString =
            "Host=ep-spring-lake-a22loh0r-pooler.eu-central-1.aws.neon.tech;" +
            "Port=5432;" +
            "Database=neondb;" +
            "Username=neondb_owner;" +
            "Password=npg_d1vs2zExTMJO;" +
            "SslMode=Require;";

        /// <summary>
        /// Статический конструктор для настройки маппинга типов
        /// </summary>
        /// <remarks>
        /// Выполняет:
        /// 1. Регистрацию enum DayOfWeekEnum
        /// 2. Настройку NodaTime для работы с временными типами
        /// 3. Установку флага совместимости для .NET 6+
        /// </remarks>
        [Obsolete("Obsolete")]
        static DatabaseController()
        {
            NpgsqlConnection.GlobalTypeMapper.MapEnum<Availability.DayOfWeekEnum>("day_of_week");

            NpgsqlConnection.GlobalTypeMapper.UseNodaTime();

            AppContext.SetSwitch("Npgsql.EnableLegacyTimestampBehavior", true);
        }

        
        /// <summary>
        /// Регистрация нового пользователя
        /// </summary>
        /// <param name="username">Имя пользователя</param>
        /// <param name="email">Электронная почта</param>
        /// <param name="password">Пароль</param>
        /// <exception cref="Exception">
        /// Возможные ошибки:
        /// - "Имя пользователя уже занято"
        /// - "Email уже занято"
        /// - Общие ошибки подключения
        /// </exception>
        public static void Register(string username, string email, string password, string salt)
        {
            using var conn = new NpgsqlConnection(ConnectionString);
            try
            {
                conn.Open();
                using var cmd = new NpgsqlCommand();
                cmd.Connection = conn;
                cmd.CommandText = @"
                            INSERT INTO users_data (username, email, password, salt) 
                            VALUES (@username, @email, @password, @salt)";

                cmd.Parameters.AddWithValue("username", username);
                cmd.Parameters.AddWithValue("email", email);
                cmd.Parameters.AddWithValue("password", password);
                cmd.Parameters.AddWithValue("salt", salt);

                cmd.ExecuteNonQuery();
            }
            catch (PostgresException ex) when (ex.SqlState == "23505")
            {
                throw ex.ConstraintName.Contains("username")
                    ? new UsernameAlreadyExists()
                    : new EmailAlreadyExists();
            }
            catch (Exception ex)
            {
                throw new RegistrationException($"Ошибка регистрации: {ex.Message}");
            }
        }

        /// <summary>
        /// Получение хэша пароля и соли пользователя по ID
        /// </summary>
        /// <param name="userId">ID пользователя</param>
        /// <returns>Кортеж (passwordHash, salt) или null если пользователь не найден</returns>
        /// <exception cref="Exception">Ошибки выполнения запроса</exception>
        public static (string passwordHash, string salt)? GetPasswordAndSalt(int userId)
        {
            using var conn = new NpgsqlConnection(ConnectionString);
            try
            {
                conn.Open();
                using var cmd = new NpgsqlCommand();
                cmd.Connection = conn;

                cmd.CommandText = @"
            SELECT password, salt FROM users_data
            WHERE id = @userId";

                cmd.Parameters.AddWithValue("userId", userId);

                using var reader = cmd.ExecuteReader();

                if (!reader.Read())
                    return null;

                return (reader.GetString(0), reader.GetString(1));
            }
            catch (Exception e)
            {
                throw new Exception($"Ошибка при получении пароля и соли: {e.Message}");
            }
        }

        /// <summary>
        /// Получение имени пользователя по его ID
        /// </summary>
        /// <param name="userId">ID пользователя</param>
        /// <returns>Имя пользователя или null, если пользователь не найден</returns>
        /// <exception cref="Exception">Ошибки выполнения запроса</exception>
        public static string? GetUsernameById(int userId)
        {
            using var conn = new NpgsqlConnection(ConnectionString);
            try
            {
                conn.Open();
                using var cmd = new NpgsqlCommand();
                cmd.Connection = conn;

                cmd.CommandText = @"
            SELECT username FROM users_data
            WHERE id = @userId";

                cmd.Parameters.AddWithValue("userId", userId);

                var result = cmd.ExecuteScalar();

                return result?.ToString();
            }
            catch (Exception e)
            {
                throw new Exception($"Ошибка при получении имени пользователя: {e.Message}");
            }
        }

        /// <summary>
        /// Получение ID пользователя по имени
        /// </summary>
        /// <param name="username">Имя пользователя</param>
        /// <returns>ID пользователя или null если не найден</returns>
        /// <exception cref="Exception">Ошибки выполнения запроса</exception>
        public static int? GetIdByUsername(string username)
        {
            using var conn = new NpgsqlConnection(ConnectionString);
            try
            {
                conn.Open();
                using var cmd = new NpgsqlCommand();
                cmd.Connection = conn;

                cmd.CommandText = @"
                            select id from users_data
                            where username = @username";

                cmd.Parameters.AddWithValue("username", username);

                using var reader = cmd.ExecuteReader();

                if (!reader.Read())
                    return null;

                var id = reader.GetInt32(0);
                return id;
            }
            catch (Exception e)
            {
                throw new Exception($"Ошибка GetIdByUsername: {e}");
            }
        }

        /// <summary>
        /// Получение ID пользователя по email
        /// </summary>
        /// <param name="email">Email пользователя</param>
        /// <returns>ID пользователя или null если не найден</returns>
        /// <exception cref="Exception">Ошибки выполнения запроса</exception>
        public static int? GetIdByEmail(string email)
        {
            using var conn = new NpgsqlConnection(ConnectionString);
            try
            {
                conn.Open();
                using var cmd = new NpgsqlCommand();
                cmd.Connection = conn;

                cmd.CommandText = @"
            SELECT id FROM users_data
            WHERE email = @email";

                cmd.Parameters.AddWithValue("email", email);

                using var reader = cmd.ExecuteReader();

                if (!reader.Read())
                    return null;

                var id = reader.GetInt32(0);
                return id;
            }
            catch (Exception e)
            {
                throw new Exception($"Ошибка GetUserIdByEmail: {e}");
            }
        }

        public static string GetUserSalt(int userId)
        {
            using var conn = new NpgsqlConnection(ConnectionString);
            conn.Open();

            try
            {
                // Выполняем запрос для получения соли пользователя по его ID
                using var cmd = new NpgsqlCommand("SELECT salt FROM users_data WHERE id = @userId", conn);
                cmd.Parameters.AddWithValue("userId", userId);

                var result = cmd.ExecuteScalar();

                if (result == null)
                {
                    throw new Exception($"Соль для пользователя с ID {userId} не найдена.");
                }

                return result.ToString();
            }
            catch (Exception ex)
            {
                throw new Exception($"Ошибка при получении соли пользователя с ID {userId}: {ex.Message}", ex);
            }
        }

        /// <summary>
        /// Возвращает существующую игру по имени или создаёт новую, если такой записи нет.
        /// </summary>
        /// <param name="gameName">Название игры.</param>
        /// <returns>Объект Game с заполненным Id и Name.</returns>
        public static Game GetOrCreateGame(string gameName)
        {
            using var conn = new NpgsqlConnection(ConnectionString);
            conn.Open();
            // Пытаемся найти игру по имени
            using var cmd = new NpgsqlCommand("SELECT game_id FROM games WHERE game_name = @gameName", conn);
            cmd.Parameters.AddWithValue("gameName", gameName);
            var result = cmd.ExecuteScalar();
            if (result != null)
            {
                var id = Convert.ToInt32(result);
                return new Game(id, gameName);
            }
            else
            {
                // Если не найдена, вставляем новую запись и возвращаем её идентификатор
                using var insertCmd =
                    new NpgsqlCommand("INSERT INTO games (game_name) VALUES (@gameName) RETURNING game_id", conn);
                insertCmd.Parameters.AddWithValue("gameName", gameName);
                var newId = Convert.ToInt32(insertCmd.ExecuteScalar());
                return new Game(newId, gameName);
            }
        }

        /// <summary>
        /// Возвращает существующую доступность по значениям или создаёт новую запись, если такой не существует.
        /// </summary>
        /// <param name="dayOfWeek">День недели (значение enum Availability.DayOfWeekEnum).</param>
        /// <param name="start">Время начала (OffsetTime).</param>
        /// <param name="end">Время окончания (OffsetTime).</param>
        /// <returns>Объект Availability с заполненным Id, DayOfWeek, StartTime и EndTime.</returns>
        public static Availability GetOrCreateAvailability(Availability.DayOfWeekEnum dayOfWeek, OffsetTime start,
            OffsetTime end)
        {
            using var conn = new NpgsqlConnection(ConnectionString);
            conn.Open();
            // Пытаемся найти запись в таблице availabilities по параметрам
            using var cmd = new NpgsqlCommand(
                "SELECT id FROM availabilities WHERE day_of_week = @day_of_week AND start_time = @start_time AND end_time = @end_time",
                conn);

            cmd.Parameters.Add(new NpgsqlParameter
            {
                ParameterName = "day_of_week",
                Value = dayOfWeek, // Значение типа Availability.DayOfWeekEnum
                DataTypeName = "day_of_week" // Имя PostgreSQL ENUM
            });
            cmd.Parameters.Add(new NpgsqlParameter("start_time", NpgsqlDbType.TimeTz)
            {
                Value = start
            });

            cmd.Parameters.Add(new NpgsqlParameter("end_time", NpgsqlDbType.TimeTz)
            {
                Value = end
            });

            var result = cmd.ExecuteScalar();
            if (result != null)
            {
                var id = Convert.ToInt32(result);
                return new Availability(id, dayOfWeek, start, end);
            }
            else
            {
                using var insertCmd = new NpgsqlCommand(
                    "INSERT INTO availabilities (day_of_week, start_time, end_time) " +
                    "VALUES (@day_of_week, @start_time, @end_time) RETURNING id",
                    conn);

                // Правильные имена параметров и добавление к insertCmd
                insertCmd.Parameters.Add(new NpgsqlParameter
                {
                    ParameterName = "day_of_week",
                    Value = dayOfWeek,
                    DataTypeName = "day_of_week"
                });
                insertCmd.Parameters.Add(new NpgsqlParameter("start_time", NpgsqlDbType.TimeTz)
                {
                    Value = start
                });
                insertCmd.Parameters.Add(new NpgsqlParameter("end_time", NpgsqlDbType.TimeTz)
                {
                    Value = end
                });

                var newId = Convert.ToInt32(insertCmd.ExecuteScalar());
                return new Availability(newId, dayOfWeek, start, end);
            }
        }

        /// <summary>
        /// Получение списка игр для пользователя или анкеты
        /// </summary>
        /// <param name="id">ID сущности</param>
        /// <param name="isUser">True для пользователя, False для анкеты</param>
        /// <returns>Список объектов Game</returns>
        /// <exception cref="Exception">Ошибки выполнения запроса</exception>
        public static List<Game> GetGames(int id, bool isUser)
        {
            using var conn = new NpgsqlConnection(ConnectionString);
            try
            {
                conn.Open();
                using var cmd = new NpgsqlCommand();
                var games = new List<Game>();
                cmd.Connection = conn;

                if (isUser)
                    cmd.CommandText = @"
	                    select g.game_id, 
	                           g.game_name
	                    from user_to_games as ug 
	                    join user_profiles as up on ug.user_id = up.user_id
	                    join games as g on ug.game_id = g.game_id
	                    where up.user_id = @id";
                else
                    cmd.CommandText = @"
						select g.game_id, 
	                           g.game_name
	                    from applications_to_games as ag
	                    join applications as a on a.id = ag.app_id
	                    join games as g on ag.game_id = g.game_id
	                    where ag.app_id = @id";

                cmd.Parameters.AddWithValue("id", id);
                using var reader = cmd.ExecuteReader();

                while (reader.Read())
                {
                    games.Add(new Game(reader.GetInt32(0), reader.GetString(1)));
                }

                return games;
            }
            catch (Exception e)
            {
                throw new Exception($"Ошибка GetGames: {e}");
            }
        }

        /// <summary>
        /// Получение списка доступностей для пользователя или анкеты
        /// </summary>
        /// <param name="id">ID сущности</param>
        /// <param name="isUser">True для пользователя, False для анкеты</param>
        /// <returns>Список объектов Availability</returns>
        /// <exception cref="Exception">Ошибки выполнения запроса</exception>
        public static List<Availability> GetAvailabilities(int id, bool isUser)
        {
            using var conn = new NpgsqlConnection(ConnectionString);
            try
            {
                conn.Open();
                using var cmd = new NpgsqlCommand();
                var availabilities = new List<Availability>();
                cmd.Connection = conn;

                if (isUser)
                    cmd.CommandText = @"
                    select 
                        a.id,
                        a.start_time,
                        a.end_time,
                        a.day_of_week
                    from users_to_availability as ua 
                    join availabilities as a on ua.availability_id = a.id
                    join user_profiles as up on ua.user_id = up.user_id
                    where ua.user_id = @id";
                else
                    cmd.CommandText = @"
                    select 
                        a.id,
                        a.start_time,
                        a.end_time,
                        a.day_of_week
                    from applications_to_availability as aa 
                    join availabilities as a on aa.availability_id = a.id
                    join applications as ap on aa.application_id = ap.id
                    where aa.application_id = @id";


                cmd.Parameters.AddWithValue("id", id);
                using var reader = cmd.ExecuteReader();

                while (reader.Read())
                {
                    var startTime = reader.GetFieldValue<OffsetTime>(1);
                    var endTime = reader.GetFieldValue<OffsetTime>(2);
                    var dayOfWeek = reader.GetFieldValue<Availability.DayOfWeekEnum>(3);
                    availabilities.Add(new Availability(reader.GetInt32(0),
                        dayOfWeek, startTime, endTime));
                }

                return availabilities;
            }
            catch (Exception e)
            {
                throw new Exception($"Ошибка GetUser: {e}");
            }
        }

        /// <summary>
        /// Получение профиля пользователя
        /// </summary>
        /// <param name="username">Имя пользователя</param>
        /// <returns>Объект UserProfile или null если не найден</returns>
        /// <exception cref="Exception">
        /// - Пользователь не найден
        /// - Ошибки выполнения запроса
        /// </exception>
        public static UserProfile GetUserProfile(string username)
        {
            using var conn = new NpgsqlConnection(ConnectionString);
            var userId = GetIdByUsername(username);

            if (userId is null)
                throw new Exception($"Ошибка GetUserProfile: пользователя с таким username:{username} не существует");

            try
            {
                conn.Open();
                using var cmd = new NpgsqlCommand();
                cmd.Connection = conn;

                cmd.CommandText = @"
                    select * from user_profiles
                    where user_id = @id";

                cmd.Parameters.AddWithValue("id", userId);
                using var reader = cmd.ExecuteReader();

                if (!reader.Read())
                    return null;

                return new UserProfile(reader.GetInt32(0),
                    reader.GetString(1));
            }
            catch (Exception e)
            {
                throw new Exception($"Ошибка GetUserProfile: {e}");
            }
        }

        /// <summary>
        /// Получение данных пользователя
        /// </summary>
        /// <param name="userId">Id пользователя</param>
        /// <returns>Объект UserData или null если не найден</returns>
        /// <exception cref="Exception">
        /// - Ошибки выполнения запроса
        /// </exception>
        public static UserData GetUserData(int userId)
        {
            using var conn = new NpgsqlConnection(ConnectionString);

            try
            {
                conn.Open();
                using var cmd = new NpgsqlCommand();
                cmd.Connection = conn;

                cmd.CommandText = @"
                    select 
                        username,
                        email
                    from users_data
                    where id = @id";

                cmd.Parameters.AddWithValue("id", userId);
                using var reader = cmd.ExecuteReader();

                if (!reader.Read())
                    return null;

                return new UserData(userId, reader.GetString(1), reader.GetString(0));
            }
            catch (Exception e)
            {
                throw new Exception($"Ошибка GetUserData: {e}");
            }
        }

        /// <summary>
        /// Получение всех анкет пользователя по его ID 
        /// </summary>
        /// <param name="userId">ID пользователя</param>
        /// <returns>Список анкет, которые создал пользователь</returns>
        public static List<Application> GetAllApplicationsByUserId(int userId)
        {
            using var conn = new NpgsqlConnection(ConnectionString);
            var applications = new List<Application>();

            try
            {
                conn.Open();

                // Получаем название цели (purpose) вместе с анкетой
                using (var cmd = new NpgsqlCommand(@"
            SELECT a.id, a.title, a.description, a.contacts, a.purpose_id, a.owner_id, a.is_hidden
            FROM applications a
            WHERE a.owner_id = @userId
            ORDER BY a.id", conn))
                {
                    cmd.Parameters.AddWithValue("userId", userId);

                    using var reader = cmd.ExecuteReader();
                    while (reader.Read())
                    {
                        applications.Add(new Application(
                            reader.GetInt32(0), // id
                            reader.GetString(1), // title
                            reader.IsDBNull(2) ? null : reader.GetString(2), // description
                            reader.IsDBNull(3) ? null : reader.GetString(3), // contacts
                            reader.GetInt32(4), // purpose (ранее было purpose_id)
                            reader.GetInt32(5), // owner_id
                            reader.GetBoolean(6)));
                    }
                }

                // Для каждой анкеты загружаем связанные игры и доступности
                foreach (var app in applications)
                {
                    app.Games = GetGames(app.Id, false);
                    app.Availabilities = GetAvailabilities(app.Id, false);
                }

                return applications;
            }
            catch (Exception ex)
            {
                throw new Exception($"Ошибка при получении анкет пользователя: {ex.Message}", ex);
            }
        }

        /// <summary>
        /// Получение всех не скрытых анкет из базы данных
        /// </summary>
        /// <returns>Список объектов Application</returns>
        /// <exception cref="Exception">Ошибки выполнения запроса</exception>
        public static List<Application> GetAllApplications()
        {
            using var conn = new NpgsqlConnection(ConnectionString);
            try
            {
                var applications = new List<Application>();

                conn.Open();
                using var cmd = new NpgsqlCommand();
                cmd.Connection = conn;
                cmd.CommandText = @"
					select * from applications
					where is_hidden = false";

                using var reader = cmd.ExecuteReader();
                while (reader.Read())
                {
                    applications.Add(new Application(reader.GetInt32(0), reader.GetString(1),
                        reader.GetString(2), reader.GetString(3),
                        reader.GetInt32(4), reader.GetInt32(5), reader.GetBoolean(6)));
                }

                return applications;
            }
            catch (Exception e)
            {
                throw new Exception($"Ошибка GetAllApplications: {e}");
            }
        }

        /// <summary>
        /// Получение не скрытой анкеты по id
        /// </summary>
        /// <returns>Список объектов Application</returns>
        /// <exception cref="Exception">Ошибки выполнения запроса</exception>
        public static List<Application> GetApplicationById(int applicationId)
        {
            using var conn = new NpgsqlConnection(ConnectionString);
            try
            {
                var applications = new List<Application>();

                conn.Open();
                using var cmd = new NpgsqlCommand();
                cmd.Connection = conn;
                cmd.CommandText = @"
					select * from applications
					where is_hidden = false and id = @application_id";
                cmd.Parameters.AddWithValue("application_id", applicationId);
                using var reader = cmd.ExecuteReader();
                while (reader.Read())
                {
                    applications.Add(new Application(reader.GetInt32(0), reader.GetString(1),
                        reader.GetString(2), reader.GetString(3),
                        reader.GetInt32(4), reader.GetInt32(5), reader.GetBoolean(6)));
                }

                return applications;
            }
            catch (Exception e)
            {
                throw new Exception($"Ошибка GetApplicationById: {e}");
            }
        }


        public static List<Game> GetAllGames()
        {
            using var conn = new NpgsqlConnection(ConnectionString);
            try
            {
                var games = new List<Game>();

                conn.Open();
                using var cmd = new NpgsqlCommand();
                cmd.Connection = conn;
                cmd.CommandText = @"
					select * from games";

                using var reader = cmd.ExecuteReader();
                while (reader.Read())
                {
                    games.Add(new Game(reader.GetInt32(0), reader.GetString(1)));
                }

                return games;
            }
            catch (Exception e)
            {
                throw new Exception($"Ошибка GetAllGames: {e}");
            }
        }


        /// <summary>
        /// Создание или обновление профиля пользователя
        /// </summary>
        /// <param name="userId">ID пользователя</param>
        /// <param name="aboutDescription">Описание профиля</param>
        /// <param name="skills">Навыки</param>
        /// <param name="games">Список игр</param>
        /// <param name="availabilities">Список доступностей</param>
        /// <remarks>
        /// Выполняет в транзакции:
        /// 1. Обновление основной информации
        /// 2. Обработку связанных игр
        /// 3. Обработку доступностей
        /// </remarks>
        /// <exception cref="Exception">Ошибки выполнения транзакции</exception>
        public static void UpsertUserProfile(int? userId, string? aboutDescription,
            List<Game>? games = null, List<Availability>? availabilities = null)
        {
            if (userId is null)
                throw new ArgumentException("userId cannot be null");
            if (aboutDescription is null)
                throw new ArgumentException("aboutDescriptio cannot be null");
            using var conn = new NpgsqlConnection(ConnectionString);
            conn.Open();

            using var transaction = conn.BeginTransaction();
            try
            {
                // 1. Upsert в user_profiles
                using (var cmd = new NpgsqlCommand(@"
                    INSERT INTO user_profiles (user_id, about_description)
                    VALUES (@user_id, @aboutDescription)
                    ON CONFLICT (user_id) DO UPDATE
                        SET about_description = EXCLUDED.about_description
                ", conn, transaction))
                {
                    cmd.Parameters.AddWithValue("user_id", userId);
                    cmd.Parameters.AddWithValue("aboutDescription", aboutDescription ?? (object)DBNull.Value);
                    cmd.ExecuteNonQuery();
                }

                // Удаляем игры из таблицы user_to_games, чтобы запихать новые
                using (var cmd = new NpgsqlCommand(@"
                    delete from user_to_games where user_id = @user_id
                ", conn, transaction))
                {
                    cmd.Parameters.AddWithValue("user_id", userId);
                    cmd.ExecuteNonQuery();
                }

                // Удаляем времена из таблицы users_to_availability, чтобы запихать новые
                using (var cmd = new NpgsqlCommand(@"
                    delete from users_to_availability where user_id = @user_id
                ", conn, transaction))
                {
                    cmd.Parameters.AddWithValue("user_id", userId);
                    cmd.ExecuteNonQuery();
                }

                if (games != null)
                {
                    // 2. Обработка списка игр
                    foreach (var game in games)
                    {
                        // 2.1. Upsert в таблицу games
                        using (var cmd = new NpgsqlCommand(@"
                        INSERT INTO games (game_id, game_name)
                        VALUES (@game_id, @game_name)
                        ON CONFLICT (game_id) DO UPDATE
                            SET game_name = EXCLUDED.game_name;
                    ", conn, transaction))
                        {
                            cmd.Parameters.AddWithValue("game_id", game.Id);
                            cmd.Parameters.AddWithValue("game_name", game.Name);
                            cmd.ExecuteNonQuery();
                        }

                        // 2.2. Добавление записи в таблицу user_to_games
                        using (var cmd = new NpgsqlCommand(@"
                        INSERT INTO user_to_games (user_id, game_id)
                        VALUES (@user_id, @game_id)
                        ON CONFLICT DO NOTHING;
                    ", conn, transaction))
                        {
                            cmd.Parameters.AddWithValue("user_id", userId);
                            cmd.Parameters.AddWithValue("game_id", game.Id);
                            cmd.ExecuteNonQuery();
                        }
                    }
                }

                if (availabilities != null)
                {
                    // 3. Обработка списка доступностей
                    foreach (var availability in availabilities)
                    {
                        // 3.1. Upsert в таблицу availabilities  
                        using (var cmd = new NpgsqlCommand(@"
                        INSERT INTO availabilities (id, day_of_week, start_time, end_time)
                        VALUES (@id, @day_of_week, @start_time, @end_time)
                        ON CONFLICT (id) DO UPDATE
                            SET day_of_week = EXCLUDED.day_of_week,
                                start_time = EXCLUDED.start_time,
                                end_time = EXCLUDED.end_time;
                    ", conn, transaction))
                        {
                            cmd.Parameters.AddWithValue("id", availability.Id);

                            cmd.Parameters.Add(new NpgsqlParameter("start_time", NpgsqlDbType.TimeTz)
                            {
                                Value = availability.StartTime
                            });

                            cmd.Parameters.Add(new NpgsqlParameter("end_time", NpgsqlDbType.TimeTz)
                            {
                                Value = availability.EndTime
                            });

                            cmd.Parameters.Add(new NpgsqlParameter
                            {
                                ParameterName = "day_of_week",
                                Value = availability.DayOfWeek, // Значение типа Availability.DayOfWeekEnum
                                DataTypeName = "day_of_week" // Имя PostgreSQL ENUM
                            });

                            cmd.ExecuteNonQuery();
                        }

                        // 3.2. Добавление записи в таблицу users_to_availability
                        using (var cmd = new NpgsqlCommand(@"
                        INSERT INTO users_to_availability (user_id, availability_id)
                        VALUES (@user_id, @availability_id)
                        ON CONFLICT DO NOTHING;
                    ", conn, transaction))
                        {
                            cmd.Parameters.AddWithValue("user_id", userId);
                            cmd.Parameters.AddWithValue("availability_id", availability.Id);
                            cmd.ExecuteNonQuery();
                        }
                    }
                }

                transaction.Commit();
            }
            catch (Exception e)
            {
                throw new Exception($"Ошибка UpsertUserProfile: {e.Message}", e);
            }
        }

        /// <summary>
        /// Создание или обновление анкеты
        /// </summary>
        /// <param name="applicationId">ID анкеты</param>
        /// <param name="purposeName">Название цели</param>
        /// <param name="title">Заголовок</param>
        /// <param name="isHidden">Скрыта ли анкета</param>
        /// <param name="description">Описание</param>
        /// <param name="contacts">Контакты</param>
        /// <param name="games">Список игр</param>
        /// <param name="availabilities">Список доступностей</param>
        /// <param name="ownerId">Id владельца</param>
        /// <remarks>
        /// Выполняет в транзакции:
        /// 1. Обновление основной информации
        /// 2. Обработку связанных игр
        /// 3. Обработку доступностей
        /// </remarks>
        /// <exception cref="Exception">Ошибки выполнения транзакции</exception>
        public static void UpsertApplication(int applicationId, string purposeName, string title, bool isHidden,
            int ownerId, string? description = null,
            string? contacts = null, List<Game>? games = null, List<Availability>? availabilities = null)
        {
            using var conn = new NpgsqlConnection(ConnectionString);
            conn.Open();

            using var transaction = conn.BeginTransaction();
            try
            {
                var purposeId = GetPurposeIdByName(purposeName);

                using (var cmd = new NpgsqlCommand(@"
                    INSERT INTO applications (id, title, description, contacts, purpose_id, owner_id, is_hidden)
                    VALUES (@application_id, @title, @description, @contacts, @purpose_id, @owner_id, @is_hidden)
                    ON CONFLICT (id) DO UPDATE
                        SET title = EXCLUDED.title,
                            description = EXCLUDED.description,
                            purpose_id = EXCLUDED.purpose_id,
                            contacts = EXCLUDED.contacts,
                            is_hidden = EXCLUDED.is_hidden,
                            owner_id = EXCLUDED.owner_id;
                ", conn, transaction))
                {
                    cmd.Parameters.AddWithValue("application_id", applicationId);
                    cmd.Parameters.AddWithValue("title", title ?? (object)DBNull.Value);
                    cmd.Parameters.AddWithValue("description", description ?? (object)DBNull.Value);
                    cmd.Parameters.AddWithValue("contacts", contacts ?? (object)DBNull.Value);
                    cmd.Parameters.AddWithValue("purpose_id", purposeId);
                    cmd.Parameters.AddWithValue("owner_id", ownerId);
                    cmd.Parameters.AddWithValue("is_hidden", isHidden);
                    cmd.ExecuteNonQuery();
                }

                // Удаляем игры из таблицы applications_to_games, чтобы запихать новые
                using (var cmd = new NpgsqlCommand(@"
                    delete from applications_to_games where app_id = @app_id
                ", conn, transaction))
                {
                    cmd.Parameters.AddWithValue("app_id", applicationId);
                    cmd.ExecuteNonQuery();
                }

                // Удаляем времена из таблицы applications_to_availability, чтобы запихать новые
                using (var cmd = new NpgsqlCommand(@"
                    delete from applications_to_availability where application_id = @app_id
                ", conn, transaction))
                {
                    cmd.Parameters.AddWithValue("app_id", applicationId);
                    cmd.ExecuteNonQuery();
                }

                if (games != null)
                {
                    // 2. Обработка списка игр
                    foreach (var game in games)
                    {
                        // 2.1. Upsert в таблицу games
                        using (var cmd = new NpgsqlCommand(@"
                        INSERT INTO games (game_id, game_name)
                        VALUES (@game_id, @game_name)
                        ON CONFLICT (game_id) DO UPDATE
                            SET game_name = EXCLUDED.game_name;
                    ", conn, transaction))
                        {
                            cmd.Parameters.AddWithValue("game_id", game.Id);
                            cmd.Parameters.AddWithValue("game_name", game.Name);
                            cmd.ExecuteNonQuery();
                        }

                        // 2.2. Добавление записи в таблицу user_to_games
                        using (var cmd = new NpgsqlCommand(@"
                        INSERT INTO applications_to_games (app_id, game_id)
                        VALUES (@app_id, @game_id)
                        ON CONFLICT DO NOTHING;
                    ", conn, transaction))
                        {
                            cmd.Parameters.AddWithValue("app_id", applicationId);
                            cmd.Parameters.AddWithValue("game_id", game.Id);
                            cmd.ExecuteNonQuery();
                        }
                    }
                }

                if (availabilities != null)
                {
                    // 3. Обработка списка доступностей
                    foreach (var availability in availabilities)
                    {
                        // 3.1. Upsert в таблицу availabilities  
                        using (var cmd = new NpgsqlCommand(@"
                        INSERT INTO availabilities (id, day_of_week, start_time, end_time)
                        VALUES (@id, @day_of_week, @start_time, @end_time)
                        ON CONFLICT (id) DO UPDATE
                            SET day_of_week = EXCLUDED.day_of_week,
                                start_time = EXCLUDED.start_time,
                                end_time = EXCLUDED.end_time;
                    ", conn, transaction))
                        {
                            cmd.Parameters.AddWithValue("id", availability.Id);

                            cmd.Parameters.Add(new NpgsqlParameter("start_time", NpgsqlDbType.TimeTz)
                            {
                                Value = availability.StartTime
                            });

                            cmd.Parameters.Add(new NpgsqlParameter("end_time", NpgsqlDbType.TimeTz)
                            {
                                Value = availability.EndTime
                            });

                            cmd.Parameters.Add(new NpgsqlParameter
                            {
                                ParameterName = "day_of_week",
                                Value = availability.DayOfWeek, // Значение типа Availability.DayOfWeekEnum
                                DataTypeName = "day_of_week" // Имя PostgreSQL ENUM
                            });

                            cmd.ExecuteNonQuery();
                        }

                        // 3.2. Добавление записи в таблицу users_to_availability
                        using (var cmd = new NpgsqlCommand(@"
                        INSERT INTO applications_to_availability (application_id, availability_id)
                        VALUES (@application_id, @availability_id)
                        ON CONFLICT DO NOTHING;
                    ", conn, transaction))
                        {
                            cmd.Parameters.AddWithValue("application_id", applicationId);
                            cmd.Parameters.AddWithValue("availability_id", availability.Id);
                            cmd.ExecuteNonQuery();
                        }
                    }
                }

                transaction.Commit();
            }
            catch (Exception e)
            {
                throw new Exception($"Ошибка UpsertUserProfile: {e.Message}", e);
            }
        }

        /// <summary>
        /// Получает общее количество анкет в системе
        /// </summary>
        /// <returns>Общее количество анкет</returns>
        public static int GetTotalApplicationsCount()
        {
            using var conn = new NpgsqlConnection(ConnectionString);

            try
            {
                conn.Open();

                using var cmd = new NpgsqlCommand("SELECT COUNT(*) FROM applications", conn);

                return Convert.ToInt32(cmd.ExecuteScalar());
            }
            catch (Exception ex)
            {
                throw new Exception($"Ошибка при получении общего количества анкет: {ex.Message}", ex);
            }
        }

        /// <summary>
        /// Deletes an application and all its related data from the database
        /// </summary>
        /// <param name="applicationId">ID of the application to delete</param>
        /// <exception cref="Exception">Throws when there's an error during deletion</exception>
        public static bool DeleteApplication(int applicationId)
        {
            using var conn = new NpgsqlConnection(ConnectionString);
            conn.Open();

            using var transaction = conn.BeginTransaction();
            try
            {
                // 1. Delete from participants table first (due to foreign key constraints)
                using (var cmd = new NpgsqlCommand(@"
            DELETE FROM participants 
            WHERE application_id = @applicationId
        ", conn, transaction))
                {
                    cmd.Parameters.AddWithValue("applicationId", applicationId);
                    cmd.ExecuteNonQuery();
                }

                // 2. Delete from applications_to_games
                using (var cmd = new NpgsqlCommand(@"
            DELETE FROM applications_to_games 
            WHERE app_id = @applicationId
        ", conn, transaction))
                {
                    cmd.Parameters.AddWithValue("applicationId", applicationId);
                    cmd.ExecuteNonQuery();
                }

                // 3. Delete from applications_to_availability
                using (var cmd = new NpgsqlCommand(@"
            DELETE FROM applications_to_availability 
            WHERE application_id = @applicationId
        ", conn, transaction))
                {
                    cmd.Parameters.AddWithValue("applicationId", applicationId);
                    cmd.ExecuteNonQuery();
                }

                // 4. Finally delete the application itself
                using (var cmd = new NpgsqlCommand(@"
            DELETE FROM applications 
            WHERE id = @applicationId
        ", conn, transaction))
                {
                    cmd.Parameters.AddWithValue("applicationId", applicationId);
                    var rowsAffected = cmd.ExecuteNonQuery();

                    if (rowsAffected == 0)
                    {
                        throw new Exception($"Application with ID {applicationId} not found");
                    }
                }

                transaction.Commit();
                return true;
            }
            catch (Exception ex)
            {
                transaction.Rollback();
                throw new Exception($"Error deleting application: {ex.Message}", ex);
            }
        }

        /// <summary>
        /// Получает ID владельца анкеты по ID анкеты
        /// </summary>
        /// <param name="applicationId">ID анкеты</param>
        /// <returns>ID пользователя-владельца или null, если анкета не найдена</returns>
        public static int? GetUserIdByApplicationId(int applicationId)
        {
            using var conn = new NpgsqlConnection(ConnectionString);

            try
            {
                conn.Open();

                using var cmd = new NpgsqlCommand(@"
            SELECT owner_id 
            FROM applications 
            WHERE id = @applicationId", conn);

                cmd.Parameters.AddWithValue("applicationId", applicationId);

                var result = cmd.ExecuteScalar();

                return result != null ? Convert.ToInt32(result) : (int?)null;
            }
            catch (Exception ex)
            {
                throw new Exception($"Ошибка при получении владельца анкеты: {ex.Message}", ex);
            }
        }

        /// <summary>
        /// Смена видимости анкеты
        /// </summary>
        /// <param name="applicationId">ID анкеты</param>
        /// <param name="isHidden">Скрыта ли анкета</param>
        public static void ChangeApplictionVisibilityById(int applicationId, bool isHidden)
        {
            using var conn = new NpgsqlConnection(ConnectionString);

            try
            {
                conn.Open();

                using var cmd = new NpgsqlCommand();
                cmd.Connection = conn;
                cmd.CommandText = @"update applications set is_hidden = @isHidden where id = @applicationId";

                cmd.Parameters.AddWithValue("applicationId", applicationId);
                cmd.Parameters.AddWithValue("isHidden", isHidden);

                cmd.ExecuteNonQuery();
            }
            catch (Exception ex)
            {
                throw new Exception($"Ошибка при изменении видимости анкеты: {ex.Message}", ex);
            }
        }

        /// <summary>
        /// Получение id цели по имени
        /// </summary>
        /// <param name="purposeName">Название цели</param>
        public static int GetPurposeIdByName(string purposeName)
        {
            using var conn = new NpgsqlConnection(ConnectionString);
            conn.Open();

            using (var cmd = new NpgsqlCommand(@"
                        SELECT id FROM purposes WHERE purpose = @purpose;
                    ", conn))
            {
                cmd.Parameters.AddWithValue("purpose", purposeName);
                var result = cmd.ExecuteScalar();

                var purposeId = 0;
                if (result != null)
                    purposeId = Convert.ToInt32(result);
                else
                    throw new Exception($"Цель с именем '{purposeName}' не найдена в таблице purposes.");

                return purposeId;
            }
        }

        /// <summary>
        /// Получает отфильтрованные анкеты по цели и играм
        /// </summary>
        /// <param name="purposeName">Название цели</param>
        /// <param name="games">Список игр</param>
        /// <returns>Список объектов Application</returns>
        public static List<Application> GetFiltredApplications(string? purposeName = null, List<Game>? games = null)
        {
            using var conn = new NpgsqlConnection(ConnectionString);
            conn.Open();

            // Собираем динамический SQL и параметры
            var sql = new StringBuilder(@"
                SELECT a.id, a.title, a.description, a.contacts, a.purpose_id, a.owner_id, a.is_hidden
                FROM applications a
            ");
            var filters = new List<string>();
            using var cmd = new NpgsqlCommand { Connection = conn };

            // Фильтрация по цели
            if (!string.IsNullOrEmpty(purposeName))
            {
                var purposeId = GetPurposeIdByName(purposeName);
                filters.Add("a.purpose_id = @purposeId");
                cmd.Parameters.AddWithValue("purposeId", purposeId);
            }

            // Фильтрация по списку игр
            if (games != null && games.Any())
            {
                // JOIN с applications_to_games
                sql.AppendLine("JOIN applications_to_games ag ON ag.app_id = a.id");
                filters.Add("ag.game_id = ANY(@gameIds)");
                // Передаём массив идентификаторов игр
                var gameIds = games.Select(g => g.Id).ToArray();
                var param = new NpgsqlParameter("gameIds", NpgsqlDbType.Array | NpgsqlDbType.Integer)
                {
                    Value = gameIds
                };
                cmd.Parameters.Add(param);
            }

            // Всегда исключаем скрытые анкеты
            filters.Add("a.is_hidden = false");

            if (filters.Any())
            {
                sql.Append(" WHERE ");
                sql.Append(string.Join(" AND ", filters));
            }

            // Если делали JOIN по играм, имеет смысл сгруппировать, чтобы не дублировать анкеты
            if (games != null && games.Any())
            {
                sql.AppendLine(
                    " GROUP BY a.id, a.title, a.description, a.contacts, a.purpose_id, a.owner_id, a.is_hidden");
            }

            cmd.CommandText = sql.ToString();

            var result = new List<Application>();
            using (var reader = cmd.ExecuteReader())
            {
                while (reader.Read())
                {
                    result.Add(new Application(
                        reader.GetInt32(0),
                        reader.GetString(1),
                        reader.IsDBNull(2) ? null : reader.GetString(2),
                        reader.IsDBNull(3) ? null : reader.GetString(3),
                        reader.GetInt32(4),
                        reader.GetInt32(5),
                        reader.GetBoolean(6)
                    ));
                }
            }

            return result;
        }
        
        /// <summary>
        /// Возвращает всех членов команды/анкеты
        /// </summary>
        /// <param name="applicationId">ID анкеты</param>
        public static List<UserData> GetAllApplicationMembers(int applicationId)
        {
            using var conn = new NpgsqlConnection(ConnectionString);

            try
            {
                var userDatas = new List<UserData>();
                conn.Open();

                using var cmd = new NpgsqlCommand();
                cmd.Connection = conn;
                cmd.CommandText = @"select id, username, email from participants
                                    join users_data on participants.user_id = users_data.id
                                    where application_id = @applicationId;";

                cmd.Parameters.AddWithValue("applicationId", applicationId);

                using var reader = cmd.ExecuteReader();
                
                while (reader.Read())
                {
                    userDatas.Add(new UserData(reader.GetInt32(0), reader.GetString(2), reader.GetString(1)));
                }

                return userDatas;
            }
            catch (Exception ex)
            {
                throw new Exception($"Ошибка при получении всех участников анкеты: {ex.Message}", ex);
            }
        }
        
        /// <summary>
        /// Возвращает список анкет, в которых состоит пользователь
        /// </summary>
        /// <param name="userId">ID пользователя</param>
        public static List<Application> GetAllUserMemberApplications(int userId)
        {
            using var conn = new NpgsqlConnection(ConnectionString);

            try
            {
                var userDatas = new List<Application>();
                conn.Open();

                using var cmd = new NpgsqlCommand();
                cmd.Connection = conn;
                cmd.CommandText = @"select id, title, description, contacts, owner_id, purpose_id, is_hidden from participants
                                    join applications on participants.application_id = applications.id
                                    where user_id = @userId;";

                cmd.Parameters.AddWithValue("userId", userId);

                using var reader = cmd.ExecuteReader();
                
                while (reader.Read())
                {
                    userDatas.Add(new Application(reader.GetInt32(0), reader.GetString(1), reader.GetString(2), reader.GetString(3),
                        reader.GetInt32(5), reader.GetInt32(4), reader.GetBoolean(6)));
                }

                return userDatas;
            }
            catch (Exception ex)
            {
                throw new Exception($"Ошибка при получении всех анкет, в которых состоит пользователь: {ex.Message}", ex);
            }
        }
        
        /// <summary>
        /// Добавление пользователя в анкету
        /// </summary>
        /// <param name="applicationId">ID анкеты</param>
        /// <param name="newMemberId">ID нового пользователя</param>
        public static void AddMemberToApplication(int applicationId, int newMemberId)
        {
            using var conn = new NpgsqlConnection(ConnectionString);

            try
            {
                conn.Open();

                using var cmd = new NpgsqlCommand();
                cmd.Connection = conn;
                cmd.CommandText = @"insert into participants (application_id, user_id)
                                        values (@applicationId, @userId)";

                cmd.Parameters.AddWithValue("applicationId", applicationId);
                cmd.Parameters.AddWithValue("userId", newMemberId);

                cmd.ExecuteNonQuery();
            }
            catch (Exception ex)
            {
                throw new Exception($"Ошибка при изменении видимости анкеты: {ex.Message}", ex);
            }
        }
        
        /// <summary>
        /// Удаление пользоваетля из анкеты
        /// </summary>
        /// <param name="applicationId">ID анкеты</param>
        /// <param name="memberId">ID пользователя</param>
        public static void DeleteMemberFromApplication(int applicationId, int memberId)
        {
            using var conn = new NpgsqlConnection(ConnectionString);

            try
            {
                conn.Open();

                using var cmd = new NpgsqlCommand();
                cmd.Connection = conn;
                cmd.CommandText = @"delete from participants where user_id = @userId;";

                cmd.Parameters.AddWithValue("applicationId", applicationId);
                cmd.Parameters.AddWithValue("userId", memberId);

                cmd.ExecuteNonQuery();
            }
            catch (Exception ex)
            {
                throw new Exception($"Ошибка при удалении пользователя из анкеты: {ex.Message}", ex);
            }
        }
    }
}