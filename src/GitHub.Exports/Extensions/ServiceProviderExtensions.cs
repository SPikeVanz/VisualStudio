﻿using System;
using System.ComponentModel.Design;
using System.Diagnostics;
using GitHub.Primitives;
using GitHub.Services;
using LibGit2Sharp;
using Microsoft.TeamFoundation.Controls;
using Microsoft.VisualStudio;
using Microsoft.VisualStudio.Shell;
using Microsoft.VisualStudio.Shell.Interop;

namespace GitHub.Extensions
{
    public static class IServiceProviderExtensions
    {
        static IUIProvider cachedUIProvider = null;

        public static object TryGetService(this IServiceProvider serviceProvider, Type type)
        {
            if (cachedUIProvider != null && type == typeof(IUIProvider))
                return cachedUIProvider;

            var ui = serviceProvider as IUIProvider;
            return ui != null
                ? ui.TryGetService(type)
                : GetServiceAndCache(serviceProvider, type, ref cachedUIProvider);
        }
        public static T GetExportedValue<T>(this IServiceProvider serviceProvider) where T : class
        {
            if (cachedUIProvider != null && typeof(T) == typeof(IUIProvider))
                return (T)cachedUIProvider;

            var ui = serviceProvider as IUIProvider;
            return ui != null
                ? ui.TryGetService(typeof(T)) as T
                : GetExportedValueAndCache<T, IUIProvider>(ref cachedUIProvider);
        }

        public static T TryGetService<T>(this IServiceProvider serviceProvider) where T : class
        {
            return serviceProvider.TryGetService(typeof(T)) as T;
        }

        public static T GetService<T>(this IServiceProvider serviceProvider) where T : class
        {
            return serviceProvider.TryGetService(typeof(T)) as T;
        }

        public static ITeamExplorerSection GetSection(this IServiceProvider serviceProvider, Guid section)
        {
            return serviceProvider?.GetService<ITeamExplorerPage>()?.GetSection(section);
        }

        static object GetServiceAndCache<CacheType>(IServiceProvider provider, Type type, ref CacheType cache)
        {
            object ret = null;
            try
            {
                ret = provider.GetService(type);
            }
            catch (Exception ex)
            {
                Debug.Print(ex.ToString());
                VisualStudio.VsOutputLogger.WriteLine("GetServiceAndCache: Could not obtain instance of '{0}'", type);
            }
            if (ret != null && type == typeof(CacheType))
                cache = (CacheType)ret;
            return ret;
        }

        static T GetExportedValueAndCache<T, CacheType>(ref CacheType cache) where T : class
        {
            object ret = VisualStudio.Services.ComponentModel.DefaultExportProvider.GetExportedValueOrDefault<T>();
            if (ret != null && typeof(T) == typeof(CacheType))
                cache = (CacheType)ret;
            return ret as T;
        }

        public static void AddTopLevelMenuItem(this IServiceProvider provider,
            Guid guid,
            int cmdId,
            EventHandler eventHandler)
        {
            var mcs = provider.GetService(typeof(IMenuCommandService)) as IMenuCommandService;
            Debug.Assert(mcs != null, "No IMenuCommandService? Something is wonky");
            if (mcs == null)
                return;
            var id = new CommandID(guid, cmdId);
            var item = new MenuCommand(eventHandler, id);
            mcs.AddCommand(item);
        }

        public static void AddDynamicMenuItem(this IServiceProvider provider,
            Guid guid,
            int cmdId,
            Func<bool> canEnable,
            Action execute)
        {
            var mcs = provider.GetService(typeof(IMenuCommandService)) as IMenuCommandService;
            Debug.Assert(mcs != null, "No IMenuCommandService? Something is wonky");
            if (mcs == null)
                return;
            var id = new CommandID(guid, cmdId);
            var item = new OleMenuCommand(
                (s, e) => execute(),
                (s, e) => { },
                (s, e) =>
                {
                    ((OleMenuCommand)s).Visible = canEnable();
                },
                id);
            mcs.AddCommand(item);
        }
    }
}
