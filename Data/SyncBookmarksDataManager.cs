using BookmarkItCommonLibrary.Data;
using BookmarkItCommonLibrary.Data.Contract;
using BookmarkItCommonLibrary.DI;
using BookmarkItCommonLibrary.Domain;
using BookmarkItCommonLibrary.Model;
using BookmarkItCommonLibrary.Model.Entity;
using BookmarkItCommonLibrary.Util;
using BookmarkItSyncLibrary.Domain;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Utilities.Extension;
using Utilities.UseCase;

/**
* @author kumar-4031
*
* @date - 2/29/2020 8:27:35 PM 
*/
namespace BookmarkItSyncLibrary.Data
{
    internal sealed class SyncBookmarksDataManager : GetBookmarksDataManager, ISyncBookmarksDataManager
    {
        public async Task SyncBookmarks(SyncBookmarksRequest request, ICallbackWithProgress<SyncBookmarksResponse> callback = null)
        {
            var userDetailsDM = CommonDIServiceProvider.Instance.GetService<IGetUserDetailsDataManager>();
            var userDetailsRequest = new GetUserDetailsRequest(RequestType.LocalStorage, request.UserId);
            var userDetails = userDetailsDM.GetUserDetailsFromDB(userDetailsRequest);
            if (userDetails == default) { throw new InvalidOperationException("UserDetails not found"); }

            NetHandler.AccessToken = userDetails.AccessToken;
            var userStat = await userDetailsDM.FetchUserStatFromServerAsync(userDetails.Id).ConfigureAwait(false);
            userDetails.ItemsCount = userStat.TotalItemsCount;
            userDetails.UnreadItemsCount = userStat.UnreadItemsCount;
            userDetails.ReadItemsCount = userStat.ReadItemsCount;
            DBHandler.AddOrReplaceUserDetails(userDetails);

            if (!userDetails.IsInitialFetchComplete)
            {
                await InitiateInitialFetchAsync(userDetails, callback).ConfigureAwait(false);
            }
            else
            {
                await InitiateIncrementalSync(userDetails, callback).ConfigureAwait(false);
            }
            userDetails = userDetailsDM.GetUserDetailsFromDB(userDetailsRequest);
            callback.OnSuccessOrFailed(ResponseType.Sync, new SyncBookmarksResponse(userDetails), IsValidResponse);
            if (!userDetails.IsArticleFetchComplete)
            {
                await InitiateArticlesFetchAsync(userDetails, callback).ConfigureAwait(false);
            }
        }

        private async Task InitiateInitialFetchAsync(UserDetails userDetails, ICallbackWithProgress<SyncBookmarksResponse> callback = default)
        {
            var getBookmarksRequest = new GetBookmarksRequest(RequestType.Network, userDetails.Id)
            {
                Offset = userDetails.InitialFetchOffset,
                Count = CommonConstants.BookmarksFetchLimit,
                SortBy = SortBy.Oldest
            };
            do
            {
                if (userDetails.InitialFetchOffset == 0)
                {
                    userDetails.LastSyncedTime = DateTimeOffset.UtcNow.ToUnixTimeSeconds();
                }
                var parsedBookmarksResponse = await FetchParsedBookmarksResponseFromServerAsync(getBookmarksRequest).ConfigureAwait(false);
                getBookmarksRequest.Offset = userDetails.InitialFetchOffset += CommonConstants.BookmarksFetchLimit;
                userDetails.IsInitialFetchComplete = parsedBookmarksResponse.Bookmarks.IsNullOrEmpty();
                if (userDetails.IsInitialFetchComplete) { userDetails.InitialFetchOffset = 0; }
                DBHandler.AddOrReplaceUserDetails(userDetails);
                callback?.OnProgress(new UseCaseResponse<SyncBookmarksResponse>(ResponseType.Sync, ResponseStatus.Success, new SyncBookmarksResponse(userDetails)));
            }
            while (!userDetails.IsInitialFetchComplete);
            await InitiateIncrementalSync(userDetails).ConfigureAwait(false);
        }

        private async Task InitiateIncrementalSync(UserDetails userDetails, ICallbackWithProgress<SyncBookmarksResponse> callback = default)
        {
            var getBookmarksRequest = new GetBookmarksRequest(RequestType.Sync, userDetails.Id)
            {
                Since = userDetails.LastSyncedTime
            };
            await FetchParsedBookmarksResponseFromServerAsync(getBookmarksRequest).ConfigureAwait(false);
        }

        private async Task InitiateArticlesFetchAsync(UserDetails userDetails, ICallbackWithProgress<SyncBookmarksResponse> callback = default)
        {
            var bookmarks = DBHandler.GetArticlesToBeFetched(userDetails.Id);
            userDetails.IsArticleFetchComplete = bookmarks.IsNullOrEmpty();
            DBHandler.AddOrReplaceUserDetails(userDetails);
            List<Article> articles = new List<Article>();
            foreach (var bookmark in bookmarks)
            {
                try
                {
                    var paginatedArticle = await FetchPaginatedArticleAsync(bookmark.Id, bookmark.ResolvedUrl).ConfigureAwait(false);
                    articles.AddRange(paginatedArticle);
                }
                catch
                {
                    DBHandler.UpdateArticleError(bookmark.Id);
                }
            }
            DBHandler.AddOrReplaceArticles(articles);
        }

        private async Task<IList<Article>> FetchPaginatedArticleAsync(string id, string url)
        {
            var paginatedArticle = await NetHandler.GetPaginatedArticleAsync(url).ConfigureAwait(false);

            if (paginatedArticle.IsNullOrEmpty()) { return default; }
            var articles = new List<Article>();
            for (var i = 0; i < paginatedArticle.Count(); i++)
            {
                articles.Add(new Article(id, i + 1) { Content = paginatedArticle[i] });
            }
            return articles;
        }

        private bool IsValidResponse(SyncBookmarksResponse syncBookmarksResponse)
        {
            return syncBookmarksResponse.User != default && syncBookmarksResponse.User.IsInitialFetchComplete;
        }
    }
}