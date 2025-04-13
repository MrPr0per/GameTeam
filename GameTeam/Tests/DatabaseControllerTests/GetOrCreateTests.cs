using GameTeam.Classes.Data;
using GameTeam.Scripts.Controllers;
using NodaTime;
using Npgsql;
using NpgsqlTypes;
using NUnit.Framework;

namespace GameTeam.Tests.DatabaseControllerTests;

[TestFixture]
    public class GetOrCreateTests
    {
        // Тестовые данные для игры
        private const string TestGameName = "TestGame_GetOrCreate";
        private const int ExistingGameId = 999;
        
        // Тестовые данные для доступности
        private const Availability.DayOfWeekEnum TestDayOfWeek = Availability.DayOfWeekEnum.Monday;
        private readonly OffsetTime testStartTime = new OffsetTime(new LocalTime(10, 0), Offset.FromHours(3));
        private readonly OffsetTime testEndTime = new OffsetTime(new LocalTime(12, 0), Offset.FromHours(3));
        private const int ExistingAvailabilityId = 88888;

        private readonly string connectionString = DatabaseController.ConnectionString;

        [SetUp]
        public void SetUp()
        {
            using var conn = new NpgsqlConnection(connectionString);
            conn.Open();

            // Создаем существующую игру для тестов
            using (var cmd = new NpgsqlCommand(
                "INSERT INTO games (game_id, game_name) VALUES (@id, @name) ON CONFLICT (game_id) DO UPDATE SET game_name = EXCLUDED.game_name;",
                conn))
            {
                cmd.Parameters.AddWithValue("id", ExistingGameId);
                cmd.Parameters.AddWithValue("name", TestGameName);
                cmd.ExecuteNonQuery();
            }

            // Создаем существующую доступность для тестов
            using (var cmd = new NpgsqlCommand(
                "INSERT INTO availabilities (id, day_of_week, start_time, end_time) VALUES (@id, @day, @start, @end) ON CONFLICT (id) DO UPDATE SET day_of_week = EXCLUDED.day_of_week, start_time = EXCLUDED.start_time, end_time = EXCLUDED.end_time;",
                conn))
            {
                cmd.Parameters.AddWithValue("id", ExistingAvailabilityId);
                cmd.Parameters.Add(new NpgsqlParameter("day", TestDayOfWeek) { DataTypeName = "day_of_week" });
                cmd.Parameters.Add(new NpgsqlParameter("start", NpgsqlDbType.TimeTz) { Value = testStartTime });
                cmd.Parameters.Add(new NpgsqlParameter("end", NpgsqlDbType.TimeTz) { Value = testEndTime });
                cmd.ExecuteNonQuery();
            }
        }

        [TearDown]
        public void TearDown()
        {
            using var conn = new NpgsqlConnection(connectionString);
            conn.Open();

            // Удаляем тестовые игры
            using (var cmd = new NpgsqlCommand("DELETE FROM games WHERE game_name = @name", conn))
            {
                cmd.Parameters.AddWithValue("name", TestGameName);
                cmd.ExecuteNonQuery();
            }

            // Удаляем тестовые доступности
            using (var cmd = new NpgsqlCommand("DELETE FROM availabilities WHERE id = @id", conn))
            {
                cmd.Parameters.AddWithValue("id", ExistingAvailabilityId);
                cmd.ExecuteNonQuery();
            }
        }

        [Test]
        public void GetOrCreateGame_ExistingGame_ReturnsExistingGame()
        {
            // Act
            var result = DatabaseController.GetOrCreateGame(TestGameName);

            // Assert
            Assert.AreEqual(ExistingGameId, result.Id);
            Assert.AreEqual(TestGameName, result.Name);
        }

        [Test]
        public void GetOrCreateGame_NewGame_CreatesNewGame()
        {
            // Arrange
            const string newGameName = "NewTestGame_123";

            // Act
            var result = DatabaseController.GetOrCreateGame(newGameName);

            // Assert
            Assert.IsTrue(result.Id > 0);
            Assert.AreEqual(newGameName, result.Name);

            // Проверка в БД
            using var conn = new NpgsqlConnection(connectionString);
            conn.Open();
            using var cmd = new NpgsqlCommand("SELECT game_id FROM games WHERE game_name = @name", conn);
            cmd.Parameters.AddWithValue("name", newGameName);
            var dbId = Convert.ToInt32(cmd.ExecuteScalar());
            
            Assert.AreEqual(result.Id, dbId);

            // Cleanup
            using var deleteCmd = new NpgsqlCommand("DELETE FROM games WHERE game_id = @id", conn);
            deleteCmd.Parameters.AddWithValue("id", result.Id);
            deleteCmd.ExecuteNonQuery();
        }

        [Test]
        public void GetOrCreateAvailability_ExistingAvailability_ReturnsExisting()
        {
            // Act
            var result = DatabaseController.GetOrCreateAvailability(TestDayOfWeek, testStartTime, testEndTime);

            // Assert
            Assert.AreEqual(ExistingAvailabilityId, result.Id);
            Assert.AreEqual(TestDayOfWeek, result.DayOfWeek);
            Assert.AreEqual(testStartTime, result.StartTime);
            Assert.AreEqual(testEndTime, result.EndTime);
        }

        [Test]
        public void GetOrCreateAvailability_NewAvailability_CreatesNew()
        {
            // Arrange
            var newDay = Availability.DayOfWeekEnum.Tuesday;
            var newStart = new OffsetTime(new LocalTime(13, 0), Offset.FromHours(3));
            var newEnd = new OffsetTime(new LocalTime(15, 0), Offset.FromHours(3));

            // Act
            var result = DatabaseController.GetOrCreateAvailability(newDay, newStart, newEnd);

            // Assert
            Assert.IsTrue(result.Id > 0);
            Assert.AreEqual(newDay, result.DayOfWeek);
            Assert.AreEqual(newStart, result.StartTime);
            Assert.AreEqual(newEnd, result.EndTime);

            // Проверка в БД
            using var conn = new NpgsqlConnection(connectionString);
            conn.Open();
            using var cmd = new NpgsqlCommand(
                "SELECT id FROM availabilities WHERE day_of_week = @day AND start_time = @start AND end_time = @end",
                conn);
            cmd.Parameters.Add(new NpgsqlParameter("day", newDay) { DataTypeName = "day_of_week" });
            cmd.Parameters.Add(new NpgsqlParameter("start", NpgsqlDbType.TimeTz) { Value = newStart });
            cmd.Parameters.Add(new NpgsqlParameter("end", NpgsqlDbType.TimeTz) { Value = newEnd });
            var dbId = Convert.ToInt32(cmd.ExecuteScalar());
            
            Assert.AreEqual(result.Id, dbId);

            // Cleanup
            using var deleteCmd = new NpgsqlCommand("DELETE FROM availabilities WHERE id = @id", conn);
            deleteCmd.Parameters.AddWithValue("id", result.Id);
            deleteCmd.ExecuteNonQuery();
        }
    }