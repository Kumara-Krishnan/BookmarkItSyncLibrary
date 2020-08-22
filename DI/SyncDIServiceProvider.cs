using BookmarkItSyncLibrary.Data;
using BookmarkItSyncLibrary.Domain;
using Microsoft.Extensions.DependencyInjection;
using System;
using System.Collections.Generic;
using System.Text;
using Utilities.Util;

namespace BookmarkItSyncLibrary.DI
{
    public sealed class SyncDIServiceProvider : DIServiceProviderBase
    {
        public static SyncDIServiceProvider Instance { get { return SyncDIServiceProviderSingleton.Instance; } }

        private SyncDIServiceProvider() { }

        protected override void AddServices(ServiceCollection serviceCollection)
        {
            serviceCollection.AddSingleton<ISyncBookmarksDataManager, SyncBookmarksDataManager>();
        }

        private class SyncDIServiceProviderSingleton
        {
            static SyncDIServiceProviderSingleton() { }

            internal static readonly SyncDIServiceProvider Instance = new SyncDIServiceProvider();
        }
    }
}