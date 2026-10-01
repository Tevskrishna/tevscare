using FluentValidation;
using Microsoft.Extensions.DependencyInjection;
using Tevscare.Application.Validation;

namespace Tevscare.Application;

public static class DependencyInjection
{
    public static IServiceCollection AddApplication(this IServiceCollection services)
    {
        services.AddValidatorsFromAssemblyContaining<RegisterRequestValidator>();
        return services;
    }
}
