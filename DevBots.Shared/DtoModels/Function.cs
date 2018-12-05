using System;
using System.Collections.Generic;
using System.Text;

namespace DevBots.Shared.DtoModels
{
    public class Function
    {
        public string Name { get; set; }
        public List<string> Params { get; set; }
        public List<Token> Tokens { get; set; }

        public Function()
        {
            Params = new List<string>();
            Tokens = new List<Token>();
        }
    }
}
