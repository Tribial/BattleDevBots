using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;
using DevBots.Data.DataAccess;
using DevBots.Data.Interfaces;
using DevBots.Shared.Models;

namespace DevBots.Data
{
    public class SettingsRepository : ISettingsRepository
    {
        private readonly ApplicationDbContext _db;

        public SettingsRepository(ApplicationDbContext db)
        {
            _db = db;
        }

        public async Task<bool> InsertAsync(AccountSettings settings)
        {
            await _db.AccountSettings.AddAsync(settings);
            return await _saveAsync();
        }

        private async Task<bool> _saveAsync()
        {
            return await _db.SaveChangesAsync() > 0;
        }
    }
}
