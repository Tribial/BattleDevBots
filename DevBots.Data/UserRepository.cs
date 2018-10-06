using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;
using DevBots.Data.DataAccess;
using DevBots.Data.Interfaces;
using DevBots.Shared.Models;
using Microsoft.EntityFrameworkCore;

namespace DevBots.Data
{
    public class UserRepository : IUserRepository
    {
        private readonly ApplicationDbContext _db;

        public UserRepository(ApplicationDbContext db)
        {
            _db = db;
        }

        public User Get(Func<User, bool> func)
        {
            return _db.Users.FirstOrDefault(func);
        }

        public async Task<bool> Insert(User user)
        {
            await _db.Users.AddAsync(user);
            return await SaveAsync();
        }

        public async Task<bool> Update(User user)
        {
            _db.Users.Update(user);
            return await SaveAsync();
        }

        private async Task<bool> SaveAsync()
        {
            return await _db.SaveChangesAsync() > 0;
        }
    }
}
