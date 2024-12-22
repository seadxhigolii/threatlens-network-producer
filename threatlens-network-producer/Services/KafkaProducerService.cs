using Confluent.Kafka;
using System.Text.Json;
using threatlens_network_producer.Common;

namespace threatlens_network_producer.Services
{
    public class KafkaProducerService
    {
        private readonly IProducer<Null, string> _producer;
        private readonly string _topic;

        public KafkaProducerService(KafkaProducerConfig config)
        {
            var producerConfig = new ProducerConfig { BootstrapServers = config.BootstrapServers, AllowAutoCreateTopics = true };
            _producer = new ProducerBuilder<Null, string>(producerConfig).Build();
            _topic = config.Topic;
        }

        public async Task ProduceAsync(object data)
        {
            var jsonData = JsonSerializer.Serialize(data);
            try
            {
                var deliveryResult = await _producer.ProduceAsync(
                    _topic,
                    new Message<Null, string> { Value = jsonData }
                );
                Console.WriteLine($"Message delivered to {deliveryResult.TopicPartitionOffset}");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Failed to produce message: {ex.Message}");
            }
        }
    }
}
