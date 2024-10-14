using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Http.Features;
using JcoolDevRoom.Data;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.Tokens;
using System.Text;
using JcoolDevRoom.Middleware;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services.AddControllersWithViews();
builder.Services.AddHttpClient();

builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(options =>
    {
        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuer = true,
            ValidateAudience = true,
            ValidateLifetime = true,
            ValidateIssuerSigningKey = true,
            ValidIssuer = builder.Configuration["Jwt:Issuer"],
            ValidAudience = builder.Configuration["Jwt:Audience"],
            IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(builder.Configuration["Jwt:Key"]))
        };
        options.Events = new JwtBearerEvents
        {
            OnMessageReceived = context =>
            {
                if (context.Request.Cookies.ContainsKey("JwtToken"))
                {
                    context.Token = context.Request.Cookies["JwtToken"];
                }
                return Task.CompletedTask;
            }
        };
    });

builder.Services.AddSession(options =>
{
    options.IdleTimeout = TimeSpan.FromHours(2);
    options.Cookie.HttpOnly = true;
    options.Cookie.IsEssential = true;
});

builder.Services.AddDbContext<ApplicationDbContext>(options =>
    options.UseSqlServer(builder.Configuration.GetConnectionString("DefaultConnection")));

builder.Services.Configure<FormOptions>(options =>
{
    options.ValueLengthLimit = int.MaxValue;
    options.MultipartBodyLengthLimit = int.MaxValue;
    options.MultipartHeadersLengthLimit = int.MaxValue;
});

builder.Services.AddAntiforgery(options => options.HeaderName = "X-CSRF-TOKEN");

var app = builder.Build();

if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Home/Error");
    app.UseHsts();
}

app.UseHttpsRedirection();
app.UseStaticFiles();

app.UseRouting();
app.UseSession();

app.Use(async (context, next) =>
{
    var token = context.Request.Query["token"].FirstOrDefault();
    if (!string.IsNullOrEmpty(token))
    {
        context.Request.Headers.Add("Authorization", "Bearer " + token);
        context.Response.Cookies.Append("JwtToken", token, new CookieOptions
        {
            HttpOnly = true,
            Secure = true,
            SameSite = SameSiteMode.Strict
        });
    }
    await next();
});

app.UseAuthentication();
app.UseAuthorization();

app.UseMiddleware<JwtRefreshMiddleware>();

app.MapControllerRoute(
    name: "cong-tac-vien-cho-thue-phong",
    pattern: "cong-tac-vien-cho-thue-phong",
    defaults: new { controller = "Home", action = "Index" });

app.MapControllerRoute(
    name: "tat-ca-phong-cho-thue",
    pattern: "tat-ca-phong-cho-thue",
    defaults: new { controller = "Home", action = "AllRooms" });

app.MapControllerRoute(
    name: "gioi-thieu-ve-thue-phong-hcm",
    pattern: "gioi-thieu-ve-thue-phong-hcm",
    defaults: new { controller = "Home", action = "About" });

app.MapControllerRoute(
    name: "lien-he-thue-phong-hcm",
    pattern: "lien-he-thue-phong-hcm",
    defaults: new { controller = "Home", action = "Contact" });

app.MapControllerRoute(
    name: "login",
    pattern: "cong-tac-vien-phong-tro-hcm/login",
    defaults: new { controller = "Account", action = "Login" });

app.MapControllerRoute(
    name: "collaborator",
    pattern: "{collaboratorCode}/{action=home}/{id?}",
    defaults: new { controller = "Collaborator" });

app.MapControllerRoute(
    name: "loginRedirect",
    pattern: "Login",
    defaults: new { controller = "Account", action = "LoginRedirect" });

app.MapControllerRoute(
    name: "manage",
    pattern: "{collaboratorCode}/manager/{action=Home}/{id?}",
    defaults: new { controller = "Manager" });

app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Home}/{action=Index}/{id?}");

app.MapControllers();

app.MapGet("/", context =>
{
    context.Response.Redirect("/cong-tac-vien-cho-thue-phong");
    return Task.CompletedTask;
});

app.Run();