using GameTeam.Classes;
using NodaTime;
using NodaTime.Serialization.SystemTextJson;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddRazorPages();
builder.Services.AddControllers()
	.AddJsonOptions(options =>
			{
                options.JsonSerializerOptions.ConfigureForNodaTime(DateTimeZoneProviders.Tzdb);
			});


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

app.UseRouting();

app.UseSession();
app.UseMiddleware<SessionAuthMiddleware>();

app.UseAuthorization();

app.MapRazorPages();


app.UseEndpoints(endpoints =>
{
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

});

app.MapControllers();

app.Run();