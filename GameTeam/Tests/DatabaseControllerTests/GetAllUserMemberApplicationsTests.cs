using System;
using System.Collections.Generic;
using System.Linq;
using Npgsql;
using NUnit.Framework;
using GameTeam.Classes.Data;
using GameTeam.Scripts.Controllers;

namespace GameTeam.Tests.DatabaseControllerTests;

[TestFixture]
    public class GetAllUserMemberApplicationsTests
    {
        private const int TestUserId = 70001;
        private const int TestApplicationId1 = 10001;
        private const int TestApplicationId2 = 10002;
        private readonly string connectionString = DatabaseController.ConnectionString;

        [SetUp]
        public void SetUp()
        {
            using var conn = new NpgsqlConnection(connectionString);
            conn.Open();

            // Insert test user
            using (var cmd = new NpgsqlCommand(
                @"INSERT INTO users_data (id, username, email, password, salt)
                  VALUES (@id, 'appuser', 'appuser@example.com', 'pwd', 'salt')
                  ON CONFLICT(id) DO NOTHING;", conn))
            {
                cmd.Parameters.AddWithValue("id", TestUserId);
                cmd.ExecuteNonQuery();
            }

            // Insert dummy purposes and applications
            using (var cmd = new NpgsqlCommand(
                @"INSERT INTO purposes (id, purpose)
                  VALUES (5001, 'Purpose1') ON CONFLICT(id) DO NOTHING;
                  INSERT INTO applications (id, title, description, contacts, purpose_id, owner_id, is_hidden)
                  VALUES (@appId1, 'Title1', 'Desc1', 'Contact1', 5001, @owner, false)
                  ON CONFLICT(id) DO NOTHING;
                  INSERT INTO applications (id, title, description, contacts, purpose_id, owner_id, is_hidden)
                  VALUES (@appId2, 'Title2', 'Desc2', 'Contact2', 5001, @owner, false)
                  ON CONFLICT(id) DO NOTHING;", conn))
            {
                cmd.Parameters.AddWithValue("appId1", TestApplicationId1);
                cmd.Parameters.AddWithValue("appId2", TestApplicationId2);
                cmd.Parameters.AddWithValue("owner", TestUserId);
                cmd.ExecuteNonQuery();
            }

            // Link user as participant to applications
            using (var cmd = new NpgsqlCommand(
                @"INSERT INTO participants (application_id, user_id) VALUES (@appId1, @uid);
                  INSERT INTO participants (application_id, user_id) VALUES (@appId2, @uid);", conn))
            {
                cmd.Parameters.AddWithValue("appId1", TestApplicationId1);
                cmd.Parameters.AddWithValue("appId2", TestApplicationId2);
                cmd.Parameters.AddWithValue("uid", TestUserId);
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
                "DELETE FROM participants WHERE user_id = @uid;", conn))
            {
                cmd.Parameters.AddWithValue("uid", TestUserId);
                cmd.ExecuteNonQuery();
            }
            // Clean up applications
            using (var cmd = new NpgsqlCommand(
                "DELETE FROM applications WHERE id IN (@a1, @a2);", conn))
            {
                cmd.Parameters.AddWithValue("a1", TestApplicationId1);
                cmd.Parameters.AddWithValue("a2", TestApplicationId2);
                cmd.ExecuteNonQuery();
            }
            // Clean up purposes
            using (var cmd = new NpgsqlCommand(
                "DELETE FROM purposes WHERE id = 5001;", conn))
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
        public void When_UserIsMember_ReturnsAllApplications()
        {
            // Act
            List<Application> apps = DatabaseController.GetAllUserMemberApplications(TestUserId);

            // Assert
            Assert.IsNotNull(apps);
            Assert.AreEqual(2, apps.Count, "Expected two applications for user membership.");

            var ids = apps.Select(a => a.Id).OrderBy(id => id).ToList();
            Assert.AreEqual(new List<int> { TestApplicationId1, TestApplicationId2 }, ids);
        }

        [Test]
        public void When_UserNotMember_ReturnsEmptyList()
        {
            // Arrange: remove membership
            using var conn = new NpgsqlConnection(connectionString);
            conn.Open();
            using var cmd = new NpgsqlCommand(
                "DELETE FROM participants WHERE user_id = @uid;", conn);
            cmd.Parameters.AddWithValue("uid", TestUserId);
            cmd.ExecuteNonQuery();

            // Act
            List<Application> apps = DatabaseController.GetAllUserMemberApplications(TestUserId);

            // Assert
            Assert.IsNotNull(apps);
            Assert.IsEmpty(apps, "Expected no applications when user is not a member.");
        }
    }