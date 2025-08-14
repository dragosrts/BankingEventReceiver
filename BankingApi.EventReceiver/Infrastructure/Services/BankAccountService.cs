using BankingApi.EventReceiver.Infrastructure.Entities;
using Microsoft.EntityFrameworkCore;

namespace BankingApi.EventReceiver.Infrastructure.Services
{
    public sealed class BankAccountService : IBankAccountService
    {
        private readonly BankingApiDbContext _context;

        public BankAccountService(BankingApiDbContext context) => _context = context;

        public async Task SaveAsync(EventMessage eventMessage, CancellationToken cancellationToken)
        {
            // Idempotency check (unique key on MessageId)
            var already = await _context.ProcessedMessages.AnyAsync(p => p.MessageId == eventMessage.Id, cancellationToken);
            if (already) return;

            cancellationToken.ThrowIfCancellationRequested();

            using var tx = await _context.Database.BeginTransactionAsync(cancellationToken);
            try
            {
                var account = await _context.BankAccounts
                    .SingleOrDefaultAsync(a => a.Id == eventMessage.BankAccountId, cancellationToken)
                    ?? throw new InvalidOperationException($"Account {eventMessage.Id} not found.");

                var previousBalance = account.Balance;

                switch (eventMessage.MessageType)
                {
                    case BankingMessageType.Credit:
                        account.Balance += eventMessage.Amount;
                        break;
                    case BankingMessageType.Debit:
                        account.Balance -= eventMessage.Amount;
                        break;
                    default:
                        throw new InvalidOperationException($"Unsupported type: {eventMessage.MessageType}");
                }

                // Audit record
                _context.BankAccountActivities.Add(
                    new BankAccountActivity
                    {
                        Id = Guid.NewGuid(),
                        BankAccountId = account.Id,
                        Type = eventMessage.MessageType,
                        PreviousBalance = previousBalance,
                        UpdatedBalance = account.Balance,
                        Amount = eventMessage.Amount,
                        ActivityDate = DateTime.UtcNow
                    });

                _context.ProcessedMessages.Add(
                    new ProcessedMessage
                    {
                        MessageId = eventMessage.Id,
                        ProcessedAtUtc = DateTime.UtcNow
                    });

                await _context.SaveChangesAsync(cancellationToken);
                await tx.CommitAsync(cancellationToken);
            }
            catch (DbUpdateConcurrencyException)
            {
                // Clear tracked state to reload fresh values and retry
                foreach (var e in _context.ChangeTracker.Entries()) e.State = EntityState.Detached;
            }
        }
    }
}