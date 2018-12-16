using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using DevBots.Data.DataAccess;
using DevBots.Data.Interfaces;
using DevBots.Shared.Models;

namespace DevBots.Data
{
    public class RobotRepository : IRobotRepository
    {
        private readonly ApplicationDbContext _db;

        public RobotRepository(ApplicationDbContext db)
        {
            _db = db;
        }

        public List<Robot> GetAll()
        {
            return _db.Robots.ToList();
        }

        public Robot Get(Func<Robot, bool> func)
        {
            return _db.Robots.FirstOrDefault(func);
        }
    }
}
