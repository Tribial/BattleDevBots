using System;
using System.Collections.Generic;
using System.Text;

namespace DevBots.Shared.DtoModels
{
    public class TokensDto : BaseDto
    {
        public string Token { get; set; }
        public string RefreshToken { get; set; }
        public DateTime TokenExpirationDate { get; set; }
        public DateTime RefreshTokenExpirationDate { get; set; }
    }
}
