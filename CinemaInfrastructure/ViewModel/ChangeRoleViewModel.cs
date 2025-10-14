using Microsoft.AspNetCore.Identity;

namespace CinemaInfrastructure.ViewModel
{
    public class ChangeRoleViewModel
    {
        public string UserId { get; set; }
        public string UserEmail { get; set; }
        public List<IdentityRole> AllRoles { get; set; }
        public IList<string> UserRoles { get; set; }
        public string SelectedRole { get; set; }

        public ChangeRoleViewModel()
        {
            AllRoles = new List<IdentityRole>();
        }
    }

}
