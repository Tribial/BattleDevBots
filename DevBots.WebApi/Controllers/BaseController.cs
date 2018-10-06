using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using DevBots.Shared.DtoModels;
using Microsoft.AspNetCore.Mvc;

namespace DevBots.WebApi.Controllers
{
    public class BaseController : Controller
    {
        protected Response<BaseDto> ModelStateErrors()
        {
            var response = new Response<BaseDto>();

            foreach (var key in ModelState.Keys)
            {
                var value = ViewData.ModelState[key];

                foreach (var error in value.Errors)
                {
                    response.Errors.Add(error.Exception != null
                        ? $"{key}: Nieprawidłowy format danych"
                        : $"{key}: {error.ErrorMessage}");
                }
            }
            return response;
        }
    }
}