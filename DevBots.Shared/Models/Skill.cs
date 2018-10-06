using System;
using System.Collections.Generic;
using System.Text;

namespace DevBots.Shared.Models
{
    public class Skill
    {
        public int Id { get; set; }
        public string Name { get; set; }
        public int Cooldown { get; set; }
        public string Target { get; set; }
        public string AffectProp { get; set; }
        public int AffectForce { get; set; }
        public string Parameters { get; set; }
    }
}
