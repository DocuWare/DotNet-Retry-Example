using retry_example_error_502_and_504_retry_policy_dot_net_standard_2.Interfaces;
using System;
using System.Net;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;

namespace retry_example_error_502_and_504_retry_policy_dot_net_standard_2
{
    public class RetryPolicy : IRetryPolicy
    {
        public HttpMessageHandler CreateHttpMessageRetryHandler(int maxRetries, TimeSpan delay)
        {
            return new RetryHandler(maxRetries, delay);
        }

        public class RetryHandler : DelegatingHandler
        {
            private readonly int _maxRetries;
            private readonly TimeSpan _delay;

            public RetryHandler(int maxRetries, TimeSpan delay)
            {
                _maxRetries = maxRetries;
                _delay = delay;
            }

            protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
            {
                HttpResponseMessage response = null;
                for (int i = 0; i < _maxRetries; i++)
                {
                    response = await base.SendAsync(request, cancellationToken);
                    if (response.StatusCode != HttpStatusCode.BadGateway && response.StatusCode != HttpStatusCode.GatewayTimeout)
                    {
                        return response;
                    }
                    await Task.Delay(_delay, cancellationToken);
                }
                return response;
            }
        }
    }
}
