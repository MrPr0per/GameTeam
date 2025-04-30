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
    public class GetFilteredApplicationsTests
    {
        private readonly string connectionString = DatabaseController.ConnectionString;

        private const string Purpose1 = "FilterPurpose1";
        private const string Purpose2 = "FilterPurpose2";
        private int purpose1Id;
        private int purpose2Id;

        private const int Game1Id = 101;
        private const int Game2Id = 102;
        private const int Game3Id = 103;

        private const int App1Id = 5001; // purpose1, game1, visible
        private const int App2Id = 5002; // purpose2, game1+game2, visible
        private const int App3Id = 5003; // purpose1, game2, hidden
        private const int App4Id = 5004; // purpose2, no games, visible

        private const int TestUserId = 99999;

        [SetUp]
        public void SetUp()
        {
            using var conn = new NpgsqlConnection(connectionString);
            conn.Open();

            // Insert test user for FK
            using (var cmd = new NpgsqlCommand(
                @"INSERT INTO users_data (id, username, email, password, salt) VALUES (@id, @u, @e, @p, @salt) ON CONFLICT (id) DO NOTHING;", conn))
            {
                cmd.Parameters.AddWithValue("id", TestUserId);
                cmd.Parameters.AddWithValue("u", "filter_user");
                cmd.Parameters.AddWithValue("e", "filter_user@example.com");
                cmd.Parameters.AddWithValue("p", "pass");
                cmd.Parameters.AddWithValue("salt", "salt");
                cmd.ExecuteNonQuery();
            }

            // Insert purposes
            using (var cmd = new NpgsqlCommand(
                @"INSERT INTO purposes (purpose) VALUES (@p1), (@p2) ON CONFLICT(purpose) DO NOTHING;", conn))
            {
                cmd.Parameters.AddWithValue("p1", Purpose1);
                cmd.Parameters.AddWithValue("p2", Purpose2);
                cmd.ExecuteNonQuery();
            }
            // Retrieve purpose IDs
            using (var cmd = new NpgsqlCommand(
                @"SELECT id FROM purposes WHERE purpose = @p;", conn))
            {
                cmd.Parameters.AddWithValue("p", Purpose1);
                purpose1Id = Convert.ToInt32(cmd.ExecuteScalar());
                cmd.Parameters.Clear();
                cmd.Parameters.AddWithValue("p", Purpose2);
                purpose2Id = Convert.ToInt32(cmd.ExecuteScalar());
            }

            // Insert games
            using (var cmd = new NpgsqlCommand(
                @"INSERT INTO games (game_id, game_name) VALUES (@g1, 'Game1'), (@g2, 'Game2'), (@g3, 'Game3')
                  ON CONFLICT (game_id) DO UPDATE SET game_name = EXCLUDED.game_name;", conn))
            {
                cmd.Parameters.AddWithValue("g1", Game1Id);
                cmd.Parameters.AddWithValue("g2", Game2Id);
                cmd.Parameters.AddWithValue("g3", Game3Id);
                cmd.ExecuteNonQuery();
            }

            // Insert applications
            using (var cmd = new NpgsqlCommand(@"
                INSERT INTO applications (id, title, description, contacts, purpose_id, owner_id, is_hidden)
                VALUES
                  (@a1, 'App1', 'Desc1', 'Cont1', @p1, @u, false),
                  (@a2, 'App2', 'Desc2', 'Cont2', @p2, @u, false),
                  (@a3, 'App3', 'Desc3', 'Cont3', @p1, @u, true),
                  (@a4, 'App4', 'Desc4', 'Cont4', @p2, @u, false)
                ON CONFLICT (id) DO UPDATE SET is_hidden = EXCLUDED.is_hidden;
            ", conn))
            {
                cmd.Parameters.AddWithValue("a1", App1Id);
                cmd.Parameters.AddWithValue("a2", App2Id);
                cmd.Parameters.AddWithValue("a3", App3Id);
                cmd.Parameters.AddWithValue("a4", App4Id);
                cmd.Parameters.AddWithValue("p1", purpose1Id);
                cmd.Parameters.AddWithValue("p2", purpose2Id);
                cmd.Parameters.AddWithValue("u", TestUserId);
                cmd.ExecuteNonQuery();
            }

            // Link applications to games
            using (var cmd = new NpgsqlCommand(@"
                INSERT INTO applications_to_games (app_id, game_id) VALUES
                  (@a1, @g1),
                  (@a2, @g1),
                  (@a2, @g2),
                  (@a3, @g2)
                ON CONFLICT DO NOTHING;", conn))
            {
                cmd.Parameters.AddWithValue("a1", App1Id);
                cmd.Parameters.AddWithValue("a2", App2Id);
                cmd.Parameters.AddWithValue("a3", App3Id);
                cmd.Parameters.AddWithValue("g1", Game1Id);
                cmd.Parameters.AddWithValue("g2", Game2Id);
                cmd.ExecuteNonQuery();
            }
        }

        [TearDown]
        public void TearDown()
        {
            using var conn = new NpgsqlConnection(connectionString);
            conn.Open();

            // Cleanup relationships and apps
            using (var cmd = new NpgsqlCommand(@"
                DELETE FROM applications_to_games WHERE app_id IN (@a1,@a2,@a3);
                DELETE FROM applications WHERE id IN (@a1,@a2,@a3,@a4);
                DELETE FROM games WHERE game_id IN (@g1,@g2,@g3);
                DELETE FROM purposes WHERE id IN (@p1,@p2);
                DELETE FROM users_data WHERE id = @u;", conn))
            {
                cmd.Parameters.AddWithValue("a1", App1Id);
                cmd.Parameters.AddWithValue("a2", App2Id);
                cmd.Parameters.AddWithValue("a3", App3Id);
                cmd.Parameters.AddWithValue("a4", App4Id);
                cmd.Parameters.AddWithValue("g1", Game1Id);
                cmd.Parameters.AddWithValue("g2", Game2Id);
                cmd.Parameters.AddWithValue("g3", Game3Id);
                cmd.Parameters.AddWithValue("p1", purpose1Id);
                cmd.Parameters.AddWithValue("p2", purpose2Id);
                cmd.Parameters.AddWithValue("u", TestUserId);
                cmd.ExecuteNonQuery();
            }
        }

        [Test]
        public void FilterByPurpose_ReturnsOnlyMatchingApplications()
        {
            var list = DatabaseController.GetFiltredApplications(Purpose1, null);
            Assert.IsNotNull(list);
            Assert.AreEqual(1, list.Count);
            Assert.AreEqual(App1Id, list[0].Id);
        }

        [Test]
        public void FilterByGames_ReturnsOnlyMatchingApplications()
        {
            var games = new List<Game> { new Game(Game1Id, "Game1") };
            var list = DatabaseController.GetFiltredApplications(null, games);
            Assert.IsNotNull(list);
            // App1 and App2 should match, App3 is hidden
            var ids = list.Select(a => a.Id).OrderBy(x => x).ToArray();
            CollectionAssert.AreEquivalent(new[] { App1Id, App2Id }, ids);
        }

        [Test]
        public void FilterByPurposeAndGames_ReturnsOnlyMatchingApplications()
        {
            var games = new List<Game> { new Game(Game2Id, "Game2") };
            var list = DatabaseController.GetFiltredApplications(Purpose2, games);
            Assert.IsNotNull(list);
            Assert.AreEqual(1, list.Count);
            Assert.AreEqual(App2Id, list[0].Id);
        }
    }
}
