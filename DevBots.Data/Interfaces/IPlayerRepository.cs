using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;
using DevBots.Shared.Models;

namespace DevBots.Data.Interfaces
{
    public interface IPlayerRepository
    {
        Task<bool> InsertAsync(Player player);
        Player Get(Func<Player, bool> func);
    }
}
