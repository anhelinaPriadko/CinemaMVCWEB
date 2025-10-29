using Microsoft.AspNetCore.Mvc;

namespace CinemaInfrastructure.Pagination
{
    public class PaginationLinkHelper
    {
        public static string CreateNextLink<T>(
            IUrlHelper urlHelper,
            string routeName,
            T parameters,
            int totalCount) where T : PaginationParameters
        {
            bool hasNext = parameters.Skip + parameters.Limit < totalCount;

            if (!hasNext)
            {
                return null;
            }

            var nextSkip = parameters.Skip + parameters.Limit;

            return urlHelper.Link(routeName, new
            {
                skip = nextSkip,
                limit = parameters.Limit
            });
        }

        public static string CreatePreviousLink<T>(
            IUrlHelper urlHelper,
            string routeName,
            T parameters) where T : PaginationParameters
        {
            bool hasPrevious = parameters.Skip > 0;

            if (!hasPrevious)
            {
                return null;
            }

            var prevSkip = Math.Max(0, parameters.Skip - parameters.Limit);

            return urlHelper.Link(routeName, new
            {
                skip = prevSkip,
                limit = parameters.Limit
            });
        }
    }
}
