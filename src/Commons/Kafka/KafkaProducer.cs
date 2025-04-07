using Confluent.Kafka;
using Microsoft.Extensions.Configuration;

namespace Commons.Kafka
{
    public class KafkaProducer : IKafkaProducer
    {
        private readonly IProducer<Null, byte[]> _producer;
        public KafkaProducer(IConfiguration configuration)
        {
            ProducerConfig config = new();
            configuration.GetSection("Kafka:Producer").Bind(config);
            _producer = new ProducerBuilder<Null, byte[]>(config).Build();
        }

        public async Task ProduceAsync(string topic, byte[] Message)
        {
            Message<Null, byte[]> kafkaMessage = new()
            { Value = Message };
            await _producer.ProduceAsync(topic, kafkaMessage);
        }
    }
}
