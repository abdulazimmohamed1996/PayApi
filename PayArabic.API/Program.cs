
using Microsoft.OpenApi.Models;

using Swashbuckle.AspNetCore.SwaggerUI;

var builder = WebApplication.CreateBuilder(args);

//Zein-1: clear all logging provider that createBuilder added them automatically
//and they are (console, debug, eventsource, eventlog (windows only))
builder.Logging.ClearProviders();
//Zein-2: Add console provider logging only
builder.Logging.AddConsole();

//Zein-3: Start Adding services to the container

//Zein-4: Add controllers
// Add authorization filter to validate tokens 
builder.Services.AddControllers(options => options.Filters.Add<PayArabicAuthorizationFilter>());
// Add model state filter to validate that the incomming object is valid
builder.Services.AddControllers(options => options.Filters.Add<ValidateModelStateFilter>());
// Add auditing filter to audit any method that needed to be audited
builder.Services.AddControllers(options => options.Filters.Add<PayArabicAuditFilter>());

//Zein: Add custom service that register DI for all DAO
builder.Services.Startup(builder.Configuration);
builder.Services.AddJWTTokenServices(builder.Configuration);

//Zein: Add Swagger Generator
builder.Services.AddEndpointsApiExplorer();

var xmlFile = $"{System.Reflection.Assembly.GetExecutingAssembly().GetName().Name}.xml";
var xmlPath = Path.Combine(AppContext.BaseDirectory, xmlFile);

builder.Services.AddSwaggerGen(options =>
{
    options.SwaggerDoc("PayArabicAPI", new OpenApiInfo
    {
        Title = "PayArabic API",
        Version = "v1",
        Description = "PayArabic API",
        Contact = new OpenApiContact()
        {
            Email = "info@payarabic.com",
            Name = "PayArabic",
            Url = new Uri(AppSettings.Instance.PayArabicURL)
        },
        License = new OpenApiLicense()
        {
            Name = "PayArabic License",
            Url = new Uri(AppSettings.Instance.PayArabicURL)
        },
    });
    // this is to enable authorization using Swagger (JWT)  
    options.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme
    {
        Scheme = "Bearer",
        BearerFormat = "JWT",
        In = ParameterLocation.Header,
        Name = "Authorization",
        Description = "JWT Authorization header using the Bearer scheme. \r\n\r\n Enter 'Bearer' [space] and then your token in the text input below.\r\n\r\nExample: \"Bearer 12345abcdef\"",
        Type = SecuritySchemeType.Http
    });
    options.AddSecurityRequirement(new OpenApiSecurityRequirement
    {
        {
            new OpenApiSecurityScheme
            {
                Reference = new OpenApiReference
                {
                    Id = "Bearer",
                    Type = ReferenceType.SecurityScheme
                }
            },
            new string[]{ }
        },
    });
    options.IncludeXmlComments(xmlPath, includeControllerXmlComments: true);
});

//Zein-7: Add authorization
builder.Services.AddAuthorization();



//Zein: Add session support to be able to block IP address that make repeated requests
builder.Services.AddDistributedMemoryCache();
builder.Services.AddSession();

//Zein-11: Enable cors
builder.Services.AddCors();

var app = builder.Build();

// Configure the HTTP request pipeline.

if (app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/error-dev"); //There should be an endpoint with route "error-dev" (it exists in common controller)    
}
else
{
    app.UseExceptionHandler("/error");//There should be an endpoint with route "error" (it exists in common controller)
}

app.UseSwagger();
app.UseSwaggerUI(options =>
{
    options.SwaggerEndpoint("/swagger/PayArabicAPI/swagger.json", "PayArabic API V1");
    options.RoutePrefix = "swagger";
    options.DocumentTitle = "PayArabic API";
    options.InjectStylesheet("/css/swagger.css");
    options.DocExpansion(DocExpansion.None);
});



app.UseCors(options => options.WithOrigins(AppSettings.Instance.PayArabicUI).AllowAnyMethod().AllowAnyHeader());
app.UseSession();
app.UseHttpsRedirection();
app.UseStaticFiles();
app.UseAuthentication();
app.UseAuthorization();
app.MapControllers();
app.Run();