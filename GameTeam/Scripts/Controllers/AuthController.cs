using GameTeam.Classes.Data;
using GameTeam.Scripts.Controllers;
using GameTeam.Scripts;
using Microsoft.AspNetCore.Mvc;
using System.ComponentModel.DataAnnotations;

[Route("api/[controller]")]
[ApiController]
public class AuthController : ControllerBase
{
    private readonly IConfiguration config;

    public AuthController(IConfiguration config)
    {
        this.config = config;
    }

    [HttpPost("login")] // Изменил на HttpPost, так как логин обычно через POST
    public IActionResult Login([FromBody] LoginDto data)
    {
        var username = "";
        var logged = DatabaseController.Login(data.Username, data.Email, data.Password, out username);
        if (logged)
        {
            var userId = DatabaseController.GetIdByUsername(username);
            // Сохраняем информацию о пользователе в сессии
            HttpContext.Session.SetString("UserId", userId.ToString());
            HttpContext.Session.SetString("Username", username);
            HttpContext.Session.SetString("IsAuthenticated", "true");

            return Ok(new { Message = "Login successful" });
        }
        return Unauthorized(new { Message = "Неправильный логин/почта или пароль" });
    }

    [HttpPost("register")]
    public IActionResult Register([FromBody] RegisterDto data)
    {
        if (data.Password != data.ConfirmPassword)
            return BadRequest(new { Message = "Пароли не совпадают" });

        try
        {
            DatabaseController.Register(data.Username, data.Email, data.Password);
        }
        catch
        {
            return BadRequest(new { Message = "Имя пользователя занято" });
        }

        var userId = DatabaseController.GetIdByUsername(data.Username);
        // Сохраняем информацию о пользователе в сессии
        HttpContext.Session.SetString("UserId", userId.ToString());
        HttpContext.Session.SetString("Username", data.Username);
        HttpContext.Session.SetString("IsAuthenticated", "true");

        return Ok(new { Message = "Registration successful" });
    }

    [HttpPost("logout")]
    public IActionResult Logout()
    {

        // Очищаем сессию
        HttpContext.Session.Clear();

        return Ok(new { Message = "Logout successful" });
    }
}