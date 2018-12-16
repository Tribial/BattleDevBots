using System;
using System.Collections.Generic;
using System.Text;
using AutoMapper;
using DevBots.Data.Interfaces;
using DevBots.Services.Interfaces;
using DevBots.Shared.DtoModels;
using DevBots.Shared.Models;

namespace DevBots.Services
{
    public class RobotService : IRobotService
    {
        private readonly IRobotRepository _robotRepository;

        public RobotService(IRobotRepository robotRepository)
        {
            _robotRepository = robotRepository;
        }

        public Responses<SimpleRobotDto> GetSimpleRobots()
        {
            var result = new Responses<SimpleRobotDto>();
            var robots = _robotRepository.GetAll();
            result.Model = Mapper.Map<List<SimpleRobotDto>>(robots);
            return result;
        }
    }
}
