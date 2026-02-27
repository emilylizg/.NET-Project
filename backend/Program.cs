using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.Tokens;
using Microsoft.Data.SqlClient;
using System.Text;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddControllers();

// JWT Authentication
builder.Services.AddAuthentication(options =>
{
    options.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
    options.DefaultChallengeScheme    = JwtBearerDefaults.AuthenticationScheme;
})
.AddJwtBearer(options =>
{
    options.TokenValidationParameters = new TokenValidationParameters
    {
        ValidateIssuerSigningKey = true,
        IssuerSigningKey         = new SymmetricSecurityKey(
                                       Encoding.UTF8.GetBytes(builder.Configuration["Jwt:Secret"]!)
                                   ),
        ValidateIssuer           = false,
        ValidateAudience         = false,
        ValidateLifetime         = true,
        ClockSkew                = TimeSpan.Zero
    };

    options.Events = new JwtBearerEvents
    {
        OnAuthenticationFailed = context =>
        {
            Console.WriteLine($"[JWT] Authentication failed: {context.Exception.Message}");
            return Task.CompletedTask;
        },
        OnTokenValidated = context =>
        {
            var userId = context.Principal?.FindFirst("id")?.Value;
            Console.WriteLine($"[JWT] Token validated for userId: {userId}");
            return Task.CompletedTask;
        }
    };
});

builder.Services.AddAuthorization();

// CORS — fully mirrors your Express cors() config
builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowReact", policy =>
    {
        policy.WithOrigins(builder.Configuration["AllowedOrigin"] ?? "http://localhost:5173")
              .WithMethods("GET", "POST", "PUT", "DELETE")
              .WithHeaders("Content-Type", "Authorization");
    });
});

var app = builder.Build();

app.UseCors("AllowReact");
app.UseAuthentication();
app.UseAuthorization();
app.MapControllers();

// Mirrors: app.get("/", (req, res) => res.send("Backend is running!"))
app.MapGet("/", () => "Backend is running!");

// Mirrors: app.get("/test-db", async (req, res) => { ... })
app.MapGet("/test-db", async (context) =>
{
    var config           = context.RequestServices.GetRequiredService<IConfiguration>();
    var connectionString = config.GetConnectionString("DefaultConnection")!;

    try
    {
        await using var conn = new SqlConnection(connectionString);
        await conn.OpenAsync();

        await using var cmd    = new SqlCommand("SELECT GETDATE() AS CurrentTime", conn);
        await using var reader = await cmd.ExecuteReaderAsync();

        if (await reader.ReadAsync())
        {
            var currentTime = reader.GetDateTime(reader.GetOrdinal("CurrentTime"));
            await context.Response.WriteAsJsonAsync(new[] { new { currentTime } });
        }
    }
    catch (Exception ex)
    {
        Console.Error.WriteLine(ex);
        context.Response.StatusCode = 500;
        await context.Response.WriteAsync("DB error");
    }
});

Console.WriteLine("Server running on port 5000");
app.Run();