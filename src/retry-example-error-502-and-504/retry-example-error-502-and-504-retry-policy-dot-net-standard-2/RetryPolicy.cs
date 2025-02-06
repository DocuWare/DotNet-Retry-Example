using retry_example_error_502_and_504_retry_policy_dot_net_standard_2.Interfaces;
using Polly;
using Polly.Contrib.WaitAndRetry;
using Polly.Extensions.Http;
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

        public HttpMessageHandler CreateHttpMessageRetryHandlerWithPolly(int maxRetries, TimeSpan delay)
        {
            return new RetryHandlerWithPolly(maxRetries, delay);
        }
        
        public HttpMessageHandler CreateHttpMessageRetryHandlerWithPollyAndExponentialBackoff(int maxRetries, TimeSpan delay)
        {
            return new RetryHandlerWithPollyAndExponentialBackoff(maxRetries, delay);
        }

        public class RetryHandler : DelegatingHandler
        {
            private readonly int _maxRetries;
            private readonly TimeSpan _delay;
            private readonly Random _jitterer;

            public RetryHandler(int maxRetries, TimeSpan delay)
            {
                _maxRetries = maxRetries;
                _delay = delay;
                _jitterer = new Random();
            }

            protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
            {
                HttpResponseMessage response = null;
                for (int i = 0; i < _maxRetries; i++)
                {
                    response = await base.SendAsync(request, cancellationToken);
                    // Retry on 502(BadGateway), 503(ServiceUnavailable), 504(GatewayTimeout) status codes
                    if (response.StatusCode != HttpStatusCode.BadGateway && 
                        response.StatusCode != HttpStatusCode.ServiceUnavailable && 
                        response.StatusCode != HttpStatusCode.GatewayTimeout)
                    {
                        return response;
                    }
                    var jitteredDelay = _delay + TimeSpan.FromMilliseconds(_jitterer.Next(0, 100));
                    await Task.Delay(jitteredDelay, cancellationToken);
                }
                return response;
            }
        }

        public class RetryHandlerWithPolly : DelegatingHandler
        {
            private readonly IAsyncPolicy<HttpResponseMessage> _retryPolicy;

            public RetryHandlerWithPolly(int maxRetries, TimeSpan delay)
            {
                _retryPolicy = HttpPolicyExtensions
                               .HandleTransientHttpError()
                               .OrResult(msg => msg.StatusCode == HttpStatusCode.BadGateway ||
                                                msg.StatusCode == HttpStatusCode.ServiceUnavailable ||
                                                msg.StatusCode == HttpStatusCode.GatewayTimeout)
                               .WaitAndRetryAsync(maxRetries, retryAttempt =>
                                                      delay + TimeSpan.FromMilliseconds(new Random().Next(0, 100)));
            }

            protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
            {
                return _retryPolicy.ExecuteAsync(ct => base.SendAsync(request, ct), cancellationToken);
            }
        }

        // usage: https://learn.microsoft.com/en-us/dotnet/architecture/microservices/implement-resilient-applications/implement-http-call-retries-exponential-backoff-polly#add-a-jitter-strategy-to-the-retry-policy
        public class RetryHandlerWithPollyAndExponentialBackoff : DelegatingHandler
        {
            private readonly IAsyncPolicy<HttpResponseMessage> _retryPolicy;

            public RetryHandlerWithPollyAndExponentialBackoff(int maxRetries, TimeSpan delay)
            {
                _retryPolicy = HttpPolicyExtensions
                               .HandleTransientHttpError()
                               .WaitAndRetryAsync(Backoff.DecorrelatedJitterBackoffV2(medianFirstRetryDelay: delay, retryCount: maxRetries));
            }

            protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
            {
                return _retryPolicy.ExecuteAsync(ct => base.SendAsync(request, ct), cancellationToken);
            }
        }
    }
}
