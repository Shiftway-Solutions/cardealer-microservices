using NotificationService.Domain.Entities;

namespace NotificationService.Domain.Interfaces.Repositories;

public interface IOnboardingReportScheduleRepository
{
    Task<OnboardingReportSchedule?> GetByIdempotencyKeyAsync(string idempotencyKey);
    Task<IEnumerable<OnboardingReportSchedule>> GetDueReportsAsync(DateTime now);
    Task AddAsync(OnboardingReportSchedule schedule);
    Task UpdateAsync(OnboardingReportSchedule schedule);
}
