using Microsoft.AspNetCore.Mvc;
using Microsoft.Data.SqlClient;
using Microsoft.IdentityModel.Tokens;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using BCrypt.Net;

namespace ThisProject.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class AuthController : ControllerBase
    {
        private readonly IConfiguration _config;
        private readonly string _connectionString;

        public AuthController(IConfiguration config)
        {
            _config = config;
            _connectionString = config.GetConnectionString("DefaultConnection")!;
        }

        [HttpPost("register")]
        public async Task<IActionResult> Register([FromBody] RegisterRequest req)
        {
            if (string.IsNullOrWhiteSpace(req.Name) ||
                string.IsNullOrWhiteSpace(req.Email) ||
                string.IsNullOrWhiteSpace(req.Password))
                return BadRequest(new { message = "All fields are required" });

            await using var conn = new SqlConnection(_connectionString);
            await conn.OpenAsync();

            // Check if email exists
            await using (var checkCmd = new SqlCommand("SELECT Id FROM Users WHERE Email = @Email", conn))
            {
                checkCmd.Parameters.AddWithValue("@Email", req.Email);
                var existing = await checkCmd.ExecuteScalarAsync();
                if (existing != null)
                    return BadRequest(new { message = "Email already registered" });
            }

            var hashedPassword = BCrypt.Net.BCrypt.HashPassword(req.Password, workFactor: 10);

            await using (var insertCmd = new SqlCommand(
                "INSERT INTO Users (Name, Email, PasswordHash) VALUES (@Name, @Email, @PasswordHash)", conn))
            {
                insertCmd.Parameters.AddWithValue("@Name", req.Name);
                insertCmd.Parameters.AddWithValue("@Email", req.Email);
                insertCmd.Parameters.AddWithValue("@PasswordHash", hashedPassword);
                await insertCmd.ExecuteNonQueryAsync();
            }
            return StatusCode(201, new { message = "User registered successfully" });
        }

        [HttpPost("login")]
        public async Task<IActionResult> Login([FromBody] LoginRequest req)
        {
            if (string.IsNullOrWhiteSpace(req.Email) || string.IsNullOrWhiteSpace(req.Password))
                return BadRequest(new { message = "Email and password required" });

            await using var conn = new SqlConnection(_connectionString);
            await conn.OpenAsync();

            await using var cmd = new SqlCommand("SELECT * FROM Users WHERE Email = @Email", conn);
            cmd.Parameters.AddWithValue("@Email", req.Email);

            await using var reader = await cmd.ExecuteReaderAsync();

            if (!await reader.ReadAsync())
                return BadRequest(new { message = "Invalid credentials" });

            var userId    = reader.GetInt32(reader.GetOrdinal("Id"));
            var userName  = reader.GetString(reader.GetOrdinal("Name"));
            var userEmail = reader.GetString(reader.GetOrdinal("Email"));
            var passHash  = reader.GetString(reader.GetOrdinal("PasswordHash"));

            if (!BCrypt.Net.BCrypt.Verify(req.Password, passHash))
                return BadRequest(new { message = "Invalid credentials" });

            var token = GenerateJwt(userId, userEmail);

            return Ok(new
            {
                token,
                user = new { id = userId, name = userName, email = userEmail }
            });
        }

        private string GenerateJwt(int userId, string email)
        {
            var secret  = _config["Jwt:Secret"]!;
            var expires = _config["Jwt:ExpiresIn"] ?? "7d";

            var key   = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(secret));
            var creds = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);

            var claims = new[]
            {
                new Claim("id", userId.ToString()),
                new Claim(ClaimTypes.Email, email)
            };
            var expiry = expires.EndsWith('d')
                ? DateTime.UtcNow.AddDays(double.Parse(expires.TrimEnd('d')))
                : DateTime.UtcNow.AddHours(double.Parse(expires.TrimEnd('h')));

            var tokenDescriptor = new JwtSecurityToken(
                claims: claims,
                expires: expiry,
                signingCredentials: creds
            );
            return new JwtSecurityTokenHandler().WriteToken(tokenDescriptor);
        }
    }
    public record RegisterRequest(string Name, string Email, string Password);
    public record LoginRequest(string Email, string Password);
}