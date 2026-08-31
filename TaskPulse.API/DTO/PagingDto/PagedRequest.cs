namespace TaskPulse.API.DTO.PagingDto
{
    // Base class for any "give me a filtered page of X" request.
    // Inherit it and add the filters that are specific to that entity:
    //
    //     public class UserQueryDto : PagedRequest
    //     {
    //         public UserRole? Role { get; set; }
    //     }
    //
    // Page and PageSize clamp themselves, so a caller asking for page 0 or
    // pageSize 5000 gets sane values instead of an error or a table scan.
    public class PagedRequest
    {
        public const int DefaultPageSize = 20;
        public const int MaxPageSize = 100;

        private int _page = 1;
        private int _pageSize = DefaultPageSize;

        public int Page
        {
            get => _page;
            set => _page = value < 1 ? 1 : value;
        }

        public int PageSize
        {
            get => _pageSize;
            set => _pageSize = value < 1 ? DefaultPageSize : Math.Min(value, MaxPageSize);
        }

        public string? Search { get; set; }

        // Property name to sort by. Unknown names are ignored rather than
        // failing the request — see PagingExtensions.ApplySorting.
        public string? SortBy { get; set; }

        // "asc" or "desc". Anything else is treated as ascending.
        public string? SortOrder { get; set; }
    }
}
