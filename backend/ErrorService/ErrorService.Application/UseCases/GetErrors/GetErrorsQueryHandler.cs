using ErrorService.Application.DTOs;
using ErrorService.Domain.Interfaces;
using MediatR;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace ErrorService.Application.UseCases.GetErrors
{
    public class GetErrorsQueryHandler : IRequestHandler<GetErrorsQuery, GetErrorsResponse>
    {
        private readonly IErrorLogRepository _errorLogRepository;

        public GetErrorsQueryHandler(IErrorLogRepository errorLogRepository)
        {
            _errorLogRepository = errorLogRepository;
        }

        public async Task<GetErrorsResponse> Handle(GetErrorsQuery query, CancellationToken cancellationToken)
        {
            var domainQuery = new ErrorQuery
            {
                ServiceName = query.Request.ServiceName,
                From = query.Request.From,
                To = query.Request.To,
                Page = query.Request.Page,
                PageSize = query.Request.PageSize
            };

            var errors = await _errorLogRepository.GetAsync(domainQuery, cancellationToken);

            var errorItems = errors.Select(error => new ErrorItemDto(
                error.Id,
                error.ServiceName,
                error.ExceptionType,
                error.Message,
                error.StackTrace,
                error.OccurredAt,
                error.Endpoint,
                error.HttpMethod,
                error.StatusCode,
                error.UserId,
                error.Metadata
            )).ToList();

            return new GetErrorsResponse(errorItems, errorItems.Count, query.Request.Page, query.Request.PageSize);
        }
    }
}