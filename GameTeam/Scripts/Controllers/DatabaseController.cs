using Npgsql;
using System;
using System.Collections.Generic;
using GameTeam.Classes.Data;

namespace GameTeam.Scripts.Controllers
{
    public static class DatabaseController
    {
        public static readonly string ConnectionString = 
            "Host=ep-spring-lake-a22loh0r-pooler.eu-central-1.aws.neon.tech;" +
            "Port=5432;" +
            "Database=neondb;" +
            "Username=neondb_owner;" +
            "Password=npg_d1vs2zExTMJO;" +
            "SslMode=Require;";

        public static void Register(string username, string email, string password)
        {
            using var conn = new NpgsqlConnection(ConnectionString);
            try
            {
                conn.Open();
                using var cmd = new NpgsqlCommand();
                cmd.Connection = conn;
                cmd.CommandText = @"
                            INSERT INTO users_data (username, email, password) 
                            VALUES (@username, @email, @password)";
                        
                cmd.Parameters.AddWithValue("username", username);
                cmd.Parameters.AddWithValue("email", email);
                cmd.Parameters.AddWithValue("password", password);
                        
                cmd.ExecuteNonQuery();
            }
            catch (PostgresException ex) when (ex.SqlState == "23505")
            {
                string errorField = ex.ConstraintName.Contains("username") 
                    ? "Имя пользователя" 
                    : "Email";
                throw new Exception($"{errorField} уже занято");
            }
            catch (Exception ex)
            {
                throw new Exception($"Ошибка регистрации: {ex.Message}");
            }
        }

        public static bool Login(string? username, string? email, string password, out string? outUsername)
        {
            outUsername = null;

            using var conn = new NpgsqlConnection(ConnectionString);
            conn.Open();
            using var cmd = new NpgsqlCommand();
            cmd.Connection = conn;
                    
            if (username != null)
            {
                cmd.CommandText = @"
                            SELECT username FROM users_data 
                            WHERE username = @username AND password = @password";
                cmd.Parameters.AddWithValue("username", username);
            }
            else if (email != null)
            {
                cmd.CommandText = @"
                            SELECT username FROM users_data 
                            WHERE email = @email AND password = @password";
                cmd.Parameters.AddWithValue("email", email);
            }
            else
            {
                return false;
            }
                    
            cmd.Parameters.AddWithValue("password", password);

            using var reader = cmd.ExecuteReader();
            
            if (!reader.Read())
                return false;
            
            outUsername = reader.GetString(0);
            return true;
        }

        public static int? GetIdByUsername(string username)
        {
            using var conn = new NpgsqlConnection(ConnectionString);
            try
            {
                conn.Open();
                using var cmd = new NpgsqlCommand();
                cmd.Connection = conn;
                
                cmd.CommandText = @"
                            select id from users_data
                            where username = @username";

                cmd.Parameters.AddWithValue("username", username);
                
                using var reader = cmd.ExecuteReader();
                
                if (!reader.Read())
                    return null;
                
                var id = reader.GetInt32(0);
                return id;
            }
            catch (Exception e)
            {
                throw new Exception($"Ошибка GetIdByUsername: {e}");
            }
        }

        public static List<Game> GetUserGames(int id)
        {
            using var conn = new NpgsqlConnection(ConnectionString);
            try
            {
                conn.Open();
                using var cmd = new NpgsqlCommand();
                var games = new List<Game>();
                cmd.Connection = conn;

                cmd.CommandText = @"
                    select g.game_id, 
                           g.game_name
                    from user_to_games as ug 
                    join user_profiles as up on ug.user_id = up.user_id
                    join games as g on ug.game_id = g.game_id
                    where up.user_id = @id";

                cmd.Parameters.AddWithValue("id", id);
                using var reader = cmd.ExecuteReader();

                while (reader.Read())
                {
                    games.Add(new Game(reader.GetInt32(0), reader.GetString(1)));
                }

                return games;
            }
            catch (Exception e)
            {
                throw new Exception($"Ошибка GetUserGames: {e}");
            }
        }

        public static UserProfile GetUserProfile(string username)
        {
            using var conn = new NpgsqlConnection(ConnectionString);
            var userId = GetIdByUsername(username);

            if (userId is null)
                throw new Exception($"Ошибка GetUserProfile: пользователя с таким username:{username} не существует");
            
            try
            {
                conn.Open();
                using var cmd = new NpgsqlCommand();
                cmd.Connection = conn;

                cmd.CommandText = @"
                    select * from user_profiles
                    where user_id = @id";

                cmd.Parameters.AddWithValue("id", userId);
                using var reader = cmd.ExecuteReader();
                
                if (!reader.Read())
                    return null;

                return new UserProfile(reader.GetInt32(0),
                    reader.GetString(1), reader.GetString(2));
            }
            catch (Exception e)
            {
                throw new Exception($"Ошибка GetUserProfile: {e}");
            }
            
        }
    }
}