using Moq;
using Moq.Protected;
using retry_example_error_502_and_504_retry_policy_dot_net_standard_2;
using System.Net;

namespace retry_example_error_502_and_504_tests
{
    public class RetryPolicyTests
    {
        [Fact]
        public void CreateHttpMessageRetryHandler_ShouldReturnRetryHandler()
        {
            // Arrange
            var retryPolicy = new RetryPolicy();
            int maxRetries = 3;
            TimeSpan delay = TimeSpan.FromSeconds(1);

            // Act
            var handler = retryPolicy.CreateHttpMessageRetryHandler(maxRetries, delay);

            // Assert
            Assert.NotNull(handler);
            Assert.IsType<RetryPolicy.RetryHandler>(handler);
        }

        [Fact]
        public async Task CreateHttpMessageRetryHandlerWithPolly_ShouldRetryOnTransientError502()
        {
            // Arrange
            var maxRetries = 3;
            var delay = TimeSpan.FromMilliseconds(1);
            var retryPolicy = new RetryPolicy();
            var handler = retryPolicy.CreateHttpMessageRetryHandlerWithPolly(maxRetries - 1, delay) as DelegatingHandler;

            var mockInnerHandler = new Mock<HttpMessageHandler>();
            mockInnerHandler
                .Protected()
                .Setup<Task<HttpResponseMessage>>(
                    "SendAsync",
                    ItExpr.IsAny<HttpRequestMessage>(),
                    ItExpr.IsAny<CancellationToken>())
                .ReturnsAsync(new HttpResponseMessage(HttpStatusCode.BadGateway));

            handler.InnerHandler = mockInnerHandler.Object;

            var invoker = new HttpMessageInvoker(handler);

            // Act
            var response = await invoker.SendAsync(new HttpRequestMessage(HttpMethod.Get, "http://test.com"), CancellationToken.None);

            // Assert
            Assert.Equal(HttpStatusCode.BadGateway, response.StatusCode);
            mockInnerHandler.Protected().Verify(
                "SendAsync",
                Times.Exactly(maxRetries),
                ItExpr.IsAny<HttpRequestMessage>(),
                ItExpr.IsAny<CancellationToken>());
        }

        [Fact]
        public async Task CreateHttpMessageRetryHandlerWithPolly_ShouldRetryOnTransientError503()
        {
            // Arrange
            var maxRetries = 3;
            var delay = TimeSpan.FromMilliseconds(1);
            var retryPolicy = new RetryPolicy();
            var handler = retryPolicy.CreateHttpMessageRetryHandlerWithPolly(maxRetries - 1, delay) as DelegatingHandler;

            var mockInnerHandler = new Mock<HttpMessageHandler>();
            mockInnerHandler
                .Protected()
                .Setup<Task<HttpResponseMessage>>(
                    "SendAsync",
                    ItExpr.IsAny<HttpRequestMessage>(),
                    ItExpr.IsAny<CancellationToken>())
                .ReturnsAsync(new HttpResponseMessage(HttpStatusCode.ServiceUnavailable));

            handler.InnerHandler = mockInnerHandler.Object;

            var invoker = new HttpMessageInvoker(handler);

            // Act
            var response = await invoker.SendAsync(new HttpRequestMessage(HttpMethod.Get, "http://test.com"), CancellationToken.None);

            // Assert
            Assert.Equal(HttpStatusCode.ServiceUnavailable, response.StatusCode);
            mockInnerHandler.Protected().Verify(
                "SendAsync",
                Times.Exactly(maxRetries),
                ItExpr.IsAny<HttpRequestMessage>(),
                ItExpr.IsAny<CancellationToken>());
        }

        [Fact]
        public async Task CreateHttpMessageRetryHandlerWithPolly_ShouldRetryOnTransientError504()
        {
            // Arrange
            var maxRetries = 3;
            var delay = TimeSpan.FromMilliseconds(1);
            var retryPolicy = new RetryPolicy();
            var handler = retryPolicy.CreateHttpMessageRetryHandlerWithPolly(maxRetries - 1, delay) as DelegatingHandler;

            var mockInnerHandler = new Mock<HttpMessageHandler>();
            mockInnerHandler
                .Protected()
                .Setup<Task<HttpResponseMessage>>(
                    "SendAsync",
                    ItExpr.IsAny<HttpRequestMessage>(),
                    ItExpr.IsAny<CancellationToken>())
                .ReturnsAsync(new HttpResponseMessage(HttpStatusCode.GatewayTimeout));

            handler.InnerHandler = mockInnerHandler.Object;

            var invoker = new HttpMessageInvoker(handler);

            // Act
            var response = await invoker.SendAsync(new HttpRequestMessage(HttpMethod.Get, "http://test.com"), CancellationToken.None);

            // Assert
            Assert.Equal(HttpStatusCode.GatewayTimeout, response.StatusCode);
            mockInnerHandler.Protected().Verify(
                "SendAsync",
                Times.Exactly(maxRetries),
                ItExpr.IsAny<HttpRequestMessage>(),
                ItExpr.IsAny<CancellationToken>());
        }

        [Fact]
        public async Task CreateHttpMessageRetryHandlerWithPolly_ShouldNotRetryOnNonTransientErrors()
        {
            // Arrange
            var maxRetries = 3;
            var delay = TimeSpan.FromMilliseconds(1);
            var handler = new RetryPolicy.RetryHandlerWithPolly(maxRetries, delay);

            var handlerMock = new Mock<HttpMessageHandler>(MockBehavior.Strict);
            handlerMock
                .Protected()
                .Setup<Task<HttpResponseMessage>>(
                    "SendAsync",
                    ItExpr.IsAny<HttpRequestMessage>(),
                    ItExpr.IsAny<CancellationToken>())
                .ReturnsAsync(new HttpResponseMessage(HttpStatusCode.OK))
                .Verifiable();

            handler.InnerHandler = handlerMock.Object;

            var httpClient = new HttpClient(handler);

            // Act
            var response = await httpClient.GetAsync("http://test.com");

            // Assert
            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
            handlerMock.Protected().Verify(
                "SendAsync",
                Times.Once(),
                ItExpr.IsAny<HttpRequestMessage>(),
                ItExpr.IsAny<CancellationToken>());
        }

        [Fact]
        public async Task CreateHttpMessageRetryHandlerWithPollyAndExponentialBackoff_ShouldRetryOnTransientError408()
        {
            // Arrange
            var maxRetries = 3;
            var delay = TimeSpan.FromMilliseconds(1);
            var retryPolicy = new RetryPolicy();
            var handler = retryPolicy.CreateHttpMessageRetryHandlerWithPollyAndExponentialBackoff(maxRetries - 1, delay) as DelegatingHandler;

            var mockInnerHandler = new Mock<HttpMessageHandler>();
            mockInnerHandler.Protected()
                            .Setup<Task<HttpResponseMessage>>("SendAsync", ItExpr.IsAny<HttpRequestMessage>(), ItExpr.IsAny<CancellationToken>())
                            .ReturnsAsync(new HttpResponseMessage(HttpStatusCode.RequestTimeout));

            handler.InnerHandler = mockInnerHandler.Object;

            var invoker = new HttpMessageInvoker(handler);

            // Act
            var response = await invoker.SendAsync(new HttpRequestMessage(HttpMethod.Get, "http://test.com"), CancellationToken.None);

            // Assert
            Assert.Equal(HttpStatusCode.RequestTimeout, response.StatusCode);
            mockInnerHandler.Protected().Verify("SendAsync", Times.Exactly(maxRetries), ItExpr.IsAny<HttpRequestMessage>(), ItExpr.IsAny<CancellationToken>());
        }

        [Fact]
        public async Task CreateHttpMessageRetryHandlerWithPollyAndExponentialBackoff_ShouldRetryOnTransientError500()
        {
            // Arrange
            var maxRetries = 3;
            var delay = TimeSpan.FromMilliseconds(1);
            var retryPolicy = new RetryPolicy();
            var handler = retryPolicy.CreateHttpMessageRetryHandlerWithPollyAndExponentialBackoff(maxRetries - 1, delay) as DelegatingHandler;

            var mockInnerHandler = new Mock<HttpMessageHandler>();
            mockInnerHandler.Protected()
                            .Setup<Task<HttpResponseMessage>>("SendAsync", ItExpr.IsAny<HttpRequestMessage>(), ItExpr.IsAny<CancellationToken>())
                            .ReturnsAsync(new HttpResponseMessage(HttpStatusCode.InternalServerError));

            handler.InnerHandler = mockInnerHandler.Object;

            var invoker = new HttpMessageInvoker(handler);

            // Act
            var response = await invoker.SendAsync(new HttpRequestMessage(HttpMethod.Get, "http://test.com"), CancellationToken.None);

            // Assert
            Assert.Equal(HttpStatusCode.InternalServerError, response.StatusCode);
            mockInnerHandler.Protected().Verify("SendAsync", Times.Exactly(maxRetries), ItExpr.IsAny<HttpRequestMessage>(), ItExpr.IsAny<CancellationToken>());
        }

        [Fact]
        public async Task CreateHttpMessageRetryHandlerWithPollyAndExponentialBackoff_ShouldRetryOnTransientError502()
        {
            // Arrange
            var maxRetries = 3;
            var delay = TimeSpan.FromMilliseconds(1);
            var retryPolicy = new RetryPolicy();
            var handler = retryPolicy.CreateHttpMessageRetryHandlerWithPollyAndExponentialBackoff(maxRetries - 1, delay) as DelegatingHandler;

            var mockInnerHandler = new Mock<HttpMessageHandler>();
            mockInnerHandler.Protected()
                            .Setup<Task<HttpResponseMessage>>("SendAsync", ItExpr.IsAny<HttpRequestMessage>(), ItExpr.IsAny<CancellationToken>())
                            .ReturnsAsync(new HttpResponseMessage(HttpStatusCode.BadGateway));

            handler.InnerHandler = mockInnerHandler.Object;

            var invoker = new HttpMessageInvoker(handler);

            // Act
            var response = await invoker.SendAsync(new HttpRequestMessage(HttpMethod.Get, "http://test.com"), CancellationToken.None);

            // Assert
            Assert.Equal(HttpStatusCode.BadGateway, response.StatusCode);
            mockInnerHandler.Protected().Verify("SendAsync", Times.Exactly(maxRetries), ItExpr.IsAny<HttpRequestMessage>(), ItExpr.IsAny<CancellationToken>());
        }

        [Fact]
        public async Task CreateHttpMessageRetryHandlerWithPollyAndExponentialBackoff_ShouldRetryOnTransientError503()
        {
            // Arrange
            var maxRetries = 3;
            var delay = TimeSpan.FromMilliseconds(1);
            var retryPolicy = new RetryPolicy();
            var handler = retryPolicy.CreateHttpMessageRetryHandlerWithPollyAndExponentialBackoff(maxRetries - 1, delay) as DelegatingHandler;

            var mockInnerHandler = new Mock<HttpMessageHandler>();
            mockInnerHandler.Protected()
                            .Setup<Task<HttpResponseMessage>>("SendAsync", ItExpr.IsAny<HttpRequestMessage>(), ItExpr.IsAny<CancellationToken>())
                            .ReturnsAsync(new HttpResponseMessage(HttpStatusCode.ServiceUnavailable));

            handler.InnerHandler = mockInnerHandler.Object;

            var invoker = new HttpMessageInvoker(handler);

            // Act
            var response = await invoker.SendAsync(new HttpRequestMessage(HttpMethod.Get, "http://test.com"), CancellationToken.None);

            // Assert
            Assert.Equal(HttpStatusCode.ServiceUnavailable, response.StatusCode);
            mockInnerHandler.Protected().Verify("SendAsync", Times.Exactly(maxRetries), ItExpr.IsAny<HttpRequestMessage>(), ItExpr.IsAny<CancellationToken>());
        }

        [Fact]
        public async Task CreateHttpMessageRetryHandlerWithPollyAndExponentialBackoff_ShouldRetryOnTransientError504()
        {
            // Arrange
            var maxRetries = 3;
            var delay = TimeSpan.FromMilliseconds(1);
            var retryPolicy = new RetryPolicy();
            var handler = retryPolicy.CreateHttpMessageRetryHandlerWithPollyAndExponentialBackoff(maxRetries - 1, delay) as DelegatingHandler;

            var mockInnerHandler = new Mock<HttpMessageHandler>();
            mockInnerHandler.Protected()
                            .Setup<Task<HttpResponseMessage>>("SendAsync", ItExpr.IsAny<HttpRequestMessage>(), ItExpr.IsAny<CancellationToken>())
                            .ReturnsAsync(new HttpResponseMessage(HttpStatusCode.GatewayTimeout));

            handler.InnerHandler = mockInnerHandler.Object;

            var invoker = new HttpMessageInvoker(handler);

            // Act
            var response = await invoker.SendAsync(new HttpRequestMessage(HttpMethod.Get, "http://test.com"), CancellationToken.None);

            // Assert
            Assert.Equal(HttpStatusCode.GatewayTimeout, response.StatusCode);
            mockInnerHandler.Protected().Verify("SendAsync", Times.Exactly(maxRetries), ItExpr.IsAny<HttpRequestMessage>(), ItExpr.IsAny<CancellationToken>());
        }

        [Fact]
        public async Task CreateHttpMessageRetryHandlerWithPollyAndExponentialBackoff_ShouldNotRetryOnNonTransientErrors()
        {
            // Arrange
            var maxRetries = 3;
            var delay = TimeSpan.FromMilliseconds(1);

            var handler = new RetryPolicy.RetryHandlerWithPollyAndExponentialBackoff(maxRetries, delay);

            var handlerMock = new Mock<HttpMessageHandler>(MockBehavior.Strict);
            handlerMock
                .Protected()
                .Setup<Task<HttpResponseMessage>>(
                                                  "SendAsync",
                                                  ItExpr.IsAny<HttpRequestMessage>(),
                                                  ItExpr.IsAny<CancellationToken>())
                .ReturnsAsync(new HttpResponseMessage(HttpStatusCode.OK))
                .Verifiable();

            handler.InnerHandler = handlerMock.Object;

            var httpClient = new HttpClient(handler);

            // Act
            var response = await httpClient.GetAsync("http://test.com");

            // Assert
            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
            handlerMock.Protected().Verify(
                                           "SendAsync",
                                           Times.Once(),
                                           ItExpr.IsAny<HttpRequestMessage>(),
                                           ItExpr.IsAny<CancellationToken>());
        }

        [Fact]
        public async Task CreateRetryHandler_ShouldRetryOn502()
        {
            // Arrange
            var maxRetries = 3;
            var delay = TimeSpan.FromMilliseconds(1);

            var handler = new RetryPolicy.RetryHandler(maxRetries, delay);

            var handlerMock = new Mock<HttpMessageHandler>(MockBehavior.Strict);
            handlerMock
                .Protected()
                .Setup<Task<HttpResponseMessage>>(
                    "SendAsync",
                    ItExpr.IsAny<HttpRequestMessage>(),
                    ItExpr.IsAny<CancellationToken>())
                .ReturnsAsync(new HttpResponseMessage(HttpStatusCode.BadGateway))
                .Verifiable();

            handler.InnerHandler = handlerMock.Object;
            
            var httpClient = new HttpClient(handler);

            // Act
            var response = await httpClient.GetAsync("http://test.com");

            // Assert
            Assert.Equal(HttpStatusCode.BadGateway, response.StatusCode);
            handlerMock.Protected().Verify(
                "SendAsync",
                Times.Exactly(3),
                ItExpr.IsAny<HttpRequestMessage>(),
                ItExpr.IsAny<CancellationToken>());
        }

        [Fact]
        public async Task CreateRetryHandler_ShouldRetryOn503()
        {
            // Arrange
            var maxRetries = 3;
            var delay = TimeSpan.FromMilliseconds(1);

            var handler = new RetryPolicy.RetryHandler(maxRetries, delay);

            var handlerMock = new Mock<HttpMessageHandler>(MockBehavior.Strict);
            handlerMock
                .Protected()
                .Setup<Task<HttpResponseMessage>>(
                                                  "SendAsync",
                                                  ItExpr.IsAny<HttpRequestMessage>(),
                                                  ItExpr.IsAny<CancellationToken>())
                .ReturnsAsync(new HttpResponseMessage(HttpStatusCode.ServiceUnavailable))
                .Verifiable();

            handler.InnerHandler = handlerMock.Object;

            var httpClient = new HttpClient(handler);

            // Act
            var response = await httpClient.GetAsync("http://test.com");

            // Assert
            Assert.Equal(HttpStatusCode.ServiceUnavailable, response.StatusCode);
            handlerMock.Protected().Verify(
                                           "SendAsync",
                                           Times.Exactly(3),
                                           ItExpr.IsAny<HttpRequestMessage>(),
                                           ItExpr.IsAny<CancellationToken>());
        }

        [Fact]
        public async Task CreateRetryHandler_ShouldRetryOn504()
        {
            // Arrange
            var maxRetries = 3;
            var delay = TimeSpan.FromMilliseconds(1);

            var handler = new RetryPolicy.RetryHandler(maxRetries, delay);

            var handlerMock = new Mock<HttpMessageHandler>(MockBehavior.Strict);
            handlerMock
                .Protected()
                .Setup<Task<HttpResponseMessage>>(
                                                  "SendAsync",
                                                  ItExpr.IsAny<HttpRequestMessage>(),
                                                  ItExpr.IsAny<CancellationToken>())
                .ReturnsAsync(new HttpResponseMessage(HttpStatusCode.GatewayTimeout))
                .Verifiable();

            handler.InnerHandler = handlerMock.Object;

            var httpClient = new HttpClient(handler);

            // Act
            var response = await httpClient.GetAsync("http://test.com");

            // Assert
            Assert.Equal(HttpStatusCode.GatewayTimeout, response.StatusCode);
            handlerMock.Protected().Verify(
                                           "SendAsync",
                                           Times.Exactly(3),
                                           ItExpr.IsAny<HttpRequestMessage>(),
                                           ItExpr.IsAny<CancellationToken>());
        }

        [Fact]
        public async Task CreateRetryHandler_ShouldNotRetryOnOtherStatusCodes()
        {
            // Arrange
            var maxRetries = 3;
            var delay = TimeSpan.FromMilliseconds(1);

            var handler = new RetryPolicy.RetryHandler(maxRetries, delay);

            var handlerMock = new Mock<HttpMessageHandler>(MockBehavior.Strict);
            handlerMock
                .Protected()
                .Setup<Task<HttpResponseMessage>>(
                    "SendAsync",
                    ItExpr.IsAny<HttpRequestMessage>(),
                    ItExpr.IsAny<CancellationToken>())
                .ReturnsAsync(new HttpResponseMessage(HttpStatusCode.OK))
                .Verifiable();

            handler.InnerHandler = handlerMock.Object;

            var httpClient = new HttpClient(handler);

            // Act
            var response = await httpClient.GetAsync("http://test.com");

            // Assert
            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
            handlerMock.Protected().Verify(
                "SendAsync",
                Times.Once(),
                ItExpr.IsAny<HttpRequestMessage>(),
                ItExpr.IsAny<CancellationToken>());
        }
    }
}
