using System.Text;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi.Models;
using Npgsql;
using VehiclePartsBackend.Data;
using VehiclePartsBackend.Services;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(options =>
{
    options.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme
    {
        Description = "Enter JWT token only, or: Bearer {token}",
        Name = "Authorization",
        In = ParameterLocation.Header,
        Type = SecuritySchemeType.Http,
        Scheme = "bearer",
        BearerFormat = "JWT"
    });

    options.AddSecurityRequirement(new OpenApiSecurityRequirement
    {
        {
            new OpenApiSecurityScheme
            {
                Reference = new OpenApiReference
                {
                    Type = ReferenceType.SecurityScheme,
                    Id = "Bearer"
                }
            },
            Array.Empty<string>()
        }
    });
});

var postgresConnection = builder.Configuration.GetConnectionString("DefaultConnection")
    ?? throw new InvalidOperationException(
        "PostgreSQL connection string 'DefaultConnection' is not configured in appsettings.");

builder.Services.AddDbContext<AppDbContext>(options =>
    options.UseNpgsql(postgresConnection));

builder.Services.AddScoped<JwtTokenService>();
builder.Services.Configure<EmailSettings>(builder.Configuration.GetSection("Email"));
builder.Services.AddScoped<IEmailSender, SmtpEmailSender>();
builder.Services.AddScoped<SalesInvoiceEmailService>();
builder.Services.Configure<SystemMonitoringSettings>(builder.Configuration.GetSection("SystemMonitoring"));
builder.Services.AddScoped<SystemMonitoringService>();
builder.Services.AddHostedService<SystemMonitoringHostedService>();

var jwtKey = builder.Configuration["Jwt:Key"] ?? throw new InvalidOperationException("JWT key not found.");
var jwtIssuer = builder.Configuration["Jwt:Issuer"] ?? "VehiclePartsBackend";
var jwtAudience = builder.Configuration["Jwt:Audience"] ?? "VehiclePartsClients";

builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(options =>
    {
        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuer = true,
            ValidateAudience = true,
            ValidateLifetime = true,
            ValidateIssuerSigningKey = true,
            ValidIssuer = jwtIssuer,
            ValidAudience = jwtAudience,
            IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtKey))
        };
    });

builder.Services.AddAuthorization();

var app = builder.Build();

using (var scope = app.Services.CreateScope())
{
    var dbContext = scope.ServiceProvider.GetRequiredService<AppDbContext>();
    var logger = scope.ServiceProvider.GetRequiredService<ILogger<Program>>();

    try
    {
        await DatabaseBootstrap.InitializeAsync(
            dbContext,
            logger,
            seedDevelopmentData: app.Environment.IsDevelopment());
    }
    catch (PostgresException ex) when (ex.SqlState == "28P01")
    {
        logger.LogCritical(
            "PostgreSQL password rejected. Update Password in appsettings.Development.json (current user: postgres).");
        throw;
    }
    catch (Exception ex)
    {
        logger.LogCritical(
            ex,
            "Could not connect to PostgreSQL. Ensure the PostgreSQL service is running and ConnectionStrings:DefaultConnection is correct.");
        throw;
    }
}

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

if (!app.Environment.IsDevelopment())
{
    app.UseHttpsRedirection();
}

app.UseAuthentication();
app.UseAuthorization();
app.MapControllers();

app.Run();
