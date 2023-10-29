using System.IO;
using System.Windows;
using Microsoft.Extensions.Configuration;

namespace AutoYoutubePlaylist.Gui.Features.EntryPoint
{
    /// <summary>
    /// Interaction logic for App.xaml
    /// </summary>
    public partial class App : Application
    {
        public static IConfiguration Configuration { get; private set; } 

        public void Application_Startup(object sender, StartupEventArgs args)
        {
            Configuration = new ConfigurationBuilder()
              .SetBasePath(Directory.GetCurrentDirectory())
              .AddJsonFile("appsettings.json", optional: false, reloadOnChange: true)
              .Build();
        }
    }
}
