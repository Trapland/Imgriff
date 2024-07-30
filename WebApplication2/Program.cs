using Imgriff.Services.Random;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Identity.UI.Services;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi.Models;
using System.Security.Cryptography;
using System.Text;
using Imgriff.Data;
using Imgriff.Services.Email;
using Imgriff;
using System.Net;
using Imgriff.Services.Kdf;
using Imgriff.Services.Hash;

var builder = WebApplication.CreateBuilder(args);

builder.WebHost.ConfigureKestrel(serverOptions =>
{
    serverOptions.Listen(IPAddress.Parse("26.5.203.178"), 5001, listenOptions =>
    {
        listenOptions.UseHttps();
    });
});
builder.WebHost.UseKestrel();


// Load configuration
var configuration = builder.Configuration;

// Set user folder path
AppConfig.UserFolderPath = configuration["UserFolderPath"];

AppConfig.Key = configuration["MyKey"];

// Add services to the container.
builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(c =>
{
    c.SwaggerDoc("v1", new OpenApiInfo { Title = "My API", Version = "v1" });
    // Добавляем схему аутентификации JWT в Swagger
    c.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme
    {
        Description = "JWT Authorization header using the Bearer scheme. Example: \"Authorization: Bearer {token}\"",
        Name = "Authorization",
        In = ParameterLocation.Header,
        Type = SecuritySchemeType.ApiKey,
        Scheme = "Bearer"
    });

    // Указываем, что запросы должны быть аутентифицированы с использованием схемы аутентификации JWT
    c.AddSecurityRequirement(new OpenApiSecurityRequirement
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
            new string[] { }
        }
    });
});

builder.Services.Configure<SmtpOptions>(builder.Configuration.GetSection("Smtp"));
builder.Services.AddSingleton<IHashService, SHA256HashService>();
builder.Services.AddSingleton<IEmailSender, EmailSender>();
builder.Services.AddSingleton<IRandomService, RandomService>();
builder.Services.AddSingleton<IKdfService, HashKdfService>();
var key = Encoding.ASCII.GetBytes("G&nYp31SAFK@$)S&dsa94nfsU8g*Ws7m$L#v!A@Q^Xz2rZpE5n\r\n"); // Секретный ключ
builder.Services.AddAuthentication(options =>
{
    options.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
    options.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
}).AddJwtBearer(options =>
{
    options.RequireHttpsMetadata = false;
    options.SaveToken = true;
    options.TokenValidationParameters = new TokenValidationParameters
    {
        ValidateIssuerSigningKey = true,
        IssuerSigningKey = new SymmetricSecurityKey(key),
        ValidateIssuer = false,
        ValidateAudience = false
    };
});

builder.Services.AddDbContext<DataContext>(options =>
    options.UseSqlServer(builder.Configuration.GetConnectionString("DbContext")));

var app = builder.Build();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI(c => c.SwaggerEndpoint("/swagger/v1/swagger.json", "My API v1"));
}
//app.UseCors(builder => builder.AllowAnyHeader().AllowAnyMethod().WithOrigins("http://26.237.60.210:4200"));
app.UseCors(builder => builder.AllowAnyHeader().AllowAnyMethod().AllowAnyOrigin());
app.UseHttpsRedirection();
app.UseRouting();


// Добавляем использование аутентификации
app.UseAuthentication();
app.UseAuthorization();

app.UseEndpoints(endpoints =>
{
    endpoints.MapControllers();
});

app.Run();