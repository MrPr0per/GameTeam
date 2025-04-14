using GameTeam.Classes.Data;
using Microsoft.AspNetCore.Mvc;
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

            if (profile is null)
            {
                Response.StatusCode = 400;
                return "";
            }

            return JsonSerializer.Serialize(profile);
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

                // Здесь должна быть бизнес-логика обработки запроса
                DatabaseController.UpsertUserProfile(
                userId,
                request.AboutDescription,
                request.Games,
                request.Availabilities);

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
            return JsonSerializer.Serialize(applications.Skip(from).Take(to).ToArray());
        }

        [HttpPost("application")]
        public IActionResult UpserApplication([FromBody] ApplicationWithPurpose data)
        {
            if (data.Title == null || data.PurposeName == null)
                return BadRequest(new { Message = "Нет title или purpose" });
            try
            {
                DatabaseController.UpsertApplication(data.Id, data.PurposeName, data.Title, data.Description, 
                                                     data.Contacts, data.Games, data.Availabilities);
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.ToString());
                return BadRequest(new { Message = "Что-то в бд не так" });
            }

            return Ok(new { Message = "Application upserted" });
        }
    }



    public class UpsertUserProfileRequest
    {


        public string? AboutDescription { get; set; }

        public string? Skills { get; set; }

        public List<Game>? Games { get; set; }

        public List<Availability>? Availabilities { get; set; }
    }


    public class ApplicationWithPurpose
    {
        public int Id { get; }
        public string Title { get; }
        public string Description { get; }
        public string Contacts { get; }
        public List<Availability> Availabilities { get; }
        public List<Game> Games { get; }
        public string PurposeName { get; }

        [JsonConstructor]
        public ApplicationWithPurpose(int id, string title, string description, string contacts, string purposeName, List<Availability> availabilities, List<Game> games)
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
}
