using GameTeam.Classes.Data;
using Microsoft.AspNetCore.Mvc;
using NodaTime;
using System.ComponentModel.DataAnnotations;
using System.Text.Json;
using System.Text.Json.Serialization;
using static AuthController;

// For more information on enabling Web API for empty projects, visit https://go.microsoft.com/fwlink/?LinkID=397860

namespace GameTeam.Scripts.Controllers
{
    [Route("[controller]")]
    [ApiController]
    public class DataController : ControllerBase
    {
        // GET: api/<DataController>
        [HttpGet("profile")]
        public string GetSelfProfile()
        {
            var username = HttpContext.Session.GetString("Username");
            if (string.IsNullOrEmpty(username))
            {
                Response.StatusCode = 401;
                return "";
            }

            var profile = DatabaseController.GetUserProfile(username);
            var userId = DatabaseController.GetIdByUsername(username);

            if (profile is null)
            {
                Response.StatusCode = 400;
                return "";            }

            var userData = DatabaseController.GetUserData(int.Parse(HttpContext.Session.GetString("UserId")));

            var profileWithUsername = new UserProfileWithData(profile, userData);

            return JsonSerializer.Serialize(profileWithUsername);
        }

        // GET api/<DataController>/5
        [HttpGet("profile/{user}")]
        public string GetUserProfile(string user)
        {
            try
            {
                var profile = DatabaseController.GetUserProfile(user);

                if (profile is null)
                {
                    Response.StatusCode = 400;
                    return "";
                }

                return JsonSerializer.Serialize(profile);
            }
            catch
            {
                Response.StatusCode = 400;
                return "";
            }
        }

        [HttpPost("upsert")]
        public IActionResult UpsertUserProfile([FromBody] UpsertUserProfileRequest request)
        {
            try
            {
                var username = HttpContext.Session.GetString("Username");
                if (string.IsNullOrEmpty(username))
                    return Unauthorized();

                var userId = DatabaseController.GetIdByUsername(username);

                if (userId is null)
                    return BadRequest();

                List<Game> games;
                List<Availability> availabilities;

                if (!(request.Games is null))
                    games = request.Games.Select(x => DatabaseController.GetOrCreateGame(x)).ToList();
                else
                    games = null;

                if (!(request.Availabilities is null))
                    availabilities = request.Availabilities.Select(x => DatabaseController.GetOrCreateAvailability(x.DayOfWeek, x.StartTime, x.EndTime)).ToList();
                else
                    availabilities = null;

                // Здесь должна быть бизнес-логика обработки запроса
                DatabaseController.UpsertUserProfile(
                userId,
                request.AboutDescription,
                games,
                availabilities);

                return Ok(new { Success = true });
            }
            catch (ValidationException ex)
            {
                return BadRequest(new { Error = ex.Message });
            }
            catch
            {
                return StatusCode(500, new { Error = "Internal server error" });
            }
        }

        [HttpGet("applications/{from}/{to}")]
        public string GetAllApplications(int from, int to)
        {
            var applicationsJson = HttpContext.Session.GetString("applications");

            if (string.IsNullOrEmpty(applicationsJson))
            {
                Response.StatusCode = 400;
                return "";
            }

            var applications = JsonSerializer.Deserialize<Application[]>(applicationsJson);
            return JsonSerializer.Serialize(applications.Skip(from).Take(to - from + 1).ToArray());
        }

        [HttpGet("application/{id}")]
        public string GetAllApplicationById(int id)
        {
            var applicationsJson = HttpContext.Session.GetString("applications");

            if (string.IsNullOrEmpty(applicationsJson))
            {
                Response.StatusCode = 400;
                return "";
            }

            var applications = JsonSerializer.Deserialize<Application[]>(applicationsJson);
            return JsonSerializer.Serialize(applications.Where(x => x.Id == id).FirstOrDefault());
        }

        [HttpPost("application")]
        public IActionResult UpsertApplication([FromBody] ApplicationWithPurpose data)
        {
            if (data.Title == null || data.PurposeName == null)
                return BadRequest(new { Message = "Нет title или purpose" });

            List<Game> games;
            List<Availability> availabilities;

            if (!(data.Games is null))
                games = data.Games.Select(x => DatabaseController.GetOrCreateGame(x)).ToList();
            else
                games = null;

            if (!(data.Availabilities is null))
                availabilities = data.Availabilities.Select(x => DatabaseController.GetOrCreateAvailability(x.DayOfWeek, x.StartTime, x.EndTime)).ToList();
            else
                availabilities = null;

            try
            {
                DatabaseController.UpsertApplication(data.Id, data.PurposeName, data.Title, data.Description, 
                                                     data.Contacts, games, availabilities);
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.ToString());
                return BadRequest(new { Message = "Что-то в бд не так" });
            }

            return Ok(new { Message = "Application upserted" });
        }
    }


    public class UserProfileWithData
    {
        public int UserId { get; }

        public string Username { get; }

        public string Email { get; }

        public string Description { get; }

        public List<Game> Games { get; }

        public List<Availability> Availabilities { get; }

        public UserProfileWithData(int userId, string username, string email, string description)
        {
            UserId = userId;
            Username = username;
            Email = email;
            Description = description;
            Games = DatabaseController.GetGames(userId, true);
            Availabilities = DatabaseController.GetAvailabilities(userId, true);
        }

        public UserProfileWithData(UserProfile profile, UserData data)
        {
            UserId = profile.UserId;
            Username = data.Username;
            Email = data.Email;
            Description = profile.Description;
            Games = profile.Games;
            Availabilities = profile.Availabilities;
        }
    }

    public class UpsertUserProfileRequest
    {


        public string? AboutDescription { get; set; }

        public string? Skills { get; set; }

        public List<string>? Games { get; set; }

        public List<AvailabilityWithoutId>? Availabilities { get; set; }
    }


    public class ApplicationWithPurpose
    {
        public int Id { get; }
        public string Title { get; }
        public string Description { get; }
        public string Contacts { get; }
        public List<AvailabilityWithoutId> Availabilities { get; }
        public List<string> Games { get; }
        public string PurposeName { get; }

        [JsonConstructor]
        public ApplicationWithPurpose(int id, string title, string description, string contacts, string purposeName, List<AvailabilityWithoutId> availabilities, List<string> games)
        {
            Id = id;
            Title = title;
            Description = description;
            Contacts = contacts;
            Availabilities = availabilities;
            Games = games;
            PurposeName = purposeName;
        }
    }

    public class AvailabilityWithoutId
    {

        public Availability.DayOfWeekEnum DayOfWeek { get; }

        public OffsetTime StartTime { get; }

        public OffsetTime EndTime { get; }


        public AvailabilityWithoutId(Availability.DayOfWeekEnum dayOfWeek, OffsetTime startTime, OffsetTime endTime)
        {
            DayOfWeek = dayOfWeek;
            StartTime = startTime;
            EndTime = endTime;
        }
    }
}
