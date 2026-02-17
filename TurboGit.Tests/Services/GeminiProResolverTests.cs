using System;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using Moq;
using Moq.Protected;
using TurboGit.Services;
using Xunit;

namespace TurboGit.Tests.Services
{
    public class GeminiProResolverTests
    {
        private readonly Mock<HttpMessageHandler> _httpMessageHandlerMock;
        private readonly HttpClient _httpClient;
        private readonly AiServiceConfig _config;

        public GeminiProResolverTests()
        {
            _httpMessageHandlerMock = new Mock<HttpMessageHandler>();
            _httpClient = new HttpClient(_httpMessageHandlerMock.Object);
            _config = new AiServiceConfig { ApiKey = "test-api-key" };
        }

        [Fact]
        public async Task ResolveConflictAsync_ThrowsInvalidOperationException_WhenApiKeyIsMissing()
        {
            // Arrange
            var config = new AiServiceConfig { ApiKey = "" };
            var resolver = new GeminiProResolver(config, _httpClient);

            // Act & Assert
            await Assert.ThrowsAsync<InvalidOperationException>(() =>
                resolver.ResolveConflictAsync("conflict", "csharp"));
        }

        [Fact]
        public async Task ResolveConflictAsync_ReturnsResolvedCode_WhenApiCallIsSuccessful()
        {
            // Arrange
            var expectedCode = "public void Test() {}";
            var jsonResponse = $@"
            {{
                ""candidates"": [
                    {{
                        ""content"": {{
                            ""parts"": [
                                {{ ""text"": ""{expectedCode}"" }}
                            ]
                        }}
                    }}
                ]
            }}";

            SetupMockResponse(HttpStatusCode.OK, jsonResponse);

            var resolver = new GeminiProResolver(_config, _httpClient);

            // Act
            var result = await resolver.ResolveConflictAsync("conflict", "csharp");

            // Assert
            Assert.Equal(expectedCode, result);
        }

        [Fact]
        public async Task ResolveConflictAsync_ThrowsHttpRequestException_WhenApiReturnsError()
        {
            // Arrange
            SetupMockResponse(HttpStatusCode.BadRequest, "Bad Request");

            var resolver = new GeminiProResolver(_config, _httpClient);

            // Act & Assert
            await Assert.ThrowsAsync<HttpRequestException>(() =>
                resolver.ResolveConflictAsync("conflict", "csharp"));
        }

        [Fact]
        public async Task ResolveConflictAsync_ThrowsInvalidOperationException_WhenResponseIsEmpty()
        {
             // Arrange
            var jsonResponse = "{}"; // No candidates

            SetupMockResponse(HttpStatusCode.OK, jsonResponse);

            var resolver = new GeminiProResolver(_config, _httpClient);

            // Act & Assert
            await Assert.ThrowsAsync<InvalidOperationException>(() =>
                resolver.ResolveConflictAsync("conflict", "csharp"));
        }

        [Fact]
        public async Task ResolveConflictAsync_ExtractsCodeBlock_WhenResponseContainsMarkdown()
        {
            // Arrange
            var code = "var x = 1;";
             // JSON string with markdown code block
             var jsonResponse = $@"
            {{
                ""candidates"": [
                    {{
                        ""content"": {{
                            ""parts"": [
                                {{ ""text"": ""Here is the solution:\n```csharp\n{code}\n```\nHope it helps."" }}
                            ]
                        }}
                    }}
                ]
            }}";

            SetupMockResponse(HttpStatusCode.OK, jsonResponse);

            var resolver = new GeminiProResolver(_config, _httpClient);

            // Act
            var result = await resolver.ResolveConflictAsync("conflict", "csharp");

            // Assert
            Assert.Equal(code, result);
        }

        [Fact]
        public async Task ResolveConflictAsync_SendsApiKeyInHeader_AndNotInUrl()
        {
            // Arrange
            HttpRequestMessage? capturedRequest = null;
            _httpMessageHandlerMock
                .Protected()
                .Setup<Task<HttpResponseMessage>>(
                    "SendAsync",
                    ItExpr.IsAny<HttpRequestMessage>(),
                    ItExpr.IsAny<CancellationToken>()
                )
                .Callback<HttpRequestMessage, CancellationToken>((r, c) => capturedRequest = r)
                .ReturnsAsync(new HttpResponseMessage
                {
                    StatusCode = HttpStatusCode.OK,
                    Content = new StringContent(@"{""candidates"": [{""content"": {""parts"": [{""text"": ""fixed""}]}}]}")
                });

            var resolver = new GeminiProResolver(_config, _httpClient);

            // Act
            await resolver.ResolveConflictAsync("conflict", "csharp");

            // Assert
            Assert.NotNull(capturedRequest);
            Assert.NotNull(capturedRequest.RequestUri);
            Assert.DoesNotContain($"key={_config.ApiKey}", capturedRequest.RequestUri.ToString());
            Assert.True(capturedRequest.Headers.Contains("x-goog-api-key"));
            Assert.Equal(_config.ApiKey, capturedRequest.Headers.GetValues("x-goog-api-key").First());
        }

        private void SetupMockResponse(HttpStatusCode statusCode, string content)
        {
            _httpMessageHandlerMock
                .Protected()
                .Setup<Task<HttpResponseMessage>>(
                    "SendAsync",
                    ItExpr.IsAny<HttpRequestMessage>(),
                    ItExpr.IsAny<CancellationToken>()
                )
                .ReturnsAsync(new HttpResponseMessage
                {
                    StatusCode = statusCode,
                    Content = new StringContent(content)
                });
        }
    }
}
