using BankingApi.EventReceiver.Infrastructure.Entities;
using Microsoft.EntityFrameworkCore;

namespace BankingApi.EventReceiver.Infrastructure
{
    public class BankingApiDbContext : DbContext
    {
        public DbSet<BankAccount> BankAccounts { get; set; }

        public DbSet<BankAccountActivity> BankAccountActivities { get; set; }

        public DbSet<ProcessedMessage> ProcessedMessages { get; set; }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            modelBuilder.Entity<BankAccount>()
                .Property(b => b.RowVersion)
                .IsRowVersion();

            modelBuilder.Entity<BankAccountActivity>()
                .HasKey(p => p.Id);

            modelBuilder.Entity<ProcessedMessage>()
                .HasKey(p => p.MessageId);
        }
    }
}
