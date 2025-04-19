using GameTeam.Scripts.Controllers;
using Npgsql;
using NUnit.Framework;

namespace GameTeam.Tests.DatabaseControllerTests;

[TestFixture]
public class GetIdByUsernameTest
{
	private const string TestUsername = "testuser";
	private const int TestUserId = 12345;
	private const string TestEmail = "biba123123@mail.ru";
	private const string TestPassword = "123123123";
	private const string TestSalt = "salt";

	// Метод, который вызывается перед каждым тестом для подготовки данных
	[SetUp]
	public void SetUp()
	{
		using var conn = new NpgsqlConnection(DatabaseController.ConnectionString);
		conn.Open();

		// Вставляем тестовую запись. Если таблица уже содержит данные с таким id/username, можно использовать транзакцию или уникальные значения
		using var cmd = new NpgsqlCommand(@"
                INSERT INTO users_data (id, username, email, password, salt)
                VALUES (@id, @username, @email, @password, @salt)
                ON CONFLICT (username) DO UPDATE SET id = EXCLUDED.id;", conn);
		
		cmd.Parameters.AddWithValue("id", TestUserId);
		cmd.Parameters.AddWithValue("username", TestUsername);
		cmd.Parameters.AddWithValue("email", TestEmail);
		cmd.Parameters.AddWithValue("password", TestPassword);
		cmd.Parameters.AddWithValue("salt", TestSalt);
		cmd.ExecuteNonQuery();
	}

	// Метод, который вызывается после каждого теста для очистки данных
	[TearDown]
	public void TearDown()
	{
		using var conn = new NpgsqlConnection(DatabaseController.ConnectionString);
		conn.Open();
		using var cmd = new NpgsqlCommand("DELETE FROM users_data WHERE username = @username", conn);
		cmd.Parameters.AddWithValue("username", TestUsername);
		cmd.ExecuteNonQuery();
	}

	// Собственно тест
	[Test]
	public void GetIdByUsername_ValidUsername_ReturnsCorrectId()
	{
		var returnedId = DatabaseController.GetIdByUsername(TestUsername);
		
		Assert.AreEqual(TestUserId, returnedId, "Метод GetIdByUsername не вернул ожидаемый id для заданного username.");
	}
}