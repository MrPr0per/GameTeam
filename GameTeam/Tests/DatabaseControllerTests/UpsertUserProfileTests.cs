using GameTeam.Classes.Data;
using GameTeam.Scripts.Controllers;
using NodaTime;
using Npgsql;
using NUnit.Framework;

namespace GameTeam.Tests.DatabaseControllerTests;

[TestFixture]
public class UpsertUserProfileTests
{
	// Тестовые данные для тестового пользователя
	private const int TestUserId = 12345;
	private const string TestUsername = "testuser";
	private const string TestEmail = "testuser@example.com";
	private const string TestPassword = "password";

	// Данные для профиля
	private const string InitialDescription = "Initial description";
	private const string UpdatedDescription = "Updated description";

	// Данные для игры
	private const int TestGameId = 987;
	private const string TestGameName = "Test Game";

	// Данные для доступности
	private const int TestAvailabilityId = 54321;
	private const Availability.DayOfWeekEnum TestDayOfWeek = Availability.DayOfWeekEnum.Monday;
	private readonly OffsetTime testStartTime = new OffsetTime(new LocalTime(10, 0, 0), Offset.FromHours(0));
	private readonly OffsetTime testEndTime = new OffsetTime(new LocalTime(12, 0, 0), Offset.FromHours(0));

	// Строка подключения
	private readonly string connectionString = DatabaseController.ConnectionString;
	
	// Добавьте в начало класса UpsertUserProfileTests:
	private const int TestGameId2 = 654;
	private const string TestGameName2 = "New Game";
	private const int TestAvailabilityId2 = 77777;
	private readonly OffsetTime testStartTime2 = new OffsetTime(new LocalTime(14, 0, 0), Offset.FromHours(0));
	private readonly OffsetTime testEndTime2   = new OffsetTime(new LocalTime(16, 0, 0), Offset.FromHours(0));


	[SetUp]
	public void SetUp()
	{
		using var conn = new NpgsqlConnection(connectionString);
		conn.Open();

		// Вставляем тестового пользователя в таблицу users_data (нужно для внешнего ключа)
		using (var cmd = new NpgsqlCommand(@"
                INSERT INTO users_data (id, username, email, password)
                VALUES (@id, @username, @email, @password)
                ON CONFLICT (id) DO UPDATE SET username = EXCLUDED.username, 
                    email = EXCLUDED.email, password = EXCLUDED.password;
            ", conn))
		{
			cmd.Parameters.AddWithValue("id", TestUserId);
			cmd.Parameters.AddWithValue("username", TestUsername);
			cmd.Parameters.AddWithValue("email", TestEmail);
			cmd.Parameters.AddWithValue("password", TestPassword);
			cmd.ExecuteNonQuery();
		}
	}

	[TearDown]
	public void TearDown()
	{
		using var conn = new NpgsqlConnection(connectionString);
		conn.Open();
		// Удаляем записи из таблицы связей и профилей
		using (var cmd = new NpgsqlCommand("DELETE FROM user_to_games WHERE user_id = @id;", conn))
		{
			cmd.Parameters.AddWithValue("id", TestUserId);
			cmd.ExecuteNonQuery();
		}

		using (var cmd = new NpgsqlCommand("DELETE FROM users_to_availability WHERE user_id = @id;", conn))
		{
			cmd.Parameters.AddWithValue("id", TestUserId);
			cmd.ExecuteNonQuery();
		}

		using (var cmd = new NpgsqlCommand("DELETE FROM user_profiles WHERE user_id = @id;", conn))
		{
			cmd.Parameters.AddWithValue("id", TestUserId);
			cmd.ExecuteNonQuery();
		}

		// Также удаляем тестовую игру и доступность, если они были добавлены
		using (var cmd = new NpgsqlCommand("DELETE FROM games WHERE game_id = @gameId;", conn))
		{
			cmd.Parameters.AddWithValue("gameId", TestGameId);
			cmd.ExecuteNonQuery();
		}

		using (var cmd = new NpgsqlCommand("DELETE FROM availabilities WHERE id = @availId;", conn))
		{
			cmd.Parameters.AddWithValue("availId", TestAvailabilityId);
			cmd.ExecuteNonQuery();
		}
		
		// и дополнительно
		using (var cmd = new NpgsqlCommand("DELETE FROM user_to_games WHERE game_id = @gameId;", conn))
		{
			cmd.Parameters.AddWithValue("gameId", TestGameId2);
			cmd.ExecuteNonQuery();
		}
		using (var cmd = new NpgsqlCommand("DELETE FROM games WHERE game_id = @gameId;", conn))
		{
			cmd.Parameters.AddWithValue("gameId", TestGameId2);
			cmd.ExecuteNonQuery();
		}
		using (var cmd = new NpgsqlCommand("DELETE FROM users_to_availability WHERE availability_id = @availId;", conn))
		{
			cmd.Parameters.AddWithValue("availId", TestAvailabilityId2);
			cmd.ExecuteNonQuery();
		}
		using (var cmd = new NpgsqlCommand("DELETE FROM availabilities WHERE id = @availId;", conn))
		{
			cmd.Parameters.AddWithValue("availId", TestAvailabilityId2);
			cmd.ExecuteNonQuery();
		}
	}

	[Test]
	public void Test_UpsertUserProfile_BasicFields()
	{
		// Вставка профиля без дополнительных списков
		DatabaseController.UpsertUserProfile(TestUserId, InitialDescription, null, null);

		using var conn = new NpgsqlConnection(connectionString);
		conn.Open();
		
		using (var cmd = new NpgsqlCommand("SELECT about_description FROM user_profiles WHERE user_id = @id",
			       conn))
		{
			cmd.Parameters.AddWithValue("id", TestUserId);
			using var reader = cmd.ExecuteReader();
			Assert.IsTrue(reader.Read(), "Profile row not found");
			var desc = reader.IsDBNull(0) ? null : reader.GetString(0);
			Assert.AreEqual(InitialDescription, desc);
		}
	}

	[Test]
	public void Test_UpsertUserProfile_WithGames()
	{
		var games = new List<Game> { new Game(TestGameId, TestGameName) };

		DatabaseController.UpsertUserProfile(TestUserId, InitialDescription, games, null);

		using var conn = new NpgsqlConnection(connectionString);
		conn.Open();

		// Проверяем профиль
		using (var cmd = new NpgsqlCommand("SELECT about_description FROM user_profiles WHERE user_id = @id",
			       conn))
		{
			cmd.Parameters.AddWithValue("id", TestUserId);
			using var reader = cmd.ExecuteReader();
			Assert.IsTrue(reader.Read(), "Profile row not found");
			var desc = reader.IsDBNull(0) ? null : reader.GetString(0);
			Assert.AreEqual(InitialDescription, desc);
		}

		// Проверяем наличие записи в таблице games
		using (var cmd = new NpgsqlCommand("SELECT COUNT(*) FROM games WHERE game_id = @gameId", conn))
		{
			cmd.Parameters.AddWithValue("gameId", TestGameId);
			var count = (long)cmd.ExecuteScalar();
			Assert.Greater(count, 0, "Game record not found in games table");
		}

		// Проверяем связь в таблице user_to_games
		using (var cmd = new NpgsqlCommand(
			       "SELECT COUNT(*) FROM user_to_games WHERE user_id = @id AND game_id = @gameId", conn))
		{
			cmd.Parameters.AddWithValue("id", TestUserId);
			cmd.Parameters.AddWithValue("gameId", TestGameId);
			var count = (long)cmd.ExecuteScalar();
			Assert.Greater(count, 0, "User-to-game link not found in user_to_games");
		}
	}

	[Test]
	public void Test_UpsertUserProfile_WithAvailabilities()
	{
		var availabilities = new List<Availability>
		{
			new Availability(TestAvailabilityId, TestDayOfWeek, testStartTime, testEndTime)
		};

		DatabaseController.UpsertUserProfile(TestUserId, InitialDescription, null, availabilities);

		using var conn = new NpgsqlConnection(connectionString);
		conn.Open();

		// Проверяем профиль
		using (var cmd = new NpgsqlCommand("SELECT about_description FROM user_profiles WHERE user_id = @id",
			       conn))
		{
			cmd.Parameters.AddWithValue("id", TestUserId);
			using var reader = cmd.ExecuteReader();
			Assert.IsTrue(reader.Read(), "Profile row not found");
			var desc = reader.IsDBNull(0) ? null : reader.GetString(0);
			Assert.AreEqual(InitialDescription, desc);
		}

		// Проверяем наличие записи в таблице availabilities
		using (var cmd = new NpgsqlCommand("SELECT COUNT(*) FROM availabilities WHERE id = @availId", conn))
		{
			cmd.Parameters.AddWithValue("availId", TestAvailabilityId);
			var count = (long)cmd.ExecuteScalar();
			Assert.Greater(count, 0, "Availability record not found in availabilities table");
		}

		// Проверяем связь в таблице users_to_availability
		using (var cmd = new NpgsqlCommand(
			       "SELECT COUNT(*) FROM users_to_availability WHERE user_id = @id AND availability_id = @availId",
			       conn))
		{
			cmd.Parameters.AddWithValue("id", TestUserId);
			cmd.Parameters.AddWithValue("availId", TestAvailabilityId);
			var count = (long)cmd.ExecuteScalar();
			Assert.Greater(count, 0, "User-to-availability link not found in users_to_availability");
		}
	}

	[Test]
	public void Test_UpsertUserProfile_UpdateProfileFields()
	{
		// Вставляем начальные данные
		DatabaseController.UpsertUserProfile(TestUserId, InitialDescription, null, null);

		// Затем обновляем профиль новыми значениями
		DatabaseController.UpsertUserProfile(TestUserId, UpdatedDescription, null, null);

		using var conn = new NpgsqlConnection(connectionString);
		conn.Open();
		using (var cmd = new NpgsqlCommand("SELECT about_description FROM user_profiles WHERE user_id = @id",
			       conn))
		{
			cmd.Parameters.AddWithValue("id", TestUserId);
			using var reader = cmd.ExecuteReader();
			Assert.IsTrue(reader.Read(), "Profile row not found after update");
			var desc = reader.IsDBNull(0) ? null : reader.GetString(0);
			Assert.AreEqual(UpdatedDescription, desc, "Profile description was not updated correctly");
		}
	}
	
	[Test]
	public void Test_UpsertUserProfile_ReplacesOldRecords()
	{
	    // 1) Сначала создаём профиль с одними игровыми связями и доступностями
	    var initialGames = new List<Game> 
	    { 
	        new Game(TestGameId, TestGameName) 
	    };
	    var initialAvailabilities = new List<Availability> 
	    { 
	        new Availability(TestAvailabilityId, TestDayOfWeek, testStartTime, testEndTime) 
	    };

	    DatabaseController.UpsertUserProfile(TestUserId, InitialDescription, initialGames, initialAvailabilities);

	    // 2) Потом вызываем с другими списками — должны удалиться все старые связи
	    var newGames = new List<Game>
	    {
	        new Game(TestGameId2, TestGameName2)
	    };
	    var newAvailabilities = new List<Availability>
	    {
	        new Availability(TestAvailabilityId2, Availability.DayOfWeekEnum.Tuesday, testStartTime2, testEndTime2)
	    };

	    DatabaseController.UpsertUserProfile(TestUserId, InitialDescription, newGames, newAvailabilities);

	    using var conn = new NpgsqlConnection(connectionString);
	    conn.Open();

	    // Проверяем, что старая связь с TestGameId удалена
	    using (var cmd = new NpgsqlCommand(
	               "SELECT COUNT(*) FROM user_to_games WHERE user_id = @id AND game_id = @gameId", conn))
	    {
	        cmd.Parameters.AddWithValue("id", TestUserId);
	        cmd.Parameters.AddWithValue("gameId", TestGameId);
	        var oldGameCount = (long)cmd.ExecuteScalar();
	        Assert.AreEqual(0, oldGameCount, "Старая связь с игрой должна быть удалена");
	    }

	    // Проверяем, что новая связь с TestGameId2 создана
	    using (var cmd = new NpgsqlCommand(
	               "SELECT COUNT(*) FROM user_to_games WHERE user_id = @id AND game_id = @gameId", conn))
	    {
	        cmd.Parameters.AddWithValue("id", TestUserId);
	        cmd.Parameters.AddWithValue("gameId", TestGameId2);
	        var newGameCount = (long)cmd.ExecuteScalar();
	        Assert.Greater(newGameCount, 0, "Новая связь с игрой должна присутствовать");
	    }

	    // Проверяем, что старая доступность удалена
	    using (var cmd = new NpgsqlCommand(
	               "SELECT COUNT(*) FROM users_to_availability WHERE user_id = @id AND availability_id = @availId", conn))
	    {
	        cmd.Parameters.AddWithValue("id", TestUserId);
	        cmd.Parameters.AddWithValue("availId", TestAvailabilityId);
	        var oldAvailCount = (long)cmd.ExecuteScalar();
	        Assert.AreEqual(0, oldAvailCount, "Старая доступность должна быть удалена");
	    }

	    // Проверяем, что новая доступность добавлена
	    using (var cmd = new NpgsqlCommand(
	               "SELECT COUNT(*) FROM users_to_availability WHERE user_id = @id AND availability_id = @availId", conn))
	    {
	        cmd.Parameters.AddWithValue("id", TestUserId);
	        cmd.Parameters.AddWithValue("availId", TestAvailabilityId2);
	        var newAvailCount = (long)cmd.ExecuteScalar();
	        Assert.Greater(newAvailCount, 0, "Новая доступность должна присутствовать");
	    }
	}
}