using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi.Models;
using LogiTrax.Models;
using System.Text;
using System.Collections.Generic;
using System.Security.Claims;
using System.Text.Json.Serialization;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services.AddControllers();

// Add memory cache services.
builder.Services.AddMemoryCache();

// Register LogiTraxContext with dependency injection.
builder.Services.AddDbContext<LogiTraxContext>(options =>
{
    string currentDir = Environment.CurrentDirectory;
    string dbPath = System.IO.Path.Combine(currentDir, "logitrax.db");
    options.UseSqlite($"Data Source={dbPath}");
});

// Add ASP.NET Identity services.
builder.Services.AddIdentity<ApplicationUser, IdentityRole>()
    .AddEntityFrameworkStores<LogiTraxContext>()
    .AddDefaultTokenProviders();

// Configure JWT authentication.
builder.Services.AddAuthentication(options =>
{
    options.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
    options.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
})
.AddJwtBearer(options =>
{
    options.TokenValidationParameters = new TokenValidationParameters
{
    ValidateIssuer = true,
    ValidateAudience = true,
    ValidateLifetime = true,
    ValidateIssuerSigningKey = true,
    ValidIssuer = builder.Configuration["JWT:ValidIssuer"],
    ValidAudience = builder.Configuration["JWT:ValidAudience"],
    IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(builder.Configuration["JWT:Secret"]!)),

    NameClaimType = ClaimTypes.Name,
    RoleClaimType = ClaimTypes.Role
};

});

builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(c =>
{
    c.SwaggerDoc("v1", new OpenApiInfo { Title = "LogiTrax API", Version = "v1" });

    // Define the JWT Bearer authentication scheme.
    c.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme
    {
        Description = "JWT Authorization header using the Bearer scheme. \r\n\r\n" +
                      "Enter 'Bearer' [space] and then your token in the text input below.\r\n\r\n" +
                      "Example: \"Bearer eyJhbGciOiJIUzI1NiIsInR5cCI6IkpXVCJ9...\"",
        Name = "Authorization",
        In = ParameterLocation.Header,
        Type = SecuritySchemeType.ApiKey,
        Scheme = "Bearer"
    });

    // Require the JWT bearer token for accessing secured endpoints.
    c.AddSecurityRequirement(new OpenApiSecurityRequirement
    {
        {
            new OpenApiSecurityScheme 
            {
                Reference = new OpenApiReference 
                { 
                    Type = ReferenceType.SecurityScheme, 
                    Id = "Bearer" 
                },
                Scheme = "Bearer",         // Changed from "oauth2" to "Bearer"
                Name = "Bearer",
                In = ParameterLocation.Header,
            },
            new List<string>()
        }
    });
});

var app = builder.Build();

// Seed the database.
using (var scope = app.Services.CreateScope())
{
    var context = scope.ServiceProvider.GetRequiredService<LogiTraxContext>();
    var roleManager = scope.ServiceProvider.GetRequiredService<RoleManager<IdentityRole>>();
    var userManager = scope.ServiceProvider.GetRequiredService<UserManager<ApplicationUser>>();
    
    // Apply any pending migrations to keep the database schema up to date.
    context.Database.Migrate();
    
    // Seed the InventoryItems table only if it's empty.
    if (!context.InventoryItems.Any())
    {
        var gamePal = new InventoryItem
        {
            Name = "GamePal",
            Quantity = 12,
            Location = "Central Hub"
        };

        var boomBeatz = new InventoryItem
        {
            Name = "BoomBeatz",
            Quantity = 10,
            Location = "Secondary Hub"
        };

        context.InventoryItems.Add(gamePal);
        context.InventoryItems.Add(boomBeatz);
        context.SaveChanges();
    }

    // Create roles if they don't exist.
    if (!await roleManager.RoleExistsAsync("Manager"))
    {
        await roleManager.CreateAsync(new IdentityRole("Manager"));
    }

    // Optionally, create a test manager user.
    string managerEmail = "manager@example.com";
    var managerUser = await userManager.FindByEmailAsync(managerEmail);
    if (managerUser == null)
    {
        managerUser = new ApplicationUser { UserName = managerEmail, Email = managerEmail };
        var result = await userManager.CreateAsync(managerUser, "ManagerSecret123!");
        if (result.Succeeded)
        {
            await userManager.AddToRoleAsync(managerUser, "Manager");
        }
    }
}


// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseHttpsRedirection();

// IMPORTANT: Authentication must come before Authorization.
app.UseAuthentication();
app.UseAuthorization();

app.MapControllers();

app.Run();
