using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using DevBots.Data.DataAccess;
using DevBots.Data.Interfaces;
using DevBots.Shared.Models;

namespace DevBots.Data
{
    public class TokenRepository : ITokenRepository
    {
        private readonly ApplicationDbContext _db;

        public TokenRepository(ApplicationDbContext db)
        {
            _db = db;
        }

        public RefreshToken Get(Func<RefreshToken, bool> func)
        {
            return _db.RefreshTokens.FirstOrDefault(func);
        }

        public async Task<bool> InsertAsync(RefreshToken token)
        {
            await _db.RefreshTokens.AddAsync(token);
            return await SaveAsync();
        }

        public async Task<bool> RemoveAsync(RefreshToken token)
        {
            _db.Remove(token);
            return await SaveAsync();
        }

        public async Task<bool> SaveAsync()
        {
            return await _db.SaveChangesAsync() > 0;
        }
    }
}
