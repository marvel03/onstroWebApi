using System.Security.Cryptography;
using System.Text;
using System.Text.Json.Serialization;
using ArticleApi.Data;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using ArticleApi.Services;


var builder = WebApplication.CreateBuilder(args);

// Database files live inside the project, in App_Data (git ignores *.mdf and *.ldf)
var dbPath = Path.Combine(builder.Environment.ContentRootPath, "App_Data", "ArticleDb.mdf");
Directory.CreateDirectory(Path.GetDirectoryName(dbPath)!);

// One database name per folder, so two copies of the project never clash
var pathHash = Convert.ToHexString(
    SHA256.HashData(Encoding.UTF8.GetBytes(dbPath.ToLowerInvariant())))[..8];

var connectionString = new SqlConnectionStringBuilder(
    builder.Configuration.GetConnectionString("DefaultConnection"))
{
    AttachDBFilename = dbPath,
    InitialCatalog = $"ArticleDb_{pathHash}"
}.ConnectionString;

builder.Services.AddControllers()
    .AddJsonOptions(options =>
        options.JsonSerializerOptions.Converters.Add(new JsonStringEnumConverter(allowIntegerValues: false)));

builder.Services.AddOpenApi();
builder.Services.AddDbContext<AppDbContext>(options =>
    options.UseSqlServer(connectionString, sql => sql.UseCompatibilityLevel(130)));
builder.Services.AddScoped<IArticleService, ArticleService>();
builder.Services.AddScoped<IContentService, ContentService>();


var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();

    using var scope = app.Services.CreateScope();
    var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
    db.Database.Migrate();
    DbSeeder.Seed(db);
}

app.UseHttpsRedirection();

app.UseAuthorization();

app.MapControllers();

app.Run();
