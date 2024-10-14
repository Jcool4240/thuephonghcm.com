using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using JcoolDevRoom.Data;
using JcoolDevRoom.Models;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using Microsoft.IdentityModel.Tokens;
using System.Text;
using Microsoft.AspNetCore.Authorization;

namespace JcoolDevRoom.Controllers
{
    public class AccountController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly IConfiguration _configuration;

        public AccountController(ApplicationDbContext context, IConfiguration configuration)
        {
            _context = context;
            _configuration = configuration;
        }

        [HttpGet]
        [Route("cong-tac-vien-phong-tro-hcm/login")]
        public IActionResult Login()
        {
            return View();
        }

        [HttpPost]
        [Route("cong-tac-vien-phong-tro-hcm/login")]
        public async Task<IActionResult> Login([FromBody] LoginViewModel model)
        {
            var admin = await _context.Admins
                .Include(a => a.Collaborator)
                .FirstOrDefaultAsync(a => a.UsernameAdmin == model.Username && a.PasswordAdmin == model.Password);

            if (admin != null && admin.Collaborator != null)
            {
                var token = GenerateJwtToken(admin);

                HttpContext.Session.SetString("CollaboratorCode", admin.Collaborator.CollaboratorCode);
                HttpContext.Session.SetInt32("AdminId", admin.AdminId);

                return Ok(new
                {
                    success = true,
                    token = token,
                    collaboratorCode = admin.Collaborator.CollaboratorCode
                });
            }

            return Unauthorized(new { success = false, message = "Tên đăng nhập hoặc mật khẩu không đúng" });
        }

        [HttpPost]
        [Authorize]
        [ValidateAntiForgeryToken]
        [Route("jcool-dev-room/logout")]
        public IActionResult Logout()
        {
            HttpContext.Session.Clear();

            Response.Cookies.Delete("JwtToken");

            return RedirectToAction("Index", "Home", new { area = "" });
        }

        private string GenerateJwtToken(Admin admin)
        {
            var claims = new[]
            {
                new Claim(ClaimTypes.Name, admin.UsernameAdmin),
                new Claim("CollaboratorCode", admin.Collaborator.CollaboratorCode),
                new Claim("AdminId", admin.AdminId.ToString())
            };

            var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_configuration["Jwt:Key"]));
            var creds = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);

            var token = new JwtSecurityToken(
                issuer: _configuration["Jwt:Issuer"],
                audience: _configuration["Jwt:Audience"],
                claims: claims,
                expires: DateTime.Now.AddMinutes(Convert.ToDouble(_configuration["Jwt:ExpireMinutes"])),
                signingCredentials: creds);

            return new JwtSecurityTokenHandler().WriteToken(token);
        }
    }

    public class LoginViewModel
    {
        public string Username { get; set; }
        public string Password { get; set; }
    }
}