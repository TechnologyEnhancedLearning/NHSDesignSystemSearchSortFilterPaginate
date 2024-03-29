﻿namespace NHSUKSearchSortFilterPaginate.Models.SearchSortFilterPaginate
{
    using System.Collections.Generic;

    public class FilterOptions
    {
        public FilterOptions(
            string? filterString,
            IEnumerable<FilterModel> availableFilters,
            string? defaultFilterString = null
        )
        {
            FilterString = filterString;
            DefaultFilterString = defaultFilterString;
            AvailableFilters = availableFilters;
        }

        public string? FilterString { get; set; }

        public string? DefaultFilterString { get; set; }

        public IEnumerable<FilterModel> AvailableFilters { get; set; }
    }
}
