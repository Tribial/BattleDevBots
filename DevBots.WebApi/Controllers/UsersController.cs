using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Claims;
using System.Threading.Tasks;
using DevBots.Services.Interfaces;
using DevBots.Shared.BindingModels;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace DevBots.WebApi.Controllers
{
    [Route("api/Users")]
    [Authorize]
    [ApiController]
    public class UsersController : BaseController
    {
        private readonly IUserService _userService;

        public UsersController(IUserService userService)
        {
            _userService = userService;
        }

        [AllowAnonymous]
        [HttpPost("Activate/{guid}")]
        public async Task<IActionResult> ActivateAccount(string guid)
        {
            var result = await _userService.ActivateUser(guid);

            if (result.ErrorOccured)
            {
                return BadRequest(result);
            }

            return Ok(result);
        }

        [AllowAnonymous]
        [HttpPost("Register")]
        public async Task<IActionResult> RegisterUser(RegisterUserBindingModel user)
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(ModelStateErrors());
            }

            var result = await _userService.RegisterUser(user);

            if (result.ErrorOccured)
            {
                return BadRequest(result);
            }

            return Ok(result);
        }

        [AllowAnonymous]
        [HttpPost("Login")]
        public async Task<IActionResult> LoginUser(LoginBindingModel loginModel)
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(ModelStateErrors());
            }

            var result = await _userService.LoginAsync(loginModel);

            if (result.ErrorOccured)
            {
                return BadRequest(result);
            }

            return Ok(result);
        }

        [HttpPost("Logout")]
        public async Task<IActionResult> LogoutUser()
        {
            var rawUserId = User.Claims.FirstOrDefault(c => c.Type == ClaimTypes.Sid)?.Value;
            var userId = Convert.ToInt64(rawUserId);

            var result = await _userService.LogoutAsync(userId);

            if (result.ErrorOccured)
            {
                return BadRequest(result);
            }

            return Ok(result);
        }

        [AllowAnonymous]
        [HttpPost("RefreshToken/{rToken}")]
        public async Task<IActionResult> RefreshToken(string rToken)
        {
            var result = await _userService.RefreshTokens(rToken);

            if (result.ErrorOccured)
            {
                return BadRequest(result);
            }

            return Ok(result);
        }
    }
}
