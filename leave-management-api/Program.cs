using FluentValidation;
using FluentValidation.AspNetCore;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using Quartz;
using System.Text;
using System.Text.Json.Serialization;
using System.Text.RegularExpressions;

var builder = WebApplication.CreateBuilder(args);

LoadDotEnv(builder.Environment.ContentRootPath);

var defaultConnectionString = ResolveConnectionString(builder.Configuration.GetConnectionString("DefaultConnection"));

//Register AppDbContext with Npgsql provider
builder.Services.AddDbContext<AppDbContext>(options =>
    options.UseNpgsql(defaultConnectionString));

var jwtSecret = Environment.GetEnvironmentVariable("JWT_SECRET");

builder.Services.AddAuthentication(options =>
{
    options.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
    options.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
})
.AddJwtBearer(options =>
{
    options.TokenValidationParameters = new Microsoft.IdentityModel.Tokens.TokenValidationParameters
    {
        ValidateIssuerSigningKey = true,
        IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtSecret)),
        ValidateIssuer = true,
        ValidIssuer = builder.Configuration["Jwt:Issuer"],
        ValidateAudience = true,
        ValidAudience = builder.Configuration["Jwt:Audience"],
        ValidateLifetime = true,           // checks expiry automatically
        ClockSkew = TimeSpan.Zero   // no grace period after expiry
    };
});
builder.Services.AddAuthorization();

builder.Services.AddQuartz(q =>
{
    var jobKey = new JobKey("YearlyLeaveBalanceJob");

    q.AddJob<YearlyLeaveBalanceJob>(opts => opts.WithIdentity(jobKey));

    q.AddTrigger(opts => opts
        .ForJob(jobKey)
        .WithIdentity("YearlyLeaveBalance-trigger")
        // Runs at 00:01 on January 1st every year
        .WithCronSchedule("0 1 0 1 1 ?"));
});

builder.Services.AddQuartzHostedService(q => q.WaitForJobsToComplete = true);


builder.Services
    .AddControllers()
    .AddJsonOptions(options =>
    {
        options.JsonSerializerOptions.Converters.Add(new JsonStringEnumConverter());
    });
builder.Services.AddOpenApi();
builder.Services.AddScoped<IAuthService, AuthService>();
builder.Services.AddScoped<ITokenService, TokenService>();
builder.Services.AddScoped<IDepartmentService, DepartmentService>();
builder.Services.AddScoped<IUserService, UserService>();
builder.Services.AddScoped<ILeaveBalanceService, LeaveBalanceService>();
builder.Services.AddScoped<ILeaveRequestService, LeaveRequestService>();

builder.Services.AddFluentValidationAutoValidation();
builder.Services.AddValidatorsFromAssemblyContaining<Program>();
var app = builder.Build();

using (var scope = app.Services.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
    await DbSeeder.SeedLeaveTypesAsync(db);
}

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
}

app.UseHttpsRedirection();
app.UseAuthentication();
app.UseAuthorization();

app.MapControllers();

app.Run();

static void LoadDotEnv(string contentRootPath)
{
    var envFilePath = Path.Combine(contentRootPath, ".env");

    if (!File.Exists(envFilePath))
    {
        return;
    }

    foreach (var rawLine in File.ReadAllLines(envFilePath))
    {
        var line = rawLine.Trim();

        if (string.IsNullOrWhiteSpace(line) || line.StartsWith('#'))
        {
            continue;
        }

        var separatorIndex = line.IndexOf('=');

        if (separatorIndex <= 0)
        {
            continue;
        }

        var key = line[..separatorIndex].Trim();
        var value = line[(separatorIndex + 1)..].Trim();

        if (value.Length >= 2 && value.StartsWith('"') && value.EndsWith('"'))
        {
            value = value[1..^1];
        }

        Environment.SetEnvironmentVariable(key, value);
    }
}

static string ResolveConnectionString(string? connectionStringTemplate)
{
    if (string.IsNullOrWhiteSpace(connectionStringTemplate))
    {
        throw new InvalidOperationException("Missing connection string template in appsettings.json.");
    }

    return Regex.Replace(connectionStringTemplate, @"\$\{(?<name>[A-Z0-9_]+)\}", match =>
    {
        var variableName = match.Groups["name"].Value;
        var variableValue = Environment.GetEnvironmentVariable(variableName);

        if (string.IsNullOrWhiteSpace(variableValue))
        {
            throw new InvalidOperationException($"Missing required environment variable '{variableName}'.");
        }

        return variableValue;
    });
}
