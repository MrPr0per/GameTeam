using GameTeam.Classes;
using NodaTime;
using NodaTime.Serialization.SystemTextJson;
using System.Security.Cryptography.X509Certificates;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddRazorPages();
builder.Services.AddControllers()
	.AddJsonOptions(options =>
			{
                options.JsonSerializerOptions.ConfigureForNodaTime(DateTimeZoneProviders.Tzdb);
			});



builder.WebHost.UseUrls("http://*:80", "https://*:443");

builder.Services.AddSession(options =>
{
	options.IdleTimeout = TimeSpan.FromDays(1); 
	options.Cookie.HttpOnly = true;
	options.Cookie.SameSite = SameSiteMode.Strict;
	options.Cookie.Name = "GameTeam.Session";
});

var app = builder.Build();

if (!app.Environment.IsDevelopment())
{
	app.UseExceptionHandler("/Error");
	app.UseHsts();
}

app.UseHttpsRedirection();
app.UseStaticFiles();
app.UseForwardedHeaders();



app.UseRouting();

app.UseSession();
app.UseMiddleware<SessionAuthMiddleware>();

app.UseAuthorization();

app.MapRazorPages();


#pragma warning disable ASP0014 // Suggest using top level route registrations
app.UseEndpoints(endpoints =>
{
    endpoints.MapGet("/", async context =>
    {
        context.Response.Redirect("/Register");
        await Task.CompletedTask;
    });

    endpoints.MapGet("/Register", async context =>
    {
        var env = context.RequestServices.GetRequiredService<IWebHostEnvironment>();
        var filePath = Path.Combine(env.WebRootPath, "pages/Register.html");

        context.Response.ContentType = "text/html";
        await context.Response.SendFileAsync(filePath);
    });

    endpoints.MapGet("/Profile", async context =>
    {
        var env = context.RequestServices.GetRequiredService<IWebHostEnvironment>();
        var filePath = Path.Combine(env.WebRootPath, "pages/Profile.html");

        context.Response.ContentType = "text/html";
        await context.Response.SendFileAsync(filePath);
    });
    
    endpoints.MapGet("/Profile/{username}", async context =>
    {
        var username = context.Request.RouteValues["username"]?.ToString();
    
        if (string.IsNullOrEmpty(username))
        {
            context.Response.Redirect("/Profile");
            return;
        }
        
        var env = context.RequestServices.GetRequiredService<IWebHostEnvironment>();
        var filePath = Path.Combine(env.WebRootPath, "pages/Profile.html");
    
        context.Response.ContentType = "text/html";
        await context.Response.SendFileAsync(filePath);
    });

    endpoints.MapGet("/Questionnaires", async context =>
    {
        var env = context.RequestServices.GetRequiredService<IWebHostEnvironment>();
        var filePath = Path.Combine(env.WebRootPath, "pages/Questionnaires.html");

        context.Response.ContentType = "text/html";
        await context.Response.SendFileAsync(filePath);
    });

    endpoints.MapGet("/My_questionnaires", async context =>
    {
        var env = context.RequestServices.GetRequiredService<IWebHostEnvironment>();
        var filePath = Path.Combine(env.WebRootPath, "pages/My_questionnaire.html");

        context.Response.ContentType = "text/html";
        await context.Response.SendFileAsync(filePath);
    });

    endpoints.MapGet("/My_teams", async context =>
    {
        var env = context.RequestServices.GetRequiredService<IWebHostEnvironment>();
        var filePath = Path.Combine(env.WebRootPath, "pages/My_teams.html");

        context.Response.ContentType = "text/html";
        await context.Response.SendFileAsync(filePath);
    });

    endpoints.MapGet("/Pending_requests", async context =>
    {
        var env = context.RequestServices.GetRequiredService<IWebHostEnvironment>();
        var filePath = Path.Combine(env.WebRootPath, "pages/Pending_requests.html");

        context.Response.ContentType = "text/html";
        await context.Response.SendFileAsync(filePath);
    });

});
#pragma warning restore ASP0014 // Suggest using top level route registrations

app.MapControllers();

app.Run();