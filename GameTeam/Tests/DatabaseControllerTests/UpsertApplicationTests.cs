using System;
using System.Collections.Generic;
using System.Linq;
using NodaTime;
using Npgsql;
using NpgsqlTypes;
using NUnit.Framework;
using GameTeam.Classes.Data;
using GameTeam.Scripts.Controllers;

namespace GameTeam.Tests.DatabaseControllerTests
{
    [TestFixture]
    public class UpsertApplicationTests
    {
        private const int TestAppId = 22222;
        private const string TestPurpose = "SomePurpose";
        private const string UpdatedPurpose = "UpdatedPurpose";
        private const string TestAppTitle = "Initial App Title";
        private const string TestAppDescription = "Initial App Description";
        private const string TestAppContacts = "Initial Contacts";
        private const int TestAppOwnerId = 55555;

        private const string UpdatedAppTitle = "Updated App Title";
        private const string UpdatedAppDescription = "Updated App Description";
        private const string UpdatedAppContacts = "Updated Contacts";

        private const int TestGameId = 888;
        private const string TestGameName = "Test Application Game";

        private const int TestAvailabilityId = 98765;
        private const Availability.DayOfWeekEnum TestDayOfWeek = Availability.DayOfWeekEnum.Friday;
        private readonly OffsetTime TestStartTime = new OffsetTime(new LocalTime(14, 0), Offset.FromHours(0));
        private readonly OffsetTime TestEndTime = new OffsetTime(new LocalTime(16, 0), Offset.FromHours(0));

        private const int TestGameId2 = 333;
        private const string TestGameName2 = "New Application Game";
        private const int TestAvailabilityId2 = 44444;
        private readonly OffsetTime TestStartTime2 = new OffsetTime(new LocalTime(18, 0), Offset.FromHours(0));
        private readonly OffsetTime TestEndTime2 = new OffsetTime(new LocalTime(20, 0), Offset.FromHours(0));

        private readonly string connectionString = DatabaseController.ConnectionString;

        [SetUp]
        public void SetUp()
        {
            using var conn = new NpgsqlConnection(connectionString);
            conn.Open();
            // Create test user for owner
            using var cmdUser = new NpgsqlCommand(
                "INSERT INTO users_data (id, username, email, password, salt) VALUES (@id, @username, @email, @password, @salt) ON CONFLICT (id) DO NOTHING;",
                conn);
            cmdUser.Parameters.AddWithValue("id", TestAppOwnerId);
            cmdUser.Parameters.AddWithValue("username", "owner_user");
            cmdUser.Parameters.AddWithValue("email", "owner@example.com");
            cmdUser.Parameters.AddWithValue("password", "pass");
            cmdUser.Parameters.AddWithValue("salt", "salt");
            cmdUser.ExecuteNonQuery();

            // Create purposes
            using var cmdP = new NpgsqlCommand(
                "INSERT INTO purposes (id, purpose) VALUES (DEFAULT, @purpose) ON CONFLICT (purpose) DO NOTHING;",
                conn);
            cmdP.Parameters.AddWithValue("purpose", TestPurpose);
            cmdP.ExecuteNonQuery();
            cmdP.Parameters.Clear();
            cmdP.Parameters.AddWithValue("purpose", UpdatedPurpose);
            cmdP.ExecuteNonQuery();
        }

        [TearDown]
        public void TearDown()
        {
            using var conn = new NpgsqlConnection(connectionString);
            conn.Open();
            // Clean application and relations
            using var cmd = new NpgsqlCommand(
                "DELETE FROM applications_to_games WHERE app_id = @id; " +
                "DELETE FROM applications_to_availability WHERE application_id = @id; " +
                "DELETE FROM applications WHERE id = @id;", conn);
            cmd.Parameters.AddWithValue("id", TestAppId);
            cmd.ExecuteNonQuery();

            // Clean games and availabilities
            using var cmdGA = new NpgsqlCommand(
                "DELETE FROM games WHERE game_id IN (@g1, @g2); " +
                "DELETE FROM availabilities WHERE id IN (@a1, @a2);", conn);
            cmdGA.Parameters.AddWithValue("g1", TestGameId);
            cmdGA.Parameters.AddWithValue("g2", TestGameId2);
            cmdGA.Parameters.AddWithValue("a1", TestAvailabilityId);
            cmdGA.Parameters.AddWithValue("a2", TestAvailabilityId2);
            cmdGA.ExecuteNonQuery();

            // Clean purposes
            using var cmdP = new NpgsqlCommand(
                "DELETE FROM purposes WHERE purpose IN (@p1, @p2);", conn);
            cmdP.Parameters.AddWithValue("p1", TestPurpose);
            cmdP.Parameters.AddWithValue("p2", UpdatedPurpose);
            cmdP.ExecuteNonQuery();

            // Clean user
            using var cmdUser = new NpgsqlCommand(
                "DELETE FROM users_data WHERE id = @id;", conn);
            cmdUser.Parameters.AddWithValue("id", TestAppOwnerId);
            cmdUser.ExecuteNonQuery();
        }

        [Test]
        public void Test_UpsertApplication_BasicFields()
        {
            DatabaseController.UpsertApplication(TestAppId, TestPurpose, TestAppTitle, false,  TestAppOwnerId, TestAppDescription, TestAppContacts, null, null);

            using var conn = new NpgsqlConnection(connectionString);
            conn.Open();

            string title = null, description = null, contacts = null;
            int purposeId = 0, ownerId = 0;
            using (var cmd = new NpgsqlCommand(
                "SELECT title, description, contacts, purpose_id, owner_id FROM applications WHERE id = @id;", conn))
            {
                cmd.Parameters.AddWithValue("id", TestAppId);
                using (var reader = cmd.ExecuteReader())
                {
                    Assert.IsTrue(reader.Read(), "Application record not found.");
                    title = reader.IsDBNull(0) ? null : reader.GetString(0);
                    description = reader.IsDBNull(1) ? null : reader.GetString(1);
                    contacts = reader.IsDBNull(2) ? null : reader.GetString(2);
                    purposeId = reader.GetInt32(3);
                    ownerId = reader.GetInt32(4);
                }
            }
            Assert.AreEqual(TestAppTitle, title, "Field title incorrect.");
            Assert.AreEqual(TestAppDescription, description, "Field description incorrect.");
            Assert.AreEqual(TestAppContacts, contacts, "Field contacts incorrect.");
            Assert.AreEqual(TestAppOwnerId, ownerId, "Field owner_id incorrect.");

            using var cmdPurpose = new NpgsqlCommand(
                "SELECT purpose FROM purposes WHERE id = @pid;", conn);
            cmdPurpose.Parameters.AddWithValue("pid", purposeId);
            var purpose = cmdPurpose.ExecuteScalar() as string;
            Assert.AreEqual(TestPurpose, purpose, "Field purpose_id incorrect.");
        }

        [Test]
        public void Test_UpsertApplication_WithGames()
        {
            var games = new List<Game> { new Game(TestGameId, TestGameName) };
            DatabaseController.UpsertApplication(TestAppId, TestPurpose, TestAppTitle, false, TestAppOwnerId, TestAppDescription, TestAppContacts, games, null);

            using var conn = new NpgsqlConnection(connectionString);
            conn.Open();
            using var cmd = new NpgsqlCommand("SELECT COUNT(*) FROM games WHERE game_id = @gameId;", conn);
            cmd.Parameters.AddWithValue("gameId", TestGameId);
            Assert.Greater((long)cmd.ExecuteScalar(), 0, "Game not found in games table.");

            using var cmdJoin = new NpgsqlCommand(
                "SELECT owner_id FROM applications WHERE id = @appId", conn);
            cmdJoin.Parameters.AddWithValue("appId", TestAppId);
            Assert.AreEqual(TestAppOwnerId, (int)cmdJoin.ExecuteScalar(), "owner_id not set for application games.");
        }

        [Test]
        public void Test_UpsertApplication_WithAvailabilities()
        {
            var avs = new List<Availability> { new Availability(TestAvailabilityId, TestDayOfWeek, TestStartTime, TestEndTime) };
            DatabaseController.UpsertApplication(TestAppId, TestPurpose, TestAppTitle, false, TestAppOwnerId, TestAppDescription, TestAppContacts, null, avs);

            using var conn = new NpgsqlConnection(connectionString);
            conn.Open();
            using var cmd = new NpgsqlCommand("SELECT COUNT(*) FROM availabilities WHERE id = @aid;", conn);
            cmd.Parameters.AddWithValue("aid", TestAvailabilityId);
            Assert.Greater((long)cmd.ExecuteScalar(), 0, "Availability not found.");

            using var cmdJoin = new NpgsqlCommand(
                "SELECT owner_id FROM applications WHERE id = @appId", conn);
            cmdJoin.Parameters.AddWithValue("appId", TestAppId);
            Assert.AreEqual(TestAppOwnerId, (int)cmdJoin.ExecuteScalar(), "owner_id not set for application availabilities.");
        }

        [Test]
        public void Test_UpsertApplication_UpdateProfileFields()
        {
            DatabaseController.UpsertApplication(TestAppId, TestPurpose, TestAppTitle, false, TestAppOwnerId, TestAppDescription, TestAppContacts, null, null);
            DatabaseController.UpsertApplication(TestAppId, UpdatedPurpose, UpdatedAppTitle, false, TestAppOwnerId, UpdatedAppDescription, UpdatedAppContacts, null, null);

            using var conn = new NpgsqlConnection(connectionString);
            conn.Open();
            string title = null, description = null, contacts = null;
            int purposeId = 0, ownerId = 0;
            using (var cmd = new NpgsqlCommand(
                "SELECT title, description, contacts, purpose_id, owner_id FROM applications WHERE id = @id;", conn))
            {
                cmd.Parameters.AddWithValue("id", TestAppId);
                using (var reader = cmd.ExecuteReader())
                {
                    Assert.IsTrue(reader.Read(), "Application not found after update.");
                    title = reader.GetString(0);
                    description = reader.GetString(1);
                    contacts = reader.GetString(2);
                    purposeId = reader.GetInt32(3);
                    ownerId = reader.GetInt32(4);
                }
            }
            Assert.AreEqual(UpdatedAppTitle, title, "title not updated.");
            Assert.AreEqual(UpdatedAppDescription, description, "description not updated.");
            Assert.AreEqual(UpdatedAppContacts, contacts, "contacts not updated.");
            Assert.AreEqual(TestAppOwnerId, ownerId, "owner_id should persist.");

            using var cmdPurpose = new NpgsqlCommand(
                "SELECT purpose FROM purposes WHERE id = @pid;", conn);
            cmdPurpose.Parameters.AddWithValue("pid", purposeId);
            var purpose = cmdPurpose.ExecuteScalar() as string;
            Assert.AreEqual(UpdatedPurpose, purpose, "purpose_id not updated.");
        }

        [Test]
        public void Test_UpsertApplication_ReplacesOldRecords()
        {
            var initialGames = new List<Game> { new Game(TestGameId, TestGameName) };
            var initialAvs = new List<Availability> { new Availability(TestAvailabilityId, TestDayOfWeek, TestStartTime, TestEndTime) };
            DatabaseController.UpsertApplication(TestAppId, TestPurpose, TestAppTitle, false, TestAppOwnerId, TestAppDescription, TestAppContacts, initialGames, initialAvs);

            var newGames = new List<Game> { new Game(TestGameId2, TestGameName2) };
            var newAvs = new List<Availability> { new Availability(TestAvailabilityId2, Availability.DayOfWeekEnum.Monday, TestStartTime2, TestEndTime2) };
            DatabaseController.UpsertApplication(TestAppId, TestPurpose, TestAppTitle, false, TestAppOwnerId, TestAppDescription, TestAppContacts, newGames, newAvs);

            using var conn = new NpgsqlConnection(connectionString);
            conn.Open();
            using var cmdOldGame = new NpgsqlCommand(
                "SELECT COUNT(*) FROM applications_to_games WHERE app_id = @id AND game_id = @old;", conn);
            cmdOldGame.Parameters.AddWithValue("id", TestAppId);
            cmdOldGame.Parameters.AddWithValue("old", TestGameId);
            Assert.AreEqual(0L, (long)cmdOldGame.ExecuteScalar(), "Old game link should be removed.");

            using var cmdNewGame = new NpgsqlCommand(
                "SELECT COUNT(*) FROM applications_to_games WHERE app_id = @id AND game_id = @new;", conn);
            cmdNewGame.Parameters.AddWithValue("id", TestAppId);
            cmdNewGame.Parameters.AddWithValue("new", TestGameId2);
            Assert.Greater((long)cmdNewGame.ExecuteScalar(), 0, "New game link should exist.");

            using var cmdOldAv = new NpgsqlCommand(
                "SELECT COUNT(*) FROM applications_to_availability WHERE application_id = @id AND availability_id = @old;", conn);
            cmdOldAv.Parameters.AddWithValue("id", TestAppId);
            cmdOldAv.Parameters.AddWithValue("old", TestAvailabilityId);
            Assert.AreEqual(0L, (long)cmdOldAv.ExecuteScalar(), "Old availability should be removed.");

            using var cmdNewAv = new NpgsqlCommand(
                "SELECT COUNT(*) FROM applications_to_availability WHERE application_id = @id AND availability_id = @new;", conn);
            cmdNewAv.Parameters.AddWithValue("id", TestAppId);
            cmdNewAv.Parameters.AddWithValue("new", TestAvailabilityId2);
            Assert.Greater((long)cmdNewAv.ExecuteScalar(), 0, "New availability should exist.");
        }
    }
}