using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using ErrorService.Domain.Entities;

namespace ErrorService.Domain.Interfaces
{
    public interface IErrorLogRepository
    {
        Task<ErrorLog?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);
        Task<IEnumerable<ErrorLog>> GetAsync(ErrorQuery query, CancellationToken cancellationToken = default);
        Task AddAsync(ErrorLog errorLog, CancellationToken cancellationToken = default);
        Task DeleteAsync(Guid id, CancellationToken cancellationToken = default);
        Task<IEnumerable<string>> GetServiceNamesAsync(CancellationToken cancellationToken = default);
        Task<ErrorStats> GetStatsAsync(DateTime? from = null, DateTime? to = null, CancellationToken cancellationToken = default);
    }

    public class ErrorQuery
    {
        public string? ServiceName { get; set; }
        public DateTime? From { get; set; }
        public DateTime? To { get; set; }
        public int Page { get; set; } = 1;
        public int PageSize { get; set; } = 50;
    }

    public class ErrorStats
    {
        public int TotalErrors { get; set; }
        public int ErrorsLast24Hours { get; set; }
        public int ErrorsLast7Days { get; set; }
        public Dictionary<string, int> ErrorsByService { get; set; } = new Dictionary<string, int>();
        public Dictionary<int, int> ErrorsByStatusCode { get; set; } = new Dictionary<int, int>();
    }
}