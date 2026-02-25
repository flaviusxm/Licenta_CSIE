using MediatR;
using System.Threading;
using System.Threading.Tasks;

namespace AskNLearn.Application.Features.Auth.Queries.GetSignIn
{
    public class GetSignInQueryHandler : IRequestHandler<GetSignInQuery, Unit>
    {
        public Task<Unit> Handle(GetSignInQuery request, CancellationToken cancellationToken)
        {
            return Task.FromResult(Unit.Value);
        }
    }
}
