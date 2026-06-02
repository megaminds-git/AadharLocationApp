using System.Text;
using AadharLocation.Api.Data;
using AadharLocation.Api.Hubs;
using AadharLocation.Api.Services;
using FluentValidation;
using FluentValidation.AspNetCore;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using Resend;
using Scalar.AspNetCore;
using Serilog;

// Load .env file into environment variables before anything else reads config.
// Safe to call when .env doesn't exist (optional: true is the default).
DotNetEnv.Env.Load(Path.Combine(AppContext.BaseDirectory, ".env"));

var builder = WebApplication.CreateBuilder(args);

// Railway (and other cloud hosts) inject PORT — honour it
var port = Environment.GetEnvironmentVariable("PORT");
if (!string.IsNullOrEmpty(port))
    builder.WebHost.UseUrls($"http://0.0.0.0:{port}");

// Runtime-editable settings (persisted by SettingsController; reloaded without restart)
builder.Configuration.AddJsonFile("appsettings.runtime.json", optional: true, reloadOnChange: true);

builder.Host.UseSerilog((ctx, cfg) =>
    cfg.ReadFrom.Configuration(ctx.Configuration)
       .WriteTo.Console()
       .WriteTo.File("logs/api-.log", rollingInterval: RollingInterval.Day));

builder.Services.AddDbContext<AppDbContext>(options =>
    options.UseNpgsql(
        builder.Configuration.GetConnectionString("Default"),
        x => x
            .MigrationsAssembly("AadharLocation.Api")
            .UseNetTopologySuite()
    )
);

builder.Services.Configure<GeofenceSettings>(builder.Configuration.GetSection("GeofenceSettings"));
builder.Services.Configure<EmailSettings>(builder.Configuration.GetSection("Email"));
builder.Services.AddResend(options =>
    options.ApiToken = builder.Configuration["Email:ApiKey"] ?? string.Empty);

builder.Services.AddHostedService<DatabaseSeeder>();
builder.Services.AddHostedService<OfflineDetectionService>();
builder.Services.AddSingleton<JwtService>();
builder.Services.AddScoped<EmailService>();
builder.Services.AddScoped<AlertService>();
builder.Services.AddScoped<GeofenceService>();
builder.Services.AddFluentValidationAutoValidation();
builder.Services.AddValidatorsFromAssemblyContaining<Program>();

var jwtKey = builder.Configuration["Jwt:Secret"]
    ?? throw new InvalidOperationException("Jwt:Secret is not configured");

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
            IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtKey))
        };

        options.Events = new JwtBearerEvents
        {
            OnMessageReceived = context =>
            {
                var accessToken = context.Request.Query["access_token"];
                if (!string.IsNullOrEmpty(accessToken) &&
                    context.HttpContext.Request.Path.StartsWithSegments("/hubs"))
                {
                    context.Token = accessToken;
                }
                return Task.CompletedTask;
            }
        };
    });

builder.Services.AddAuthorization(options =>
{
    options.AddPolicy("AdminOnly", p => p.RequireRole("Admin"));
    options.AddPolicy("TrackerOnly", p => p.RequireRole("Tracker"));
});
builder.Services.AddControllers();
builder.Services.AddSignalR();
builder.Services.AddOpenApi();

var app = builder.Build();

if (app.Environment.IsDevelopment() || app.Environment.IsProduction())
{
    app.MapOpenApi();
    app.MapScalarApiReference();
}

if (app.Environment.IsDevelopment())
    app.UseHttpsRedirection();
app.UseSerilogRequestLogging();
app.UseAuthentication();
app.UseAuthorization();
app.MapGet("/health", () => Results.Ok("healthy"));
app.MapControllers();
app.MapHub<AadharLocationHub>("/hubs/tracking");

app.Run();
