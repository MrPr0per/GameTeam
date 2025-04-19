using GameTeam.Classes.Data;
using GameTeam.Scripts.Controllers;
using GameTeam.Scripts;
using Microsoft.AspNetCore.Mvc;
using System.ComponentModel.DataAnnotations;
using Microsoft.IdentityModel.Tokens;

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
    public string[] Login([FromBody] LoginDto data)
    {
        int? userId;
        if (data.Username is not null)
            userId = DatabaseController.GetIdByUsername(data.Username);
        else if (data.Email is not null)
            userId = DatabaseController.GetIdByEmail(data.Email);
        else
        {
            Response.StatusCode = 400;
            return new string[] { };
        }

        var userData = DatabaseController.GetPasswordAndSalt(userId.Value);
        Console.WriteLine(userData.Value.passwordHash);
        var challenge = HashOperator.GenerateSalt();

        HttpContext.Session.SetString("UserPassword", HashOperator.HashPassword(userData.Value.passwordHash, challenge));
        HttpContext.Session.SetString("UserId", userId.ToString());

        return new[] { userData.Value.salt, challenge };
    }

    [HttpPost("loginpass")] // Изменил на HttpPost, так как логин обычно через POST
    public IActionResult LoginSecond([FromBody] LoginDtoPass data)
    {
        Console.WriteLine(data.Password);
        
        var passwordReal = HttpContext.Session.GetString("UserPassword");
        Console.WriteLine(passwordReal);
        if (passwordReal == data.Password)
        {
            var userId = int.Parse(HttpContext.Session.GetString("UserId"));
            var username = DatabaseController.GetUsernameById(userId);
            HttpContext.Session.Clear();

            HttpContext.Session.SetString("UserId", userId.ToString());
            HttpContext.Session.SetString("Username", username);
            HttpContext.Session.SetString("IsAuthenticated", "true");

            return Ok(new { Message = "Login successful" });
        }

        return BadRequest(new { Message = "Login failed" });
    }

    [HttpGet("salt")]
    public string Salt()
    {
        if (string.IsNullOrEmpty(HttpContext.Session.GetString("Salt")))
        {
            var salt = HashOperator.GenerateSalt();
            HttpContext.Session.SetString("Salt", salt);
            return salt;
        }

        return HttpContext.Session.GetString("Salt");
    }

    [HttpPost("register")]
    public IActionResult Register([FromBody] RegisterDto data)
    {
        var salt = HttpContext.Session.GetString("Salt");
        Console.WriteLine(salt);
        try
        {
            DatabaseController.Register(data.Username, data.Email, data.Password, salt);
        }
        catch
        {
            return BadRequest(new { Message = "Имя пользователя занято" });
        }

        var userId = DatabaseController.GetIdByUsername(data.Username);

        HttpContext.Session.Clear();
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