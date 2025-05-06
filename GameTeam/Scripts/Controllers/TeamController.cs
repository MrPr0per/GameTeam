using Microsoft.AspNetCore.Mvc;
using System.Text.Json;

// For more information on enabling Web API for empty projects, visit https://go.microsoft.com/fwlink/?LinkID=397860

namespace GameTeam.Scripts.Controllers
{
    [Route("[controller]")]
    [ApiController]
    public class TeamController : ControllerBase
    {
        // GET: api/<TeamController>
        [HttpPost("join/{id}/")]
        public IActionResult Join(int id)
        {
            var userId = HttpContext.Session.GetString("UserId");

            if (string.IsNullOrEmpty(userId))
                return Unauthorized(new { Message = "Войдите в аккаунт" });

            var ownerId = DatabaseController.GetUserIdByApplicationId(id);
            if (ownerId is null)
                return BadRequest(new { Message = "Нет анкеты или владельца" });

            TeamManager.JoinTeam(ownerId.Value, int.Parse(userId), id);

            return Ok(new { Message = "Вы успешно подали заявку на вступление" });
        }

        [HttpPost("approve/{userId}/{applicationId}")]
        public IActionResult Approve(int userId, int applicationId)
        {
            var selfId = HttpContext.Session.GetString("UserId");

            if (string.IsNullOrEmpty(selfId))
                return Unauthorized(new { Message = "Войдите в аккаунт" });

            try
            {
                TeamManager.DeleteFromPending(int.Parse(selfId), userId, applicationId);
            }
            catch
            {
                return BadRequest(new { Message = "Нет такой заявки" });
            }

            DatabaseController.AddMemberToApplication(applicationId, userId);

            return Ok(new { Message = "Игрок добавлен" });
        }

        [HttpPost("deny/{userId}/{applicationId}")]
        public IActionResult Deny(int userId, int applicationId)
        {
            var selfId = HttpContext.Session.GetString("UserId");

            if (string.IsNullOrEmpty(selfId))
                return Unauthorized(new { Message = "Войдите в аккаунт" });

            TeamManager.DeleteFromPending(int.Parse(selfId), userId, applicationId);

            return Ok(new { Message = "Заявка отклонена" });
        }

        [HttpPost("remove/{userId}/{applicationId}")]
        public IActionResult LeaveTeam(int userId, int applicationId)
        {
            var selfId = HttpContext.Session.GetString("UserId");

            if (string.IsNullOrEmpty(selfId))
                return Unauthorized(new { Message = "Войдите в аккаунт" });

            var ownerId = DatabaseController.GetUserIdByApplicationId(applicationId);

            if (int.Parse(selfId) == ownerId)
            {
                DatabaseController.DeleteMemberFromApplication(applicationId, userId);
                return Ok(new { Message = "Вы кикнули человека" });
            }

            return BadRequest(new { Message = "Вы не являетесь владельцем анкеты" });
        }

        [HttpPost("leave/{applicationId}")]
        public IActionResult LeaveTeam(int applicationId)
        {
            var selfId = HttpContext.Session.GetString("UserId");

            if (string.IsNullOrEmpty(selfId))
                return Unauthorized(new { Message = "Войдите в аккаунт" });

            DatabaseController.DeleteMemberFromApplication(applicationId, int.Parse(selfId));

            return Ok(new { Message = "Вы вышли" });
        }

        [HttpGet("pending")]
        public string GetPendings()
        {
            var userId = HttpContext.Session.GetString("UserId");

            if (string.IsNullOrEmpty(userId))
            {
                Response.StatusCode = 401;
                return "Войдите в аккаунт";
            }

            var pendings = TeamManager.GetPending(int.Parse(userId));

            return JsonSerializer.Serialize(pendings);
        }

        
    }
}
