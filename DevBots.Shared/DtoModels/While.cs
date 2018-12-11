using System;
using System.Collections.Generic;
using System.Text;

namespace DevBots.Shared.DtoModels
{
    public class While
    {
        public List<Token> Condition { get; set; }
        public List<Token> Tokens { get; set; }

        public While ()
        {
            Condition = new List<Token>();
            Tokens = new List<Token>();
        }
    }
}
