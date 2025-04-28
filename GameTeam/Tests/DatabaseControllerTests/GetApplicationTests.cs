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
    public class GetApplicationTests
    {
        // Основные тестовые данные
        private const int TestAppId = 22222;
        private const string TestPurpose = "TestPurpose";
        private const string TestAppTitle = "TestApplication";
        private const string TestAppDescription = "Описание заявки";
        private const string TestAppContacts = "Контакты";
        private const int TestAppOwnerId = 12345;

        // Доступность
        private const int TestAppAvailabilityId = 98765;
        private const Availability.DayOfWeekEnum TestAppDayOfWeek = Availability.DayOfWeekEnum.Friday;
        private readonly OffsetTime testAppStartTime = new OffsetTime(new LocalTime(14, 0, 0), Offset.FromHours(3));
        private readonly OffsetTime testAppEndTime = new OffsetTime(new LocalTime(16, 0, 0), Offset.FromHours(3));

        // Игра
        private const int TestAppGameId = 888;
        private const string TestAppGameName = "TestAppGame";

        // Для GetAllApplications теста
        private const int HiddenAppId = 33333;
        private const int VisibleAppId = 33334;

        private readonly string connectionString = DatabaseController.ConnectionString;
        private int purposeId;

        [SetUp]
        public void SetUp()
        {
            using var conn = new NpgsqlConnection(connectionString);
            conn.Open();

            // Создание цели
            using (var cmd = new NpgsqlCommand(@"
                INSERT INTO purposes (purpose)
                VALUES (@purpose)
                ON CONFLICT (purpose) DO NOTHING;", conn))
            {
                cmd.Parameters.AddWithValue("purpose", TestPurpose);
                cmd.ExecuteNonQuery();
            }

            // Получение purpose_id
            using (var cmd = new NpgsqlCommand(
                "SELECT id FROM purposes WHERE purpose = @purpose;", conn))
            {
                cmd.Parameters.AddWithValue("purpose", TestPurpose);
                purposeId = Convert.ToInt32(cmd.ExecuteScalar());
            }

            // Вставка основной заявки
            using (var cmd = new NpgsqlCommand(@"
                INSERT INTO applications (id, title, description, contacts, purpose_id, owner_id, is_hidden)
                VALUES (@id, @title, @description, @contacts, @purpose_id, @owner_id, @is_hidden)
                ON CONFLICT (id) DO UPDATE SET
                    title = EXCLUDED.title,
                    description = EXCLUDED.description,
                    contacts = EXCLUDED.contacts,
                    purpose_id = EXCLUDED.purpose_id,
                    owner_id = EXCLUDED.owner_id,
                    is_hidden = EXCLUDED.is_hidden;", conn))
            {
                cmd.Parameters.AddWithValue("id", TestAppId);
                cmd.Parameters.AddWithValue("title", TestAppTitle);
                cmd.Parameters.AddWithValue("description", TestAppDescription);
                cmd.Parameters.AddWithValue("contacts", TestAppContacts);
                cmd.Parameters.AddWithValue("purpose_id", purposeId);
                cmd.Parameters.AddWithValue("owner_id", TestAppOwnerId);
                cmd.Parameters.AddWithValue("is_hidden", false);
                cmd.ExecuteNonQuery();
            }

            // Вставка доступности
            using (var cmd = new NpgsqlCommand(@"
                INSERT INTO availabilities (id, day_of_week, start_time, end_time)
                VALUES (@id, @day_of_week, @start_time, @end_time)
                ON CONFLICT (id) DO UPDATE SET
                    day_of_week = EXCLUDED.day_of_week,
                    start_time = EXCLUDED.start_time,
                    end_time = EXCLUDED.end_time;", conn))
            {
                cmd.Parameters.AddWithValue("id", TestAppAvailabilityId);
                cmd.Parameters.Add(new NpgsqlParameter("start_time", NpgsqlDbType.TimeTz) { Value = testAppStartTime });
                cmd.Parameters.Add(new NpgsqlParameter("end_time", NpgsqlDbType.TimeTz) { Value = testAppEndTime });
                cmd.Parameters.Add(new NpgsqlParameter
                {
                    ParameterName = "day_of_week",
                    Value = TestAppDayOfWeek,
                    DataTypeName = "day_of_week"
                });
                cmd.ExecuteNonQuery();
            }

            // Связь заявка-доступность
            using (var cmd = new NpgsqlCommand(@"
                INSERT INTO applications_to_availability (application_id, availability_id)
                VALUES (@application_id, @availability_id)
                ON CONFLICT DO NOTHING;", conn))
            {
                cmd.Parameters.AddWithValue("application_id", TestAppId);
                cmd.Parameters.AddWithValue("availability_id", TestAppAvailabilityId);
                cmd.ExecuteNonQuery();
            }

            // Вставка игры
            using (var cmd = new NpgsqlCommand(@"
                INSERT INTO games (game_id, game_name)
                VALUES (@game_id, @game_name)
                ON CONFLICT (game_id) DO UPDATE SET game_name = EXCLUDED.game_name;", conn))
            {
                cmd.Parameters.AddWithValue("game_id", TestAppGameId);
                cmd.Parameters.AddWithValue("game_name", TestAppGameName);
                cmd.ExecuteNonQuery();
            }

            // Связь заявка-игра
            using (var cmd = new NpgsqlCommand(@"
                INSERT INTO applications_to_games (app_id, game_id)
                VALUES (@app_id, @game_id)
                ON CONFLICT DO NOTHING;", conn))
            {
                cmd.Parameters.AddWithValue("app_id", TestAppId);
                cmd.Parameters.AddWithValue("game_id", TestAppGameId);
                cmd.ExecuteNonQuery();
            }

            // Вставка для GetAllApplications теста: скрытая и видимая заявки
            using (var cmd = new NpgsqlCommand(@"
                INSERT INTO applications (id, title, description, contacts, purpose_id, owner_id, is_hidden)
                VALUES (@id, @title, @description, @contacts, @purpose_id, @owner_id, @is_hidden)
                ON CONFLICT (id) DO UPDATE SET is_hidden = EXCLUDED.is_hidden;", conn))
            {
                // скрытая
                cmd.Parameters.AddWithValue("id", HiddenAppId);
                cmd.Parameters.AddWithValue("title", "HiddenApp");
                cmd.Parameters.AddWithValue("description", "Should not be returned");
                cmd.Parameters.AddWithValue("contacts", "none");
                cmd.Parameters.AddWithValue("purpose_id", purposeId);
                cmd.Parameters.AddWithValue("owner_id", TestAppOwnerId);
                cmd.Parameters.AddWithValue("is_hidden", true);
                cmd.ExecuteNonQuery();
                cmd.Parameters.Clear();

                // видимая
                cmd.Parameters.AddWithValue("id", VisibleAppId);
                cmd.Parameters.AddWithValue("title", "VisibleApp");
                cmd.Parameters.AddWithValue("description", "Should be returned");
                cmd.Parameters.AddWithValue("contacts", "none");
                cmd.Parameters.AddWithValue("purpose_id", purposeId);
                cmd.Parameters.AddWithValue("owner_id", TestAppOwnerId);
                cmd.Parameters.AddWithValue("is_hidden", false);
                cmd.ExecuteNonQuery();
            }
        }

        [TearDown]
        public void TearDown()
        {
            using var conn = new NpgsqlConnection(connectionString);
            conn.Open();

            // Удаление связей и заявок
            using (var cmd = new NpgsqlCommand(@"
                DELETE FROM applications_to_availability WHERE application_id = @appId;
                DELETE FROM applications_to_games      WHERE app_id          = @appId;
                DELETE FROM applications                WHERE id              IN (@main, @hidden, @visible);
                DELETE FROM games                       WHERE game_id         = @gameId;
                DELETE FROM availabilities              WHERE id              = @availId;", conn))
            {
                cmd.Parameters.AddWithValue("appId", TestAppId);
                cmd.Parameters.AddWithValue("main", TestAppId);
                cmd.Parameters.AddWithValue("hidden", HiddenAppId);
                cmd.Parameters.AddWithValue("visible", VisibleAppId);
                cmd.Parameters.AddWithValue("gameId", TestAppGameId);
                cmd.Parameters.AddWithValue("availId", TestAppAvailabilityId);
                cmd.ExecuteNonQuery();
            }

            // Удаление цели
            using (var cmd = new NpgsqlCommand(
                "DELETE FROM purposes WHERE purpose = @purpose;", conn))
            {
                cmd.Parameters.AddWithValue("purpose", TestPurpose);
                cmd.ExecuteNonQuery();
            }
        }

        [Test]
        public void Test_GetAvailabilities_ForApplication_Returns_Correct_Availability_List()
        {
            var availabilities = DatabaseController.GetAvailabilities(TestAppId, false);
            Assert.IsNotNull(availabilities);
            Assert.IsNotEmpty(availabilities);

            var availability = availabilities.FirstOrDefault(a => a.Id == TestAppAvailabilityId);
            Assert.IsNotNull(availability);
            Assert.AreEqual(TestAppDayOfWeek, availability.DayOfWeek);
            Assert.AreEqual(testAppStartTime, availability.StartTime);
            Assert.AreEqual(testAppEndTime, availability.EndTime);
        }

        [Test]
        public void Test_GetGames_ForApplication_Returns_Correct_Game_List()
        {
            var games = DatabaseController.GetGames(TestAppId, false);
            Assert.IsNotNull(games);
            Assert.IsNotEmpty(games);

            var game = games.FirstOrDefault(g => g.Id == TestAppGameId);
            Assert.IsNotNull(game);
            Assert.AreEqual(TestAppGameName, game.Name);
        }

        [Test]
        public void Test_GetAllApplications_Returns_Valid_Application_With_Availabilities_And_Games_And_Owner()
        {
            var applications = DatabaseController.GetAllApplications();
            Assert.IsNotNull(applications);
            Assert.IsNotEmpty(applications);

            var testApplication = applications.FirstOrDefault(a => a.Id == TestAppId);
            Assert.IsNotNull(testApplication);
            Assert.AreEqual(TestAppTitle, testApplication.Title);
            Assert.AreEqual(TestAppDescription, testApplication.Description);
            Assert.AreEqual(TestAppContacts, testApplication.Contacts);
            Assert.AreEqual(TestAppOwnerId, testApplication.OwnerId);
        }

        [Test]
        public void Test_GetAllApplications_ReturnsOnlyVisibleApplications()
        {
            var allApps = DatabaseController.GetAllApplications();
            Assert.IsNotNull(allApps);
            Assert.IsTrue(allApps.Any(a => a.Id == VisibleAppId),  "VisibleApp должна присутствовать");
            Assert.IsFalse(allApps.Any(a => a.Id == HiddenAppId), "HiddenApp не должна присутствовать");
        }
    }
}
