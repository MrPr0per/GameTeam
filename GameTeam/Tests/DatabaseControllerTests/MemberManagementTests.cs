using System;
using System.Collections.Generic;
using System.Linq;
using Npgsql;
using NUnit.Framework;
using GameTeam.Scripts.Controllers;
using GameTeam.Classes.Data;

namespace GameTeam.Tests.DatabaseControllerTests
{
    [TestFixture]
    public class MemberManagementTests
    {
        private const int TestUserId = 80001;
        private const int TestApplicationId = 9002;
        private readonly string connectionString = DatabaseController.ConnectionString;

        [SetUp]
        public void SetUp()
        {
            using var conn = new NpgsqlConnection(connectionString);
            conn.Open();

            // Insert test user
            using (var cmd = new NpgsqlCommand(
                @"INSERT INTO users_data (id, username, email, password, salt)
                  VALUES (@id, 'memberuser', 'memberuser@example.com', 'pwd', 'salt')
                  ON CONFLICT(id) DO NOTHING;", conn))
            {
                cmd.Parameters.AddWithValue("id", TestUserId);
                cmd.ExecuteNonQuery();
            }

            // Insert dummy purpose and application
            using (var cmd = new NpgsqlCommand(
                @"INSERT INTO purposes (id, purpose)
                  VALUES (6001, 'MemberPurpose') ON CONFLICT(id) DO NOTHING;
                  INSERT INTO applications (id, title, description, contacts, purpose_id, owner_id, is_hidden)
                  VALUES (@appId, 'MemberApp', 'Desc', 'Contact', 6001, @owner, false)
                  ON CONFLICT(id) DO NOTHING;", conn))
            {
                cmd.Parameters.AddWithValue("appId", TestApplicationId);
                cmd.Parameters.AddWithValue("owner", TestUserId);
                cmd.ExecuteNonQuery();
            }
        }

        [TearDown]
        public void TearDown()
        {
            using var conn = new NpgsqlConnection(connectionString);
            conn.Open();

            // Remove participant entries
            using (var cmd = new NpgsqlCommand(
                "DELETE FROM participants WHERE application_id = @appId AND user_id = @uid;", conn))
            {
                cmd.Parameters.AddWithValue("appId", TestApplicationId);
                cmd.Parameters.AddWithValue("uid", TestUserId);
                cmd.ExecuteNonQuery();
            }

            // Clean up application
            using (var cmd = new NpgsqlCommand(
                "DELETE FROM applications WHERE id = @appId;", conn))
            {
                cmd.Parameters.AddWithValue("appId", TestApplicationId);
                cmd.ExecuteNonQuery();
            }

            // Clean up purpose
            using (var cmd = new NpgsqlCommand(
                "DELETE FROM purposes WHERE id = 6001;", conn))
            {
                cmd.ExecuteNonQuery();
            }

            // Clean up user
            using (var cmd = new NpgsqlCommand(
                "DELETE FROM users_data WHERE id = @uid;", conn))
            {
                cmd.Parameters.AddWithValue("uid", TestUserId);
                cmd.ExecuteNonQuery();
            }
        }

        [Test]
        public void AddMemberToApplication_InsertsParticipant()
        {
            // Act
            DatabaseController.AddMemberToApplication(TestApplicationId, TestUserId);

            // Assert: Check that participant row exists
            using var conn = new NpgsqlConnection(connectionString);
            conn.Open();
            using var cmd = new NpgsqlCommand(
                "SELECT COUNT(*) FROM participants WHERE application_id = @appId AND user_id = @uid;", conn);
            cmd.Parameters.AddWithValue("appId", TestApplicationId);
            cmd.Parameters.AddWithValue("uid", TestUserId);
            var count = Convert.ToInt32(cmd.ExecuteScalar());

            Assert.AreEqual(1, count, "Participant record should be inserted.");
        }

        [Test]
        public void DeleteMemberFromApplication_RemovesParticipant()
        {
            // Arrange: ensure participant exists
            using (var conn = new NpgsqlConnection(connectionString))
            {
                conn.Open();
                using var cmd = new NpgsqlCommand(
                    "INSERT INTO participants (application_id, user_id) VALUES (@appId, @uid) ON CONFLICT DO NOTHING;", conn);
                cmd.Parameters.AddWithValue("appId", TestApplicationId);
                cmd.Parameters.AddWithValue("uid", TestUserId);
                cmd.ExecuteNonQuery();
            }

            // Act
            DatabaseController.DeleteMemberFromApplication(TestApplicationId, TestUserId);

            // Assert: Participant record should be removed
            using (var conn = new NpgsqlConnection(connectionString))
            {
                conn.Open();
                using var cmd = new NpgsqlCommand(
                    "SELECT COUNT(*) FROM participants WHERE application_id = @appId AND user_id = @uid;", conn);
                cmd.Parameters.AddWithValue("appId", TestApplicationId);
                cmd.Parameters.AddWithValue("uid", TestUserId);
                var count = Convert.ToInt32(cmd.ExecuteScalar());

                Assert.AreEqual(0, count, "Participant record should be removed.");
            }
        }

        [Test]
        public void DeleteMemberFromApplication_NoExistingParticipant_NoError()
        {
            // Act & Assert: Should not throw
            Assert.DoesNotThrow(() =>
                DatabaseController.DeleteMemberFromApplication(TestApplicationId, TestUserId)
            , "Deleting non-existing participant should not throw.");
        }
    }
}
