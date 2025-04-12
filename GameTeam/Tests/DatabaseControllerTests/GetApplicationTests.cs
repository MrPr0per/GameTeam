using GameTeam.Classes.Data;
using GameTeam.Scripts.Controllers;
using NodaTime;
using Npgsql;
using NpgsqlTypes;
using NUnit.Framework;

namespace GameTeam.Tests.DatabaseControllerTests;

[TestFixture]
public class GetApplicationTests
{

    // Тестовые данные для заявок (анкет)
    private const int TestAppId = 22222;
    private const string TestAppTitle = "TestApplication";
    private const string TestAppDescription = "Описание заявки";
    private const string TestAppContacts = "Контакты";
    private const int TestAppAvailabilityId = 98765;
    private const Availability.DayOfWeekEnum TestAppDayOfWeek = Availability.DayOfWeekEnum.Friday;
    private readonly OffsetTime testAppStartTime = new OffsetTime(new LocalTime(14, 0, 0), Offset.FromHours(3));
    private readonly OffsetTime testAppEndTime = new OffsetTime(new LocalTime(16, 0, 0), Offset.FromHours(3));
    private const int TestAppGameId = 888;
    private const string TestAppGameName = "TestAppGame";

    private readonly string connectionString = DatabaseController.ConnectionString;

    [SetUp]
    public void SetUp()
    {
        using var conn = new NpgsqlConnection(connectionString);
        conn.Open();
        // ========= Тестовые данные для заявки =========

        // Вставка заявки (applications)
        using (var cmd = new NpgsqlCommand(@"
            INSERT INTO applications (id, title, description, contacts)
            VALUES (@id, @title, @description, @contacts)
            ON CONFLICT (id) DO UPDATE SET 
                title = EXCLUDED.title, 
                description = EXCLUDED.description, 
                contacts = EXCLUDED.contacts;", conn))
        {
            cmd.Parameters.AddWithValue("id", TestAppId);
            cmd.Parameters.AddWithValue("title", TestAppTitle);
            cmd.Parameters.AddWithValue("description", TestAppDescription);
            cmd.Parameters.AddWithValue("contacts", TestAppContacts);
            cmd.ExecuteNonQuery();
        }

        // Вставка доступности для заявки (availabilities)
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

        // Связь заявка-доступность (applications_to_availability)
        using (var cmd = new NpgsqlCommand(@"
            INSERT INTO applications_to_availability (application_id, availability_id)
            VALUES (@application_id, @availability_id)
            ON CONFLICT DO NOTHING;", conn))
        {
            cmd.Parameters.AddWithValue("application_id", TestAppId);
            cmd.Parameters.AddWithValue("availability_id", TestAppAvailabilityId);
            cmd.ExecuteNonQuery();
        }

        // Вставка игры для заявки (games) – можно использовать ту же таблицу games, добавляя новую игру
        using (var cmd = new NpgsqlCommand(@"
            INSERT INTO games (game_id, game_name)
            VALUES (@game_id, @game_name)
            ON CONFLICT (game_id) DO UPDATE SET game_name = EXCLUDED.game_name;", conn))
        {
            cmd.Parameters.AddWithValue("game_id", TestAppGameId);
            cmd.Parameters.AddWithValue("game_name", TestAppGameName);
            cmd.ExecuteNonQuery();
        }

        // Связь заявка-игра (applications_to_games)
        using (var cmd = new NpgsqlCommand(@"
            INSERT INTO applications_to_games (app_id, game_id)
            VALUES (@app_id, @game_id)
            ON CONFLICT DO NOTHING;", conn))
        {
            cmd.Parameters.AddWithValue("app_id", TestAppId);
            cmd.Parameters.AddWithValue("game_id", TestAppGameId);
            cmd.ExecuteNonQuery();
        }
    }

    [TearDown]
    public void TearDown()
    {
        using var conn = new NpgsqlConnection(connectionString);
        conn.Open();
        // ===== Очистка данных для заявки =====
        using (var cmd = new NpgsqlCommand("DELETE FROM applications_to_availability WHERE application_id = @app_id;", conn))
        {
            cmd.Parameters.AddWithValue("app_id", TestAppId);
            cmd.ExecuteNonQuery();
        }
        using (var cmd = new NpgsqlCommand("DELETE FROM applications_to_games WHERE app_id = @app_id;", conn))
        {
            cmd.Parameters.AddWithValue("app_id", TestAppId);
            cmd.ExecuteNonQuery();
        }
        using (var cmd = new NpgsqlCommand("DELETE FROM applications WHERE id = @id;", conn))
        {
            cmd.Parameters.AddWithValue("id", TestAppId);
            cmd.ExecuteNonQuery();
        }

        // ===== Очистка общих данных =====
        using (var cmd = new NpgsqlCommand("DELETE FROM games WHERE game_id IN (@appGameId);", conn))
        {
            cmd.Parameters.AddWithValue("appGameId", TestAppGameId);
            cmd.ExecuteNonQuery();
        }
        using (var cmd = new NpgsqlCommand("DELETE FROM availabilities WHERE id IN (@appAvailId);", conn))
        {
            cmd.Parameters.AddWithValue("appAvailId", TestAppAvailabilityId);
            cmd.ExecuteNonQuery();
        }
    }

    // ===== Тесты для метода GetAvailabilities =====

    [Test]
    public void Test_GetAvailabilities_ForApplication_Returns_Correct_Availability_List()
    {
        var availabilities = DatabaseController.GetAvailabilities(TestAppId, false);
        Assert.IsNotNull(availabilities, "Список доступностей заявки не должен быть null.");
        Assert.IsNotEmpty(availabilities, "Список доступностей заявки не должен быть пустым.");

        var availability = availabilities.FirstOrDefault(a => a.Id == TestAppAvailabilityId);
        Assert.IsNotNull(availability, "Тестовая доступность для заявки не найдена.");
        Assert.AreEqual(TestAppDayOfWeek, availability.DayOfWeek, "День недели доступности заявки не соответствует ожидаемому.");
        Assert.AreEqual(testAppStartTime, availability.StartTime, "Время начала доступности заявки не соответствует ожидаемому.");
        Assert.AreEqual(testAppEndTime, availability.EndTime, "Время окончания доступности заявки не соответствует ожидаемому.");
    }

    // ===== Тесты для метода GetGames =====

    [Test]
    public void Test_GetGames_ForApplication_Returns_Correct_Game_List()
    {
        var games = DatabaseController.GetGames(TestAppId, false);
        Assert.IsNotNull(games, "Список игр заявки не должен быть null.");
        Assert.IsNotEmpty(games, "Список игр заявки не должен быть пустым.");

        var game = games.FirstOrDefault(g => g.Id == TestAppGameId);
        Assert.IsNotNull(game, "Тестовая игра для заявки не найдена.");
        Assert.AreEqual(TestAppGameName, game.Name, "Имя игры для заявки не соответствует ожидаемому.");
    }
    
    [Test]
        public void Test_GetAllApplications_Returns_Valid_Application_With_Availabilities_And_Games()
        {
            var applications = DatabaseController.GetAllApplications();
            
            // Assert: список не должен быть null и содержать хотя бы одну заявку
            Assert.IsNotNull(applications, "Список заявок не должен быть null.");
            Assert.IsNotEmpty(applications, "Список заявок не должен быть пустым.");

            // Ищем тестовую заявку по уникальному идентификатору
            var testApplication = applications.FirstOrDefault(a => a.Id == TestAppId);
            Assert.IsNotNull(testApplication, "Тестовая заявка не найдена в возвращаемом списке.");

            // Проверка основных полей заявки
            Assert.AreEqual(TestAppTitle, testApplication.Title, "Заголовок заявки не соответствует ожидаемому.");
            Assert.AreEqual(TestAppDescription, testApplication.Description, "Описание заявки не соответствует ожидаемому.");
            Assert.AreEqual(TestAppContacts, testApplication.Contacts, "Контакты заявки не соответствуют ожидаемым.");
        }
}