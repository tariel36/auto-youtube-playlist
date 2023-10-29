using AutoYoutubePlaylist.Logic.Features.Database.Models;

namespace AutoYoutubePlaylist.Logic.Features.Database.Services
{
    public interface IDatabaseService
    {
        Task<TModel?> GetById<TModel>(Guid id) where TModel : IDatabaseEntity;

        Task<ICollection<TModel>> GetAll<TModel>() where TModel : IDatabaseEntity;

        Task<TModel?> Insert<TModel>(TModel model) where TModel : IDatabaseEntity;

        Task<TModel?> Update<TModel>(TModel model) where TModel : IDatabaseEntity;

        Task Delete<TModel>(Guid id) where TModel : IDatabaseEntity;

        Task Delete<TModel>(TModel model) where TModel : IDatabaseEntity;

        Task DeleteAll<TModel>() where TModel : IDatabaseEntity;
    }
}
