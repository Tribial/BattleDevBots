using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;
using DevBots.Shared.BindingModels;
using DevBots.Shared.DtoModels;
using DevBots.Shared.Models;

namespace DevBots.Services.Interfaces
{
    public interface IUserService
    {
        Task<Response<BaseDto>> RegisterUser(RegisterUserBindingModel user);
        Task<Response<BaseDto>> ActivateUser(string guid);
        Task<Response<LoginDto>> LoginAsync(LoginBindingModel loginModel);
        Task<Response<BaseDto>> LogoutAsync(long userId);
        Task<Response<TokensDto>> GenerateTokensAsync(User user);
        Task<Response<TokensDto>> RefreshTokens(string rToken);
    }
}
