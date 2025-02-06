using BenchmarkDotNet.Attributes;
using Moq;
using Moq.Protected;
using retry_example_error_502_and_504_retry_policy_dot_net_standard_2;
using System.Net;
using static retry_example_error_502_and_504_retry_policy_dot_net_standard_2.RetryPolicy;

public class RetryPolicyBenchmark
{
    private readonly RetryPolicy _retryPolicy;
    private readonly HttpRequestMessage _request;
    private readonly CancellationToken _cancellationToken;
    private readonly Mock<HttpMessageHandler> _mockHandler;

    public RetryPolicyBenchmark()
    {
        _retryPolicy = new RetryPolicy();
        _request = new HttpRequestMessage(HttpMethod.Get, "http://example.com");
        _cancellationToken = new CancellationToken();
        _mockHandler = new Mock<HttpMessageHandler>();

        // Setup the mock to return a 502 Bad Gateway response
        _mockHandler.Protected()
                    .Setup<Task<HttpResponseMessage>>(
                                                      "SendAsync",
                                                      ItExpr.IsAny<HttpRequestMessage>(),
                                                      ItExpr.IsAny<CancellationToken>())
                    .ReturnsAsync(new HttpResponseMessage(HttpStatusCode.BadGateway));
    }

    [Benchmark]
    [MinIterationCount(10)]
    [MaxIterationCount(100)]
    [WarmupCount(5)]
    public async Task BenchmarkRetryHandler()
    {
        try
        {
            var handler = _retryPolicy.CreateHttpMessageRetryHandler(3, TimeSpan.FromMilliseconds(100)) as DelegatingHandler;
            handler.InnerHandler = _mockHandler.Object;
            var httpClient = new HttpClient(handler);
            await httpClient.GetAsync("http://test.com", _cancellationToken);
        }
        catch (Exception exception)
        {
            Console.WriteLine($"BenchmarkRetryHandler failed: {exception.Message}");
        }
    }

    [Benchmark]
    [MinIterationCount(10)]
    [MaxIterationCount(100)]
    [WarmupCount(5)]
    public async Task BenchmarkRetryHandlerWithPolly()
    {
        try
        {
            var handler = _retryPolicy.CreateHttpMessageRetryHandlerWithPolly(3, TimeSpan.FromMilliseconds(100)) as DelegatingHandler;
            handler.InnerHandler = _mockHandler.Object;
            var httpClient = new HttpClient(handler);
            await httpClient.GetAsync("http://test.com", _cancellationToken);
        }
        catch (Exception exception)
        {
            Console.WriteLine($"BenchmarkRetryHandlerWithPolly failed: {exception.Message}");
        }
    }

    [Benchmark]
    [MinIterationCount(10)]
    [MaxIterationCount(100)]
    [WarmupCount(5)]
    public async Task BenchmarkRetryHandlerWithPollyAndExponentialBackoff()
    {
        try
        {
            var handler = _retryPolicy.CreateHttpMessageRetryHandlerWithPollyAndExponentialBackoff(3, TimeSpan.FromMilliseconds(100)) as DelegatingHandler;
            handler.InnerHandler = _mockHandler.Object;
            var httpClient = new HttpClient(handler);
            await httpClient.GetAsync("http://test.com", _cancellationToken);
        }
        catch (Exception exception)
        {
            Console.WriteLine($"BenchmarkRetryHandlerWithPollyAndExponentialBackoff failed: {exception.Message}");
        }
    }
}