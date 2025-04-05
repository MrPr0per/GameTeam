using Microsoft.AspNetCore.Mvc;
using GameTeam.Classes.Data;

// For more information on enabling Web API for empty projects, visit https://go.microsoft.com/fwlink/?LinkID=397860

namespace GameTeam.Scripts.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class AuthController : ControllerBase
    {
        private readonly IConfiguration config;

        public AuthController(IConfiguration config)
        {
            this.config = config;
        }

        // GET: api/<AuthController>
        [HttpGet("login")]
        public IActionResult Get([FromBody] LoginDto data)
        {
            var username = "";
            var logged = DatabaseController.Login(data.Username, data.Email, data.Password, out username);
            if (logged)
            {
                //Надо куда-то записывать этот токен, чтобы сессию сохранять, а то пока что мы это не делаем
                var token = CookieGenerator.GenerateJwtToken(username, config);

                Response.Cookies.Append(
                    "AuthToken",                     // Имя cookie
                    token,         // Значение cookie
                    new CookieOptions
                    {
                        Expires = DateTime.UtcNow.AddDays(1), // Срок действия (1 день)
                        HttpOnly = true,                      // Доступ только через HTTP (защита от XSS)
                        Secure = true,                        // Только HTTPS (в production)
                        SameSite = SameSiteMode.Strict        // Защита от CSRF
                    });

                return Ok(new { Message = "Registration successful" });
            }
            return Unauthorized(new { Message = "Неправильный логин/почта или пароль" });
        }

        // POST api/<AuthController>
        [HttpPost("register")]
        public IActionResult Post([FromBody] RegisterDto data)
        {
            if (data.Password != data.ConfirmPassword)
                return BadRequest(new { Message = "Пароли не совпадают" });

            DatabaseController.Register(data.Username, data.Email, data.Password);

            //Надо куда-то записывать этот токен, чтобы сессию сохранять, а то пока что мы это не делаем
            var token = CookieGenerator.GenerateJwtToken(data.Username, config);

            Response.Cookies.Append(
                "AuthToken",                     // Имя cookie
                token,         // Значение cookie
                new CookieOptions
                {
                    Expires = DateTime.UtcNow.AddDays(1), // Срок действия (1 день)
                    HttpOnly = true,                      // Доступ только через HTTP (защита от XSS)
                    Secure = true,                        // Только HTTPS (в production)
                    SameSite = SameSiteMode.Strict        // Защита от CSRF
                });

            return Ok(new { Message = "Registration successful" });
        }
    }
}


//Это код чтобы проверять cookie токен

/*
[HttpGet("secure-data")]
public IActionResult GetSecureData()
{
    if (!Request.Cookies.TryGetValue("AuthToken", out var token))
        return Unauthorized();

    try
    {
        var handler = new JwtSecurityTokenHandler();
        var jwt = handler.ReadJwtToken(token);
        var username = jwt.Claims.First(c => c.Type == ClaimTypes.Name).Value;
        
        return Ok(new { Data = $"Hello, {username}!" });
    }
    catch
    {
        return Unauthorized();
    }
}
*/