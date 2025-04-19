using System;
using System.Collections.Generic;
using System.Linq;
using Npgsql;
using NUnit.Framework;
using GameTeam.Classes.Data;
using GameTeam.Scripts.Controllers;

namespace GameTeam.Tests.DatabaseControllerTests
{
    [TestFixture]
    public class GetApplicationsByUserIdTests
    {
        private const int TestUserId1 = 55555;
        private const int TestUserId2 = 77777;
        private readonly string connectionString = DatabaseController.ConnectionString;

        private int purposeId;

        [SetUp]
        public void SetUp()
        {
            using var conn = new NpgsqlConnection(connectionString);
            conn.Open();
            // Create test users
            using (var cmd = new NpgsqlCommand(
                "INSERT INTO users_data (id, username, email, password, salt) VALUES (@id, @username, @email, @password, @salt) ON CONFLICT(id) DO NOTHING;",
                conn))
            {
                cmd.Parameters.AddWithValue("id", TestUserId1);
                cmd.Parameters.AddWithValue("username", "user1");
                cmd.Parameters.AddWithValue("email", "user1@example.com");
                cmd.Parameters.AddWithValue("password", "pass1");
                cmd.Parameters.AddWithValue("salt", "salt1");
                cmd.ExecuteNonQuery();
                cmd.Parameters.Clear();
                cmd.Parameters.AddWithValue("id", TestUserId2);
                cmd.Parameters.AddWithValue("username", "user2");
                cmd.Parameters.AddWithValue("email", "user2@example.com");
                cmd.Parameters.AddWithValue("password", "pass2");
                cmd.Parameters.AddWithValue("salt", "salt2");
                cmd.ExecuteNonQuery();
            }
            // Create purpose
            using (var cmd = new NpgsqlCommand(
                "INSERT INTO purposes (purpose) VALUES (@p) ON CONFLICT(purpose) DO NOTHING;",
                conn))
            {
                cmd.Parameters.AddWithValue("p", "TestPurpose");
                cmd.ExecuteNonQuery();
            }
            // Retrieve purpose_id
            using (var cmd = new NpgsqlCommand(
                "SELECT id FROM purposes WHERE purpose = @p;",
                conn))
            {
                cmd.Parameters.AddWithValue("p", "TestPurpose");
                purposeId = Convert.ToInt32(cmd.ExecuteScalar());
            }
        }

        [TearDown]
        public void TearDown()
        {
            using var conn = new NpgsqlConnection(connectionString);
            conn.Open();
            // Clean up applications
            using (var cmd = new NpgsqlCommand(
                "DELETE FROM applications WHERE owner_id IN (@u1, @u2);",
                conn))
            {
                cmd.Parameters.AddWithValue("u1", TestUserId1);
                cmd.Parameters.AddWithValue("u2", TestUserId2);
                cmd.ExecuteNonQuery();
            }
            // Clean up users
            using (var cmd = new NpgsqlCommand(
                "DELETE FROM users_data WHERE id IN (@u1, @u2);",
                conn))
            {
                cmd.Parameters.AddWithValue("u1", TestUserId1);
                cmd.Parameters.AddWithValue("u2", TestUserId2);
                cmd.ExecuteNonQuery();
            }
            // Clean up purposes
            using (var cmd = new NpgsqlCommand(
                "DELETE FROM purposes WHERE id = @pid;",
                conn))
            {
                cmd.Parameters.AddWithValue("pid", purposeId);
                cmd.ExecuteNonQuery();
            }
        }

        [Test]
        public void Test_GetAllApplicationsByUserId_ReturnsOnlyUserApplications()
        {
            using var conn = new NpgsqlConnection(connectionString);
            conn.Open();

            // Insert application for user1
            using (var cmd = new NpgsqlCommand(
                "INSERT INTO applications (id, title, description, contacts, purpose_id, owner_id) " +
                "VALUES (@id, @title, @desc, @cont, @pid, @uid);",
                conn))
            {
                cmd.Parameters.AddWithValue("id", 1001);
                cmd.Parameters.AddWithValue("title", "App1");
                cmd.Parameters.AddWithValue("desc", "Desc1");
                cmd.Parameters.AddWithValue("cont", "Cont1");
                cmd.Parameters.AddWithValue("pid", purposeId);
                cmd.Parameters.AddWithValue("uid", TestUserId1);
                cmd.ExecuteNonQuery();
            }
            // Insert application for user2
            using (var cmd = new NpgsqlCommand(
                "INSERT INTO applications (id, title, description, contacts, purpose_id, owner_id) " +
                "VALUES (@id, @title, @desc, @cont, @pid, @uid);",
                conn))
            {
                cmd.Parameters.AddWithValue("id", 1002);
                cmd.Parameters.AddWithValue("title", "App2");
                cmd.Parameters.AddWithValue("desc", "Desc2");
                cmd.Parameters.AddWithValue("cont", "Cont2");
                cmd.Parameters.AddWithValue("pid", purposeId);
                cmd.Parameters.AddWithValue("uid", TestUserId2);
                cmd.ExecuteNonQuery();
            }

            // Act
            var appsUser1 = DatabaseController.GetAllApplicationsByUserId(TestUserId1);
            var appsUser2 = DatabaseController.GetAllApplicationsByUserId(TestUserId2);

            // Assert
            Assert.IsNotNull(appsUser1);
            Assert.AreEqual(1, appsUser1.Count);
            Assert.AreEqual(1001, appsUser1.First().Id);
            Assert.AreEqual(TestUserId1, appsUser1.First().OwnerId);

            Assert.IsNotNull(appsUser2);
            Assert.AreEqual(1, appsUser2.Count);
            Assert.AreEqual(1002, appsUser2.First().Id);
            Assert.AreEqual(TestUserId2, appsUser2.First().OwnerId);
        }
    }
}
