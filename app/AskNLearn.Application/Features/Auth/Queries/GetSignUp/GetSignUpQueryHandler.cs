using MediatR;
using System.Threading;
using System.Threading.Tasks;

namespace AskNLearn.Application.Features.Auth.Queries.GetSignUp
{
    public class GetSignUpQueryHandler : IRequestHandler<GetSignUpQuery, Unit>
    {
        public Task<Unit> Handle(GetSignUpQuery request, CancellationToken cancellationToken)
        {
            return Task.FromResult(Unit.Value);
        }
    }
}
