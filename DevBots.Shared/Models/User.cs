using System;
using System.Collections.Generic;
using System.Text;

namespace DevBots.Shared.Models
{
    public class User
    {
        public long Id { get; set; }
        public string Guid { get; set; }
        public string Username { get; set; }
        public string PasswordHash { get; set; }
        public string Email { get; set; }
        public bool IsConfirmed { get; set; }
        public DateTime CreatedAt { get; set; }
        public bool IsAdmin { get; set; }
        public bool IsDeleted { get; set; }
    }
}
