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
    public class GetAllTeamMembersTests
    {
        private const int TestUserId1 = 60001;
        private const int TestUserId2 = 60002;
        private const int TestApplicationId = 9001;
        private readonly string connectionString = DatabaseController.ConnectionString;

        [SetUp]
        public void SetUp()
        {
            using var conn = new NpgsqlConnection(connectionString);
            conn.Open();

            // Insert test users
            using (var cmd = new NpgsqlCommand(
                @"INSERT INTO users_data (id, username, email, password, salt)
                  VALUES (@id, @username, @email, @password, @salt)
                  ON CONFLICT(id) DO NOTHING;", conn))
            {
                cmd.Parameters.AddWithValue("id", TestUserId1);
                cmd.Parameters.AddWithValue("username", "teamuser1");
                cmd.Parameters.AddWithValue("email", "teamuser1@example.com");
                cmd.Parameters.AddWithValue("password", "pwd1");
                cmd.Parameters.AddWithValue("salt", "salt");
                cmd.ExecuteNonQuery();
                cmd.Parameters.Clear();

                cmd.Parameters.AddWithValue("id", TestUserId2);
                cmd.Parameters.AddWithValue("username", "teamuser2");
                cmd.Parameters.AddWithValue("email", "teamuser2@example.com");
                cmd.Parameters.AddWithValue("password", "pwd2");
                cmd.Parameters.AddWithValue("salt", "salt");
                cmd.ExecuteNonQuery();
            }

            // Insert a dummy application (for foreign key constraint)
            using (var cmd = new NpgsqlCommand(
                @"INSERT INTO applications (id, title, description, contacts, purpose_id, owner_id, is_hidden)
                  VALUES (@appId, 'AppTitle', 'Desc', 'Contact', 1, @owner, false)
                  ON CONFLICT(id) DO NOTHING;", conn))
            {
                cmd.Parameters.AddWithValue("appId", TestApplicationId);
                cmd.Parameters.AddWithValue("owner", TestUserId1);
                cmd.ExecuteNonQuery();
            }

            // Link users to application participants
            using (var cmd = new NpgsqlCommand(
                @"INSERT INTO participants (application_id, user_id)
                  VALUES (@appId, @uid);", conn))
            {
                cmd.Parameters.AddWithValue("appId", TestApplicationId);
                cmd.Parameters.AddWithValue("uid", TestUserId1);
                cmd.ExecuteNonQuery();
                cmd.Parameters.Clear();

                cmd.Parameters.AddWithValue("appId", TestApplicationId);
                cmd.Parameters.AddWithValue("uid", TestUserId2);
                cmd.ExecuteNonQuery();
            }
        }

        [TearDown]
        public void TearDown()
        {
            using var conn = new NpgsqlConnection(connectionString);
            conn.Open();

            // Clean up participants
            using (var cmd = new NpgsqlCommand(
                "DELETE FROM participants WHERE application_id = @appId;", conn))
            {
                cmd.Parameters.AddWithValue("appId", TestApplicationId);
                cmd.ExecuteNonQuery();
            }

            // Clean up application
            using (var cmd = new NpgsqlCommand(
                "DELETE FROM applications WHERE id = @appId;", conn))
            {
                cmd.Parameters.AddWithValue("appId", TestApplicationId);
                cmd.ExecuteNonQuery();
            }

            // Clean up users
            using (var cmd = new NpgsqlCommand(
                "DELETE FROM users_data WHERE id IN (@u1, @u2);", conn))
            {
                cmd.Parameters.AddWithValue("u1", TestUserId1);
                cmd.Parameters.AddWithValue("u2", TestUserId2);
                cmd.ExecuteNonQuery();
            }
        }

        [Test]
        public void When_ParticipantsExist_ReturnsAllTeamMembers()
        {
            // Act
            List<UserData> members = DatabaseController.GetAllTeamMembers(TestApplicationId);

            // Assert
            Assert.IsNotNull(members);
            Assert.AreEqual(2, members.Count, "Expected two team members returned.");

            var ids = members.Select(m => m.UserId).OrderBy(id => id).ToList();
            Assert.AreEqual(new List<int> { TestUserId1, TestUserId2 }, ids);

            var user1 = members.First(m => m.UserId == TestUserId1);
            Assert.AreEqual("teamuser1", user1.Username);
            Assert.AreEqual("teamuser1@example.com", user1.Email);
        }

        [Test]
        public void When_NoParticipants_ReturnsEmptyList()
        {
            // Arrange: remove existing participants
            using var conn = new NpgsqlConnection(connectionString);
            conn.Open();
            using (var cmd = new NpgsqlCommand(
                "DELETE FROM participants WHERE application_id = @appId;", conn))
            {
                cmd.Parameters.AddWithValue("appId", TestApplicationId);
                cmd.ExecuteNonQuery();
            }

            // Act
            List<UserData> members = DatabaseController.GetAllTeamMembers(TestApplicationId);

            // Assert
            Assert.IsNotNull(members);
            Assert.IsEmpty(members, "Expected no team members when none are linked.");
        }
    }
}
