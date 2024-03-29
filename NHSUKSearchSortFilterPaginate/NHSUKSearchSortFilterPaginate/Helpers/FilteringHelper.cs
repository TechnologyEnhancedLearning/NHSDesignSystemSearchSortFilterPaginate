﻿namespace NHSUKSearchSortFilterPaginate.Helpers
{
    using System.Collections.Generic;
    using System.ComponentModel;
    using System.Linq;
    using NHSUKSearchSortFilterPaginate.Enums;
    using NHSUKSearchSortFilterPaginate.Extensions;
    using NHSUKSearchSortFilterPaginate.Models.SearchSortFilterPaginate;
    using Microsoft.AspNetCore.Http;

    public static class FilteringHelper
    {
        public const char Separator = '|';
        public const char FilterSeparator = '╡';
        public const string EmptyValue = "╳";
        public const string FreeTextBlankValue = "FREETEXTBLANKVALUE";
        public const string FreeTextNotBlankValue = "FREETEXTNOTBLANKVALUE";

        public static IEnumerable<T> FilterItems<T>(
            IQueryable<T> items,
            string? filterString
        ) where T : BaseSearchableItem
        {
            var listOfFilters = filterString?.Split(FilterSeparator).ToList() ?? new List<string>();

            var appliedFilters = listOfFilters.Select(filter => new AppliedFilter(filter));

            foreach (var filterGroup in appliedFilters.GroupBy(a => a.Group))
            {
                var itemsToFilter = items;
                var setOfFilteredLists = filterGroup.Select(
                    af => FilterGroupItems(itemsToFilter, af.PropertyName, af.PropertyValue)
                );
                items = setOfFilteredLists.SelectMany(x => x).Distinct().AsQueryable();
            }

            return items;
        }

        public static (IEnumerable<T> filteredItems, string? appliedFilterString) FilterOrResetFilterToDefault<T>(
            IEnumerable<T> items,
            FilterOptions filterOptions
        )
            where T : BaseSearchableItem
        {
            if (AvailableFiltersContainsAllSelectedFilters(filterOptions))
            {
                return (FilterItems(items.AsQueryable(), filterOptions.FilterString),
                    filterOptions.FilterString);
            }

            return filterOptions.DefaultFilterString != null
                ? (FilterItems(items.AsQueryable(), filterOptions.DefaultFilterString),
                    filterOptions.DefaultFilterString)
                : (items, null);
        }

        private static bool AvailableFiltersContainsAllSelectedFilters(FilterOptions filterOptions)
        {
            var currentFilters = filterOptions.FilterString?.Split(FilterSeparator).ToList() ??
                                 new List<string>();

            return currentFilters.All(filter => AvailableFiltersContainsFilter(filterOptions.AvailableFilters, filter));
        }

        private static bool AvailableFiltersContainsFilter(IEnumerable<FilterModel> availableFilters, string filter)
        {
            return availableFilters.Any(filterModel => FilterOptionsContainsFilter(filter, filterModel.FilterOptions));
        }

        private static bool FilterOptionsContainsFilter(
            string filter,
            IEnumerable<FilterOptionModel> filterOptions
        )
        {
            return filterOptions.Any(filterOption => filterOption.FilterValue == filter);
        }

        private static IQueryable<T> FilterGroupItems<T>(
            IQueryable<T> items,
            string propertyName,
            string propertyValueString
        )
        {
            var propertyType = typeof(T).GetProperty(propertyName)!.PropertyType;
            var propertyValue = TypeDescriptor.GetConverter(propertyType).ConvertFromString(propertyValueString);
            switch (propertyValue)
            {
                case EmptyValue:
                case FreeTextBlankValue:
                    return items.WhereNullOrEmpty(propertyName);
                case FreeTextNotBlankValue:
                    return items.Where(item => !items.WhereNullOrEmpty(propertyName).Contains(item));
                default:
                    return items.Where(propertyName, propertyValue);
            }
        }

        private class AppliedFilter
        {
            public AppliedFilter(string filterOption)
            {
                var splitFilter = filterOption.Split(Separator);
                Group = splitFilter[0];
                PropertyName = splitFilter[1];
                PropertyValue = splitFilter[2];
            }

            public string Group { get; }

            public string PropertyName { get; }

            public string PropertyValue { get; }
        }
    }
}
