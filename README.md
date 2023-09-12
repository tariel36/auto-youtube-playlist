# AutoYoutubePlaylist

Tool used to create daily playlist based on provided channels' rss.

## Used libraries

* [Google.Apis.YouTube.v3](https://www.nuget.org/packages/Google.Apis.YouTube.v3);
* [LiteDB](https://github.com/mbdavid/LiteDB);
* [CodeHollow.FeedReader](https://github.com/arminreiter/FeedReader);

## Starting the project

* Copy `appsettings.Template.json` and rename it to `appsettings.json` in `Logic` project;
* Fill the `appsettings` properties as necessary;

## Configuration

The `appsettings.json` file.

| Config key | Example value | Description |
|------------|---------------|-------------|
| `Logging->LogLevel->Default` | `Trace` | Default log level. |
| `Logging->LogLevel->Microsoft.AspNetCore` | `Trace` | Log level for AspNetCore. |
| `AllowedHosts` | `*` | List of allowed hosts. |
| `ConnectionString` | `db.bin` | Connection string for LiteDB. |
| `YouTubeUser` | `user` | Name of YouTube user to create playlists for. |
| `ClientSecretsFilePath` | `secrets.json` | Path to file with YT api secrets. |
| `BrowserPath` | `C:\\Program Files\\Vivaldi\\Application\\vivaldi.exe` | Path to browser that's opened with new playlist. |
| `OpenPlaylist` | `1` | Indicates whether new playlist should be opened - `1` for yes, other value for no. |

**Besides the config file, proper OAuth credentials should be created in Google Console.**

## Projects

* `AutoYoutubePlaylist.Console` - The playlist creation console app;
* `AutoYoutubePlaylist.Gui` - Simple GUI to add channels to database and display data within database;
* `AutoYoutubePlaylist.Service` - API service for managing playlist and database;
* `AutoYoutubePlaylist.Logic` - Most of the app logic;

## License

MTI
