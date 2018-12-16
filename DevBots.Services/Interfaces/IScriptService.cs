using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;
using DevBots.Shared.BindingModels;
using DevBots.Shared.DtoModels;
using DevBots.Shared.Models;

namespace DevBots.Services.Interfaces
{
    public interface IScriptService
    {
        Task<Response<BaseDto>> AddAsync(ScriptBindingModel scriptBindingModel, string userName);
        Responses<ScriptForListDto> GetToDisplay(long userId);
        Task<Response<BaseDto>> RemoveAsync(long scriptId, long userId);
    }
}
