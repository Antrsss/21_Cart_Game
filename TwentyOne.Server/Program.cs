using TwentyOne.Server.GameLogic;
using TwentyOne.Server.Hubs;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container
builder.Services.AddSignalR();
builder.Services.AddSingleton<GameManager>();
builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowBlazorClient", policy =>
    {
        policy.WithOrigins("http://localhost:5002")
              .AllowAnyHeader()
              .AllowAnyMethod()
              .AllowCredentials();
    });
});

var app = builder.Build();

// Configure the HTTP request pipeline
app.UseCors("AllowBlazorClient");

// Map SignalR hub
app.MapHub<GameHub>("/gamehub");

app.MapGet("/", () => "Twenty One Game Server is running.");

app.Run();

