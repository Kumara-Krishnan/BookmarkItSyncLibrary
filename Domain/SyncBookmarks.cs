using BookmarkItCommonLibrary.DI;
using BookmarkItCommonLibrary.Model.Entity;
using BookmarkItSyncLibrary.DI;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Utilities.UseCase;

namespace BookmarkItSyncLibrary.Domain
{
    internal interface ISyncBookmarksDataManager
    {
        Task SyncBookmarks(SyncBookmarksRequest request, ICallbackWithProgress<SyncBookmarksResponse> callback = default);
    }

    public sealed class SyncBookmarksRequest : AuthenticatedUseCaseRequest
    {
        public SyncBookmarksRequest(string userId, CancellationTokenSource cts = default) : base(RequestType.Sync, userId, cts)
        {

        }
    }

    public sealed class SyncBookmarksResponse
    {
        public readonly UserDetails User;

        public SyncBookmarksResponse(UserDetails user)
        {
            User = user;
        }
    }

    public interface ISyncBookmarksPresenterCallback : ICallbackWithProgress<SyncBookmarksResponse>
    {

    }

    public sealed class SyncBookmarks : UseCaseBase<SyncBookmarksRequest, SyncBookmarksResponse>
    {
        private readonly ISyncBookmarksDataManager DataManager;

        public SyncBookmarks(SyncBookmarksRequest request, ISyncBookmarksPresenterCallback callback = default) : base(request, callback)
        {
            DataManager = SyncDIServiceProvider.Instance.GetService<ISyncBookmarksDataManager>();
        }

        protected override Task Action()
        {
            return DataManager.SyncBookmarks(Request, new UseCaseCallback(this));
        }

        class UseCaseCallback : CallbackBase<SyncBookmarksResponse>
        {
            private readonly SyncBookmarks UseCase;

            public UseCaseCallback(SyncBookmarks useCase)
            {
                UseCase = useCase;
            }

            public override void OnProgress(IUseCaseResponse<SyncBookmarksResponse> response)
            {
                if (UseCase.PresenterCallback is ICallbackWithProgress<SyncBookmarksResponse> callback)
                {
                    callback.OnProgress(response);
                }
            }

            public override void OnError(UseCaseError error)
            {
                UseCase.PresenterCallback?.OnError(error);
            }

            public override void OnFailed(IUseCaseResponse<SyncBookmarksResponse> response)
            {
                UseCase.PresenterCallback?.OnFailed(response);
            }

            public override void OnSuccess(IUseCaseResponse<SyncBookmarksResponse> response)
            {
                UseCase.PresenterCallback?.OnSuccess(response);
            }
        }
    }
}
