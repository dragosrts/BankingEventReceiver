namespace BankingApi.EventReceiver.Infrastructure.Services
{
    public interface IBankAccountService
    {
        Task SaveAsync(EventMessage eventMessage, CancellationToken ct);
    }
}
