using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;
using DevBots.Shared.Models;

namespace DevBots.Data.Interfaces
{
    public interface IUserRepository
    {
        User Get(Func<User, bool> func);
        Task<bool> Insert(User user);
        Task<bool> Update(User user);
    }
}
