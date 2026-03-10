using Microsoft.EntityFrameworkCore;
using NotificationService.Domain.Entities;
using NotificationService.Domain.Interfaces.Repositories;

namespace NotificationService.Infrastructure.Persistence;

public class EfOnboardingReportScheduleRepository : IOnboardingReportScheduleRepository
{
    private readonly ApplicationDbContext _context;

    public EfOnboardingReportScheduleRepository(ApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<OnboardingReportSchedule?> GetByIdempotencyKeyAsync(string idempotencyKey)
    {
        return await _context.OnboardingReportSchedules
            .AsNoTracking()
            .FirstOrDefaultAsync(s => s.IdempotencyKey == idempotencyKey);
    }

    public async Task<IEnumerable<OnboardingReportSchedule>> GetDueReportsAsync(DateTime now)
    {
        return await _context.OnboardingReportSchedules
            .Where(s => s.Status == "Scheduled" && s.DueAt <= now)
            .OrderBy(s => s.DueAt)
            .Take(50) // Process max 50 at a time to avoid long locks
            .ToListAsync();
    }

    public async Task AddAsync(OnboardingReportSchedule schedule)
    {
        await _context.OnboardingReportSchedules.AddAsync(schedule);
        await _context.SaveChangesAsync();
    }

    public async Task UpdateAsync(OnboardingReportSchedule schedule)
    {
        _context.OnboardingReportSchedules.Update(schedule);
        await _context.SaveChangesAsync();
    }
}
