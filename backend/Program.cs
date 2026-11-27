using CorrespondenceApi.Common;
using CorrespondenceApi.Features.Access;
using CorrespondenceApi.Features.Auth;
using CorrespondenceApi.Features.Parties;
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
using CorrespondenceApi.Infrastructure.Conversion;
using CorrespondenceApi.Infrastructure.Cors;
using CorrespondenceApi.Infrastructure.Editor;
using CorrespondenceApi.Infrastructure.DataService;
using CorrespondenceApi.Infrastructure.Storage;
using CorrespondenceSystem.Contracts.Common;
using FluentValidation;
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

// ---- Перегляд вкладень: конвертація DOC/DOCX/XLS/XLSX… у PDF (Gotenberg з Aspire AppHost) ----
var conversionOptions = builder.Configuration.GetSection("DocumentConversion").Get<DocumentConversionOptions>()
                        ?? new DocumentConversionOptions();
#pragma warning disable EXTEXP0001 // RemoveAllResilienceHandlers — експериментальний API
builder.Services.AddHttpClient<IDocumentConverter, GotenbergDocumentConverter>(http =>
        http.BaseAddress = new Uri(conversionOptions.BaseUrl))
    .RemoveAllResilienceHandlers() // замість стандартного з ServiceDefaults (30 с, повтори)
    .AddStandardResilienceHandler(o =>
    {
        // Конвертація великих документів триває довше за стандартні 30 с; POST не повторюємо
        var timeout = TimeSpan.FromSeconds(conversionOptions.TimeoutSeconds);
        o.AttemptTimeout.Timeout = timeout;
        o.TotalRequestTimeout.Timeout = timeout + TimeSpan.FromSeconds(5);
        o.CircuitBreaker.SamplingDuration = timeout * 2;
        o.Retry.ShouldHandle = _ => ValueTask.FromResult(false);
    });
#pragma warning restore EXTEXP0001

// ---- Редагування документів Word: ONLYOFFICE Docs (адреса й секрет — з Aspire AppHost) ----
var editorOptions = builder.Configuration.GetSection("DocumentEditor").Get<DocumentEditorOptions>()
                    ?? new DocumentEditorOptions();
builder.Services.AddSingleton(editorOptions);
builder.Services.AddSingleton<OnlyOfficeJwt>();
builder.Services.AddSingleton<EditorLinkTokens>();
builder.Services.AddScoped<DocumentEditorService>();
builder.Services.AddScoped<AttachmentContentService>();
builder.Services.AddHttpClient("onlyoffice-download"); // збережений файл з Document Server

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
// Ключ підпису JWT: під Aspire — параметр jwt-key з AppHost, без Aspire — appsettings.Development.json.
// Без ключа (або з коротким) сервіс не стартує: інакше токени можна було б підробити.
var jwt = JwtOptions.Load(builder.Configuration);
builder.Services.AddSingleton(jwt);
builder.Services.AddSingleton<TokenService>();
builder.Services.AddSingleton<IAuthorizationPolicyProvider, PermissionPolicyProvider>();
builder.Services.AddScoped<IAuthorizationHandler, PermissionHandler>();
builder.Services.AddAuthorization();

builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(options =>
    {
        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuer = true,
            ValidIssuer = jwt.Issuer,
            ValidateAudience = true,
            ValidAudience = jwt.Audience,
            ValidateIssuerSigningKey = true,
            IssuerSigningKey = jwt.SigningKey,
            ValidateLifetime = true,
        };
        options.Events = new JwtBearerEvents
        {
            // Браузер передає JWT у HttpOnly cookie (див. AuthCookie); заголовок Authorization теж підтримується
            OnMessageReceived = AuthCookie.ReadToken,
            // Динамічні права: після перевірки підпису пермішени вантажаться із сервісу даних на кожен запит;
            // деактивований користувач або відкликаний токен (вихід) -> 401 (див. PermissionClaims)
            OnTokenValidated = PermissionClaims.OnTokenValidated,
        };
    });

// Захист від перебору паролів: ліміт спроб входу з однієї IP (+ блокування облікового запису в сервісі даних)
builder.Services.AddLoginRateLimiting(builder.Configuration);

// За зворотним проксі: IP клієнта й HTTPS — із X-Forwarded-* лише від довірених проксі (секція ForwardedHeaders)
var behindProxy = builder.Services.AddTrustedForwardedHeaders(builder.Configuration);

builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

// ---- CORS для фронтенду (секція "Cors" в appsettings.json) ----
builder.Services.AddFrontendCors(builder.Configuration);

var app = builder.Build();

if (behindProxy) app.UseForwardedHeaders(); // першим: решта middleware бачить справжні IP і схему

app.MapDefaultEndpoints(); // /health, /alive (Aspire)

app.UseExceptionHandler();
app.UseFrontendCors();
app.UseAuthentication();
app.UseAuthorization();
app.UseRateLimiter();

// Swagger (опис API) — лише для розробки
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
    HomeEndpoints.MapHome(app);
}

// Vertical slices
AuthEndpoints.MapAuth(app);
AccessEndpoints.MapAccess(app);
OrganizationsEndpoints.MapOrganizations(app);
UsersEndpoints.MapUsers(app);
SettingsEndpoints.MapSettings(app);
LookupsEndpoints.MapLookups(app);
DocumentsEndpoints.MapDocuments(app);
AttachmentsEndpoints.MapAttachments(app);
PartiesEndpoints.MapParties(app);
DepartmentsEndpoints.MapDepartments(app);
EmployeesEndpoints.MapEmployees(app);
TasksEndpoints.MapTasks(app);
DictionariesEndpoints.MapDictionaries(app);
ReportsEndpoints.MapReports(app);

app.Run();

/// <summary>Точка входу доступна інтеграційним тестам (WebApplicationFactory).</summary>
public partial class Program;
