using AutoYoutubePlaylist.Logic.Features.Playlists.Services;
using Microsoft.AspNetCore.Mvc;

namespace AutoYoutubePlaylist.Logic.Features.Playlists.Controllers
{
    [ApiController]
    [Route("playlist")]
    public class PlaylistController
        : Controller
    {
        private readonly IPlaylistService _playlistService;

        public PlaylistController(IPlaylistService playlistService)
        {
            _playlistService = playlistService ?? throw new ArgumentNullException(nameof(playlistService));
        }

        [HttpGet("latest")]
        public async Task<IActionResult> GetLatestPlaylist()
        {
            return Ok(await _playlistService.GetLatestPlaylist());
        }

        [HttpGet("all")]
        public async Task<IActionResult> GetAllPlaylists()
        {
            return Ok(await _playlistService.GetAllPlaylists());
        }

        [HttpPost("trigger-create")]
        public async Task<IActionResult> TriggerPlaylistCreation()
        {
            return Ok(await _playlistService.TriggerPlaylistCreation());
        }

        [HttpPost("add-channel")]
        public async Task<IActionResult> AddChannel([FromBody] string url)
        {
            await _playlistService.AddChannel(url);

            return Ok();
        }

        [HttpDelete("delete-all")]
        public async Task<IActionResult> DeleteAllPlaylists()
        {
            await _playlistService.DeleteAllPlaylists();

            return Ok();
        }
    }
}
