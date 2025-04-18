using GameTeam.Scripts.Controllers;
using Npgsql;
using NUnit.Framework;

namespace GameTeam.Tests.DatabaseControllerTests;

[TestFixture]
public class GetUserDataTests
{
    private const int TestUserId = 11111;
    private const string TestUsername = "testuser_data";
    private const string TestEmail = "testdata@mail.ru";
    private const string TestPassword = "password123";

    private readonly string connectionString = DatabaseController.ConnectionString;

    [SetUp]
    public void SetUp()
    {
        using var conn = new NpgsqlConnection(connectionString);
        conn.Open();

        // Insert or update test user into users_data
        using var cmd = new NpgsqlCommand(@"
            INSERT INTO users_data (id, username, email, password)
            VALUES (@id, @username, @email, @password)
            ON CONFLICT (id) DO UPDATE
              SET username = EXCLUDED.username,
                  email = EXCLUDED.email,
                  password = EXCLUDED.password;", conn);

        cmd.Parameters.AddWithValue("id", TestUserId);
        cmd.Parameters.AddWithValue("username", TestUsername);
        cmd.Parameters.AddWithValue("email", TestEmail);
        cmd.Parameters.AddWithValue("password", TestPassword);
        cmd.ExecuteNonQuery();
    }

    [TearDown]
    public void TearDown()
    {
        using var conn = new NpgsqlConnection(connectionString);
        conn.Open();

        // Remove test user
        using var cmd = new NpgsqlCommand(
            "DELETE FROM users_data WHERE id = @id;", conn);
        cmd.Parameters.AddWithValue("id", TestUserId);
        cmd.ExecuteNonQuery();
    }

    [Test]
    public void Test_GetUserData_WithValidId_ReturnsCorrectUserData()
    {
        // Act
        var userData = DatabaseController.GetUserData(TestUserId);

        // Assert
        Assert.IsNotNull(userData, "UserData should not be null for existing user.");
        Assert.AreEqual(TestUserId, userData.UserId, "UserId does not match expected value.");
        Assert.AreEqual(TestUsername, userData.Username, "Username does not match expected value.");
        Assert.AreEqual(TestEmail, userData.Email, "Email does not match expected value.");
    }

    [Test]
    public void Test_GetUserData_WithInvalidId_ReturnsNull()
    {
        // Arrange
        const int nonExistentId = -99999;

        // Act
        var userData = DatabaseController.GetUserData(nonExistentId);

        // Assert
        Assert.IsNull(userData, "UserData should be null for non-existent user.");
    }
}