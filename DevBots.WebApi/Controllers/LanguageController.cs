using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net.Mime;
using System.Security.Claims;
using System.Threading.Tasks;
using DevBots.Services.Interfaces;
using DevBots.Shared.DtoModels;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace DevBots.WebApi.Controllers
{
    [Route("api/Language")]
    [Authorize]
    [ApiController]
    public class LanguageController : BaseController
    {
        private readonly ILanguageService _languageService;

        public LanguageController(ILanguageService languageService)
        {
            _languageService = languageService;
        }

        [HttpGet("Decode/{scriptId}")]
        public IActionResult DecodeScript(long scriptId)
        {
            var userId = Convert.ToInt64(HttpContext.User.Claims.First(c => c.Type == ClaimTypes.Sid).Value);
            var result = new Responses<RobotCommand>();
            
            result = _languageService.Decode(scriptId, userId);
            if (result.ErrorOccured)
            {
                return BadRequest(result);
            }

            return Ok(result);
        }
    }
}