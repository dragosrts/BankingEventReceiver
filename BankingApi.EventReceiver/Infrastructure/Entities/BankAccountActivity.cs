namespace BankingApi.EventReceiver.Infrastructure.Entities;

public class BankAccountActivity
{
    public Guid Id { get; set; }

    public Guid BankAccountId { get; set; }

    public BankingMessageType Type { get; set; }

    public decimal PreviousBalance { get; set; }

    public decimal UpdatedBalance { get; set; }

    public decimal Amount { get; set; }

    public DateTime ActivityDate { get; set; }

    // Optimistic concurrency
    public byte[] RowVersion { get; set; } = default!;
}
