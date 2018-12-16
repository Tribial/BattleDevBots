using System;
using System.Collections.Generic;
using System.Text;

namespace DevBots.Shared.DtoModels
{
    public class SimpleRobotDto : BaseDto
    {
        public long Id { get; set; }
        public string Name { get; set; }
    }
}
