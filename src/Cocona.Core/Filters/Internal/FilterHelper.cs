using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;

namespace Cocona.Filters.Internal
{
    internal class FilterHelper
    {
        public static IEnumerable<IFilterFactory> GetFilterFactories(MethodInfo methodInfo)
        {
            foreach (var filter in GetFilterFactoriesFromCustomAttributes(methodInfo.GetCustomAttributes(true)))
            {
                yield return filter;
            }
            foreach (var filter in GetFilterFactoriesFromCustomAttributes(methodInfo.DeclaringType.GetCustomAttributes(true)))
            {
                yield return filter;
            }
        }

        private static IEnumerable<IFilterFactory> GetFilterFactoriesFromCustomAttributes(object[] attributes)
        {
            foreach (var attr in attributes)
            {
                if (attr is IFilterFactory factory) yield return factory;
                if (attr is IFilterMetadata filter) yield return new InstancedFilterFactory(filter, (filter is IOrderedFilter orderedFilter ? orderedFilter.Order : 0));
            }
        }

        private static T[] GetFiltersCore<T>(IEnumerable<IFilterFactory> factories, IServiceProvider serviceProvider)
        {
            var filters = new List<(int Order, T Instance)>(10);
            foreach (var filter in factories)
            {
                if (filter.CreateInstance(serviceProvider) is T instance)
                {
                    filters.Add((filter is IOrderedFilter orderedFilter ? orderedFilter.Order : 0, instance));
                }
            }
            filters.Sort((a, b) => a.Order.CompareTo(b.Order));

            var newFilters = new T[filters.Count];
            for (var i = 0; i < filters.Count; i++)
            {
                newFilters[i] = filters[i].Instance;
            }

            return newFilters;
        }

        public static IReadOnlyList<T> GetFilters<T>(MethodInfo methodInfo, IServiceProvider serviceProvider)
            where T: IFilterMetadata
        {
            return GetFiltersCore<T>(GetFilterFactories(methodInfo), serviceProvider);
        }

        public static IReadOnlyList<T> GetFilters<T>(Type t, IServiceProvider serviceProvider)
            where T : IFilterMetadata
        {
            return GetFiltersCore<T>(GetFilterFactoriesFromCustomAttributes(t.GetCustomAttributes(true)), serviceProvider);
        }
    }
}
