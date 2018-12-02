using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace DevBots.Shared.DtoModels
{
    public class Responses<T> where T : BaseDto
    {
        public List<T> Model { get; set; }
        public List<string> Errors { get; set; }
        public bool ErrorOccured => Errors.Any();

        public Responses()
        {
            Errors = new List<string>();
        }
    }
}
