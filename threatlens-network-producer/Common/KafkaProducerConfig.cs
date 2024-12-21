namespace threatlens_network_producer.Common
{
    public class KafkaProducerConfig
    {
        public string BootstrapServers { get; set; } = "localhost:9092";
        public string Topic { get; set; } = "network-packets";
    }

}
