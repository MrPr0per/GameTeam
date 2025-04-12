using GameTeam.Classes.Data;
using GameTeam.Scripts.Controllers;
using Npgsql;
using NUnit.Framework;

namespace GameTeam.Tests.DatabaseControllerTests;

[TestFixture]
public class GetUserProfileAndGetUserGamesTest
{
    private const int TestUserId = 12345;
    private const string TestUsername = "testuser"; 
    private const string TestEmail = "biba123123@mail.ru";
    private const string TestPassword = "123123123";
    private const string TestDescription = "Тестовое описание";
    private const string TestSkills = "Тестовые навыки";
    private const int TestGameId = 987;
    private const string TestGameName = "TestGame";
    
    private readonly string connectionString = DatabaseController.ConnectionString;

    [SetUp]
    public void SetUp()
    {
        using var conn = new NpgsqlConnection(connectionString);
        conn.Open();

        
        using (var cmd = new NpgsqlCommand(@"
            INSERT INTO users_data (id, username, email, password)
            VALUES (@id, @username, @email, @password)
            ON CONFLICT (username) DO UPDATE SET id = EXCLUDED.id;", conn))
        {
            cmd.Parameters.AddWithValue("id", TestUserId);
            cmd.Parameters.AddWithValue("username", TestUsername);
            cmd.Parameters.AddWithValue("email", TestEmail);
            cmd.Parameters.AddWithValue("password", TestPassword);
            cmd.ExecuteNonQuery();
        }
        
        using (var cmd = new NpgsqlCommand(@"
            INSERT INTO user_profiles (user_id, about_description, skills)
            VALUES (@user_id, @description, @skills)
            ON CONFLICT (user_id) DO UPDATE SET about_description = EXCLUDED.about_description, skills = EXCLUDED.skills;", conn))
        {
            cmd.Parameters.AddWithValue("user_id", TestUserId);
            cmd.Parameters.AddWithValue("description", TestDescription);
            cmd.Parameters.AddWithValue("skills", TestSkills);
            cmd.ExecuteNonQuery();
        }

        // Вставляем тестовую игру в таблицу games
        using (var cmd = new NpgsqlCommand(@"
            INSERT INTO games (game_id, game_name)
            VALUES (@game_id, @game_name)
            ON CONFLICT (game_id) DO UPDATE SET game_name = EXCLUDED.game_name;", conn))
        {
            cmd.Parameters.AddWithValue("game_id", TestGameId);
            cmd.Parameters.AddWithValue("game_name", TestGameName);
            cmd.ExecuteNonQuery();
        }

        // Добавляем связь между пользователем и игрой в таблицу user_to_games
        using (var cmd = new NpgsqlCommand(@"
            INSERT INTO user_to_games (user_id, game_id)
            VALUES (@user_id, @game_id)
            ON CONFLICT DO NOTHING;", conn))
        {
            cmd.Parameters.AddWithValue("user_id", TestUserId);
            cmd.Parameters.AddWithValue("game_id", TestGameId);
            cmd.ExecuteNonQuery();
        }
    }

    [TearDown]
    public void TearDown()
    {
        using var conn = new NpgsqlConnection(connectionString);
        conn.Open();

        // Удаляем связь из таблицы user_to_games
        using (var cmd = new NpgsqlCommand("DELETE FROM user_to_games WHERE user_id = @user_id;", conn))
        {
            cmd.Parameters.AddWithValue("user_id", TestUserId);
            cmd.ExecuteNonQuery();
        }

        // Удаляем запись из таблицы user_profiles
        using (var cmd = new NpgsqlCommand("DELETE FROM user_profiles WHERE user_id = @user_id;", conn))
        {
            cmd.Parameters.AddWithValue("user_id", TestUserId);
            cmd.ExecuteNonQuery();
        }

        // Удаляем запись из таблицы users_data
        using (var cmd = new NpgsqlCommand("DELETE FROM users_data WHERE id = @id;", conn))
        {
            cmd.Parameters.AddWithValue("id", TestUserId);
            cmd.ExecuteNonQuery();
        }

        // Удаляем тестовую игру
        using (var cmd = new NpgsqlCommand("DELETE FROM games WHERE game_id = @game_id;", conn))
        {
            cmd.Parameters.AddWithValue("game_id", TestGameId);
            cmd.ExecuteNonQuery();
        }
    }

    [Test]
    public void Test_GetUserGames_Returns_Correct_Game_List()
    {
        // Act: вызываем метод загрузки игр для тестового пользователя
        List<Game> games = DatabaseController.GetUserGames(TestUserId);

        // Assert: список игр не должен быть null и должен содержать хотя бы одну игру
        Assert.IsNotNull(games, "Список игр не должен быть null.");
        Assert.IsNotEmpty(games, "Список игр не должен быть пустым.");

        // Находим тестовую игру в списке
        var game = games.FirstOrDefault(g => g.Id == TestGameId);
        Assert.IsNotNull(game, "Тестовая игра не найдена в списке игр.");
        Assert.AreEqual(TestGameName, game.Name, "Название игры не соответствует ожидаемому.");
    }

    [Test]
    public void Test_GetUserProfile_WithValidUsername_ReturnsCorrectProfile()
    {
        // Act: получаем профиль по username
        var profile = DatabaseController.GetUserProfile(TestUsername);

        // Assert: проверяем, что профиль не null и данные совпадают с ожидаемыми
        Assert.IsNotNull(profile, "Профиль пользователя не должен быть null.");
        Assert.AreEqual(TestUserId, profile.UserId, "UserId профиля не соответствует ожидаемому значению.");
        Assert.AreEqual(TestDescription, profile.Description, "Описание профиля не соответствует ожидаемому.");
        Assert.AreEqual(TestSkills, profile.Skills, "Навыки профиля не соответствуют ожидаемому.");
    }
    
    [Test]
    public void Test_GetUserProfile_WithInvalidUsername_ThrowsException()
    {
        // Arrange: задаём несуществующий username
        var nonExistentUsername = "nonexistentuser";

        // Act & Assert: ожидается выбрасывание исключения с соответствующим сообщением
        var ex = Assert.Throws<Exception>(() => DatabaseController.GetUserProfile(nonExistentUsername));
        Assert.That(ex.Message, Does.Contain($"Ошибка GetUserProfile: пользователя с таким username:{nonExistentUsername} не существует"),
            "Ожидаемое сообщение об ошибке не совпадает.");
    }
}