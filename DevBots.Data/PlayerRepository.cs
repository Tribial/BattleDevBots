using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;
using DevBots.Data.DataAccess;
using DevBots.Data.Interfaces;
using DevBots.Shared.Models;

namespace DevBots.Data
{
    public class PlayerRepository : IPlayerRepository
    {
        private readonly ApplicationDbContext _db;

        public PlayerRepository(ApplicationDbContext db)
        {
            _db = db;
        }

        public async Task<bool> InsertAsync(Player player)
        {
            await _db.Players.AddAsync(player);
            return await _saveAsync();
        }

        private async Task<bool> _saveAsync()
        {
            return await _db.SaveChangesAsync() > 0;
        }
    }
}
