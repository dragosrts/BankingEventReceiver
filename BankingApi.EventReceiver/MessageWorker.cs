using BankingApi.EventReceiver.Infrastructure.Services;
using Microsoft.Extensions.Logging;
using System.Threading;

namespace BankingApi.EventReceiver
{
    public class MessageWorker
    {
        private readonly IServiceBusReceiver _serviceBusReceiver;
        private readonly IBankAccountService _bankAccountService;
        private readonly ILogger<MessageWorker> _logger;

        public MessageWorker(IServiceBusReceiver serviceBusReceiver, IBankAccountService bankAccountService, ILogger<MessageWorker> logger)
        {
            _serviceBusReceiver = serviceBusReceiver;
            _bankAccountService = bankAccountService;
            _logger = logger;
        }

        public async Task RunAsync(CancellationToken cancellationToken)
        {
            _logger.LogInformation("MessageWorker started.");

            EventMessage? message;

            while (!cancellationToken.IsCancellationRequested)
            {
                try
                {
                    // Get the next message from the queue
                    message = await _serviceBusReceiver.Peek();
                    if (message == null)
                    {
                        await Task.Delay(10000, cancellationToken); // no messages, wait 10 seconds
                        continue;
                    }

                    _logger.LogInformation("Processing message {MessageId} of type {MessageType}",
                        message.Id, message.MessageType);

                    try
                    {
                        // Example processing
                        await ProcessMessageAsync(message, cancellationToken);

                        // Mark as processed
                        await _serviceBusReceiver.Complete(message);

                        _logger.LogInformation("Message {MessageId} processed successfully.", message.Id);
                    }
                    catch (Exception ex)
                    {
                        _logger.LogError(ex, "Error processing message {MessageId}", message.Id);

                        message.ProcessingCount++;

                        if (message.ProcessingCount >= 3)
                        {
                            _logger.LogWarning("Message {MessageId} reached max retries. Moving to DLQ.", message.Id);
                            await _serviceBusReceiver.MoveToDeadLetter(message);
                        }
                        else
                        {
                            var retryTime = DateTime.UtcNow.AddSeconds(10 * message.ProcessingCount);
                            _logger.LogInformation("Rescheduling message {MessageId} for {RetryTime}", message.Id, retryTime);

                            await _serviceBusReceiver.ReSchedule(message, retryTime);
                        }
                    }
                }
                catch (TaskCanceledException)
                {
                    _logger.LogInformation("MessageWorker stopping...");
                    break;
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Unexpected error in worker loop.");
                    await Task.Delay(1000, cancellationToken);
                }
            }

            _logger.LogInformation("MessageWorker stopped.");
        }

        private async Task ProcessMessageAsync(EventMessage message, CancellationToken cancellationToken)
        {
            // Simulate processing
            _logger.LogInformation(
                "Processing {MessageType} of {Amount} for account {AccountId}",
                message.MessageType, message.Amount, message.BankAccountId
            );

            await _bankAccountService.SaveAsync(message, cancellationToken);
        }
    }
}