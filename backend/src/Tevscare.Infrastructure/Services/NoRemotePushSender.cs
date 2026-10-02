using Tevscare.Application.Abstractions;

namespace Tevscare.Infrastructure.Services;

public sealed class NoRemotePushSender : IPushNotificationSender
{
    public Task SendAsync(Guid userId, string title, string body, CancellationToken cancellationToken)
    {
        return Task.CompletedTask;
    }
}
