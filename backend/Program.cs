using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.Tokens;
using System.Text;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddControllers();

// JWT Authentication — replaces authenticateToken middleware
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
        ValidateIssuer           = false,  // set to true if you added an issuer in AuthController
        ValidateAudience         = false,  // set to true if you added an audience in AuthController
        ValidateLifetime         = true,   // rejects expired tokens — equivalent to jwt.verify catching expiry
        ClockSkew                = TimeSpan.Zero // no grace period on expiry
    };

    // Mirrors your console.log("HEADERS:", req.headers.authorization)
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

// CORS — allows your React frontend to connect
builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowReact", policy =>
    {
        policy.WithOrigins(builder.Configuration["AllowedOrigin"] ?? "http://localhost:5173")
              .AllowAnyHeader()
              .AllowAnyMethod();
    });
});

var app = builder.Build();

app.UseCors("AllowReact");
app.UseAuthentication(); // must come before UseAuthorization
app.UseAuthorization();
app.MapControllers();

app.Run();