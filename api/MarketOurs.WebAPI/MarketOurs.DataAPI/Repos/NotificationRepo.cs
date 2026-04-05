using MarketOurs.Data;
using MarketOurs.Data.DataModels;
using Microsoft.EntityFrameworkCore;

namespace MarketOurs.DataAPI.Repos;

public interface INotificationRepo
{
    Task<List<NotificationModel>> GetUserNotificationsAsync(string userId, int pageIndex, int pageSize);
    Task<int> GetUserUnreadCountAsync(string userId);
    Task<int> CountAsync(string userId);
    Task<NotificationModel?> GetByIdAsync(string id);
    Task CreateAsync(NotificationModel notification);
    Task UpdateAsync(NotificationModel notification);
    Task MarkAllAsReadAsync(string userId);
    Task DeleteAsync(string id);
}

public class NotificationRepo(IDbContextFactory<MarketContext> factory) : INotificationRepo
{
    public async Task<List<NotificationModel>> GetUserNotificationsAsync(string userId, int pageIndex, int pageSize)
    {
        await using var context = await factory.CreateDbContextAsync();
        return await context.Notifications
            .Where(x => x.UserId == userId)
            .OrderByDescending(x => x.CreatedAt)
            .Skip((pageIndex - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync();
    }

    public async Task<int> GetUserUnreadCountAsync(string userId)
    {
        await using var context = await factory.CreateDbContextAsync();
        return await context.Notifications.CountAsync(x => x.UserId == userId && !x.IsRead);
    }

    public async Task<int> CountAsync(string userId)
    {
        await using var context = await factory.CreateDbContextAsync();
        return await context.Notifications.CountAsync(x => x.UserId == userId);
    }

    public async Task<NotificationModel?> GetByIdAsync(string id)
    {
        await using var context = await factory.CreateDbContextAsync();
        return await context.Notifications.FindAsync(id);
    }

    public async Task CreateAsync(NotificationModel notification)
    {
        await using var context = await factory.CreateDbContextAsync();
        context.Notifications.Add(notification);
        await context.SaveChangesAsync();
    }

    public async Task UpdateAsync(NotificationModel notification)
    {
        await using var context = await factory.CreateDbContextAsync();
        context.Notifications.Update(notification);
        await context.SaveChangesAsync();
    }

    public async Task MarkAllAsReadAsync(string userId)
    {
        await using var context = await factory.CreateDbContextAsync();
        await context.Notifications
            .Where(x => x.UserId == userId && !x.IsRead)
            .ExecuteUpdateAsync(s => s.SetProperty(b => b.IsRead, true));
    }

    public async Task DeleteAsync(string id)
    {
        await using var context = await factory.CreateDbContextAsync();
        await context.Notifications
            .Where(x => x.Id == id)
            .ExecuteDeleteAsync();
    }
}
