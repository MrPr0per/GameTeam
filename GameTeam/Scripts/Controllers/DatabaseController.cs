using Npgsql;
using System;
using System.Collections.Generic;

namespace GameTeam.Scripts.Controllers
{
    public static class DatabaseController
    {
        private static readonly string ConnectionString = 
            "Host=ep-spring-lake-a22loh0r-pooler.eu-central-1.aws.neon.tech;" +
            "Port=5432;" +
            "Database=neondb;" +
            "Username=neondb_owner;" +
            "Password=npg_d1vs2zExTMJO;" +
            "SslMode=Require;";

        public static void Register(string username, string email, string password)
        {
            using (var conn = new NpgsqlConnection(ConnectionString))
            {
                try
                {
                    conn.Open();
                    using (var cmd = new NpgsqlCommand())
                    {
                        cmd.Connection = conn;
                        cmd.CommandText = @"
                            INSERT INTO users_data (username, email, password) 
                            VALUES (@username, @email, @password)";
                        
                        cmd.Parameters.AddWithValue("username", username);
                        cmd.Parameters.AddWithValue("email", email);
                        cmd.Parameters.AddWithValue("password", password);
                        
                        cmd.ExecuteNonQuery();
                    }
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
        }

        public static bool Login(string? username, string? email, string password, out string? outUsername)
        {
            outUsername = null;
            
            using (var conn = new NpgsqlConnection(ConnectionString))
            {
                conn.Open();
                using (var cmd = new NpgsqlCommand())
                {
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
                    
                    using (var reader = cmd.ExecuteReader())
                    {
                        if (reader.Read())
                        {
                            outUsername = reader.GetString(0);
                            return true;
                        }
                    }
                }
            }
            return false;
        }
    }
}