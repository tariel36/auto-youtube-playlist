using AutoYoutubePlaylist.Logic.Features.Chrono.Providers;
using AutoYoutubePlaylist.Logic.Features.Database.Services;
using AutoYoutubePlaylist.Logic.Features.Playlists.Services;
using AutoYoutubePlaylist.Logic.Features.YouTube.Services;

WebApplicationBuilder builder = WebApplication.CreateBuilder(args);

builder.Services.AddSingleton<IDateTimeProvider, DateTimeProvider>();
builder.Services.AddSingleton<IDatabaseService, DatabaseService>();
builder.Services.AddSingleton<IPlaylistService, PlaylistService>();
builder.Services.AddSingleton<IYouTubeService, YouTubeService>();

builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

WebApplication app = builder.Build();

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseHttpsRedirection();

app.UseAuthorization();

app.MapControllers();

app.Run();
