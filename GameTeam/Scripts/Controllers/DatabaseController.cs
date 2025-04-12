using Npgsql;
using System;
using System.Collections.Generic;
using System.Data;
using GameTeam.Classes.Data;
using NodaTime;
using NpgsqlTypes;

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

		[Obsolete("Obsolete")]
		static DatabaseController()
		{
			// Регистрация ENUM
			NpgsqlConnection.GlobalTypeMapper.MapEnum<Availability.DayOfWeekEnum>("day_of_week");

			// Регистрация NodaTime
			NpgsqlConnection.GlobalTypeMapper.UseNodaTime();

			// Для .NET 6+
			AppContext.SetSwitch("Npgsql.EnableLegacyTimestampBehavior", true);
		}

		public static void Register(string username, string email, string password)
		{
			using var conn = new NpgsqlConnection(ConnectionString);
			try
			{
				conn.Open();
				using var cmd = new NpgsqlCommand();
				cmd.Connection = conn;
				cmd.CommandText = @"
                            INSERT INTO users_data (username, email, password) 
                            VALUES (@username, @email, @password)";

				cmd.Parameters.AddWithValue("username", username);
				cmd.Parameters.AddWithValue("email", email);
				cmd.Parameters.AddWithValue("password", password);

				cmd.ExecuteNonQuery();
			}
			catch (PostgresException ex) when (ex.SqlState == "23505")
			{
				string errorField = ex.ConstraintName.Contains("username")
					? "Имя пользователя"
					: "Email";
				throw new Exception($"{errorField} уже занято");
			}
			catch (Exception ex)
			{
				throw new Exception($"Ошибка регистрации: {ex.Message}");
			}
		}

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
					reader.GetString(1), reader.GetString(2));
			}
			catch (Exception e)
			{
				throw new Exception($"Ошибка GetUserProfile: {e}");
			}
		}

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
						reader.GetString(2), reader.GetString(3)));
				}

				return applications;
			}
			catch (Exception e)
			{
				throw new Exception($"Ошибка GetAllApplications: {e}");
			}
		}

		// Добавление данных профиля пользователя в бд
		public static void UpsertUserProfile(int userId, string? aboutDescription = null, string? skills = null,
			List<Game>? games = null, List<Availability>? availabilities = null)
		{
			using var conn = new NpgsqlConnection(ConnectionString);
			conn.Open();
			
			using var transaction = conn.BeginTransaction();
			try
			{
				// 1. Upsert в user_profiles
				using (var cmd = new NpgsqlCommand(@"
                    INSERT INTO user_profiles (user_id, about_description, skills)
                    VALUES (@user_id, @aboutDescription, @skills)
                    ON CONFLICT (user_id) DO UPDATE
                        SET about_description = EXCLUDED.about_description,
                            skills = EXCLUDED.skills;
                ", conn, transaction))
				{
					cmd.Parameters.AddWithValue("user_id", userId);
					cmd.Parameters.AddWithValue("aboutDescription", aboutDescription ?? (object)DBNull.Value);
					cmd.Parameters.AddWithValue("skills", skills ?? (object)DBNull.Value);
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
	}
}