using System;
using System.Collections.Generic;
using System.Text;

namespace DevBots.Shared.DtoModels
{
    public class SimpleObjectDto : BaseDto
    {
        public long Id { get; set; }
        public string Name { get; set; }
    }
}
