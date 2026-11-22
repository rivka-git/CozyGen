using Confluent.Kafka;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace Services
{
    /// <summary>
    /// Thin wrapper around a Confluent.Kafka producer. Registered as a singleton:
    /// a producer is thread-safe and expensive to create, so one instance is shared.
    /// </summary>
    public sealed class KafkaProducerService : IKafkaProducerService, IDisposable
    {
        private readonly IProducer<string, string> _producer;
        private readonly string _topicName;
        private readonly ILogger<KafkaProducerService> _logger;

        public KafkaProducerService(IConfiguration configuration, ILogger<KafkaProducerService> logger)
        {
            _logger = logger;
            _topicName = configuration["KafkaConfig:TopicName"] ?? "order-notifications";

            var config = new ProducerConfig
            {
                BootstrapServers = configuration["KafkaConfig:BootstrapServers"] ?? "localhost:9092"
            };

            _producer = new ProducerBuilder<string, string>(config).Build();
        }

        public async Task SendNotificationAsync(string key, string messageValue)
        {
            try
            {
                var message = new Message<string, string> { Key = key, Value = messageValue };
                var result = await _producer.ProduceAsync(_topicName, message);
                _logger.LogInformation("Kafka message sent to {Topic} partition {Partition} offset {Offset}",
                    result.Topic, result.Partition.Value, result.Offset.Value);
            }
            catch (ProduceException<string, string> e)
            {
                _logger.LogError("Kafka produce failed: {Reason}", e.Error.Reason);
            }
        }

        public void Dispose()
        {
            _producer.Flush(TimeSpan.FromSeconds(5));
            _producer.Dispose();
        }
    }
}
