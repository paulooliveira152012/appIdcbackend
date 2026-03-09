using BibliaStudy.Api.Data;
using DotNetEnv;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using System.Text;

var builder = WebApplication.CreateBuilder(args);

// carregar variáveis do arquivo .env
Env.Load();

// pegar variáveis de ambiente
var connectionString = Environment.GetEnvironmentVariable("NEON_DB_CONNECTION_STRING");
var jwtKey = Environment.GetEnvironmentVariable("JWT_KEY");

// validar se as variáveis existem
if (string.IsNullOrWhiteSpace(connectionString))
{
    throw new Exception("A variável de ambiente NEON_DB_CONNECTION_STRING não foi encontrada.");
}

if (string.IsNullOrWhiteSpace(jwtKey))
{
    throw new Exception("A variável de ambiente JWT_KEY não foi encontrada.");
}

// registrar controllers
builder.Services.AddControllers();

// registrar OpenAPI
builder.Services.AddOpenApi();

// registrar CORS
builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowFrontend", policy =>
    {
        policy.WithOrigins("http://localhost:3001", "https://bible-sepia.vercel.app/")
              .AllowAnyHeader()
              .AllowAnyMethod();
    });
});

// registrar banco de dados PostgreSQL
builder.Services.AddDbContext<AppDbContext>(options =>
    options.UseNpgsql(connectionString));

// registrar autenticação JWT
builder.Services.AddAuthentication(options =>
{
    options.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
    options.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
})
.AddJwtBearer(options =>
{
    options.TokenValidationParameters = new TokenValidationParameters
    {
        ValidateIssuer = false,
        ValidateAudience = false,
        ValidateIssuerSigningKey = true,
        ValidateLifetime = true,
        IssuerSigningKey = new SymmetricSecurityKey(
            Encoding.UTF8.GetBytes(jwtKey)
        ),
        ClockSkew = TimeSpan.Zero
    };
});

// registrar autorização
builder.Services.AddAuthorization();

var app = builder.Build();

// habilitar OpenAPI em desenvolvimento
if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
}

// habilitar CORS
app.UseCors("AllowFrontend");

// autenticação e autorização
app.UseAuthentication();
app.UseAuthorization();

// rotas
app.MapControllers();

// rota de teste
app.MapGet("/", () => "API raiz funcionando");

app.Run();