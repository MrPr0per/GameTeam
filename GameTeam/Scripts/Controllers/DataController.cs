using GameTeam.Classes.Data;
using Microsoft.AspNetCore.Mvc;
using Microsoft.IdentityModel.Tokens;
using Microsoft.VisualStudio.TestPlatform.CommunicationUtilities;
using NodaTime;
using System.ComponentModel.DataAnnotations;
using System.Security.Cryptography;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Threading.Tasks.Dataflow;
using static Microsoft.ApplicationInsights.MetricDimensionNames.TelemetryContext;

// For more information on enabling Web API for empty projects, visit https://go.microsoft.com/fwlink/?LinkID=397860

namespace GameTeam.Scripts.Controllers
{
    [Route("[controller]")]
    [ApiController]
    public class DataController : ControllerBase
    {
        [HttpGet("profile")]
        public string GetSelfProfile()
        {
            var username = HttpContext.Session.GetString("Username");
            if (string.IsNullOrEmpty(username))
            {
                Response.StatusCode = 200;
                return "";
            }

            var profile = DatabaseController.GetUserProfile(username);

            if (profile is null)
            {
                Response.StatusCode = 400;
                return "";
            }

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
                var userId = HttpContext.Session.GetString("UserId");
                if (string.IsNullOrEmpty(userId))
                    return Unauthorized();

                List<Game> games;
                List<Availability> availabilities;

                if (!(request.Games is null))
                    games = request.Games.Select(x => DatabaseController.GetOrCreateGame(x)).ToList();
                else
                    games = null;

                if (!(request.Availabilities is null))
                    availabilities = request.Availabilities.Select(x =>
                        DatabaseController.GetOrCreateAvailability(x.DayOfWeek, x.StartTime, x.EndTime)).ToList();
                else
                    availabilities = null;

                DatabaseController.UpsertUserProfile(
                    int.Parse(userId),
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

        [HttpGet("selfapplications")]
        public string GetSelfApplications()
        {
            var userId = HttpContext.Session.GetString("UserId");
            if (string.IsNullOrEmpty(userId))
            {
                Response.StatusCode = 401;
                return "";
            }

            var applications = DatabaseController.GetAllApplicationsByUserId(int.Parse(userId))
                                                 .Select(x => {
                                                     var members = DatabaseController.GetAllApplicationMembers(x.Id);
                                                     return new ApplicationWithMembers(x, members);
                                                 }).ToList();

            return JsonSerializer.Serialize(applications);
        }

        [HttpGet("teamapplications")]
        public string GetTeamApplications()
        {
            var userId = HttpContext.Session.GetString("UserId");
            if (string.IsNullOrEmpty(userId))
            {
                Response.StatusCode = 401;
                return "";
            }

            var applications = DatabaseController.GetAllUserMemberApplications(int.Parse(userId))
                                                 .Select(x => {
                                                   var members = DatabaseController.GetAllApplicationMembers(x.Id);
                                                   return new ApplicationWithMembers(x, members);
                                               }).ToList();

            return JsonSerializer.Serialize(applications);
        }

        
        [HttpPost("applications/{from}/{to}")]
        public string GetAllApplications(int from, int to, [FromBody] FilterData? filters)
        {
            var applicationsJson = HttpContext.Session.GetString("applications");

            if (string.IsNullOrEmpty(applicationsJson) || filters == null)
            {
                var applicationsData = DatabaseController.GetAllApplications();
                HttpContext.Session.SetString("Filters", "");
                HttpContext.Session.SetString("applications", JsonSerializer.Serialize(applicationsData));
                return JsonSerializer.Serialize(applicationsData.Skip(from)
                                                                .Take(to - from + 1)
                                                                .Select(x => {
                                                                  var members = DatabaseController.GetAllApplicationMembers(x.Id);
                                                                  return new ApplicationWithMembersWithoutContacts(x, members);
                                                              }).ToList()
                                                );
            }

            if (filters != null && HttpContext.Session.GetString("Filters") != filters.ToString())
            {
                HttpContext.Session.SetString("Filters", filters.ToString());
                var filterGames = filters.Games.Select(x => DatabaseController.GetOrCreateGame(x)).ToList();
                try
                {
                    var applicationsData = DatabaseController.GetFiltredApplications(filters.PurposeName, filterGames);
                    HttpContext.Session.SetString("applications", JsonSerializer.Serialize(applicationsData));
                    return JsonSerializer.Serialize(applicationsData.Skip(from)
                                                                    .Take(to - from + 1)
                                                                    .Select(x => {
                                                                        var members = DatabaseController.GetAllApplicationMembers(x.Id);
                                                                        return new ApplicationWithMembersWithoutContacts(x, members);
                                                                    }).ToList()
                                                    );
                }
                catch
                {
                    Response.StatusCode = 400;
                    return "Неверные фильтры";
                }
            }

            var applications = JsonSerializer.Deserialize<Application[]>(applicationsJson);
            return JsonSerializer.Serialize(applications.Skip(from)
                                                        .Take(to - from + 1)
                                                        .Select(x => {
                                                            var members = DatabaseController.GetAllApplicationMembers(x.Id);
                                                            return new ApplicationWithMembersWithoutContacts(x, members);
                                                        }).ToList()
                                            );
        }

        [HttpGet("application/{id}")]
        public string GetAllApplicationById(int id)
        {
            var application = DatabaseController.GetApplicationById(id);
            if (application.Count == 0)
            {
                Response.StatusCode = 400;
                return "Нет анкеты с таким номером";
            }
            var members = DatabaseController.GetAllApplicationMembers(id);

            return JsonSerializer.Serialize(new ApplicationWithMembersWithoutContacts(application[0], members));
        }

        [HttpGet("applicationid")]
        public string GetApplicationId()
        {
            return (DatabaseController.GetTotalApplicationsCount() + 1).ToString();
        }

        [HttpPost("application")]
        public IActionResult UpsertApplication([FromBody] ApplicationWithPurpose data)
        {
            var userId = HttpContext.Session.GetString("UserId");

            if (string.IsNullOrEmpty(userId))
                return Unauthorized();

            if (data.Title == null || data.PurposeName == null)
                return BadRequest(new { Message = "Нет title или purpose" });

            List<Game> games;
            List<Availability> availabilities;

            if (!(data.Games is null))
                games = data.Games.Select(x => DatabaseController.GetOrCreateGame(x)).ToList();
            else
                games = null;

            if (!(data.Availabilities is null))
                availabilities = data.Availabilities.Select(x =>
                    DatabaseController.GetOrCreateAvailability(x.DayOfWeek, x.StartTime, x.EndTime)).ToList();
            else
                availabilities = null;

            try
            {
                DatabaseController.UpsertApplication(data.Id, data.PurposeName, data.Title, true, int.Parse(userId),
                    data.Description,
                    data.Contacts, games, availabilities);
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.ToString());
                return BadRequest(new { Message = "Что-то в бд не так" });
            }

            return Ok(new { Message = "Application upserted" });
        }


        [HttpGet("deleteapplication/{id}")]
        public IActionResult DeleteApplicationById(int id)
        {
            var ownerId = DatabaseController.GetUserIdByApplicationId(id);
            var userId = HttpContext.Session.GetString("UserId");

            if (string.IsNullOrEmpty(userId))
                return Unauthorized();

            if (ownerId != int.Parse(userId))
                return Unauthorized(new { Message = "Попытка удалить не свою анкету" });

            var success = DatabaseController.DeleteApplication(id);

            if (success)
                return Ok(new { Message = "Анкета удалена" });

            return BadRequest(new { Message = "Не удалилось" });
        }

        [HttpPost("hide/{id}")]
        public IActionResult Hide(int id)
        {
            try
            {
                var userAppId = DatabaseController.GetUserIdByApplicationId(id);
                var userId = HttpContext.Session.GetString("UserId");
                if (string.IsNullOrEmpty(userId))
                    return Unauthorized();

                if (userAppId != int.Parse(userId))
                    return Unauthorized(new { Message = "Вы не владелец анкеты" });

                DatabaseController.ChangeApplictionVisibilityById(id, true);

                return Ok(new { Message = "Application hidden" });
            }
            catch
            {
                return BadRequest(new { Message = "Что-то в бд не так" });
            }
        }

        [HttpPost("show/{id}")]
        public IActionResult Show(int id)
        {
            try
            {
                var userAppId = DatabaseController.GetUserIdByApplicationId(id);
                var userId = HttpContext.Session.GetString("UserId");

                if (string.IsNullOrEmpty(userId))
                    return Unauthorized();

                if (userAppId != int.Parse(userId))
                    return Unauthorized(new { Message = "Вы не владелец анкеты" });

                DatabaseController.ChangeApplictionVisibilityById(id, false);

                return Ok(new { Message = "Application shown" });
            }
            catch
            {
                return BadRequest(new { Message = "Что-то в бд не так" });
            }
        }

        [HttpGet("games")]
        public string GetAllGames()
        {
            var games = DatabaseController.GetAllGames();

            return JsonSerializer.Serialize(games);
        }
    }

    public class FilterData
    {
        public string? PurposeName { get; set; }
        public List<string>? Games { get; set; }

        public override string ToString()
        {
            return PurposeName + " " + string.Join(' ', Games);
        }
    }


    public class ApplicationWithMembersWithoutContacts
    {
        public int Id { get; set; }

        public string Title { get; set; }

        public string Description { get; set; }

        public List<Availability> Availabilities { get; set; }

        public List<Game> Games { get; set; }

        public int PurposeId { get; set; }

        public int OwnerId { get; set; }

        public bool IsHidden { get; set; }
        public List<UserData> Members { get; set; }

        public ApplicationWithMembersWithoutContacts(Application app, List<UserData> members)
        {
            Id = app.Id;
            Title = app.Title;
            Description = app.Description;
            Availabilities = app.Availabilities;
            Games = app.Games;
            PurposeId = app.PurposeId;
            OwnerId = app.OwnerId;
            IsHidden = app.IsHidden;
            Members = members;
        }
    }

    public class ApplicationWithMembers
    {
        public int Id { get; set; }

        public string Title { get; set; }

        public string Description { get; set; }

        public string Contacts { get; set; }

        public List<Availability> Availabilities { get; set; }

        public List<Game> Games { get; set; }

        public int PurposeId { get; set; }

        public int OwnerId { get; set; }

        public bool IsHidden { get; set; }
        public List<UserData> Members {  get; set; } 

        public ApplicationWithMembers(Application app, List<UserData> members)
        {
            Id = app.Id;
            Title = app.Title;
            Description = app.Description;
            Contacts = app.Contacts;
            Availabilities = app.Availabilities;
            Games = app.Games;
            PurposeId = app.PurposeId;
            OwnerId = app.OwnerId;
            IsHidden = app.IsHidden;
            Members = members;
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

    public class ApplicationWithoutContacts
    {
        public int Id { get; set; }

        public string Title { get; set; }

        public string Description { get; set; }

        public List<Availability> Availabilities { get; set; }

        public List<Game> Games { get; set; }

        public int PurposeId { get; set; }

        public int OwnerId { get; set; }

        public bool IsHidden { get; set; }

        public ApplicationWithoutContacts(int id, string title, string description, int purposeId, int ownerId, bool isHidden)
        {
            Id = id;
            Title = title;
            Description = description;
            Availabilities = DatabaseController.GetAvailabilities(id, false);
            Games = DatabaseController.GetGames(id, false);
            PurposeId = purposeId;
            OwnerId = ownerId;
            IsHidden = isHidden;
        }

        public ApplicationWithoutContacts(Application app)
        {
            Id = app.Id;
            Title = app.Title;
            Description = app.Description;
            Availabilities = app.Availabilities;
            Games = app.Games;
            PurposeId = app.PurposeId;
            OwnerId = app.OwnerId;
            IsHidden = app.IsHidden;
        }
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
        public ApplicationWithPurpose(int id, string title, string description, string contacts, string purposeName,
            List<AvailabilityWithoutId> availabilities, List<string> games)
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