using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Threading.Tasks;
using AutoMapper;
using DevBots.Data.Interfaces;
using DevBots.Services.Interfaces;
using DevBots.Shared.BindingModels;
using DevBots.Shared.DtoModels;
using DevBots.Shared.Models;

namespace DevBots.Services
{
    public class ScriptService : IScriptService
    {
        private readonly IRobotRepository _robotRepository;
        private readonly IUserRepository _userRepository;
        private readonly IScriptRepository _scriptRepository;
        private readonly IPlayerRepository _playerRepository;

        public ScriptService(IRobotRepository robotRepository, IUserRepository userRepository, IPlayerRepository playerRepository, IScriptRepository scriptRepository)
        {
            _robotRepository = robotRepository;
            _userRepository = userRepository;
            _playerRepository = playerRepository;
            _scriptRepository = scriptRepository;
        }

        public async Task<Response<BaseDto>> AddAsync(ScriptBindingModel scriptBindingModel, string userName)
        {
            var result = new Response<BaseDto>();
            var lines = scriptBindingModel.Script.Split('\n').Length;
            var robot = _robotRepository.Get(r => r.Id == scriptBindingModel.RobotId);
            var user = _userRepository.Get(u => u.Username == userName);
            var filePath = $@"../Scripts/{userName}/{scriptBindingModel.Name}.rl";

            if (user == null)
            {
                result.Errors.Add("Something went wrong, please log out and log in to proceed");
                return result;
            }

            var player = _playerRepository.Get(p => p.UserId == user.Id);

            if (robot == null)
            {
                result.Errors.Add("The robot you have selected does not exist");
                return result;
            }
            if (File.Exists(filePath))
            {
                result.Errors.Add($"A script named {scriptBindingModel.Name} already exists");
                return result;
            }
            //try
            //{
            //    File.CreateText(filePath);
            //}
            //catch (Exception e)
            //{
            //    result.Errors.Add(e.Message);
            //    result.Errors.Add(e.InnerException.Message);
            //    return result;
            //}
            using (var writer = File.CreateText(filePath))
            {
                try
                {
                    writer.Write(scriptBindingModel.Script); //or .Write(), if you wish
                }
                catch (Exception e)
                {
                    result.Errors.Add(e.Message);
                    result.Errors.Add(e.InnerException.Message);
                    return result;
                }
            }
            

            //here goes script validation

            var script = new Script
            {
                CreatedAt = DateTime.Now,
                ForRobot = robot,
                Lines = lines,
                Name = scriptBindingModel.Name,
                ServerPath = filePath,
                Owner = player,
                UpdatedAt = DateTime.Now,
            };

            var insertResult = await _scriptRepository.AddAsync(script);

            if (!insertResult)
            {
                result.Errors.Add("Something went wrong, please contact DevBots support");
                return result;
            }

            return result;
        }

        public Responses<ScriptForListDto> GetToDisplay(long userId)
        {
            var result = new Responses<ScriptForListDto>();
            var scripts = _scriptRepository.GetByUser(userId);
            result.Model = Mapper.Map<List<ScriptForListDto>>(scripts);
            return result;
        }

        public async Task<Response<BaseDto>> RemoveAsync(long scriptId, long userId)
        {
            var result = new Response<BaseDto>();
            var script = _scriptRepository.Get(s => s.Id == scriptId);
            if (script == null)
            {
                result.Errors.Add("Something went wrong, this script does not exist");
                return result;
            }

            if (script.Owner.UserId != userId)
            {
                result.Errors.Add("You can't remove this script, since you are not the owner");
                return result;
            }

            var removeResult = await _scriptRepository.RemoveAsync(script);
            if (!removeResult)
            {
                result.Errors.Add("Something went wrong, try again later");
                return result;
            }

            File.Delete(script.ServerPath);

            return result;
        }

        public Responses<SimpleObjectDto> GetSimpleByRobotId(long robotId, long userId)
        {
            var result = new Responses<SimpleObjectDto>();
            var scripts = _scriptRepository.GetBy(s => s.ForRobot.Id == robotId && s.Owner.UserId == userId);
            result.Model = Mapper.Map<List<SimpleObjectDto>>(scripts);
            return result;
        }
    }
}
