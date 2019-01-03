using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using GraphQL.Http;
using GraphQL.Server.Transports.Subscriptions.Abstractions;
using Newtonsoft.Json;
using Newtonsoft.Json.Serialization;
using Xunit;

namespace GraphQL.Server.Transports.WebSockets.Tests
{
    public class WebSocketWriterPipelineFacts
    {
        private readonly WebSocketWriterPipeline _webSocketWriterPipeline;
        private readonly TestWebSocket _testWebSocket;

        public WebSocketWriterPipelineFacts()
        {
            _testWebSocket = new TestWebSocket();
            _webSocketWriterPipeline = new WebSocketWriterPipeline(_testWebSocket, new DocumentWriter(Formatting.None,
                new JsonSerializerSettings
                {
                    ContractResolver = new CamelCasePropertyNamesContractResolver(),
                    NullValueHandling = NullValueHandling.Ignore
                }));
        }

        public static IEnumerable<object[]> TestData =>
            new List<object[]>
            {
                new object[]
                {
                    new OperationMessage
                    {
                        Payload = new ExecutionResult
                        {
                            Data = new TestMessage
                            {
                                Content = "Hello world",
                                SentAt = new DateTimeOffset(2018, 12, 12, 10, 0,0, TimeSpan.Zero)
                            }
                        }
                    },
                    83
                },
                new object[]
                {
                    new OperationMessage
                    {
                        Payload = new ExecutionResult
                        {
                            Data = Enumerable.Repeat(new TestMessage
                            {
                                Content = "Hello world",
                                SentAt = new DateTimeOffset(2018, 12, 12, 10, 0,0, TimeSpan.Zero)
                            }, 10)
                        }
                    },
                    652
                },
                new object[]
                {
                    new OperationMessage
                    {
                        Payload = new ExecutionResult
                        {
                            Data = Enumerable.Repeat(new TestMessage
                            {
                                Content = "Hello world",
                                SentAt = new DateTimeOffset(2018, 12, 12, 10, 0,0, TimeSpan.Zero)
                            }, 16_000)
                        }
                    },
                    // About 1 megabyte
                    1_008_022
                },
                new object[]
                {
                    new OperationMessage
                    {
                        Payload = new ExecutionResult
                        {
                            Data = Enumerable.Repeat(new TestMessage
                            {
                                Content = "Hello world",
                                SentAt = new DateTimeOffset(2018, 12, 12, 10, 0,0, TimeSpan.Zero)
                            }, 160_000)
                        }
                    },
                    // About 10 megabytes
                    10_080_022
                },
                new object[]
                {
                    new OperationMessage
                    {
                        Payload = new ExecutionResult
                        {
                            Data = Enumerable.Repeat(new TestMessage
                            {
                                Content = "Hello world",
                                SentAt = new DateTimeOffset(2018, 12, 12, 10, 0,0, TimeSpan.Zero)
                            }, 1_600_000)
                        }
                    },
                    // About 100 megabytes
                    100_800_022
                },
            };

        [Fact]
        public async Task should_post_single_message()
        {
            var message = new OperationMessage
            {
                Payload = new ExecutionResult
                {
                    Data = new TestMessage
                    {
                        Content = "Hello world",
                        SentAt = new DateTimeOffset(2018, 12, 12, 10, 0, 0, TimeSpan.Zero)
                    }
                }
            };
            Assert.True(_webSocketWriterPipeline.Post(message));
            await _webSocketWriterPipeline.Complete();
            await _webSocketWriterPipeline.Completion;
            Assert.Single(_testWebSocket.Messages);

            var resultingJson = Encoding.UTF8.GetString(_testWebSocket.Messages.First().ToArray());
            Assert.Equal(
                "{\"payload\":{\"data\":{\"content\":\"Hello world\",\"sentAt\":\"2018-12-12T10:00:00+00:00\"}}}",
                resultingJson);
        }

        [Fact]
        public async Task should_post_array_of_10_messages()
        {
            var message = new OperationMessage
            {
                Payload = new ExecutionResult
                {
                    Data = Enumerable.Repeat(new TestMessage
                    {
                        Content = "Hello world",
                        SentAt = new DateTimeOffset(2018, 12, 12, 10, 0, 0, TimeSpan.Zero)
                    }, 10)
                }
            };
            Assert.True(_webSocketWriterPipeline.Post(message));
            await _webSocketWriterPipeline.Complete();
            await _webSocketWriterPipeline.Completion;
            Assert.Single(_testWebSocket.Messages);

            var resultingJson = Encoding.UTF8.GetString(_testWebSocket.Messages.First().ToArray());
            Assert.Equal("{\"payload\":{\"data\":[{\"content\":\"Hello world\",\"sentAt\":\"2018-12-12T10:00:00+00:00\"}," +
                         "{\"content\":\"Hello world\",\"sentAt\":\"2018-12-12T10:00:00+00:00\"}," +
                         "{\"content\":\"Hello world\",\"sentAt\":\"2018-12-12T10:00:00+00:00\"}," +
                         "{\"content\":\"Hello world\",\"sentAt\":\"2018-12-12T10:00:00+00:00\"}," +
                         "{\"content\":\"Hello world\",\"sentAt\":\"2018-12-12T10:00:00+00:00\"}," +
                         "{\"content\":\"Hello world\",\"sentAt\":\"2018-12-12T10:00:00+00:00\"}," +
                         "{\"content\":\"Hello world\",\"sentAt\":\"2018-12-12T10:00:00+00:00\"}," +
                         "{\"content\":\"Hello world\",\"sentAt\":\"2018-12-12T10:00:00+00:00\"}," +
                         "{\"content\":\"Hello world\",\"sentAt\":\"2018-12-12T10:00:00+00:00\"}," +
                         "{\"content\":\"Hello world\",\"sentAt\":\"2018-12-12T10:00:00+00:00\"}]}}",
                resultingJson);
        }

        [Theory]
        [MemberData(nameof(TestData))]
        public async Task should_post_for_any_message_length(OperationMessage message, long expectedLength)
        {
            Assert.True(_webSocketWriterPipeline.Post(message));
            await _webSocketWriterPipeline.Complete();
            await _webSocketWriterPipeline.Completion;
            Assert.Single(_testWebSocket.Messages);
            Assert.Equal(expectedLength, _testWebSocket.Messages.First().Length);
        }

        [Theory]
        [MemberData(nameof(TestData))]
        public async Task should_send_for_any_message_length(OperationMessage message, long expectedLength)
        {
            await _webSocketWriterPipeline.SendAsync(message);
            await _webSocketWriterPipeline.Complete();
            await _webSocketWriterPipeline.Completion;
            Assert.Single(_testWebSocket.Messages);
            Assert.Equal(expectedLength, _testWebSocket.Messages.First().Length);
        }
    }
}