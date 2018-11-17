using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;
using DevBots.Shared.Models;

namespace DevBots.Data.Interfaces
{
    public interface ISettingsRepository
    {
        Task<bool> InsertAsync(AccountSettings settings);
    }
}
