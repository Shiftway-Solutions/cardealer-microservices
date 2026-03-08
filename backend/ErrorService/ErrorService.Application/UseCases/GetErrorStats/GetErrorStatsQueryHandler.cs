using ErrorService.Application.DTOs;
using ErrorService.Domain.Interfaces;
using MediatR;
using System.Threading;
using System.Threading.Tasks;

namespace ErrorService.Application.UseCases.GetErrorStats
{
    public class GetErrorStatsQueryHandler : IRequestHandler<GetErrorStatsQuery, GetErrorStatsResponse>
    {
        private readonly IErrorLogRepository _errorLogRepository;

        public GetErrorStatsQueryHandler(IErrorLogRepository errorLogRepository)
        {
            _errorLogRepository = errorLogRepository;
        }

        public async Task<GetErrorStatsResponse> Handle(GetErrorStatsQuery query, CancellationToken cancellationToken)
        {
            var stats = await _errorLogRepository.GetStatsAsync(query.Request.From, query.Request.To, cancellationToken);

            return new GetErrorStatsResponse(
                stats.TotalErrors,
                stats.ErrorsLast24Hours,
                stats.ErrorsLast7Days,
                stats.ErrorsByService,
                stats.ErrorsByStatusCode
            );
        }
    }
}