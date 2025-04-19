using Npgsql;
using GameTeam.Classes.Data;
using NodaTime;
using NpgsqlTypes;
using System.Security.Cryptography;
using System.Text;

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
				var errorField = ex.ConstraintName.Contains("username")
					? "Имя пользователя"
					: "Email";
				throw new Exception($"{errorField} уже занято");
			}
			catch (Exception ex)
			{
				throw new Exception($"Ошибка регистрации: {ex.Message}");
			}
		}

		/// <summary>
		/// Аутентификация пользователя
		/// </summary>
		/// <param name="username">Имя пользователя (null если используется email)</param>
		/// <param name="email">Электронная почта (null если используется имя)</param>
		/// <param name="password">Пароль</param>
		/// <param name="outUsername">Возвращаемое имя пользователя</param>
		/// <returns>true если аутентификация успешна</returns>
		public static bool Login(string? username, string? email, string password, out string? outUsername)
		{
			outUsername = null;

			using var conn = new NpgsqlConnection(ConnectionString);
			conn.Open();
			using var cmd = new NpgsqlCommand();
			cmd.Connection = conn;

			if (username != null)
			{
				cmd.CommandText = @"
                            SELECT username FROM users_data 
                            WHERE username = @username AND password = @password";
				cmd.Parameters.AddWithValue("username", username);
			}
			else if (email != null)
			{
				cmd.CommandText = @"
                            SELECT username FROM users_data 
                            WHERE email = @email AND password = @password";
				cmd.Parameters.AddWithValue("email", email);
			}
			else
			{
				return false;
			}

			cmd.Parameters.AddWithValue("password", password);

			using var reader = cmd.ExecuteReader();

			if (!reader.Read())
				return false;

			outUsername = reader.GetString(0);
			return true;
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
                using var insertCmd = new NpgsqlCommand("INSERT INTO games (game_name) VALUES (@gameName) RETURNING game_id", conn);
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
        public static Availability GetOrCreateAvailability(Availability.DayOfWeekEnum dayOfWeek, OffsetTime start, OffsetTime end)
        {
            using var conn = new NpgsqlConnection(ConnectionString);
            conn.Open();
            // Пытаемся найти запись в таблице availabilities по параметрам
            using var cmd = new NpgsqlCommand(
                "SELECT id FROM availabilities WHERE day_of_week = @day_of_week AND start_time = @start_time AND end_time = @end_time", conn);
           
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
		/// Получение всех анкет из базы данных
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
					select * from applications";

				using var reader = cmd.ExecuteReader();
				while (reader.Read())
				{
					applications.Add(new Application(reader.GetInt32(0), reader.GetString(1),
						reader.GetString(2), reader.GetString(3), reader.GetInt32(4)));
				}

				return applications;
			}
			catch (Exception e)
			{
				throw new Exception($"Ошибка GetAllApplications: {e}");
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
		public static void UpsertUserProfile(int? userId, string? aboutDescription = null,
			List<Game>? games = null, List<Availability>? availabilities = null)
		{
			if (userId is null) 
				throw new ArgumentException("userId cannot be null");
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
		/// <param name="description">Описание</param>
		/// <param name="contacts">Контакты</param>
		/// <param name="games">Список игр</param>
		/// <param name="availabilities">Список доступностей</param>
		/// <remarks>
		/// Выполняет в транзакции:
		/// 1. Обновление основной информации
		/// 2. Обработку связанных игр
		/// 3. Обработку доступностей
		/// </remarks>
		/// <exception cref="Exception">Ошибки выполнения транзакции</exception>
		public static void UpsertApplication(int applicationId, string purposeName, string title, string? description = null,
			string? contacts = null, List<Game>? games = null, List<Availability>? availabilities = null)
		{
			using var conn = new NpgsqlConnection(ConnectionString);
			conn.Open();
			
			using var transaction = conn.BeginTransaction();
			try
			{
				var purposeId = 0;
				
				using (var cmd = new NpgsqlCommand(@"
                        SELECT id FROM purposes WHERE purpose = @purpose;
                    ", conn, transaction))
				{
					cmd.Parameters.AddWithValue("purpose", purposeName);
					var result = cmd.ExecuteScalar();
					
					if (result != null)
						purposeId = Convert.ToInt32(result);
					else
						throw new Exception($"Цель с именем '{purposeName}' не найдена в таблице purposes.");
				}
				
				using (var cmd = new NpgsqlCommand(@"
                    INSERT INTO applications (id, title, description, contacts, purpose_id)
                    VALUES (@application_id, @title, @description, @contacts, @purpose_id)
                    ON CONFLICT (id) DO UPDATE
                        SET title = EXCLUDED.title,
                            description = EXCLUDED.description,
                            purpose_id = EXCLUDED.purpose_id,
                            contacts = EXCLUDED.contacts;
                ", conn, transaction))
				{
					cmd.Parameters.AddWithValue("application_id", applicationId);
					cmd.Parameters.AddWithValue("title", title ?? (object)DBNull.Value);
					cmd.Parameters.AddWithValue("description", description ?? (object)DBNull.Value);
					cmd.Parameters.AddWithValue("contacts", contacts ?? (object)DBNull.Value);
					cmd.Parameters.AddWithValue("purpose_id", purposeId);
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
	}
}