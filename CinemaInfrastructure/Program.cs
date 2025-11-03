using Azure;
using Azure.Search.Documents;
using Azure.Search.Documents.Indexes;
using CinemaDomain.Model;
using CinemaInfrastructure;
using CinemaInfrastructure.Services;
using CinemaInfrastructure.ViewModel;
using CinemaInfrastructure.Hubs;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.IdentityModel.Tokens;
using Microsoft.Extensions.Logging;
using System.Text;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddControllersWithViews();

builder.Services.AddDbContext<CinemaContext>(options =>
    options.UseSqlServer(builder.Configuration.GetConnectionString("DefaultConnection")));

builder.Services.AddIdentity<User, IdentityRole>(options =>
{
    options.Password.RequireDigit = false;
    options.Password.RequiredLength = 4;
    options.Password.RequireNonAlphanumeric = false;
    options.Password.RequireUppercase = false;
    options.Password.RequireLowercase = false;
    options.SignIn.RequireConfirmedEmail = false;
    options.SignIn.RequireConfirmedPhoneNumber = false;
    options.Lockout.AllowedForNewUsers = false;
})
.AddEntityFrameworkStores<CinemaContext>()
.AddDefaultTokenProviders();

builder.Services.AddAuthentication()
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
            IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(builder.Configuration["Jwt:Key"]!))
        };
    });

builder.Services.AddAuthorization();

builder.Services.AddSingleton(sp =>
{
    var config = sp.GetRequiredService<IConfiguration>();
    var serviceUrl = config["AzureSearch:ServiceUrl"];
    var apiKey = config["AzureSearch:ApiKey"];
    return new SearchIndexClient(new Uri(serviceUrl!), new AzureKeyCredential(apiKey!));
});

builder.Services.AddSingleton(sp =>
{
    var config = sp.GetRequiredService<IConfiguration>();
    var serviceUrl = config["AzureSearch:ServiceUrl"];
    var apiKey = config["AzureSearch:ApiKey"];
    var indexName = config["AzureSearch:IndexName"];
    return new SearchClient(new Uri(serviceUrl!), indexName!, new AzureKeyCredential(apiKey!));
});

builder.Services.AddScoped<AzureSearchService>();

builder.Services.AddScoped<IImportService<Film>, CategoryFilmCompanyImportService>();
builder.Services.AddScoped<IExportService<Film>, FilmExportService>();

builder.Services.AddScoped<BookingService>();
builder.Services.AddSignalR(options =>
{
    options.EnableDetailedErrors = true;
});
var app = builder.Build();

using (var scope = app.Services.CreateScope())
{
    var services = scope.ServiceProvider;

    var dbContext = services.GetRequiredService<CinemaContext>();
    dbContext.Database.Migrate();

    var userManager = services.GetRequiredService<UserManager<User>>();
    var roleManager = services.GetRequiredService<RoleManager<IdentityRole>>();
    await RoleInitializer.InitializeAsync(userManager, roleManager);

    try
    {
        var logger = services.GetRequiredService<ILogger<Program>>();

        var azureSearchService = services.GetRequiredService<AzureSearchService>();
        await azureSearchService.InitializeAndIndexAsync();

        logger.LogInformation("Azure Search initialized and documents indexed (if any).");
    }
    catch (Exception ex)
    {
        var logger = services.GetRequiredService<ILogger<Program>>();
        logger.LogError(ex, "Помилка під час ініціалізації Azure Search");
    }
}

if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Home/Error");
    app.UseHsts();
}

app.UseHttpsRedirection();
app.UseStaticFiles();
app.UseRouting();

app.UseAuthentication();
app.UseAuthorization();

app.MapHub<BookingHub>("/hubs/booking");
app.MapStaticAssets();

app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Home}/{action=Index}/{id?}");

await app.RunAsync();
