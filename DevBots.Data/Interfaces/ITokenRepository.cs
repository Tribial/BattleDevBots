using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;
using DevBots.Shared.Models;

namespace DevBots.Data.Interfaces
{
    public interface ITokenRepository
    {
        RefreshToken Get(Func<RefreshToken, bool> func);
        Task<bool> InsertAsync(RefreshToken token);
        Task<bool> RemoveAsync(RefreshToken token);
    }
}
