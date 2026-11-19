using System.Text;
using CorrespondenceApi.Common;
using CorrespondenceApi.Features.Access;
using CorrespondenceApi.Features.Auth;
using CorrespondenceApi.Features.Correspondents;
using CorrespondenceApi.Features.Departments;
using CorrespondenceApi.Features.Dictionaries;
using CorrespondenceApi.Features.Documents;
using CorrespondenceApi.Features.Employees;
using CorrespondenceApi.Features.Home;
using CorrespondenceApi.Features.Lookups;
using CorrespondenceApi.Features.Organizations;
using CorrespondenceApi.Features.Reports;
using CorrespondenceApi.Features.Settings;
using CorrespondenceApi.Features.Tasks;
using CorrespondenceApi.Features.Users;
using CorrespondenceApi.Features.Attachments;
using CorrespondenceApi.Infrastructure.Cors;
using CorrespondenceApi.Infrastructure.DataService;
using CorrespondenceApi.Infrastructure.Storage;
using CorrespondenceSystem.Contracts.Common;
using FluentValidation;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Authorization;
using Microsoft.IdentityModel.Tokens;

var builder = WebApplication.CreateBuilder(args);

// .NET Aspire: телеметрія, health checks, service discovery, стійкість HTTP
builder.AddServiceDefaults();

// ---- Доступ до даних: окремий сервіс даних (Data API) — у backend немає підключення до БД ----
// Запити MediatR із Contracts пересилаються в dataservice (POST /rpc/{операція}).
builder.Services.AddHttpContextAccessor();
builder.Services.AddDataApi(builder.Configuration);

// ---- Файлове сховище вкладень: MinIO / S3 (рядок підключення "minio" — з Aspire AppHost) ----
var storageOptions = builder.Configuration.GetSection("FileStorage").Get<FileStorageOptions>() ?? new FileStorageOptions();
storageOptions.ApplyConnectionString(builder.Configuration.GetConnectionString(FileStorageOptions.ConnectionStringName));
builder.Services.AddSingleton(storageOptions);
builder.Services.AddSingleton<IFileStorage, S3FileStorage>();
builder.Services.AddHealthChecks().AddCheck<FileStorageHealthCheck>("file-storage", tags: ["ready"]);

// Локальні хендлери backend (вхід, вкладення) + валідація запитів до пересилання в сервіс даних
builder.Services.AddMediatR(cfg =>
{
    cfg.RegisterServicesFromAssembly(typeof(Program).Assembly);
    cfg.TypeEvaluator = t => !t.IsGenericTypeDefinition; // пересилачі Data API реєструє AddDataApi
    cfg.AddOpenBehavior(typeof(ValidationBehavior<,>));
});
builder.Services.AddValidatorsFromAssembly(typeof(Program).Assembly);
builder.Services.AddValidatorsFromAssembly(typeof(DataApi).Assembly);

builder.Services.AddExceptionHandler<GlobalExceptionHandler>();
builder.Services.AddProblemDetails();

// ---- Авторизація / автентифікація ----
builder.Services.AddScoped<TokenService>();
builder.Services.AddSingleton<IAuthorizationPolicyProvider, PermissionPolicyProvider>();
builder.Services.AddScoped<IAuthorizationHandler, PermissionHandler>();
// Динамічні права: пермішени вантажаться із сервісу даних на кожен запит
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

// ---- CORS для фронтенду (секція "Cors" в appsettings.json) ----
builder.Services.AddFrontendCors(builder.Configuration);

var app = builder.Build();

app.MapDefaultEndpoints(); // /health, /alive (Aspire)

app.UseExceptionHandler();
app.UseFrontendCors();
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
AttachmentsEndpoints.MapAttachments(app);
CorrespondentsEndpoints.MapCorrespondents(app);
DepartmentsEndpoints.MapDepartments(app);
EmployeesEndpoints.MapEmployees(app);
TasksEndpoints.MapTasks(app);
DictionariesEndpoints.MapDictionaries(app);
ReportsEndpoints.MapReports(app);
HomeEndpoints.MapHome(app);

app.Run();
