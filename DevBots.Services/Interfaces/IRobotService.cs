using System;
using System.Collections.Generic;
using System.Text;
using DevBots.Shared.DtoModels;

namespace DevBots.Services.Interfaces
{
    public interface IRobotService
    {
        Responses<SimpleObjectDto> GetSimpleRobots();
    }
}
