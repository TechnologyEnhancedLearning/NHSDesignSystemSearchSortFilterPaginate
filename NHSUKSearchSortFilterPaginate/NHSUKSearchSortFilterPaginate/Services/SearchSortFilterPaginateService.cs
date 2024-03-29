﻿namespace NHSUKSearchSortFilterPaginate.Services
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using NHSUKSearchSortFilterPaginate.Extensions;
    using NHSUKSearchSortFilterPaginate.Helpers;
    using NHSUKSearchSortFilterPaginate.Models.SearchSortFilterPaginate;
    using Microsoft.Extensions.Configuration;

    public interface ISearchSortFilterPaginateService
    {
        SearchSortFilterPaginationResult<T> SearchFilterSortAndPaginate<T>(
            IEnumerable<T> items,
            SearchSortFilterAndPaginateOptions searchSortFilterAndPaginateOptions
        ) where T : BaseSearchableItem;
    }

    public class SearchSortFilterPaginateService : ISearchSortFilterPaginateService
    {
        private readonly IConfiguration configuration;

        public SearchSortFilterPaginateService(IConfiguration configuration)
        {
            this.configuration = configuration;
        }

        public SearchSortFilterPaginationResult<T> SearchFilterSortAndPaginate<T>(
            IEnumerable<T> items,
            SearchSortFilterAndPaginateOptions searchSortFilterAndPaginateOptions
        ) where T : BaseSearchableItem
        {
            var allItems = items.ToList();
            var itemsToReturn = allItems;
            string? appliedFilterString = null;
            var javascriptSearchSortFilterPaginateShouldBeEnabled =
                allItems.Count <= configuration.GetJavascriptSearchSortFilterPaginateItemLimit();

            if (searchSortFilterAndPaginateOptions.SearchOptions != null)
            {
                itemsToReturn = (searchSortFilterAndPaginateOptions.SearchOptions.UseTokeniseScorer
                    ? GenericSearchHelper.SearchItemsUsingTokeniseScorer(
                        itemsToReturn,
                        searchSortFilterAndPaginateOptions.SearchOptions.SearchString,
                        searchSortFilterAndPaginateOptions.SearchOptions.SearchMatchCutoff
                    )
                    : GenericSearchHelper.SearchItems(
                        itemsToReturn,
                        searchSortFilterAndPaginateOptions.SearchOptions.SearchString,
                        searchSortFilterAndPaginateOptions.SearchOptions.SearchMatchCutoff,
                        scorer: searchSortFilterAndPaginateOptions.SearchOptions.Scorer
                    )).ToList();
            }

            if (searchSortFilterAndPaginateOptions.FilterOptions != null)
            {
                var filteringReturnTuple = FilteringHelper.FilterOrResetFilterToDefault(
                    itemsToReturn,
                    searchSortFilterAndPaginateOptions.FilterOptions
                );
                itemsToReturn = filteringReturnTuple.filteredItems.ToList();
                appliedFilterString = filteringReturnTuple.appliedFilterString;
            }

            if (searchSortFilterAndPaginateOptions.SortOptions != null)
            {
                itemsToReturn = GenericSortingHelper.SortAllItems(
                    itemsToReturn.AsQueryable(),
                    searchSortFilterAndPaginateOptions.SortOptions.SortBy,
                    searchSortFilterAndPaginateOptions.SortOptions.SortDirection
                ).ToList();
            }

            var paginateResult = PaginateItems(
                itemsToReturn,
                searchSortFilterAndPaginateOptions.PaginationOptions,
                javascriptSearchSortFilterPaginateShouldBeEnabled
            );

            return new SearchSortFilterPaginationResult<T>(
                paginateResult,
                searchSortFilterAndPaginateOptions.SearchOptions?.SearchString,
                searchSortFilterAndPaginateOptions.SortOptions?.SortBy,
                searchSortFilterAndPaginateOptions.SortOptions?.SortDirection,
                appliedFilterString
            );
        }

        private static PaginationResult<T> PaginateItems<T>(
            IEnumerable<T> items,
            PaginationOptions? paginationOptions,
            bool javascriptShouldBeEnabled
        )
            where T : BaseSearchableItem
        {
            var paginationOptionsToUse = paginationOptions ?? new PaginationOptions(1, int.MaxValue);

            var listedItems = items.ToList();
            var matchingSearchResults = listedItems.Count;
            var totalPages = GetTotalPages(matchingSearchResults, paginationOptionsToUse.ItemsPerPage);
            var page = paginationOptionsToUse.PageNumber < 1 || paginationOptionsToUse.PageNumber > totalPages
                ? 1
                : paginationOptionsToUse.PageNumber;
            var itemsToDisplay = GetItemsOnCurrentPage(
                listedItems,
                page,
                paginationOptionsToUse.ItemsPerPage
            );
            return new PaginationResult<T>(
                itemsToDisplay,
                page,
                totalPages,
                paginationOptionsToUse.ItemsPerPage,
                matchingSearchResults,
                javascriptShouldBeEnabled
            );
        }

        private static IEnumerable<T> GetItemsOnCurrentPage<T>(IList<T> items, int page, int itemsPerPage)
        {
            return items.Count > itemsPerPage
                ? items.Skip(OffsetFromPageNumber(page, itemsPerPage)).Take(itemsPerPage).ToList()
                : items;
        }

        private static int GetTotalPages(int matchingSearchResults, int itemsPerPage)
        {
            return (int)Math.Ceiling(matchingSearchResults / (double)itemsPerPage);
        }

        private static int OffsetFromPageNumber(int page, int itemsPerPage)
        {
            return (page - 1) * itemsPerPage;
        }
    }
}
