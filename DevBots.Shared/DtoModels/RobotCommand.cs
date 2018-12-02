using System;
using System.Collections.Generic;
using System.Text;

namespace DevBots.Shared.DtoModels
{
    public class RobotCommand : BaseDto
    {
        public bool CountsAsCommand { get; set; }
        public string Type { get; set; }
        public int? Direction { get; set; }
        public Position Position { get; set; }
        public string Error { get; set; }
        public string Say { get; set; }
        public string Console { get; set; }
    }
}
