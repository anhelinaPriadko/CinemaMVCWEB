namespace CinemaInfrastructure.ViewModel
{
    public class UserReadDTO
    {
        public string Id { get; set; }
        public string? UserName { get; set; }
        public string? Email { get; set; }
    }

    public class ViewerReadDTO
    {
        public int Id { get; set; }
        public string Name { get; set; }
        public DateOnly DateOfBirth { get; set; }
        public string UserId { get; set; }
        public UserReadDTO? User { get; set; }
    }
}
