namespace BankingApi.EventReceiver.Infrastructure.Entities
{
    public sealed class ProcessedMessage
    {
        public Guid MessageId { get; set; } = default!;
        public DateTime ProcessedAtUtc { get; set; }
    }
}
