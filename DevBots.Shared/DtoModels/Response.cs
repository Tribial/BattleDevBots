using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace DevBots.Shared.DtoModels
{
    public class Response<T> where T: BaseDto
    {
        public T Model { get; set; }
        public List<String> Errors { get; set; }
        public bool ErrorOccured => Errors.Any();

        public Response()
        {
            Errors = new List<string>();
        }
    }
}
