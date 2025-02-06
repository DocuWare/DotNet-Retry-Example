using System;
using System.Net.Http;

namespace retry_example_error_502_and_504_retry_policy_dot_net_standard_2.Interfaces
{
    public interface IRetryPolicy
    {
        HttpMessageHandler CreateHttpMessageRetryHandler(int maxRetries, TimeSpan delay);

        HttpMessageHandler CreateHttpMessageRetryHandlerWithPolly(int maxRetries, TimeSpan delay);

        HttpMessageHandler CreateHttpMessageRetryHandlerWithPollyAndExponentialBackoff(int maxRetries, TimeSpan delay);
    }
}