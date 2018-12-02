using System;
using System.Collections.Generic;
using System.Text;
using DevBots.Shared.Enums;

namespace DevBots.Shared.DtoModels
{
    public class Token
    {
        public Types Type { get; set; }
        public string Value { get; set; }
        public int Index { get; set; }
        public int LineNumber { get; set; }
    }
}
