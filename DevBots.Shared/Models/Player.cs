using System;
using System.Collections.Generic;
using System.Text;

namespace DevBots.Shared.Models
{
    public class Player
    {
        public long Id { get; set; }
        public long UserId { get; set; }
        public virtual List<Match> Matches { get; set; }
        public virtual List<Script> Scripts { get; set; }
        
    }
}
