using BankingApi.EventReceiver.Infrastructure.Entities;

namespace BankingApi.EventReceiver;

public class EventMessage
{
    public Guid Id { get; set; }
    public int ProcessingCount { get; set; }
    public BankingMessageType MessageType { get; set; }
    public Guid BankAccountId { get; set; }
    public decimal Amount { get; set; }
}
