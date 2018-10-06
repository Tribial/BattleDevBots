using System;
using System.Collections.Generic;
using System.Text;

namespace DevBots.Shared.Models
{
    public class Script
    {
        public long Id { get; set; }
        public virtual Player Owner { get; set; }
        public DateTime CreatedAt { get; set; }
        public Robot ForRobot { get; set; }
        public string Path { get; set; }
        public bool IsValid { get; set; }

    }
}
