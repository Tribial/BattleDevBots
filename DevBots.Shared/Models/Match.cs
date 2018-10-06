using System;
using System.Collections.Generic;
using System.Text;

namespace DevBots.Shared.Models
{
    public class Match
    {
        public long Id { get; set; }
        public virtual Player Player { get; set; }
        public virtual Player Enemy { get; set; }
        public bool HasWon { get; set; }
        public DateTime Date { get; set; }

    }
}
