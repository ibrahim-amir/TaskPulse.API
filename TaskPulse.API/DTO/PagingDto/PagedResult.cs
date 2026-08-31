namespace TaskPulse.API.DTO.PagingDto
{
    // One page of results plus the counts a frontend needs to draw pagination.
    // Goes inside CommonResponse.Data, so the response envelope doesn't change.
    public class PagedResult<T>
    {
        public List<T> Items { get; set; } = new();
        public int TotalCount { get; set; }
        public int Page { get; set; }
        public int PageSize { get; set; }

        public int TotalPages => PageSize == 0 ? 0 : (int)Math.Ceiling(TotalCount / (double)PageSize);
        public bool HasPrevious => Page > 1;
        public bool HasNext => Page < TotalPages;

        public static PagedResult<T> Create(List<T> items, int totalCount, PagedRequest request)
        {
            return new PagedResult<T>
            {
                Items = items,
                TotalCount = totalCount,
                Page = request.Page,
                PageSize = request.PageSize
            };
        }

        // Repositories page entities; services return DTOs. This converts the
        // items without the caller having to rebuild the paging numbers:
        //
        //     var page = await _userRepository.GetPagedAsync(query);
        //     var dtoPage = page.MapItems(UserDTO.CreateUserDtoFromUserEntity);
        public PagedResult<TOut> MapItems<TOut>(Func<T, TOut> map)
        {
            return new PagedResult<TOut>
            {
                Items = Items.Select(map).ToList(),
                TotalCount = TotalCount,
                Page = Page,
                PageSize = PageSize
            };
        }
    }
}
