namespace Commons.Kafka
{
    public interface IKafkaProducer
    {
        Task ProduceAsync(string topic, byte[] Message);
    }
}
