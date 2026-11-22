using Confluent.Kafka;

namespace OrderConsumer
{
    /// <summary>
    /// Standalone consumer for the "order-notifications" topic.
    /// The API publishes an event after every new order; this process reads them
    /// independently, so a slow or failing consumer never blocks checkout.
    /// Next step: send the order-confirmation email from here.
    /// </summary>
    class Program
    {
        static void Main(string[] args)
        {
            var bootstrapServers = Environment.GetEnvironmentVariable("KAFKA_BOOTSTRAP_SERVERS") ?? "localhost:9092";
            var topic = Environment.GetEnvironmentVariable("KAFKA_TOPIC") ?? "order-notifications";

            var config = new ConsumerConfig
            {
                BootstrapServers = bootstrapServers,
                // A fixed group id: Kafka remembers the committed offset of the group, so a restart
                // continues where it stopped, and several instances share the partitions.
                GroupId = "order-notifications-consumer",
                AutoOffsetReset = AutoOffsetReset.Earliest,
                // Commit manually, only after a message was handled (at-least-once delivery).
                EnableAutoCommit = false,
                ApiVersionFallbackMs = 0,
                BrokerAddressFamily = BrokerAddressFamily.V4,
                SessionTimeoutMs = 45000,
                HeartbeatIntervalMs = 3000,
            };

            using var cts = new CancellationTokenSource();
            Console.CancelKeyPress += (_, e) => { e.Cancel = true; cts.Cancel(); };

            using var consumer = new ConsumerBuilder<string, string>(config)
                .SetErrorHandler((_, err) =>
                    Console.WriteLine($"[ERROR] {err.Code} | {err.Reason} | Fatal:{err.IsFatal}"))
                .SetPartitionsAssignedHandler((c, partitions) =>
                    Console.WriteLine($"[ASSIGNED] {string.Join(", ", partitions)}"))
                .SetPartitionsRevokedHandler((c, partitions) =>
                    Console.WriteLine($"[REVOKED] {string.Join(", ", partitions)}"))
                .Build();

            consumer.Subscribe(topic);
            Console.WriteLine($"[Consumer] Subscribed to '{topic}' on {bootstrapServers}. Waiting for messages...");

            try
            {
                while (!cts.Token.IsCancellationRequested)
                {
                    try
                    {
                        var result = consumer.Consume(cts.Token);
                        if (result is null) continue;

                        Console.ForegroundColor = ConsoleColor.Green;
                        Console.WriteLine($"\n[Order] Key:{result.Message.Key} Partition:{result.Partition} Offset:{result.Offset}");
                        Console.ResetColor();
                        Console.WriteLine($"Value: {result.Message.Value}");

                        consumer.Commit(result);
                    }
                    catch (ConsumeException e)
                    {
                        Console.WriteLine($"[ConsumeException] {e.Error.Code} | {e.Error.Reason}");
                    }
                }
            }
            catch (OperationCanceledException) { }
            finally
            {
                consumer.Close();
                Console.WriteLine("[Consumer] Stopped.");
            }
        }
    }
}
