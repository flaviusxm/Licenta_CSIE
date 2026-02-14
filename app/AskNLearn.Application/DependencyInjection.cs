using AskNLearn.Application.Common.Behaviours;
using MediatR;
using Microsoft.Extensions.DependencyInjection;
using System.Reflection;

namespace AskNLearn.Application
{
    public static class DependencyInjection
    {
        public static IServiceCollection AddApplicationServices(this IServiceCollection services)
        {
            services.AddMediatR(cfg => {
                cfg.RegisterServicesFromAssembly(Assembly.GetExecutingAssembly());
                cfg.AddOpenBehavior(typeof(LoggingBehavior<,>));
            });

            return services;
        }
    }
}
