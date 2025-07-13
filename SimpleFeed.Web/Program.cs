using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using SimpleFeed.Application.Interfaces;
using SimpleFeed.Application.Services;
using SimpleFeed.Domain.Entities;
using SimpleFeed.Infrastructure.Persistence;
using SimpleFeed.Infrastructure.Repositories;
using SimpleFeed.Web.Models;
using System.Text;

var builder = WebApplication.CreateBuilder(args);

// Configurar a porta corretamente
// var port = Environment.GetEnvironmentVariable("PORT") ?? "8080";
// builder.WebHost.UseUrls($"http://*:{port}");

// Configurar o banco de dados
var environment = Environment.GetEnvironmentVariable("ASPNETCORE_ENVIRONMENT");

if (!string.IsNullOrEmpty(environment) && environment.Equals("Development", StringComparison.OrdinalIgnoreCase))
{
    builder.Services.AddDbContext<ApplicationDbContext>(options =>
        options.UseNpgsql(Environment.GetEnvironmentVariable("CONNECTION_STRING_DEV")));

    builder.Services.AddSingleton(provider =>
        Environment.GetEnvironmentVariable("CONNECTION_STRING_DEV"));
}
else
{
    builder.Services.AddDbContext<ApplicationDbContext>(options =>
        options.UseNpgsql(Environment.GetEnvironmentVariable("CONNECTION_STRING_PROD")));

    builder.Services.AddSingleton(provider =>
        Environment.GetEnvironmentVariable("CONNECTION_STRING_PROD"));
}

builder.Services.Configure<OpenAiSettings>(options =>
{
    options.ApiKey = Environment.GetEnvironmentVariable("API_KEY_OPEN_AI");
});
// Configurar o Identity
builder.Services.AddIdentity<ApplicationUser, IdentityRole>()
    .AddEntityFrameworkStores<ApplicationDbContext>()
    .AddDefaultTokenProviders();

// Configurar autenticação JWT
var key = Encoding.UTF8.GetBytes(builder.Configuration["JwtSettings:Secret"]);

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
        ValidIssuer = builder.Configuration["JwtSettings:Issuer"],
        ValidAudience = builder.Configuration["JwtSettings:Audience"],
        IssuerSigningKey = new SymmetricSecurityKey(key)
    };
});

// Configurar tempo de expiração de sessões
builder.Services.ConfigureApplicationCookie(options =>
{
    options.ExpireTimeSpan = TimeSpan.FromMinutes(60);
    options.SlidingExpiration = true;
    options.Events.OnRedirectToLogin = context =>
    {
        context.Response.StatusCode = 401; // Retorna status HTTP 401 em vez de redirecionar
        return Task.CompletedTask;
    };
});

builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowFrontend", builder =>
    {
        builder.WithOrigins(
          "https://app.simplefeed.com.br",
          "http://localhost:4200",
          "https://simple-feed-front-git-develop-felipes-projects-b3f64dcf.vercel.app"
        )
        .AllowAnyMethod()
        .AllowAnyHeader();
    });
});

builder.Services.Configure<IdentityOptions>(options =>
{
    options.Password.RequireDigit = false; // Não exige números
    options.Password.RequireNonAlphanumeric = false; // Não exige caracteres especiais
    options.Password.RequireUppercase = false; // Não exige letras maiúsculas
    options.Password.RequiredLength = 6; // Mantém o comprimento mínimo de 6 caracteres
    options.Password.RequireLowercase = false; // Permitir senhas sem letras minúsculas
    options.Password.RequiredUniqueChars = 1; // Exige pelo menos 1 caractere único
});

// Configurar a política de cookies
builder.Services.Configure<CookiePolicyOptions>(options =>
{
    options.CheckConsentNeeded = context => false; // Não exige consentimento do usuário
    options.MinimumSameSitePolicy = SameSiteMode.None; // Permitir Cross-Origin
});


// Registrar repositórios
builder.Services.AddScoped<IFormRepository, FormRepository>();
builder.Services.AddScoped<IFeedbackRepository, FeedbackRepository>();
builder.Services.AddScoped<IFieldTypeRepository, FieldTypeRepository>();
builder.Services.AddScoped<IClientRepository, ClientRepository>();
builder.Services.AddScoped<ITemplateRepository, TemplateRepository>();
builder.Services.AddScoped<IClientRepository, ClientRepository>();
builder.Services.AddScoped<IFeedbackFormRepository, FeedbackFormRepository>();
builder.Services.AddScoped<IAccountRepository, AccountRepository>();
builder.Services.AddScoped<IFormStyleRepository, FormStyleRepository>();
builder.Services.AddScoped<IActionsLogRepository, ActionsLogRepository>();
builder.Services.AddScoped<IBillingSummaryRepository, BillingSummaryRepository>();
builder.Services.AddScoped<IPlansRepository, PlansRepository>();
builder.Services.AddScoped<IReportsRepository, ReportsRepository>();



// Registrar serviços
builder.Services.AddScoped<FormService>();
builder.Services.AddScoped<FeedbackService>();
builder.Services.AddScoped<FieldTypeService>();
builder.Services.AddScoped<ClientService>();
builder.Services.AddScoped<TemplateService>();
builder.Services.AddScoped<ClientService>();
builder.Services.AddScoped<FeedbackFormService>();
builder.Services.AddScoped<AccountService>();
builder.Services.AddScoped<ActionsLogService>();
builder.Services.AddScoped<BillingSummaryService>();
builder.Services.AddScoped<PlansService>();
builder.Services.AddHttpClient<ReportsService>();

builder.Services.AddControllers();

builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(c =>
{
    c.SwaggerDoc("v1", new Microsoft.OpenApi.Models.OpenApiInfo
    {
        Title = "SimpleFeed API",
        Version = "v1"
    });

    // Configurar suporte para JWT
    c.AddSecurityDefinition("Bearer", new Microsoft.OpenApi.Models.OpenApiSecurityScheme
    {
        Name = "Authorization",
        Type = Microsoft.OpenApi.Models.SecuritySchemeType.Http,
        Scheme = "Bearer",
        BearerFormat = "JWT",
        In = Microsoft.OpenApi.Models.ParameterLocation.Header,
        Description = "Insira o token JWT no formato: Bearer {seu_token}"
    });

    c.AddSecurityRequirement(new Microsoft.OpenApi.Models.OpenApiSecurityRequirement
    {
        {
            new Microsoft.OpenApi.Models.OpenApiSecurityScheme
            {
                Reference = new Microsoft.OpenApi.Models.OpenApiReference
                {
                    Type = Microsoft.OpenApi.Models.ReferenceType.SecurityScheme,
                    Id = "Bearer"
                }
            },
            new string[] {}
        }
    });
});

var app = builder.Build();

app.UseSwagger();
app.UseSwaggerUI(c =>
{
    c.SwaggerEndpoint("/swagger/v1/swagger.json", "SimpleFeed API v1");
    c.RoutePrefix = "swagger";
});

app.UseCors("AllowFrontend");
app.UseRouting();
app.UseCookiePolicy(); // Aplica a política de cookies

app.UseAuthorization();
app.MapControllers();

app.Run();
