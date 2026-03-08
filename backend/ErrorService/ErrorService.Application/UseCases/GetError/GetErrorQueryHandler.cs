using ErrorService.Application.DTOs;
using ErrorService.Domain.Interfaces;
using MediatR;
using System.Threading;
using System.Threading.Tasks;

namespace ErrorService.Application.UseCases.GetError
{
    public class GetErrorQueryHandler : IRequestHandler<GetErrorQuery, GetErrorResponse?>
    {
        private readonly IErrorLogRepository _errorLogRepository;

        public GetErrorQueryHandler(IErrorLogRepository errorLogRepository)
        {
            _errorLogRepository = errorLogRepository;
        }

        public async Task<GetErrorResponse?> Handle(GetErrorQuery query, CancellationToken cancellationToken)
        {
            var errorLog = await _errorLogRepository.GetByIdAsync(query.Request.Id, cancellationToken);
            if (errorLog == null)
                return null; // Esto está bien con el tipo nullable

            return new GetErrorResponse(
                errorLog.Id,
                errorLog.ServiceName,
                errorLog.ExceptionType,
                errorLog.Message,
                errorLog.StackTrace,
                errorLog.OccurredAt,
                errorLog.Endpoint,
                errorLog.HttpMethod,
                errorLog.StatusCode,
                errorLog.UserId,
                errorLog.Metadata
            );
        }
    }
}