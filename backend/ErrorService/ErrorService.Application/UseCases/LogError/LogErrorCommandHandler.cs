using ErrorService.Application.DTOs;
using ErrorService.Application.Helpers;
using ErrorService.Domain.Entities;
using ErrorService.Domain.Interfaces;
using ErrorService.Application.Metrics;
using MediatR;
using System.Threading;
using System.Threading.Tasks;
using System.Diagnostics;

namespace ErrorService.Application.UseCases.LogError
{
    public class LogErrorCommandHandler : IRequestHandler<LogErrorCommand, LogErrorResponse>
    {
        private readonly IErrorLogRepository _errorLogRepository;
        private readonly ErrorServiceMetrics _metrics;

        public LogErrorCommandHandler(
            IErrorLogRepository errorLogRepository,
            ErrorServiceMetrics metrics)
        {
            _errorLogRepository = errorLogRepository;
            _metrics = metrics;
        }

        public async Task<LogErrorResponse> Handle(LogErrorCommand command, CancellationToken cancellationToken)
        {
            var stopwatch = Stopwatch.StartNew();
            var success = false;

            try
            {
                var errorLog = new ErrorLog
                {
                    Id = System.Guid.NewGuid(),
                    ServiceName = command.Request.ServiceName,
                    ExceptionType = command.Request.ExceptionType,
                    Message = command.Request.Message,
                    StackTrace = StackTraceSanitizer.Sanitize(command.Request.StackTrace),
                    OccurredAt = command.Request.OccurredAt ?? System.DateTime.UtcNow,
                    Endpoint = command.Request.Endpoint,
                    HttpMethod = command.Request.HttpMethod,
                    StatusCode = command.Request.StatusCode,
                    UserId = command.Request.UserId,
                    Metadata = command.Request.Metadata ?? new System.Collections.Generic.Dictionary<string, object>()
                };

                await _errorLogRepository.AddAsync(errorLog, cancellationToken);

                // Registrar métricas
                _metrics.RecordErrorLogged(
                    serviceName: command.Request.ServiceName,
                    statusCode: command.Request.StatusCode ?? 500,
                    exceptionType: command.Request.ExceptionType);

                success = true;
                return new LogErrorResponse(errorLog.Id);
            }
            finally
            {
                stopwatch.Stop();
                _metrics.RecordProcessingDuration(
                    durationMs: stopwatch.Elapsed.TotalMilliseconds,
                    serviceName: command.Request.ServiceName,
                    success: success);
            }
        }
    }
}