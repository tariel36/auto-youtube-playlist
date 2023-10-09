using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.ObjectModel;
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

        private async void DeleteUrl_Click(object sender, RoutedEventArgs args)
        {
            await DeleteData<YouTubeRssUrl>(args, DgrUrls);
        }

        private async void DeleteVideo_Click(object sender, RoutedEventArgs args)
        {
            await DeleteData<YouTubeVideo>(args, DgrVideos);
        }

        private async void DeletePlaylist_Click(object sender, RoutedEventArgs args)
        {
            await DeleteData<YouTubePlaylist>(args, DgrPlaylists);
        }

        private async Task RefreshData<TModel>(DataGrid dgr, Label lbl) where TModel : IDatabaseEntity
        {
            dgr.ItemsSource = new ObservableCollection<DataItem>((await _databaseService.GetAll<TModel>()).Select(x => new DataItem(JsonConvert.SerializeObject(x), x.Id, x.Added) { Value = x is YouTubeRssUrl rss ? rss.Url : null }).ToList());
            lbl.Content = (dgr.ItemsSource as ICollection)?.Count ?? 0;
        }

        private async Task DeleteData<TType>(RoutedEventArgs args, DataGrid dgr)
            where TType : IDatabaseEntity
        {
            if (!((args.Source as Button)?.DataContext is DataItem dataItem))
            {
                return;
            }

            await _databaseService.Delete<TType>(dataItem.Id);

            (dgr.ItemsSource as ObservableCollection<DataItem>)?.Remove(dataItem);
        }
    }
}
