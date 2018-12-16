using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net.Mime;
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

        [AllowAnonymous] //Delete this after testing!!!!!
        [HttpGet("Decode")]
        public IActionResult DecodeScript()
        {
            var result = new Responses<RobotCommand>();
            
            result = _languageService.Decode(@"..\Scripts\Tribial\myScript.rl");
            if (result.ErrorOccured)
            {
                return BadRequest(result);
            }

            return Ok(result);
        }
    }
}