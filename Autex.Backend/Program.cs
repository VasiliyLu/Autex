using Autex.Backend.DAL;
using Autex.Backend.TextViewer;
using Autex.Backend.Transcriber;
using MassTransit;
using Microsoft.AspNetCore.Authentication.JwtBearer;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services.AddControllers();
//builder.Services.AddCors();
// Learn more about configuring Swagger/OpenAPI at https://aka.ms/aspnetcore/swashbuckle
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

builder.Services.AddDbContext<AutexContext>();

builder.Services.AddAuthentication(options =>
{
    options.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
    options.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
    options.DefaultScheme = JwtBearerDefaults.AuthenticationScheme;
}).AddJwtBearer();

builder.Services.AddAuthorization();

builder.Services.AddMassTransit(x =>
{
    x.AddConsumer<AudioMessageConsumer>();
    x.AddConsumer<TextEventConsumer>();
    x.UsingInMemory((context, cfg) => { cfg.ConfigureEndpoints(context); });
});

builder.Services.AddSingleton<SttServiceFactory>();
builder.Services.AddSingleton<WSManager>();
builder.Services.AddSingleton<SttServiceManager>();

// Configure FFMpeg
/*GlobalFFOptions.Configure(new FFOptions
{
    BinaryFolder = builder.Configuration["FFMpeg:BinaryFolder"]!,
    TemporaryFilesFolder = builder.Configuration["FFMpeg:TemporaryFilesFolder"]!,
});*/

var app = builder.Build();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
    
    /*app.UseCors(corsPolicyBuilder =>
{
    corsPolicyBuilder.WithOrigins("http://front")
        .AllowAnyHeader()
        .AllowAnyMethod()
        .AllowCredentials();
});*/
}

app.UseWebSockets(new WebSocketOptions
{
    KeepAliveInterval = TimeSpan.FromMinutes(2)
});

//app.UseHttpsRedirection();

app.UseAuthentication();
app.UseAuthorization();

app.MapControllers();

app.MapGet("/api/getMessage", (() => "Hello, world!"));

app.Run();