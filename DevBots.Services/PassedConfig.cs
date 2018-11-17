using System;
using AutoMapper;
using DevBots.Data;
using DevBots.Data.Interfaces;
using DevBots.Shared;
using DevBots.Shared.BindingModels;
using DevBots.Shared.DtoModels;
using DevBots.Shared.Models;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace DevBots.Services
{
    public class PassedConfig
    {
        public static void Config(IConfiguration configuration, IServiceCollection services)
        {
            var config = new StartUpConfig(configuration);
            config.PartOfConfigureServices(services);

            services.AddTransient<IUserRepository, UserRepository>();
            services.AddTransient<ITokenRepository, TokenRepository>();
            services.AddTransient<IPlayerRepository, PlayerRepository>();
            services.AddTransient<ISettingsRepository, SettingsRepository>();

            AutoMapperConfiguration();
        }

        private static void AutoMapperConfiguration()
        {
            Mapper.Initialize(config =>
            {
                config.CreateMap<RegisterUserBindingModel, User>()
                    .ForMember(dest => dest.PasswordHash, opt => opt.MapFrom(src => src.Password.ToHash()));

                config.CreateMap<User, LoginDto>();
            });
        }
    }
}
