using GameTeam.Classes.Data;
using Microsoft.AspNetCore.Mvc;
using System.ComponentModel.DataAnnotations;
using System.Text.Json;
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
        public string Get()
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
        public string Get(string user)
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
    }



    public class UpsertUserProfileRequest
    {


        public string? AboutDescription { get; set; }

        public string? Skills { get; set; }

        public List<Game>? Games { get; set; }

        public List<Availability>? Availabilities { get; set; }
    }
}
