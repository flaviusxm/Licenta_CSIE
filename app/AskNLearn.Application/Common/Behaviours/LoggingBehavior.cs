using MediatR;
using Serilog;
using System.Diagnostics;

namespace AskNLearn.Application.Common.Behaviours
{
    public class LoggingBehavior<TRequest, TResponse> : IPipelineBehavior<TRequest, TResponse>
        where TRequest : notnull
    {
        public async Task<TResponse> Handle(TRequest request, RequestHandlerDelegate<TResponse> next, CancellationToken cancellationToken)
        {
            var requestName = typeof(TRequest).Name;
            var uniqueId = Guid.NewGuid().ToString();
            var logger = Log.ForContext<LoggingBehavior<TRequest, TResponse>>();
            
            logger.Information("Begin Request Id:{UniqueId} RequestName:{Name} {@Request}", uniqueId, requestName, request);
            
            var timer = new Stopwatch();
            timer.Start();

            try
            {
                var response = await next();
                
                timer.Stop();
                var elapsedMilliseconds = timer.ElapsedMilliseconds;

                if (elapsedMilliseconds > 500)
                {
                    logger.Warning("Long Running Request Id:{UniqueId} RequestName:{Name} ({ElapsedMilliseconds} milliseconds) {@Request}", 
                        uniqueId, requestName, elapsedMilliseconds, request);
                }
                else
                {
                    logger.Information("End Request Id:{UniqueId} RequestName:{Name} ({ElapsedMilliseconds} milliseconds)", 
                        uniqueId, requestName, elapsedMilliseconds);
                }

                return response;
            }
            catch (Exception ex)
            {
                timer.Stop();
                logger.Error(ex, "Request Failure Id:{UniqueId} RequestName:{Name} {@Request}", uniqueId, requestName, request);
                throw;
            }
        }
    }
}
