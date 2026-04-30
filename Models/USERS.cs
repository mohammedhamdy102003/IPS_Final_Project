using Microsoft.AspNetCore.Identity;
using System;

namespace IPS_PROJECT.Models
{
    public class USERS : IdentityUser
    {
        public string FullName { get; set; } = string.Empty;
        public DateTime CreatedAt { get; set; } = DateTime.Now;

        public string RequestedRole { get; set; }
    }
}
