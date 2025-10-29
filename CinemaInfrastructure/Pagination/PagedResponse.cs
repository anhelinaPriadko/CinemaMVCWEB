using System.Collections.Generic;
namespace CinemaInfrastructure.Pagination
{
    public class PagedResponse<T>
    {
        public IEnumerable<T> Data { get; set; }
        public int TotalCount { get; set; }
        public string NextLink { get; set; }
        public string PreviousLink { get; set; }

        public PagedResponse(IEnumerable<T> data, int totalCount, string nextLink, string prevLink)
        {
            Data = data;
            TotalCount = totalCount;
            NextLink = nextLink;
            PreviousLink = prevLink;
        }
    }
}
