using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Claims;
using System.Threading.Tasks;
using DevBots.Services.Interfaces;
using DevBots.Shared.BindingModels;
using DevBots.Shared.DtoModels;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace DevBots.WebApi.Controllers
{
    [Route("api/Script")]
    [Authorize]
    [ApiController]
    public class ScriptController : BaseController
    {
        private readonly IScriptService _scriptService;

        public ScriptController(IScriptService scriptService)
        {
            _scriptService = scriptService;
        }

        [HttpPost]
        public async Task<IActionResult> AddScript(ScriptBindingModel scriptBindingModel)
        {
            var result = new Response<BaseDto>();
            var username = HttpContext.User.Claims.First(c => c.Type == ClaimTypes.GivenName).Value;
            result = await _scriptService.AddAsync(scriptBindingModel, username);

            if (result.ErrorOccured)
            {
                return BadRequest(result);
            }
            return Ok(result);
        }

        [HttpGet("ByUser")]
        public IActionResult GetByUser()
        {
            var userId = HttpContext.User.Claims.First(c => c.Type == ClaimTypes.Sid).Value;
            var result = _scriptService.GetToDisplay(Convert.ToInt64(userId));
            if (result.ErrorOccured)
            {
                return BadRequest(result);
            }

            return Ok(result);
        }

        [HttpDelete("{id}")]
        public async Task<IActionResult> RemoveAsync(long id)
        {
            var userId = Convert.ToInt64(HttpContext.User.Claims.First(c => c.Type == ClaimTypes.Sid).Value);
            var result = await _scriptService.RemoveAsync(id, userId);

            if (result.ErrorOccured)
            {
                return BadRequest(result);
            }

            return Ok(result);
        }

        [HttpGet("Simple/{robotId}")]
        public IActionResult GetSimpleByRobotId(long robotId)
        {
            var userId = Convert.ToInt64(HttpContext.User.Claims.First(c => c.Type == ClaimTypes.Sid).Value);
            var result = _scriptService.GetSimpleByRobotId(robotId, userId);

            if (result.ErrorOccured)
            {
                return BadRequest(result);
            }

            return Ok(result);
        }
    }
}