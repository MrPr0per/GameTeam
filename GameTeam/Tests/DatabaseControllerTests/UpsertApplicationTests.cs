using System;
using System.Collections.Generic;
using System.Linq;
using NodaTime;
using Npgsql;
using NpgsqlTypes;
using NUnit.Framework;
using GameTeam.Classes.Data;
using GameTeam.Scripts.Controllers;

namespace GameTeam.Tests.DatabaseControllerTests
{
    [TestFixture]
    public class UpsertApplicationTests
    {
        // Тестовые данные для приложения (application)
        private const int TestAppId = 22222;
        private const string TestPurpose = "SomePurpose"; // начальная цель
        private const string UpdatedPurpose = "UpdatedPurpose"; // цель для обновления
        private const string TestAppTitle = "Initial App Title";
        private const string TestAppDescription = "Initial App Description";
        private const string TestAppContacts = "Initial Contacts";

        private const string UpdatedAppTitle = "Updated App Title";
        private const string UpdatedAppDescription = "Updated App Description";
        private const string UpdatedAppContacts = "Updated Contacts";

        // Данные для игры
        private const int TestGameId = 888;
        private const string TestGameName = "Test Application Game";

        // Данные для доступности
        private const int TestAvailabilityId = 98765;
        private const Availability.DayOfWeekEnum TestDayOfWeek = Availability.DayOfWeekEnum.Friday;
        private readonly OffsetTime TestStartTime = new OffsetTime(new LocalTime(14, 0, 0), Offset.FromHours(0));
        private readonly OffsetTime TestEndTime = new OffsetTime(new LocalTime(16, 0, 0), Offset.FromHours(0));

        // Строка подключения
        private readonly string connectionString = DatabaseController.ConnectionString;

        // Новый тестовый набор
        private const int TestGameId2 = 333;
        private const string TestGameName2 = "New Application Game";
        private const int TestAvailabilityId2 = 44444;
        private readonly OffsetTime TestStartTime2 = new OffsetTime(new LocalTime(18, 0, 0), Offset.FromHours(0));
        private readonly OffsetTime TestEndTime2   = new OffsetTime(new LocalTime(20, 0, 0), Offset.FromHours(0));
        
        [SetUp]
        public void SetUp()
        {
            using var conn = new NpgsqlConnection(connectionString);
            conn.Open();
            // Создаем запись для начальной цели и для цели обновления, если их ещё нет.
            using (var cmd = new NpgsqlCommand(@"
                INSERT INTO purposes (id, purpose)
                VALUES (DEFAULT, @purpose)
                ON CONFLICT (purpose) DO NOTHING;
            ", conn))
            {
                // Создаем начальную цель TestPurpose
                cmd.Parameters.AddWithValue("purpose", TestPurpose);
                cmd.ExecuteNonQuery();
                cmd.Parameters.Clear();
                // Создаем цель для обновления UpdatedPurpose
                cmd.Parameters.AddWithValue("purpose", UpdatedPurpose);
                cmd.ExecuteNonQuery();
            }
        }

        [TearDown]
        public void TearDown()
        {
            using var conn = new NpgsqlConnection(connectionString);
            conn.Open();

            // Удаляем связи в таблицах приложения
            using (var cmd = new NpgsqlCommand("DELETE FROM applications_to_games WHERE app_id = @id;", conn))
            {
                cmd.Parameters.AddWithValue("id", TestAppId);
                cmd.ExecuteNonQuery();
            }
            using (var cmd = new NpgsqlCommand("DELETE FROM applications_to_availability WHERE application_id = @id;", conn))
            {
                cmd.Parameters.AddWithValue("id", TestAppId);
                cmd.ExecuteNonQuery();
            }
            // Удаляем запись из таблицы applications
            using (var cmd = new NpgsqlCommand("DELETE FROM applications WHERE id = @id;", conn))
            {
                cmd.Parameters.AddWithValue("id", TestAppId);
                cmd.ExecuteNonQuery();
            }
            // Удаляем тестовые записи из мастер‑таблиц (игры и доступности)
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
            // Удаляем тестовые цели из purposes
            using (var cmd = new NpgsqlCommand("DELETE FROM purposes WHERE purpose = @purpose;", conn))
            {
                cmd.Parameters.AddWithValue("purpose", TestPurpose);
                cmd.ExecuteNonQuery();
                cmd.Parameters.Clear();
                cmd.Parameters.AddWithValue("purpose", UpdatedPurpose);
                cmd.ExecuteNonQuery();
            }
            
            // Удаляем связи и мастер‑записи второго набора
            using (var cmd = new NpgsqlCommand("DELETE FROM applications_to_games WHERE game_id = @gameId;", conn))
            {
                cmd.Parameters.AddWithValue("gameId", TestGameId2);
                cmd.ExecuteNonQuery();
            }
            using (var cmd = new NpgsqlCommand("DELETE FROM games WHERE game_id = @gameId;", conn))
            {
                cmd.Parameters.AddWithValue("gameId", TestGameId2);
                cmd.ExecuteNonQuery();
            }
            using (var cmd = new NpgsqlCommand("DELETE FROM applications_to_availability WHERE availability_id = @availId;", conn))
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
        public void Test_UpsertApplication_BasicFields()
        {
            // Act: вызываем метод для вставки базовых данных приложения без игр и доступностей, используя TestPurpose.
            DatabaseController.UpsertApplication(TestAppId, TestPurpose, TestAppTitle, TestAppDescription, TestAppContacts, null, null);

            using var conn = new NpgsqlConnection(connectionString);
            conn.Open();
            int purposeId = 0;

            // Читаем запись из applications
            using (var cmd = new NpgsqlCommand("SELECT title, description, contacts, purpose_id FROM applications WHERE id = @id", conn))
            {
                cmd.Parameters.AddWithValue("id", TestAppId);
                using (var reader = cmd.ExecuteReader())
                {
                    Assert.IsTrue(reader.Read(), "Запись приложения не найдена.");
                    var title = reader.IsDBNull(0) ? null : reader.GetString(0);
                    var description = reader.IsDBNull(1) ? null : reader.GetString(1);
                    var contacts = reader.IsDBNull(2) ? null : reader.GetString(2);
                    purposeId = reader.GetInt32(3);

                    Assert.AreEqual(TestAppTitle, title, "Неверное значение поля title.");
                    Assert.AreEqual(TestAppDescription, description, "Неверное значение поля description.");
                    Assert.AreEqual(TestAppContacts, contacts, "Неверное значение поля contacts.");
                }
            }

            // Проверяем, что purpose_id соответствует TestPurpose
            using (var cmdPurpose = new NpgsqlCommand("SELECT purpose FROM purposes WHERE id = @pid", conn))
            {
                cmdPurpose.Parameters.AddWithValue("pid", purposeId);
                var purpose = cmdPurpose.ExecuteScalar() as string;
                Assert.AreEqual(TestPurpose, purpose, "Неверное значение поля purpose_id.");
            }
        }

        [Test]
        public void Test_UpsertApplication_WithGames()
        {
            // Подготавливаем список игр
            var games = new List<Game> { new Game(TestGameId, TestGameName) };

            // Act: вызываем метод для вставки приложения с играми, используя TestPurpose.
            DatabaseController.UpsertApplication(TestAppId, TestPurpose, TestAppTitle, TestAppDescription, TestAppContacts, games, null);

            using var conn = new NpgsqlConnection(connectionString);
            conn.Open();
            // Проверяем запись в таблице games
            using (var cmd = new NpgsqlCommand("SELECT COUNT(*) FROM games WHERE game_id = @gameId", conn))
            {
                cmd.Parameters.AddWithValue("gameId", TestGameId);
                var count = (long)cmd.ExecuteScalar();
                Assert.Greater(count, 0, "Запись игры не найдена в таблице games.");
            }

            // Проверяем связь в applications_to_games
            using (var cmd = new NpgsqlCommand("SELECT COUNT(*) FROM applications_to_games WHERE app_id = @appId AND game_id = @gameId", conn))
            {
                cmd.Parameters.AddWithValue("appId", TestAppId);
                cmd.Parameters.AddWithValue("gameId", TestGameId);
                var count = (long)cmd.ExecuteScalar();
                Assert.Greater(count, 0, "Связь приложения с игрой не установлена в applications_to_games.");
            }
        }

        [Test]
        public void Test_UpsertApplication_WithAvailabilities()
        {
            // Подготавливаем список доступностей
            var availabilities = new List<Availability>
            {
                new Availability(TestAvailabilityId, TestDayOfWeek, TestStartTime, TestEndTime)
            };

            // Act: вызываем метод для вставки приложения с доступностями, используя TestPurpose.
            DatabaseController.UpsertApplication(TestAppId, TestPurpose, TestAppTitle, TestAppDescription, TestAppContacts, null, availabilities);

            using var conn = new NpgsqlConnection(connectionString);
            conn.Open();
            // Проверяем наличие записи в таблице availabilities
            using (var cmd = new NpgsqlCommand("SELECT COUNT(*) FROM availabilities WHERE id = @availId", conn))
            {
                cmd.Parameters.AddWithValue("availId", TestAvailabilityId);
                var count = (long)cmd.ExecuteScalar();
                Assert.Greater(count, 0, "Запись доступности не найдена в таблице availabilities.");
            }
            // Проверяем связь в applications_to_availability
            using (var cmd = new NpgsqlCommand("SELECT COUNT(*) FROM applications_to_availability WHERE application_id = @appId AND availability_id = @availId", conn))
            {
                cmd.Parameters.AddWithValue("appId", TestAppId);
                cmd.Parameters.AddWithValue("availId", TestAvailabilityId);
                var count = (long)cmd.ExecuteScalar();
                Assert.Greater(count, 0, "Связь приложения с доступностью не установлена в applications_to_availability.");
            }
        }

        [Test]
        public void Test_UpsertApplication_UpdateProfileFields()
        {
            // Сначала вставляем исходные данные с целью TestPurpose
            DatabaseController.UpsertApplication(TestAppId, TestPurpose, TestAppTitle, TestAppDescription, TestAppContacts, null, null);
            // Затем обновляем данные, включая изменение цели на UpdatedPurpose
            DatabaseController.UpsertApplication(TestAppId, UpdatedPurpose, UpdatedAppTitle, UpdatedAppDescription, UpdatedAppContacts, null, null);

            using var conn = new NpgsqlConnection(connectionString);
            conn.Open();
            int purposeId;
            var title = "";
            var description = "";
            var contacts = "";
            // Читаем обновленные данные из applications
            using (var cmd = new NpgsqlCommand("SELECT title, description, contacts, purpose_id FROM applications WHERE id = @id", conn))
            {
                cmd.Parameters.AddWithValue("id", TestAppId);
                using (var reader = cmd.ExecuteReader())
                {
                    Assert.IsTrue(reader.Read(), "Запись приложения не найдена после обновления.");
                    title = reader.IsDBNull(0) ? "" : reader.GetString(0);
                    description = reader.IsDBNull(1) ? "" : reader.GetString(1);
                    contacts = reader.IsDBNull(2) ? "" : reader.GetString(2);
                    purposeId = reader.GetInt32(3);
                }
            }
            Assert.AreEqual(UpdatedAppTitle, title, "Значение title не обновилось корректно.");
            Assert.AreEqual(UpdatedAppDescription, description, "Значение description не обновилось корректно.");
            Assert.AreEqual(UpdatedAppContacts, contacts, "Значение contacts не обновилось корректно.");

            // Проверяем, что purpose_id теперь соответствует UpdatedPurpose
            using (var cmdPurpose = new NpgsqlCommand("SELECT purpose FROM purposes WHERE id = @pid", conn))
            {
                cmdPurpose.Parameters.AddWithValue("pid", purposeId);
                var purpose = cmdPurpose.ExecuteScalar() as string;
                Assert.AreEqual(UpdatedPurpose, purpose, "Значение поля purpose_id не обновилось корректно.");
            }
        }
        
        [Test]
        public void Test_UpsertApplication_ReplacesOldRecords()
        {
            // 1) Сначала создаём приложение с первоначальными играми и доступностями
            var initialGames = new List<Game> { new Game(TestGameId, TestGameName) };
            var initialAvailabilities = new List<Availability>
            {
                new Availability(TestAvailabilityId, TestDayOfWeek, TestStartTime, TestEndTime)
            };
            DatabaseController.UpsertApplication(TestAppId, TestPurpose, TestAppTitle, TestAppDescription, TestAppContacts, initialGames, initialAvailabilities);

            // 2) Повторный вызов с новыми списками — старые связи должны удалиться
            var newGames = new List<Game> { new Game(TestGameId2, TestGameName2) };
            var newAvailabilities = new List<Availability>
            {
                new Availability(TestAvailabilityId2, Availability.DayOfWeekEnum.Monday, TestStartTime2, TestEndTime2)
            };
            DatabaseController.UpsertApplication(TestAppId, TestPurpose, TestAppTitle, TestAppDescription, TestAppContacts, newGames, newAvailabilities);

            using var conn = new NpgsqlConnection(connectionString);
            conn.Open();

            // Старые игровые связи удалены
            using (var cmd = new NpgsqlCommand(
                       "SELECT COUNT(*) FROM applications_to_games WHERE app_id = @appId AND game_id = @gameId", conn))
            {
                cmd.Parameters.AddWithValue("appId", TestAppId);
                cmd.Parameters.AddWithValue("gameId", TestGameId);
                var oldCount = (long)cmd.ExecuteScalar();
                Assert.AreEqual(0, oldCount, "Старая связь с игрой не должна существовать");
            }
            // Новые игровые связи созданы
            using (var cmd = new NpgsqlCommand(
                       "SELECT COUNT(*) FROM applications_to_games WHERE app_id = @appId AND game_id = @gameId", conn))
            {
                cmd.Parameters.AddWithValue("appId", TestAppId);
                cmd.Parameters.AddWithValue("gameId", TestGameId2);
                var newCount = (long)cmd.ExecuteScalar();
                Assert.Greater(newCount, 0, "Новая связь с игрой должна присутствовать");
            }

            // Старая доступность удалена
            using (var cmd = new NpgsqlCommand(
                       "SELECT COUNT(*) FROM applications_to_availability WHERE application_id = @appId AND availability_id = @availId", conn))
            {
                cmd.Parameters.AddWithValue("appId", TestAppId);
                cmd.Parameters.AddWithValue("availId", TestAvailabilityId);
                var oldAvCount = (long)cmd.ExecuteScalar();
                Assert.AreEqual(0, oldAvCount, "Старая доступность не должна существовать");
            }
            // Новая доступность добавлена
            using (var cmd = new NpgsqlCommand(
                       "SELECT COUNT(*) FROM applications_to_availability WHERE application_id = @appId AND availability_id = @availId", conn))
            {
                cmd.Parameters.AddWithValue("appId", TestAppId);
                cmd.Parameters.AddWithValue("availId", TestAvailabilityId2);
                var newAvCount = (long)cmd.ExecuteScalar();
                Assert.Greater(newAvCount, 0, "Новая доступность должна присутствовать");
            }
        }
    }
}
