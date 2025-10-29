namespace CinemaInfrastructure.Pagination
{
    public class PaginationParameters
    {
        private const int MaxPageSize = 50;

        public int Skip { get; set; } = 0;

        private int _limit = 10;

        public int Limit
        {
            get => _limit;
            set => _limit = (value > MaxPageSize) ? MaxPageSize : value;
        }
    }
}
