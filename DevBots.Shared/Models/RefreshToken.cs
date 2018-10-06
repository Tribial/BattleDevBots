using System;
using System.Collections.Generic;
using System.Text;

namespace DevBots.Shared.Models
{
    public class RefreshToken
    {
        public long Id { get; set; }
        public long UserId { get; set; }
        public string Token { get; set; }
        public DateTime TokenExpirationDate { get; set; }
    }
}
