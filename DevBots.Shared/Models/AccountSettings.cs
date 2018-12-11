using System;
using System.Collections.Generic;
using System.Text;

namespace DevBots.Shared.Models
{
    public class AccountSettings
    {
        public long Id { get; set; }
        public long UserId { get; set; }
        public bool MouseEnabled { get; set; }
        public string Theme { get; set; }
    }
}
