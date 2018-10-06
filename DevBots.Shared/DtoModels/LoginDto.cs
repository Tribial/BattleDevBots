using System;
using System.Collections.Generic;
using System.Text;

namespace DevBots.Shared.DtoModels
{
    public class LoginDto : BaseDto
    {
        public TokensDto Tokens { get; set; }
        public long Id { get; set; }
        public string Username { get; set; }
        public string Email { get; set; }
        public bool IsAdmin { get; set; }
    }
}
