using GymManagementSystem_DAL.Data;
using GymManagementSystem_DAL.Data.Interceptors;
using GymManagementSystem_DAL.Repositories;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace GymManagementSystem_DAL
{
    public static class DependencyInjection
    {
        public static IServiceCollection AddGymDataAccess(this IServiceCollection services, IConfiguration configuration)
        {
            services.AddSingleton<BaseEntityInterceptor>();

            services.AddDbContext<GymDbContext>((sp, options) =>
            {
                options.UseSqlServer(configuration.GetConnectionString("DefaultConnection"))
                       .AddInterceptors(sp.GetRequiredService<BaseEntityInterceptor>());
            });

            services.AddScoped(typeof(IGenericRepository<>), typeof(GenericRepository<>));
            services.AddScoped<IPlanRepository, PlanRepository>();
            services.AddScoped<IMemberRepository, MemberRepository>();
            services.AddScoped<ITrainerRepository, TrainerRepository>();

            return services;
        }
    }
}
