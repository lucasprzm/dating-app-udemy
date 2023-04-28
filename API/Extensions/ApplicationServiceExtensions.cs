using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using API.Data;
using API.Helpers;
using API.Interfaces;
using API.Services;
using API.SignalR;
using Microsoft.EntityFrameworkCore;

namespace API.Extensions
{
    public static class ApplicationServiceExtensions
    {
        public static IServiceCollection AddApplicationServices(this IServiceCollection services, IConfiguration config)
        {
          services.AddCors();
          services.AddScoped<ITokenService, TokenService>();
          services.AddAutoMapper(typeof(AutoMapperProfiles).Assembly);
          services.AddDbContext<DataContext>(options =>
          {
              options.UseSqlite(config.GetConnectionString("DefaultConnection"));
          });
          services.Configure<CloudinarySettings>(config.GetSection("CloudinarySettings"));
          services.AddScoped<IPhotoService, PhotoService>();
          services.AddScoped<LogUserActivity>();
          services.AddSignalR();
          services.AddSingleton<PresenceTracker>();
          services.AddScoped<IUnitOfWork, UnitOfWork>();
          return services;
        }
    }
}