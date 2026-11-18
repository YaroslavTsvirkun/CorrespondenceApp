using System.Text;
using CorrespondenceApi.Common;
using CorrespondenceApi.Features.Access;
using CorrespondenceApi.Features.Auth;
using CorrespondenceApi.Features.Correspondents;
using CorrespondenceApi.Features.Departments;
using CorrespondenceApi.Features.Dictionaries;
using CorrespondenceApi.Features.Documents;
using CorrespondenceApi.Features.Employees;
using CorrespondenceApi.Features.Lookups;
using CorrespondenceApi.Features.Organizations;
using CorrespondenceApi.Features.Reports;
using CorrespondenceApi.Features.Settings;
using CorrespondenceApi.Features.Tasks;
using CorrespondenceApi.Features.Users;
using CorrespondenceApi.Infrastructure.Database;
using FluentValidation;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Authorization;
using Microsoft.IdentityModel.Tokens;

var builder = WebApplication.CreateBuilder(args);

// .NET Aspire: телеметрія, health checks, service discovery, стійкість HTTP
builder.AddServiceDefaults();

// ---- Доступ до даних: Dapper + SQL-код (функції, процедури, тригери) у БД ----
var dbOptions = builder.Configuration.GetSection("Database").Get<DatabaseOptions>() ?? new DatabaseOptions();
dbOptions.ConnectionString = builder.Configuration.GetConnectionString("Default") ?? dbOptions.ConnectionString;
builder.Services.AddSingleton(dbOptions);
builder.Services.AddSingleton<SqlScriptDeployer>();
builder.Services.AddScoped<Db>();
DapperConfig.Configure();

builder.Services.AddMediatR(cfg =>
{
    cfg.RegisterServicesFromAssembly(typeof(Program).Assembly);
    cfg.AddOpenBehavior(typeof(ValidationBehavior<,>));
});
builder.Services.AddValidatorsFromAssembly(typeof(Program).Assembly);

builder.Services.AddExceptionHandler<GlobalExceptionHandler>();
builder.Services.AddProblemDetails();

// ---- Авторизація / автентифікація ----
builder.Services.AddHttpContextAccessor();
builder.Services.AddScoped<ICurrentUser, CurrentUser>();
builder.Services.AddScoped<TokenService>();
builder.Services.AddSingleton<IAuthorizationPolicyProvider, PermissionPolicyProvider>();
builder.Services.AddScoped<IAuthorizationHandler, PermissionHandler>();
// Динамічні права: пермішени вантажаться з БД на кожен запит
builder.Services.AddScoped<IClaimsTransformation, PermissionClaimsTransformation>();
builder.Services.AddAuthorization();

var jwtKey = builder.Configuration["Jwt:Key"]!;
builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(options =>
    {
        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuer = true,
            ValidIssuer = builder.Configuration["Jwt:Issuer"],
            ValidateAudience = true,
            ValidAudience = builder.Configuration["Jwt:Audience"],
            ValidateIssuerSigningKey = true,
            IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtKey)),
            ValidateLifetime = true,
        };
    });

builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

const string CorsPolicy = "frontend";
builder.Services.AddCors(o => o.AddPolicy(CorsPolicy, p => p
    .WithOrigins("http://localhost:5180", "http://localhost:5173")
    .AllowAnyHeader()
    .AllowAnyMethod()));

var app = builder.Build();

app.MapDefaultEndpoints(); // /health, /alive (Aspire)

app.UseExceptionHandler();
app.UseCors(CorsPolicy);
app.UseAuthentication();
app.UseAuthorization();

app.UseSwagger();
app.UseSwaggerUI();

// Vertical slices
AuthEndpoints.MapAuth(app);
AccessEndpoints.MapAccess(app);
OrganizationsEndpoints.MapOrganizations(app);
UsersEndpoints.MapUsers(app);
SettingsEndpoints.MapSettings(app);
LookupsEndpoints.MapLookups(app);
DocumentsEndpoints.MapDocuments(app);
CorrespondentsEndpoints.MapCorrespondents(app);
DepartmentsEndpoints.MapDepartments(app);
EmployeesEndpoints.MapEmployees(app);
TasksEndpoints.MapTasks(app);
DictionariesEndpoints.MapDictionaries(app);
ReportsEndpoints.MapReports(app);

app.MapGet("/", () => Results.Redirect("/swagger")).AllowAnonymous();

// Автоматичне розгортання SQL-коду з директорії Database/ (схема, функції, процедури, тригери, дані).
// `dotnet run -- --deploy-db` — лише розгорнути БД і завершити роботу.
var deployOnly = args.Contains("--deploy-db");
if (dbOptions.DeployOnStartup || deployOnly)
    await app.Services.GetRequiredService<SqlScriptDeployer>().DeployAsync();
if (deployOnly) return;

app.Run();
