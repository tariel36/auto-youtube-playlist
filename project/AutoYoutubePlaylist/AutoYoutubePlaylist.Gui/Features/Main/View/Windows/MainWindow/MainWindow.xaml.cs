using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using AutoYoutubePlaylist.Gui.Features.Data.Models;
using AutoYoutubePlaylist.Logic.Features.Chrono.Providers;
using AutoYoutubePlaylist.Logic.Features.Database.Models;
using AutoYoutubePlaylist.Logic.Features.Database.Services;
using AutoYoutubePlaylist.Logic.Features.YouTube.Models;
using Newtonsoft.Json;

namespace AutoYoutubePlaylist.Gui
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        private readonly IDatabaseService _databaseService;

        public MainWindow()
        {
            InitializeComponent();

            _databaseService = new DatabaseService(Features.EntryPoint.App.Configuration, new DateTimeProvider());
        }

        private async void BtnAddChannels_Click(object sender, RoutedEventArgs args)
        {
            ICollection<string> urls = TbxChannels.Text.Split(new[] { "\r\n", "\n" }, StringSplitOptions.RemoveEmptyEntries)
                .Where(x => !string.IsNullOrWhiteSpace(x))
                .ToList();

            HashSet<string> existingUrls = (await _databaseService.GetAll<YouTubeRssUrl>()).Select(x => x.Url).ToHashSet();

            foreach (string url in urls)
            {
                string actualUrl = url.StartsWith("http") ? url : $"https://www.youtube.com/feeds/videos.xml?channel_id={url}";

                if (existingUrls.Contains(actualUrl))
                {
                    continue;
                }

                await _databaseService.Insert(new YouTubeRssUrl()
                {
                    Url = actualUrl,
                });
            }
        }

        private async void BtnRefreshUrls_Click(object sender, RoutedEventArgs args)
        {
            await RefreshData<YouTubeRssUrl>(DgrUrls, LblRefreshUrlsCount);
        }

        private async void BtnRefreshVideos_Click(object sender, RoutedEventArgs args)
        {
            await RefreshData<YouTubeVideo>(DgrVideos, LblRefreshVideosCount);
        }

        private async void BtnRefreshPlaylists_Click(object sender, RoutedEventArgs args)
        {
            await RefreshData<YouTubePlaylist>(DgrPlaylists, LblRefreshPlaylistsCount);
        }

        private async Task RefreshData<TModel>(DataGrid dgr, Label lbl) where TModel : IDatabaseEntity
        {
            dgr.ItemsSource = (await _databaseService.GetAll<TModel>()).Select(x => JsonConvert.SerializeObject(x)).Select(x => new DataItem(x)).ToList();
            lbl.Content = (dgr.ItemsSource as ICollection)?.Count ?? 0;
        }
    }
}
