using IPS_PROJECT.Models;

namespace IPS_PROJECT.Models.ViewModels
{
    public class UsersViewModel
    {
        public IEnumerable<UserDto> Users { get; set; }
        public IEnumerable<AdminRequest> PendingAdminRequests { get; set; }
    }

    public class UserDto
    {
        public string Id { get; set; }
        public string UserName { get; set; }
        public string Email { get; set; }
        public string Role { get; set; }
        public string Status { get; set; }
        public DateTime CreatedAt { get; set; }
    }

    public class UpdateRoleDto
    {
        public string UserId { get; set; }
        public string Role { get; set; }
    }
}