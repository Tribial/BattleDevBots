using System;
using System.Collections.Generic;
using System.Text;
using DevBots.Services.Interfaces;
using Microsoft.Extensions.Configuration;

namespace DevBots.Services
{
    public class ConfigurationService : IConfigurationService
    {
        private readonly IConfiguration _configuration;

        public ConfigurationService(IConfiguration configuration)
        {
            _configuration = configuration;
        }

        public string GetValue(string key)
        {
            return _configuration[key];
        }
    }
}
