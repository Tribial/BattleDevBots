using System;
using System.Collections.Generic;
using System.Text;

namespace DevBots.Shared.DtoModels
{
    public class ScriptForListDto : BaseDto
    {
        public long Id { get; set; }
        public string Name { get; set; }
        public DateTime LastUpdate { get; set; }
        public string ForBot { get; set; }
        public int Lines { get; set; }
    }
}
