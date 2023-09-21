using AutoYoutubePlaylist.Logic.Features.Chrono.Providers;
using AutoYoutubePlaylist.Logic.Features.Configuration;
using AutoYoutubePlaylist.Logic.Features.Database.Models;
using LiteDB;
using Microsoft.Extensions.Configuration;

namespace AutoYoutubePlaylist.Logic.Features.Database.Services
{
    public class DatabaseService
        : IDatabaseService
    {
        private readonly IDateTimeProvider _dateTimeProvider;
        private readonly LiteDatabase _db;

        public DatabaseService(IConfiguration configuration, IDateTimeProvider dateTimeProvider)
        {
            configuration = configuration ?? throw new ArgumentNullException(nameof(configuration));
            _dateTimeProvider = dateTimeProvider ?? throw new ArgumentNullException(nameof(dateTimeProvider));

            _db = new LiteDatabase(configuration[ConfigurationKeys.ConnectionString]);
        }

        public async Task Delete<TModel>(Guid id) where TModel : IDatabaseEntity
        {
            await Task.Factory.StartNew(() => { _db.GetCollection<TModel>().Delete(id); });
        }

        public async Task Delete<TModel>(TModel model) where TModel : IDatabaseEntity
        {
            await Task.Factory.StartNew(() => { _db.GetCollection<TModel>().Delete(model.Id); });
        }

        public async Task DeleteAll<TModel>() where TModel : IDatabaseEntity
        {
            await Task.Factory.StartNew(() => { _db.GetCollection<TModel>().DeleteAll(); });
        }

        public async Task<ICollection<TModel>> GetAll<TModel>() where TModel : IDatabaseEntity
        {
            return await Task.Factory.StartNew(() => _db.GetCollection<TModel>().FindAll().ToList());
        }

        public async Task<TModel> GetById<TModel>(Guid id) where TModel : IDatabaseEntity
        {
            return await Task.Factory.StartNew(() => _db.GetCollection<TModel>().FindById(id));
        }

        public async Task<TModel> Insert<TModel>(TModel model) where TModel : IDatabaseEntity
        {
            model.Added = _dateTimeProvider.UtcNow;

            Guid id = await Task.Factory.StartNew(() => _db.GetCollection<TModel>().Insert(model));

            return await GetById<TModel>(id);
        }

        public async Task<TModel> Update<TModel>(TModel model) where TModel : IDatabaseEntity
        {
            await Task.Factory.StartNew(() => _db.GetCollection<TModel>().Update(model));

            return await GetById<TModel>(model.Id);
        }
    }
}
