using GameTeam.Classes.Data;
using GameTeam.Scripts.Controllers;
using NodaTime;
using Npgsql;
using NUnit.Framework;

namespace GameTeam.Tests.DatabaseControllerTests;

[TestFixture]
public class UpsertApplicationProfileTests
{
    // Тестовые данные для приложения (application)
    private const int TestAppId = 22222;
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
    // Для теста используем NodaTime.OffsetTime – в таблице поле типа time (без часового пояса)
    private readonly OffsetTime TestStartTime = new OffsetTime(new LocalTime(14, 0, 0), Offset.FromHours(0));
    private readonly OffsetTime TestEndTime = new OffsetTime(new LocalTime(16, 0, 0), Offset.FromHours(0));

    // Строка подключения
    private readonly string connectionString = DatabaseController.ConnectionString;

    [TearDown]
    public void TearDown()
    {
        using var conn = new NpgsqlConnection(connectionString);
        conn.Open();

        // Удаляем связи в таблицах приложений
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
        // Удаляем саму запись из таблицы applications
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
    }

    [Test]
    public void Test_UpsertApplicationProfile_BasicFields()
    {
        // Act: вызываем метод для вставки базовых данных приложения без игр и доступностей
        DatabaseController.UpsertApplicationProfile(TestAppId, TestAppTitle, TestAppDescription, TestAppContacts, null, null);

        // Проверяем запись в таблице applications
        using var conn = new NpgsqlConnection(connectionString);
        conn.Open();
        using (var cmd = new NpgsqlCommand("SELECT title, description, contacts FROM applications WHERE id = @id", conn))
        {
            cmd.Parameters.AddWithValue("id", TestAppId);
            using var reader = cmd.ExecuteReader();
            Assert.IsTrue(reader.Read(), "Запись приложения не найдена.");
            var title = reader.IsDBNull(0) ? null : reader.GetString(0);
            var description = reader.IsDBNull(1) ? null : reader.GetString(1);
            var contacts = reader.IsDBNull(2) ? null : reader.GetString(2);
            Assert.AreEqual(TestAppTitle, title, "Неверное значение поля title.");
            Assert.AreEqual(TestAppDescription, description, "Неверное значение поля description.");
            Assert.AreEqual(TestAppContacts, contacts, "Неверное значение поля contacts.");
        }
    }

    [Test]
    public void Test_UpsertApplicationProfile_WithGames()
    {
        // Подготавливаем список игр
        var games = new List<Game>
        {
            new Game(TestGameId, TestGameName)
        };

        // Act: вызываем метод для вставки профиля приложения с играми
        DatabaseController.UpsertApplicationProfile(TestAppId, TestAppTitle, TestAppDescription, TestAppContacts, games, null);

        using var conn = new NpgsqlConnection(connectionString);
        conn.Open();
        
        // Проверяем наличие записи в таблице games
        using (var cmd = new NpgsqlCommand("SELECT COUNT(*) FROM games WHERE game_id = @gameId", conn))
        {
            cmd.Parameters.AddWithValue("gameId", TestGameId);
            var count = (long)cmd.ExecuteScalar();
            Assert.Greater(count, 0, "Запись игры не найдена в таблице games.");
        }
        
        // Проверяем связь в таблице applications_to_games
        using (var cmd = new NpgsqlCommand("SELECT COUNT(*) FROM applications_to_games WHERE app_id = @appId AND game_id = @gameId", conn))
        {
            cmd.Parameters.AddWithValue("appId", TestAppId);
            cmd.Parameters.AddWithValue("gameId", TestGameId);
            var count = (long)cmd.ExecuteScalar();
            Assert.Greater(count, 0, "Связь приложения с игрой не установлена в applications_to_games.");
        }
    }

    [Test]
    public void Test_UpsertApplicationProfile_WithAvailabilities()
    {
        // Подготавливаем список доступностей
        var availabilities = new List<Availability>
        {
            new Availability(TestAvailabilityId, TestDayOfWeek, TestStartTime, TestEndTime)
        };

        // Act: вызываем метод для вставки профиля приложения с доступностями
        DatabaseController.UpsertApplicationProfile(TestAppId, TestAppTitle, TestAppDescription, TestAppContacts, null, availabilities);

        using var conn = new NpgsqlConnection(connectionString);
        conn.Open();
        
        // Проверяем наличие записи в таблице availabilities
        using (var cmd = new NpgsqlCommand("SELECT COUNT(*) FROM availabilities WHERE id = @availId", conn))
        {
            cmd.Parameters.AddWithValue("availId", TestAvailabilityId);
            var count = (long)cmd.ExecuteScalar();
            Assert.Greater(count, 0, "Запись доступности не найдена в таблице availabilities.");
        }
        // Проверяем связь в таблице applications_to_availability
        using (var cmd = new NpgsqlCommand("SELECT COUNT(*) FROM applications_to_availability WHERE application_id = @appId AND availability_id = @availId", conn))
        {
            cmd.Parameters.AddWithValue("appId", TestAppId);
            cmd.Parameters.AddWithValue("availId", TestAvailabilityId);
            var count = (long)cmd.ExecuteScalar();
            Assert.Greater(count, 0, "Связь приложения с доступностью не установлена в applications_to_availability.");
        }
    }

    [Test]
    public void Test_UpsertApplicationProfile_UpdateProfileFields()
    {
        // Сначала вставляем исходные данные
        DatabaseController.UpsertApplicationProfile(TestAppId, TestAppTitle, TestAppDescription, TestAppContacts, null, null);
        // Затем обновляем профиль новыми значениями
        DatabaseController.UpsertApplicationProfile(TestAppId, UpdatedAppTitle, UpdatedAppDescription, UpdatedAppContacts, null, null);

        using var conn = new NpgsqlConnection(connectionString);
        conn.Open();
        using (var cmd = new NpgsqlCommand("SELECT title, description, contacts FROM applications WHERE id = @id", conn))
        {
            cmd.Parameters.AddWithValue("id", TestAppId);
            using var reader = cmd.ExecuteReader();
            Assert.IsTrue(reader.Read(), "Запись приложения не найдена после обновления.");
            var title = reader.IsDBNull(0) ? null : reader.GetString(0);
            var description = reader.IsDBNull(1) ? null : reader.GetString(1);
            var contacts = reader.IsDBNull(2) ? null : reader.GetString(2);
            Assert.AreEqual(UpdatedAppTitle, title, "Значение title не обновилось корректно.");
            Assert.AreEqual(UpdatedAppDescription, description, "Значение description не обновилось корректно.");
            Assert.AreEqual(UpdatedAppContacts, contacts, "Значение contacts не обновилось корректно.");
        }
    }
}