using System;
using System.Collections.Generic;
using System.Text;

namespace DevBots.Shared.Models
{
    public class Robot
    {
        public int Id { get; set; }
        public string Name { get; set; }
        public int Speed { get; set; }
        public int Damage { get; set; }
        public int Health { get; set; }
        public int FireSpeed { get; set; }
        public virtual List<Skill> Skills { get; set; }
    }
}
