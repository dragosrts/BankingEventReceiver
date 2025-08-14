namespace BankingApi.EventReceiver.Infrastructure.Entities;

public class BankAccount
{
    public Guid Id { get; set; }

    public decimal Balance { get; set; }

    // Optimistic concurrency
    public byte[] RowVersion { get; set; } = default!;
}
