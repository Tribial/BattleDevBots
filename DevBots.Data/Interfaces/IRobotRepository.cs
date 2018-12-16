using System;
using System.Collections.Generic;
using System.Text;
using DevBots.Shared.Models;

namespace DevBots.Data.Interfaces
{
    public interface IRobotRepository
    {
        List<Robot> GetAll();
        Robot Get(Func<Robot, bool> func);
    }
}
