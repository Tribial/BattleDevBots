using System;
using System.Collections.Generic;
using System.Text;

namespace DevBots.Services.Interfaces
{
    public interface IConfigurationService
    {
        string GetValue(string key);
    }
}
