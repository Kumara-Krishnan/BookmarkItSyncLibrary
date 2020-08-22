using BookmarkItCommonLibrary.Data.Handler.Contract;
using BookmarkItCommonLibrary.DI;
using BookmarkItCommonLibrary.Util;
using System;
using System.Collections.Generic;
using System.Text;


namespace BookmarkItSyncLibrary
{
    public sealed class SyncServiceManager
    {
        public static SyncServiceManager Instance { get { return SyncServiceManagerSingleton.Instance; } }

        private SyncServiceManager() { }

        public bool Initialize(string dbFileName, string path)
        {
            bool isInitialized = false;
            try
            {
                var dbHandler = CommonDIServiceProvider.Instance.GetService<IDBHandler>();
                dbHandler.Initialize(dbFileName, path);
                isInitialized = true;
            }
            catch { }
            return isInitialized;
        }

        private static class SyncServiceManagerSingleton
        {
            static SyncServiceManagerSingleton() { }

            internal static readonly SyncServiceManager Instance = new SyncServiceManager();
        }
    }
}