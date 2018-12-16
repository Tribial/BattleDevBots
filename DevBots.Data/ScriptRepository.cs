using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using DevBots.Data.DataAccess;
using DevBots.Data.Interfaces;
using DevBots.Shared.Models;
using Microsoft.EntityFrameworkCore;

namespace DevBots.Data
{
    public class ScriptRepository : IScriptRepository
    {
        private readonly ApplicationDbContext _db;

        public ScriptRepository(ApplicationDbContext db)
        {
            _db = db;
        }

        public async Task<bool> AddAsync(Script script)
        {
            await _db.Scripts.AddAsync(script);
            return await _saveAsync();
        }

        public List<Script> GetByUser(long userId)
        {
            return _db.Scripts.Include(s => s.ForRobot).Where(s => s.Owner.UserId == userId).ToList();
        }

        public async Task<bool> RemoveAsync(Script script)
        {
            _db.Scripts.Remove(script);
            return await _saveAsync();
        }

        public Script Get(Func<Script, bool> func)
        {
            return _db.Scripts.Include(s => s.Owner).FirstOrDefault(func);
        }

        private async Task<bool> _saveAsync()
        {
            return await _db.SaveChangesAsync() > 0;
        }
    }
}
