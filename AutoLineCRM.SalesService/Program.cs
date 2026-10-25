using AutoLineCRM.SalesService.Data;
using AutoLineCRM.SalesService.Services;
using Microsoft.EntityFrameworkCore;

var builder = WebApplication.CreateBuilder(args);

// 1. Підключення до бази даних
builder.Services.AddDbContext<SalesDbContext>(o =>
    o.UseNpgsql(builder.Configuration.GetConnectionString("SalesDb")));

// 2. Реєстрація нашого сервісу (саме цього не вистачало!)
builder.Services.AddScoped<RequestService>();

builder.Services.AddControllers();
builder.Services.AddOpenApi();

// 3. Налаштування CORS
builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowAll", policy => policy.AllowAnyOrigin().AllowAnyMethod().AllowAnyHeader());
});

var app = builder.Build();

// 4. Автоматичне застосування міграцій при запуску
using (var scope = app.Services.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<SalesDbContext>();
    db.Database.Migrate();
}

if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
}

app.UseCors("AllowAll"); // CORS обов'язково має бути ПЕРЕД авторизацією та контролерами!
app.UseHttpsRedirection();
app.UseAuthorization();
app.MapControllers();

app.Run();