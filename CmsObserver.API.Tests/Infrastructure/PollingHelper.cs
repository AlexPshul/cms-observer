namespace CmsObserver.API.Tests.Infrastructure;

public static class PollingHelper
{
    public static async Task<T> WaitForAsync<T>(
        Func<Task<T>> getCurrentAsync,
        Func<T, bool> condition,
        int maxAttempts = 30,
        int delayMilliseconds = 100)
    {
        for (var attempt = 0; attempt < maxAttempts; attempt++)
        {
            var current = await getCurrentAsync();
            if (condition(current)) return current;

            await Task.Delay(delayMilliseconds);
        }

        return await getCurrentAsync();
    }
}
