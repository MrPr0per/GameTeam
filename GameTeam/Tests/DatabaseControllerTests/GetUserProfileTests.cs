using System;
using System.Collections.Generic;
using System.Linq;
using GameTeam.Classes.Data;
using GameTeam.Scripts.Controllers;
using NodaTime;
using Npgsql;
using NpgsqlTypes;
using NUnit.Framework;

namespace GameTeam.Tests.DatabaseControllerTests;

[TestFixture]
public class GetUserProfileAndGetUserGamesTest
{
    // Общие тестовые данные
    private const int TestUserId = 12345;
    private const string TestUsername = "testuser";
    private const string TestEmail = "biba123123@mail.ru";
    private const string TestPassword = "123123123";
    private const string TestDescription = "Тестовое описание";
    private const string TestSkills = "Тестовые навыки";
    private const int TestGameId = 987;
    private const string TestGameName = "TestGame";
    
    // Данные для тестирования доступностей
    private const int TestAvailabilityId = 54321;
    private const  Availability.DayOfWeekEnum TestDayOfWeek = Availability.DayOfWeekEnum.Monday;
    private const Availability.DayOfWeekEnum TestDayOfWeekValue = Availability.DayOfWeekEnum.Monday;
    private readonly OffsetTime testStartTime = new OffsetTime(new LocalTime(11, 11, 11), Offset.FromHours(3));
    private readonly OffsetTime testEndTime = new OffsetTime(new LocalTime(12, 12, 12), Offset.FromHours(3));
    
    private readonly string connectionString = DatabaseController.ConnectionString;

    [SetUp]
    public void SetUp()
    {
        using var conn = new NpgsqlConnection(connectionString);
        conn.Open();

        // Вставляем тестового пользователя (users_data)
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
        
        // Вставляем профиль пользователя (user_profiles)
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

        // Вставляем тестовую игру (games)
        using (var cmd = new NpgsqlCommand(@"
            INSERT INTO games (game_id, game_name)
            VALUES (@game_id, @game_name)
            ON CONFLICT (game_id) DO UPDATE SET game_name = EXCLUDED.game_name;", conn))
        {
            cmd.Parameters.AddWithValue("game_id", TestGameId);
            cmd.Parameters.AddWithValue("game_name", TestGameName);
            cmd.ExecuteNonQuery();
        }

        // Связь пользователь-игра (user_to_games)
        using (var cmd = new NpgsqlCommand(@"
            INSERT INTO user_to_games (user_id, game_id)
            VALUES (@user_id, @game_id)
            ON CONFLICT DO NOTHING;", conn))
        {
            cmd.Parameters.AddWithValue("user_id", TestUserId);
            cmd.Parameters.AddWithValue("game_id", TestGameId);
            cmd.ExecuteNonQuery();
        }

        // Вставляем доступность (availabilities)
        using (var cmd = new NpgsqlCommand(@"
            INSERT INTO availabilities (id, day_of_week, start_time, end_time)
            VALUES (@id, @day_of_week, @start_time, @end_time)
            ON CONFLICT (id) DO UPDATE SET day_of_week = EXCLUDED.day_of_week, 
            start_time = EXCLUDED.start_time, end_time = EXCLUDED.end_time;", conn))
        {
            cmd.Parameters.AddWithValue("id", TestAvailabilityId);
            
            cmd.Parameters.Add(new NpgsqlParameter("start_time", NpgsqlDbType.TimeTz)
            {
                Value = testStartTime 
            });
            
            cmd.Parameters.Add(new NpgsqlParameter("end_time", NpgsqlDbType.TimeTz)
            {
                Value = testEndTime 
            });
            
            cmd.Parameters.Add(new NpgsqlParameter
            {
                ParameterName = "day_of_week",
                Value = TestDayOfWeek, // Значение типа Availability.DayOfWeekEnum
                DataTypeName = "day_of_week" // Имя PostgreSQL ENUM
            });
            cmd.ExecuteNonQuery();
        }

        // Связь пользователь-доступность (users_to_availability)
        using (var cmd = new NpgsqlCommand(@"
            INSERT INTO users_to_availability (user_id, availability_id)
            VALUES (@user_id, @availability_id)
            ON CONFLICT DO NOTHING;", conn))
        {
            cmd.Parameters.AddWithValue("user_id", TestUserId);
            cmd.Parameters.AddWithValue("availability_id", TestAvailabilityId);
            cmd.ExecuteNonQuery();
        }
    }

    [TearDown]
    public void TearDown()
    {
        using var conn = new NpgsqlConnection(connectionString);
        conn.Open();

        // Удаляем связи и данные
        using (var cmd = new NpgsqlCommand("DELETE FROM users_to_availability WHERE user_id = @user_id;", conn))
        {
            cmd.Parameters.AddWithValue("user_id", TestUserId);
            cmd.ExecuteNonQuery();
        }

        using (var cmd = new NpgsqlCommand("DELETE FROM user_to_games WHERE user_id = @user_id;", conn))
        {
            cmd.Parameters.AddWithValue("user_id", TestUserId);
            cmd.ExecuteNonQuery();
        }

        using (var cmd = new NpgsqlCommand("DELETE FROM user_profiles WHERE user_id = @user_id;", conn))
        {
            cmd.Parameters.AddWithValue("user_id", TestUserId);
            cmd.ExecuteNonQuery();
        }

        using (var cmd = new NpgsqlCommand("DELETE FROM users_data WHERE id = @id;", conn))
        {
            cmd.Parameters.AddWithValue("id", TestUserId);
            cmd.ExecuteNonQuery();
        }

        using (var cmd = new NpgsqlCommand("DELETE FROM games WHERE game_id = @game_id;", conn))
        {
            cmd.Parameters.AddWithValue("game_id", TestGameId);
            cmd.ExecuteNonQuery();
        }

        using (var cmd = new NpgsqlCommand("DELETE FROM availabilities WHERE id = @id;", conn))
        {
            cmd.Parameters.AddWithValue("id", TestAvailabilityId);
            cmd.ExecuteNonQuery();
        }
    }

    [Test]
    public void Test_GetUserGames_Returns_Correct_Game_List()
    {
        var games = DatabaseController.GetGames(TestUserId, true);

        Assert.IsNotNull(games, "Список игр не должен быть null.");
        Assert.IsNotEmpty(games, "Список игр не должен быть пустым.");

        var game = games.FirstOrDefault(g => g.Id == TestGameId);
        Assert.IsNotNull(game, "Тестовая игра не найдена в списке игр.");
        Assert.AreEqual(TestGameName, game.Name, "Название игры не соответствует ожидаемому.");
    }

    [Test]
    public void Test_GetUserProfile_WithValidUsername_ReturnsCorrectProfile()
    {
        var profile = DatabaseController.GetUserProfile(TestUsername);

        Assert.IsNotNull(profile, "Профиль пользователя не должен быть null.");
        Assert.AreEqual(TestUserId, profile.UserId, "UserId профиля не соответствует ожидаемому значению.");
        Assert.AreEqual(TestDescription, profile.Description, "Описание профиля не соответствует ожидаемому.");
        Assert.AreEqual(TestSkills, profile.Skills, "Навыки профиля не соответствуют ожидаемому.");
    }
    
    [Test]
    public void Test_GetUserProfile_WithInvalidUsername_ThrowsException()
    {
        var nonExistentUsername = "nonexistentuser";

        var ex = Assert.Throws<Exception>(() => DatabaseController.GetUserProfile(nonExistentUsername));
        Assert.That(ex.Message, Does.Contain($"Ошибка GetUserProfile: пользователя с таким username:{nonExistentUsername} не существует"),
            "Ожидаемое сообщение об ошибке не совпадает.");
    }

    [Test]
    public void Test_GetUserAvailabilities_Returns_Correct_Availability_List()
    {
        var availabilities = DatabaseController.GetAvailabilities(TestUserId, true);

        Assert.IsNotNull(availabilities, "Список доступностей не должен быть null.");
        Assert.IsNotEmpty(availabilities, "Список доступностей не должен быть пустым.");

        var availability = availabilities.FirstOrDefault(a => a.Id == TestAvailabilityId);
        Assert.IsNotNull(availability, "Тестовая доступность не найдена в списке.");

        Assert.AreEqual(TestDayOfWeek, availability.DayOfWeek, "Значение дня недели не соответствует ожидаемому.");
        Assert.AreEqual(testStartTime, availability.StartTime, "Время начала не соответствует ожидаемому.");
        Assert.AreEqual(testEndTime, availability.EndTime, "Время окончания не соответствует ожидаемому.");
    }
}