using System;
using Npgsql;
using NUnit.Framework;
using GameTeam.Scripts.Controllers;

namespace GameTeam.Tests.DatabaseControllerTests
{
    [TestFixture]
    public class ChangeApplicationVisibilityTests
    {
        private const int TestUserId1 = 55555;
        private const int TestUserId2 = 77777;
        private const int TestAppId1  = 2001;
        private const int TestAppId2  = 2002;
        private readonly string connectionString = DatabaseController.ConnectionString;
        private int purposeId;

        [SetUp]
        public void SetUp()
        {
            using var conn = new NpgsqlConnection(connectionString);
            conn.Open();

            // Create test users
            using (var cmd = new NpgsqlCommand(
                @"INSERT INTO users_data (id, username, email, password, salt)
                  VALUES (@id, @username, @email, @password, @salt)
                  ON CONFLICT(id) DO NOTHING;",
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
                @"INSERT INTO purposes (purpose)
                  VALUES (@p)
                  ON CONFLICT(purpose) DO NOTHING;",
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
        public void ChangeVisibility_ShouldSetHiddenTrue_WhenInitiallyFalse()
        {
            using var conn = new NpgsqlConnection(connectionString);
            conn.Open();

            // Insert application initially visible
            using (var cmd = new NpgsqlCommand(
                @"INSERT INTO applications
                    (id, title, description, contacts, purpose_id, owner_id, is_hidden)
                  VALUES
                    (@id, @title, @desc, @cont, @pid, @uid, @hidden);",
                conn))
            {
                cmd.Parameters.AddWithValue("id", TestAppId1);
                cmd.Parameters.AddWithValue("title", "AppVisible");
                cmd.Parameters.AddWithValue("desc", "Desc");
                cmd.Parameters.AddWithValue("cont", "Cont");
                cmd.Parameters.AddWithValue("pid", purposeId);
                cmd.Parameters.AddWithValue("uid", TestUserId1);
                cmd.Parameters.AddWithValue("hidden", false);
                cmd.ExecuteNonQuery();
            }

            // Act
            DatabaseController.ChangeApplictionVisibilityById(TestAppId1, isHidden: true);

            // Assert
            using (var cmd = new NpgsqlCommand(
                "SELECT is_hidden FROM applications WHERE id = @id;",
                conn))
            {
                cmd.Parameters.AddWithValue("id", TestAppId1);
                bool isHidden = (bool)cmd.ExecuteScalar();
                Assert.IsTrue(isHidden, "Application should be hidden after ChangeApplictionVisibilityById");
            }
        }

        [Test]
        public void ChangeVisibility_ShouldSetHiddenFalse_WhenInitiallyTrue()
        {
            using var conn = new NpgsqlConnection(connectionString);
            conn.Open();

            // Insert application initially hidden
            using (var cmd = new NpgsqlCommand(
                @"INSERT INTO applications
                    (id, title, description, contacts, purpose_id, owner_id, is_hidden)
                  VALUES
                    (@id, @title, @desc, @cont, @pid, @uid, @hidden);",
                conn))
            {
                cmd.Parameters.AddWithValue("id", TestAppId2);
                cmd.Parameters.AddWithValue("title", "AppHidden");
                cmd.Parameters.AddWithValue("desc", "Desc");
                cmd.Parameters.AddWithValue("cont", "Cont");
                cmd.Parameters.AddWithValue("pid", purposeId);
                cmd.Parameters.AddWithValue("uid", TestUserId1);
                cmd.Parameters.AddWithValue("hidden", true);
                cmd.ExecuteNonQuery();
            }

            // Act
            DatabaseController.ChangeApplictionVisibilityById(TestAppId2, isHidden: false);

            // Assert
            using (var cmd = new NpgsqlCommand(
                "SELECT is_hidden FROM applications WHERE id = @id;",
                conn))
            {
                cmd.Parameters.AddWithValue("id", TestAppId2);
                bool isHidden = (bool)cmd.ExecuteScalar();
                Assert.IsFalse(isHidden, "Application should be visible after ChangeApplictionVisibilityById");
            }
        }

        [Test]
        public void ChangeVisibility_InvalidId_ShouldNotThrow()
        {
            // Act & Assert
            Assert.DoesNotThrow(() =>
                DatabaseController.ChangeApplictionVisibilityById(-1, isHidden: true)
            );
        }
    }
}
