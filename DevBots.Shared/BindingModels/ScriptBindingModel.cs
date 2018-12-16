using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.IO;
using System.Text;
using Microsoft.AspNetCore.Http;

namespace DevBots.Shared.BindingModels
{
    public class ScriptBindingModel
    {
        [Required(ErrorMessage = "You have to set a name for this script")]
        public string Name { get; set; }
        [Required(ErrorMessage = "You need to choose a robot for this script")]
        public long RobotId { get; set; }

        [Required(ErrorMessage = "You need to select a file")]
        public string Script { get; set; }
    }
}
