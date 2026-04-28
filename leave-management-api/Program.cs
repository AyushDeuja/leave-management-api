using Microsoft.EntityFrameworkCore;
using System.Text.Json.Serialization;
using System.Text.RegularExpressions;

var builder = WebApplication.CreateBuilder(args);

LoadDotEnv(builder.Environment.ContentRootPath);

var defaultConnectionString = ResolveConnectionString(builder.Configuration.GetConnectionString("DefaultConnection"));

//Register AppDbContext with Npgsql provider
builder.Services.AddDbContext<AppDbContext>(options =>
    options.UseNpgsql(defaultConnectionString));

builder.Services
    .AddControllers()
    .AddJsonOptions(options =>
    {
        options.JsonSerializerOptions.Converters.Add(new JsonStringEnumConverter());
    });
builder.Services.AddOpenApi();
builder.Services.AddScoped<IDepartmentService, DepartmentService>();
builder.Services.AddScoped<IUserService, UserService>();

var app = builder.Build();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
}

app.UseHttpsRedirection();

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
