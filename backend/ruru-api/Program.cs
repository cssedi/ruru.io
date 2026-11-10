using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Identity;
using Microsoft.IdentityModel.Tokens;
using ruru_api.DTOs.Auth;
using ruru_api.services.AuthService;
using ruru_api.services.WorkoutService;
using ruru_api.data;
using ruru_api.model;
using ruru_api.repositories;
using ruru_api.repositories.AuthRepository;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
// Learn more about configuring OpenAPI at https://aka.ms/aspnet/openapi
builder.Services.AddOpenApi();
builder.Services.AddControllers();


builder.Services.AddDbContext<RuruDbContext>(options =>
    options.UseNpgsql(builder.Configuration.GetConnectionString("DefaultConnection"))
);


builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer("WebUI", options =>
    {
        options.MetadataAddress = builder.Configuration["Api:MetadataAddress"]!;
        options.Authority = builder.Configuration["Api:Authority"];
        options.Audience = builder.Configuration["Api:Audience"];
        options.TokenValidationParameters = new TokenValidationParameters()
        {
            ValidateIssuer = true,
            ValidateAudience = true,
            ValidateIssuerSigningKey = true,
            ValidAudience = builder.Configuration["Api:Audience"],
            ValidIssuer = builder.Configuration["Api:Issuer"],
            ValidAudiences = builder.Configuration.GetSection("Api:ValidAudiences").Get<string[]>(),
            ValidIssuers = builder.Configuration.GetSection("Api:ValidAudiences").Get<string[]>(),
        };
        
        options.MapInboundClaims = false;
    });

builder.Services.AddIdentity<User, IdentityRole<Guid>>(options =>
{
    options.SignIn.RequireConfirmedAccount = true;
    
    options.Password.RequiredLength = 6;
    options.Password.RequireNonAlphanumeric = false;
    options.Password.RequireDigit = false;
    options.Password.RequireUppercase = false;
    options.Password.RequireLowercase = false;
    
    options.User.RequireUniqueEmail = true;
}).AddRoles<IdentityRole<Guid>>()
    .AddEntityFrameworkStores<RuruDbContext>()
    .AddDefaultTokenProviders();


builder.Services.AddCors(option =>
{
    option.AddPolicy("OpenPolicy", policy =>
    {
        policy.AllowAnyOrigin()
                .AllowAnyHeader()
                .AllowAnyMethod();
    });
});

builder.Services.AddScoped<IWorkoutRepository, WorkoutRepository>();
builder.Services.AddScoped<IWorkoutService, WorkoutService>();
builder.Services.AddScoped<IAuthRepository, AuthRepository>();
builder.Services.AddScoped<IAuthService, AuthService>();


var app = builder.Build();

// Make sure every role in the UserRole enum exists in the database (safe to run on every startup)
using (var scope = app.Services.CreateScope())
{
    var roleManager = scope.ServiceProvider.GetRequiredService<RoleManager<IdentityRole<Guid>>>();
    foreach (var roleName in Enum.GetNames<UserRole>())
    {
        if (!await roleManager.RoleExistsAsync(roleName))
        {
            await roleManager.CreateAsync(new IdentityRole<Guid>(roleName));
        }
    }
}


// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
    app.UseSwaggerUI(options =>
    {
        options.SwaggerEndpoint("/openapi/v1.json", "ruru-api v1");
    });
}

app.UseHttpsRedirection();

app.UseCors("OpenPolicy");

app.MapControllers();

app.Run();
