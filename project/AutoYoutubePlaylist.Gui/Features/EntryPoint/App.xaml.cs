using System.IO;
using System.Windows;
using Microsoft.Extensions.Configuration;

namespace AutoYoutubePlaylist.Gui.Features.EntryPoint
{
    public partial class App
    {
        private const string ConfigurationFileName = "appsettings.json";

        public static IConfiguration Configuration { get; private set; } = null!;

        public void Application_Startup(object sender, StartupEventArgs args)
        {
            Configuration = new ConfigurationBuilder()
              .SetBasePath(Directory.GetCurrentDirectory())
              .AddJsonFile(ConfigurationFileName, optional: false, reloadOnChange: true)
              .Build();
        }
    }
}
