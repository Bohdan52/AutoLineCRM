var builder = WebApplication.CreateBuilder(args);

// Додаємо YARP і кажемо йому читати налаштування з appsettings.json
builder.Services.AddReverseProxy()
    .LoadFromConfig(builder.Configuration.GetSection("ReverseProxy"));

// Налаштовуємо CORS для шлюзу
builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowAll", policy => policy.AllowAnyOrigin().AllowAnyMethod().AllowAnyHeader());
});

var app = builder.Build();

app.UseCors("AllowAll");

// Вмикаємо маршрутизацію
app.MapReverseProxy();

app.Run();