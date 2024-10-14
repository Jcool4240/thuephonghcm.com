using Microsoft.IdentityModel.Tokens;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;

namespace JcoolDevRoom.Middleware
{
    public class JwtRefreshMiddleware
    {
        private readonly RequestDelegate _next;
        private readonly IConfiguration _configuration;

        public JwtRefreshMiddleware(RequestDelegate next, IConfiguration configuration)
        {
            _next = next;
            _configuration = configuration;
        }

        public async Task InvokeAsync(HttpContext context)
        {
            if (context.User.Identity.IsAuthenticated)
            {
                var expirationClaim = context.User.FindFirst("exp");
                if (expirationClaim != null)
                {
                    var expirationTime = DateTimeOffset.FromUnixTimeSeconds(long.Parse(expirationClaim.Value));
                    if (expirationTime.AddMinutes(-5) < DateTimeOffset.UtcNow)
                    {
                        var claims = context.User.Claims.ToList();
                        var newToken = GenerateJwtToken(claims);
                        context.Response.Headers.Add("X-New-Token", newToken);
                    }
                }
            }

            await _next(context);
        }

        private string GenerateJwtToken(IEnumerable<Claim> claims)
        {
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
}