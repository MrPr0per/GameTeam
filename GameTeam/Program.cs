using GameTeam.Classes;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services.AddRazorPages();
builder.Services.AddControllers();

// ��������� ������� ������
builder.Services.AddSession(options =>
{
    options.IdleTimeout = TimeSpan.FromDays(1); // ����� ����� ������
    options.Cookie.HttpOnly = true;
    options.Cookie.SameSite = SameSiteMode.Strict;
    options.Cookie.Name = "GameTeam.Session";
});

var app = builder.Build();

// Configure the HTTP request pipeline.
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Error");
    app.UseHsts();
}

app.UseHttpsRedirection();
app.UseStaticFiles();

app.UseRouting();

// ��������� middleware ������ ����� ������������
app.UseSession();
app.UseMiddleware<SessionAuthMiddleware>();

app.UseAuthorization();

app.MapRazorPages();
app.UseEndpoints(endpoints =>
{

    // ��� �������� �������� ���������� �����
    endpoints.MapGet("/Register", async context =>
    {
        var env = context.RequestServices.GetRequiredService<IWebHostEnvironment>();
        var filePath = Path.Combine(env.WebRootPath, "pages/Register.html");

        context.Response.ContentType = "text/html";
        await context.Response.SendFileAsync(filePath);
    });
});
app.MapControllers();

app.Run();