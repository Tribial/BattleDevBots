using System;
using DevBots.Data.DataAccess;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace DevBots.Data
{
    public class StartUpConfig
    {
        private readonly IConfiguration _configuration;

        public StartUpConfig(IConfiguration configuration)
        {
            _configuration = configuration;
        }

        public void PartOfConfigureServices(IServiceCollection services)
        {
            services.AddDbContext<ApplicationDbContext>(options =>
                options.UseSqlServer(_configuration.GetConnectionString("DefaultConnection")));
        }
    }
}
