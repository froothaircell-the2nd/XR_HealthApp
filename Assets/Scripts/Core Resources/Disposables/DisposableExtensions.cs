using System;
using System.Collections.Generic;

namespace CoreResources.Disposables
{
    public static class DisposableExtensions
    {
        public static void ClearDisposables<T>(this T container) where T : List<IDisposable>
        {
            if (container == null || container.Count <= 0)
            {
                return;
            }

            foreach (var disposable in container)
            {
                disposable?.Dispose();
            }
            
            container.Clear();
        }
    }
}