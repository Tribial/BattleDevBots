using System;
using System.Collections.Generic;
using System.IdentityModel.Tokens.Jwt;
//using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using System.Threading.Tasks;
using AutoMapper;
using DevBots.Data.Interfaces;
using DevBots.Services.Interfaces;
using DevBots.Shared;
using DevBots.Shared.BindingModels;
using DevBots.Shared.DtoModels;
using DevBots.Shared.Models;
using Microsoft.IdentityModel.Tokens;

//using Microsoft.IdentityModel.Tokens;

//using Microsoft.Extensions.Configuration;

namespace DevBots.Services
{
    public class UserService : IUserService
    {
        private readonly IConfigurationService _configurationService;
        private readonly IUserRepository _userRepository;
        private readonly ITokenRepository _tokenRepository;
        private readonly IPlayerRepository _playerRepository;
        private readonly ISettingsRepository _settingsRepository;

        public UserService(IConfigurationService configuration, IUserRepository userRepository, ITokenRepository tokenRepository, IPlayerRepository playerRepository, ISettingsRepository settingsRepository)
        {
            _configurationService = configuration;
            _userRepository = userRepository;
            _tokenRepository = tokenRepository;
            _playerRepository = playerRepository;
            _settingsRepository = settingsRepository;
        }

        public async Task<Response<BaseDto>> RegisterUser(RegisterUserBindingModel user)
        {
            var result = new Response<BaseDto>();

            if (_userRepository.Get(u => u.Email == user.Email) != null)
            {
                result.Errors.Add($"The email {user.Email} is already associated with an account");
                return result;
            }

            if (_userRepository.Get(u => u.Username == user.Username) != null)
            {
                result.Errors.Add($"The username {user.Username} is already taken");
                return result;
            }

            if (user.Password != user.ConfirmPassword)
            {
                result.Errors.Add("The passwords should be the same");
                return result;
            }

            var currentMasterPassword = _configurationService.GetValue("MasterPassword");
            if (currentMasterPassword == user.MasterPassword)
            {
                result = await CreateUser(user);
            }
            else
            {
                result = await CreateUser(user);
            }
            return result;
        }

        public async Task<Response<BaseDto>> ActivateUser(string guid)
        {
            var result = new Response<BaseDto>();
            var user = _userRepository.Get(u => u.Guid == guid);

            if (user == null)
            {
                result.Errors.Add("Your activation link is not correct");
                return result;
            }

            user.IsConfirmed = true;
            var updateResult = await _userRepository.Update(user);
            if (!updateResult)
            {
                result.Errors.Add("Something went horribly wrong, please try again or contact the support");
                return result;
            }

            return result;
        }

        private async Task<Response<BaseDto>> CreateUser(RegisterUserBindingModel user)
        {
            var result = new Response<BaseDto>();
            var newUser = Mapper.Map<User>(user);
            newUser.CreatedAt = DateTime.Now;
            newUser.Guid = Guid.NewGuid().ToString();
            newUser.IsConfirmed = false;
            newUser.IsAdmin = false;

            var insertResult = await _userRepository.Insert(newUser);

            if (!insertResult)
            {
                result.Errors.Add("Something went horribly wrong. Please try again later.");
                return result;
            }

            var addedUser = _userRepository.Get(u => u.Email == user.Email);
            var player = new Player
            {
                Id = addedUser.Id,
                Matches = new List<Match>(),
                Scripts = new List<Script>(),
            };

            insertResult = await _playerRepository.InsertAsync(player);
            if (!insertResult)
            {
                result.Errors.Add("Something went horribly wrong. Please try again later.");
                return result;
            }

            var settings = new AccountSettings
            {
                MouseEnabled = true,
                Theme = "default",
                UserId = addedUser.Id,
            };
            insertResult = await _settingsRepository.InsertAsync(settings);
            if (!insertResult)
            {
                result.Errors.Add("Something went horribly wrong. Please try again later.");
                return result;
            }

            var activationUrl = _configurationService.GetValue("ActivationLink").Replace("<userGuid>", newUser.Guid);
            ExtensionMethods.SendEmail(user.Email, user.Username, activationUrl);

            return result;
        }

        private async Task<Response<BaseDto>> CreateAdministrator(RegisterUserBindingModel user)
        {
            var result = new Response<BaseDto>();
            var newUser = Mapper.Map<User>(user);
            newUser.CreatedAt = DateTime.Now;
            newUser.Guid = Guid.NewGuid().ToString();
            newUser.IsConfirmed = true;
            newUser.IsAdmin = true;

            var insertResult = await _userRepository.Insert(newUser);

            if (!insertResult)
            {
                result.Errors.Add("Something went horribly wrong. Please try again later.");
                return result;
            }

            //ExtensionMethods.SendEmail(user.Email, user.Username);

            return result;
        }

        public async Task<Response<LoginDto>> LoginAsync(LoginBindingModel loginModel)
        {
            var result = new Response<LoginDto>();

            loginModel.Password = loginModel.Password.ToHash();
            var user = _userRepository.Get(u => u.Email == loginModel.EmailOrUsername || u.Username == loginModel.EmailOrUsername);

            if (user == null)
            {
                result.Errors.Add("Those credentials are not valid, please try again.");
                return result;
            }

            if (user.PasswordHash != loginModel.Password || user.IsDeleted)
            {
                result.Errors.Add("Those credentials are not valid, please try again.");
                return result;
            }

            if (!user.IsConfirmed)
            {
                result.Errors.Add($"This account is not activated yet. Please click the link, which was send on {user.Email}");
                return result;
            }

            var tokensResponse = await GenerateTokensAsync(user);

            if (tokensResponse.ErrorOccured)
            {
                result.Errors = tokensResponse.Errors;
                return result;
            }

            result.Model = Mapper.Map<LoginDto>(user);
            result.Model.Tokens = tokensResponse.Model;

            return result;
        }

        public async Task<Response<BaseDto>> LogoutAsync(long userId)
        {
            var result = new Response<BaseDto>();
            var user = _userRepository.Get(u => u.Id == userId);

            if (user == null)
            {
                result.Errors.Add($"User with id {userId} does not exist.");
                return result;
            }

            var token = _tokenRepository.Get(t => t.UserId == userId);

            if (token == null)
            {
                result.Errors.Add("You are not logged in.");
                return result;
            }

            var logoutResult = await _tokenRepository.RemoveAsync(token);

            if (!logoutResult)
            {
                result.Errors.Add("A critical error occurred, please constant the websites support or try again later.");
            }

            return result;

        }

        public async Task<Response<TokensDto>> GenerateTokensAsync(User user)
        {
            var secretKey = _configurationService.GetValue("Jwt:Key");
            var issuer = _configurationService.GetValue("Jwt:Issuer");
            var expirationDate = DateTime.Now.AddDays(Convert.ToDouble(_configurationService.GetValue("Jwt:ExpDays")));

            var result = new Response<TokensDto>()
            {
                Model = new TokensDto()
            };

            var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(secretKey));
            var creds = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);

            var claims = new[]
            {
                new Claim(ClaimTypes.GivenName, user.Username),
                new Claim(ClaimTypes.Email, user.Email),
                new Claim(ClaimTypes.Sid, user.Id.ToString()),
                new Claim(ClaimTypes.Hash, Guid.NewGuid().ToString()),
                new Claim(ClaimTypes.Role, user.IsAdmin ? "Admin" : "User")
            };

            var token = new JwtSecurityToken(
                issuer,
                issuer,
                claims,
                expires: expirationDate,
                signingCredentials: creds
            );

            result.Model.Token = new JwtSecurityTokenHandler().WriteToken(token);
            result.Model.TokenExpirationDate = expirationDate;


            var refreshTokenRaw = user.Email + Guid.NewGuid() + user.Id + DateTime.Now.ToShortDateString() + secretKey;
            var refreshToken = new RefreshToken()
            {
                UserId = user.Id,
                Token = refreshTokenRaw.ToHash(),
                TokenExpirationDate =
                    DateTime.Now.AddDays(Convert.ToDouble(_configurationService.GetValue("Jwt:ExpRefreshToken")))
            };

            result.Model.RefreshToken = refreshToken.Token;
            result.Model.RefreshTokenExpirationDate = refreshToken.TokenExpirationDate;

            var existingRefreshToken = _tokenRepository.Get(t => t.UserId == user.Id);

            if (existingRefreshToken != null)
            {
                var removingResult = await _tokenRepository.RemoveAsync(existingRefreshToken);
                if (!removingResult)
                {
                    result.Errors.Add("A critical error occurred, please constant the websites support or try again later.");
                    return result;
                }
            }

            var insertResult = await _tokenRepository.InsertAsync(refreshToken);

            if (insertResult) return result;

            result.Errors.Add("A critical error occurred, please constant the websites support or try again later.");
            return result;
        }

        public async Task<Response<TokensDto>> RefreshTokens(string rToken)
        {
            var result = new Response<TokensDto>();
            var refreshToken = _tokenRepository.Get(t => t.Token == rToken);

            if (refreshToken == null)
            {
                result.Errors.Add("You are not logged in");
                return result;
            }

            var user = _userRepository.Get(u => u.Id == refreshToken.UserId);

            result = await GenerateTokensAsync(user);

            return result;
        }
    }
}
