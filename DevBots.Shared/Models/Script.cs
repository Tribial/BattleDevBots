using System;
using System.Collections.Generic;
using System.Text;

namespace DevBots.Shared.Models
{
    public class Script
    {
        public long Id { get; set; }
        public string Name { get; set; }
        public Int32 Lines { get; set; }
        public virtual Player Owner { get; set; }
        public DateTime CreatedAt { get; set; }
        public DateTime UpdatedAt { get; set; }
        public Robot ForRobot { get; set; }
        public string ServerPath { get; set; }
    }
}
