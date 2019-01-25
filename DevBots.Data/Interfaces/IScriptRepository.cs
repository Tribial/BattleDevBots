using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;
using DevBots.Shared.Models;

namespace DevBots.Data.Interfaces
{
    public interface IScriptRepository
    {
        Task<bool> AddAsync(Script script);
        List<Script> GetByUser(long userId);
        Task<bool> RemoveAsync(Script script);
        Script Get(Func<Script, bool> func);
        List<Script> GetBy(Func<Script, bool> func);
    }
}
