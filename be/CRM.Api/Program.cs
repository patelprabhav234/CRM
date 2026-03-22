using System.Security.Claims;
using System.Text;
using CRM.Api;
using CRM.Api.Filters;
using CRM.Api.Middleware;
using CRM.Infrastructure.Identity;
using CRM.Infrastructure.Persistence;
using CRM.Infrastructure.Tenancy;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi.Models;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddControllers(o => o.Filters.Add<DbUnavailableExceptionFilter>());
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(c =>
{
    c.SwaggerDoc("v1", new OpenApiInfo { Title = "FireOps CRM API (MaX)", Version = "v1" });
    c.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme
    {
        Description = "JWT Bearer. Example: \"Authorization: Bearer {token}\"",
        Name = "Authorization",
        In = ParameterLocation.Header,
        Type = SecuritySchemeType.Http,
        Scheme = "bearer",
        BearerFormat = "JWT",
    });
    c.AddSecurityRequirement(new OpenApiSecurityRequirement
    {
        {
            new OpenApiSecurityScheme
            {
                Reference = new OpenApiReference { Type = ReferenceType.SecurityScheme, Id = "Bearer" },
            },
            Array.Empty<string>()
        },
    });
});

var jwtSection = builder.Configuration.GetSection(JwtSettings.SectionName);
builder.Services.Configure<JwtSettings>(jwtSection);
var jwt = jwtSection.Get<JwtSettings>() ?? throw new InvalidOperationException("Jwt section missing.");
var key = Encoding.UTF8.GetBytes(jwt.Key);

builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(o =>
    {
        o.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuer = true,
            ValidateAudience = true,
            ValidateLifetime = true,
            ValidateIssuerSigningKey = true,
            ValidIssuer = jwt.Issuer,
            ValidAudience = jwt.Audience,
            IssuerSigningKey = new SymmetricSecurityKey(key),
            RoleClaimType = ClaimTypes.Role,
        };
    });
builder.Services.AddAuthorization();

builder.Services.AddSingleton<ITokenService, TokenService>();
builder.Services.AddScoped<ITenantContext, TenantContext>();

var connectionString = builder.Configuration.GetConnectionString("DefaultConnection")
    ?? throw new InvalidOperationException(
        "Connection string 'DefaultConnection' is missing. Set it in appsettings.json, appsettings.Development.json, or environment variable ConnectionStrings__DefaultConnection.");

builder.Services.AddDbContext<CrmDbContext>(o =>
    o.UseNpgsql(connectionString, npgsql =>
        npgsql.MigrationsAssembly(typeof(CrmDbContext).Assembly.GetName().Name)));

builder.Services.AddCors(o =>
{
    o.AddDefaultPolicy(p =>
        p.WithOrigins("http://localhost:5173", "http://127.0.0.1:5173")
            .AllowAnyHeader()
            .AllowAnyMethod());
});

var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseCors();
app.UseHttpsRedirection();
app.UseAuthentication();
app.UseMiddleware<TenantResolutionMiddleware>();
app.UseAuthorization();
app.MapControllers();

using (var scope = app.Services.CreateScope())
{
    var logger = scope.ServiceProvider.GetRequiredService<ILoggerFactory>().CreateLogger("Startup");
    try
    {
        await DbInitializer.SeedAsync(app.Services, logger);
    }
    catch (Exception ex) when (app.Environment.IsDevelopment() && DatabaseExceptionHelper.IsTransientConnectionFailure(ex))
    {
        logger.LogError(
            ex,
            """
            Cannot reach PostgreSQL — migrations and seed were skipped.
            Fix: start PostgreSQL on the host/port in ConnectionStrings:DefaultConnection (see appsettings.Development.json).
            Docker example: docker run --name fireops-pg -e POSTGRES_PASSWORD=postgres -e POSTGRES_DB=fireops_crm_dev -p 5432:5432 -d postgres:16
            The API will start; database calls will fail until the server is available. Restart the API after PostgreSQL is up.
            """);
    }
}

app.Run();
