namespace Services
{
    public interface IKafkaProducerService
    {
        /// <summary>
        /// Publishes a message to the configured Kafka topic.
        /// The key decides the partition, so all messages with the same key keep their order.
        /// </summary>
        Task SendNotificationAsync(string key, string messageValue);
    }
}
