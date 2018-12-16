using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using DevBots.Services.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace DevBots.WebApi.Controllers
{
    [Route("api/Robots")]
    [Authorize]
    [ApiController]
    public class RobotsController : Controller
    {
        private readonly IRobotService _robotService;

        public RobotsController(IRobotService robotService)
        {
            _robotService = robotService;
        }

        [HttpGet("Simple")]
        public IActionResult GetSimpleRobots()
        {
            var result = _robotService.GetSimpleRobots();

            return Ok(result);
        }
    }
}