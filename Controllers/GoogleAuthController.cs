using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;
using Microsoft.IdentityModel.Tokens;
using System.IdentityModel.Tokens.Jwt;
using System.Text;

namespace AuthLogins.Controllers
{
    [ApiController]
    [Route("api/auth")]
    public class GoogleAuthController : ControllerBase
    {
        private readonly IConfiguration _config;

        public GoogleAuthController(IConfiguration config)
        {
            _config = config;
        }

        [HttpGet("google-login")]
        public IActionResult GoogleLogin()
        {
            Console.WriteLine("Initiating Google login...");

            var redirectUrl = Url.Action("GoogleResponse", "Auth", null, Request.Scheme);
            Console.WriteLine($"Redirect URL: {redirectUrl}");

            if (string.IsNullOrEmpty(redirectUrl))
                return BadRequest("Redirect URL is not configured correctly.");

            var properties = new AuthenticationProperties
            {
                RedirectUri = redirectUrl
            };

            return Challenge(properties, "Google");
        }

        [HttpGet("google-response")]
        public async Task<IActionResult> GoogleResponse()
        {
            var result = await HttpContext.AuthenticateAsync("Google");

            if (!result.Succeeded || result.Principal == null)
                return BadRequest("Google authentication failed.");

            var email = result.Principal.FindFirst(ClaimTypes.Email)?.Value;
            var name = result.Principal.FindFirst(ClaimTypes.Name)?.Value;

            if (string.IsNullOrEmpty(email))
                return BadRequest("Email not found in Google account.");

            // Generate JWT token
            var claims = new[]
            {
                new Claim(JwtRegisteredClaimNames.Sub, email),
                new Claim("name", name ?? ""),
                new Claim(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString())
            };

            var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes("eyJhbGciOiJIUzI1NiJ9.eyJJc3N1ZXIiOiJJc3N1ZXIiLCJleHAiOjE3NTAyMjgxOTYsImlhdCI6MTc1MDIyODE5Nn0.Yvgjs6gF2eoK_E0mCZc0jrMYfxSBv8K8fcNwnHSiKVg"));
            var creds = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);

            var token = new JwtSecurityToken(
                issuer: _config["Jwt:Issuer"],
                audience: _config["Jwt:Audience"],
                claims: claims,
                expires: DateTime.UtcNow.AddHours(1),
                signingCredentials: creds);

            var tokenStr = new JwtSecurityTokenHandler().WriteToken(token);

             var htmlContent = $@"
                    <html>
                    <head><title>Authenticating...</title></head>
                    <body>
                        <script>
                            window.opener.postMessage({{
                                token: '{tokenStr}',
                                email: '{email}',
                                name: '{name}'
                            }}, '*');
                            window.close();
                        </script>
                    </body>
                    </html>";


            return Content(htmlContent, "text/html");
        }

        [HttpGet("hello")]
        public IActionResult SayHello()
        {
            return Ok("Hello from Google Auth!");
        }
    }
}
