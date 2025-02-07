using System.Reflection;
using Microsoft.AspNetCore.Mvc;
using Data.AppDbContext;
using Microsoft.EntityFrameworkCore;

var builder = WebApplication.CreateBuilder(args);

var connectionString = builder.Configuration.GetConnectionString("M8Dev");
builder.Services.AddDbContext<AppDbContext>(options =>
    options.UseNpgsql(connectionString),
    ServiceLifetime.Singleton);

builder.Services.AddControllers(options =>
{
    // define application/json como o tipo de conteúdo padrão
    options.Filters.Add(new ProducesAttribute("application/json"));
});

builder.Services.AddCors(p => p.AddPolicy("corsapp", builder =>
{
    builder.WithOrigins("*").AllowAnyMethod().AllowAnyHeader();
}));

builder.Services.AddSwaggerGen(c =>
{
    string ambientName = Environment.GetEnvironmentVariable("ASPNETCORE_ENVIRONMENT") == "Development" ? "Desenvolvimento" : "Produção";

    c.SwaggerDoc("v1", new()
    {
        Title = $"Painel de Utilidades (.NET 8.0) - Ambiente de {ambientName}",
        Description = "API para o Painel de Utilidades 🛠️",
        Version = "v1"
    });

    // expõe os comentários XML das controllers        
    c.IncludeXmlComments(Path.Combine(AppContext.BaseDirectory, $"{Assembly.GetExecutingAssembly().GetName().Name}.xml"), includeControllerXmlComments: true);
});

var app = builder.Build();

app.UseSwagger();
app.UseSwaggerUI(options =>
{
    options.SwaggerEndpoint("/swagger/v1/swagger.json", "v1");
    // elimina a necessidade de adicionar o prefixo /swagger
    options.RoutePrefix = string.Empty;
});

app.UseCors("corsapp");

app.UseHttpsRedirection();

app.MapControllers();

app.Run();