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
    /// <summary>
    /// A class which implements the IRetryPolicy interface and provides methods to create HttpMessageHandler with different retry policies.
    /// </summary>
    public class RetryPolicy : IRetryPolicy
    {
        /// <summary>
        /// Creates a HttpMessageHandler with a retry policy which retries the request for a specified number of times with a fixed delay.
        /// </summary>
        /// <param name="maxRetries">The maximum number of retries.</param>
        /// <param name="delay">The delay time for the wait between the calls.</param>
        /// <returns>A HttpMessageHandler with a retry policy which retries the request for a specified number of times with a fixed delay.</returns>
        public HttpMessageHandler CreateHttpMessageRetryHandler(int maxRetries, TimeSpan delay)
        {
            return new RetryHandler(maxRetries, delay);
        }

        /// <summary>
        /// Creates a HttpMessageHandler with a retry policy which retries the request for a specified number of times with a fixed delay using Polly.
        /// </summary>
        /// <param name="maxRetries">The maximum number of retries.</param>
        /// <param name="delay">The delay time for the wait between the calls.</param>
        /// <returns>A HttpMessageHandler with a retry policy which retries the request for a specified number of times with a fixed delay using Polly.</returns>
        public HttpMessageHandler CreateHttpMessageRetryHandlerWithPolly(int maxRetries, TimeSpan delay)
        {
            return new RetryHandlerWithPolly(maxRetries, delay);
        }

        /// <summary>
        /// Creates a HttpMessageHandler with a retry policy which retries the request for a specified number of times with an exponential backoff using Polly.
        /// </summary>
        /// <param name="maxRetries">The maximum number of retries.</param>
        /// <param name="delay">The delay time for the initial wait between the calls.</param>
        /// <returns>A HttpMessageHandler with a retry policy which retries the request for a specified number of times with an exponential backoff using Polly.</returns>
        public HttpMessageHandler CreateHttpMessageRetryHandlerWithPollyAndExponentialBackoff(int maxRetries, TimeSpan delay)
        {
            return new RetryHandlerWithPollyAndExponentialBackoff(maxRetries, delay);
        }

        /// <summary>
        /// A class which extends the DelegatingHandler class and implements a retry policy which retries the request for a specified number of times with a fixed delay.
        /// </summary>
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

            /// <summary>
            /// Sends the request and retries the request for a specified number of times with a fixed delay.
            /// </summary>
            /// <param name="request">The HttpRequestMessage object which represents the request to be sent.</param>
            /// <param name="cancellationToken">The CancellationToken object which propagates notification that operations should be canceled.</param>
            /// <returns>A Task object which represents the asynchronous operation.</returns>
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

        /// <summary>
        /// A class which extends the DelegatingHandler class and implements a retry policy which retries the request for a specified number of times with a fixed delay using Polly.
        /// </summary>
        public class RetryHandlerWithPolly : DelegatingHandler
        {
            private readonly IAsyncPolicy<HttpResponseMessage> _retryPolicy;

            /// <summary>
            /// Constructor to create a RetryHandlerWithPolly object.
            /// </summary>
            /// <param name="maxRetries">The maximum number of retries.</param>
            /// <param name="delay">The delay time for the wait between the calls.</param>
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

            /// <summary>
            /// Sends the request and retries the request for a specified number of times with a fixed delay using Polly.
            /// </summary>
            /// <param name="request">The HttpRequestMessage object which represents the request to be sent.</param>
            /// <param name="cancellationToken">The CancellationToken object which propagates notification that operations should be canceled.</param>
            /// <returns>A Task object which represents the asynchronous operation.</returns>
            protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
            {
                return _retryPolicy.ExecuteAsync(ct => base.SendAsync(request, ct), cancellationToken);
            }
        }

        // usage: https://learn.microsoft.com/en-us/dotnet/architecture/microservices/implement-resilient-applications/implement-http-call-retries-exponential-backoff-polly#add-a-jitter-strategy-to-the-retry-policy
        /// <summary>
        /// A class which extends the DelegatingHandler class and implements a retry policy which retries the request for a specified number of times with an exponential backoff using Polly.
        /// </summary>
        public class RetryHandlerWithPollyAndExponentialBackoff : DelegatingHandler
        {
            private readonly IAsyncPolicy<HttpResponseMessage> _retryPolicy;

            /// <summary>
            /// Constructor to create a RetryHandlerWithPollyAndExponentialBackoff object.
            /// </summary>
            /// <param name="maxRetries">The maximum number of retries.</param>
            /// <param name="delay">The delay time for the wait between the calls.</param>
            public RetryHandlerWithPollyAndExponentialBackoff(int maxRetries, TimeSpan delay)
            {
                _retryPolicy = HttpPolicyExtensions
                               .HandleTransientHttpError()
                               .WaitAndRetryAsync(Backoff.DecorrelatedJitterBackoffV2(medianFirstRetryDelay: delay, retryCount: maxRetries));
            }

            /// <summary>
            /// Sends the request and retries the request for a specified number of times with an exponential backoff using Polly.
            /// </summary>
            /// <param name="request">The HttpRequestMessage object which represents the request to be sent.</param>
            /// <param name="cancellationToken">The CancellationToken object which propagates notification that operations should be canceled.</param>
            /// <returns>A Task object which represents the asynchronous operation</returns>
            protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
            {
                return _retryPolicy.ExecuteAsync(ct => base.SendAsync(request, ct), cancellationToken);
            }
        }
    }
}
