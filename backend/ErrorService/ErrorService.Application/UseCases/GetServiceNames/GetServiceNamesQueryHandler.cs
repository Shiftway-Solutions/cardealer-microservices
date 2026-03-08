using ErrorService.Application.DTOs;
using ErrorService.Domain.Interfaces;
using MediatR;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace ErrorService.Application.UseCases.GetServiceNames
{
    public class GetServiceNamesQueryHandler : IRequestHandler<GetServiceNamesQuery, GetServiceNamesResponse>
    {
        private readonly IErrorLogRepository _errorLogRepository;

        public GetServiceNamesQueryHandler(IErrorLogRepository errorLogRepository)
        {
            _errorLogRepository = errorLogRepository;
        }

        public async Task<GetServiceNamesResponse> Handle(GetServiceNamesQuery query, CancellationToken cancellationToken)
        {
            var serviceNames = await _errorLogRepository.GetServiceNamesAsync(cancellationToken);
            return new GetServiceNamesResponse(serviceNames.ToList());
        }
    }
}