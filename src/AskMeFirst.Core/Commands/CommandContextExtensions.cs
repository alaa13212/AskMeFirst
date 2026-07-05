using Microsoft.Extensions.DependencyInjection;

namespace AskMeFirst.Core.Commands;

public static class CommandContextExtensions
{
    public static T Resolve<T>(this CommandContext context) where T : notnull
    {
        return context.Services.GetRequiredService<T>();
    }
}
