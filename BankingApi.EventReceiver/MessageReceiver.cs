using Azure.Messaging.ServiceBus;
using BankingApi.EventReceiver;
using System.Text.Json;

public class MessageReceiver : IServiceBusReceiver
{
    private readonly ServiceBusReceiver _receiver;
    private readonly ServiceBusClient _client;

    // Store messages we have received but not yet completed/abandoned
    private readonly Dictionary<Guid, ServiceBusReceivedMessage> _inFlightMessages = new();

    public MessageReceiver(ServiceBusClient client, string queueName)
    {
        _client = client;
        _receiver = client.CreateReceiver(queueName);
    }

    public async Task<EventMessage?> Peek()
    {
        // Receive and lock the message so we can complete/abandon later
        var sbMessage = await _receiver.ReceiveMessageAsync(TimeSpan.FromSeconds(5));
        if (sbMessage == null)
            return null;

        // Deserialize from the Service Bus body
        var eventMessage = JsonSerializer.Deserialize<EventMessage>(sbMessage.Body);
        if (eventMessage == null)
            return null;

        _inFlightMessages[eventMessage.Id] = sbMessage;

        return eventMessage;
    }

    public async Task Abandon(EventMessage message)
    {
        if (!_inFlightMessages.TryGetValue(message.Id, out var sbMessage))
            throw new InvalidOperationException("Message not found in in-flight store.");

        await _receiver.AbandonMessageAsync(sbMessage);
        _inFlightMessages.Remove(message.Id);
    }

    public async Task Complete(EventMessage message)
    {
        if (!_inFlightMessages.TryGetValue(message.Id, out var sbMessage))
            throw new InvalidOperationException("Message not found in in-flight store.");

        await _receiver.CompleteMessageAsync(sbMessage);
        _inFlightMessages.Remove(message.Id);
    }

    public async Task ReSchedule(EventMessage message, DateTime nextAvailableTime)
    {
        if (!_inFlightMessages.TryGetValue(message.Id, out var sbMessage))
            throw new InvalidOperationException("Message not found in in-flight store.");

        // Clone the original message
        var cloned = new ServiceBusMessage(sbMessage.Body)
        {
            ContentType = sbMessage.ContentType,
            CorrelationId = sbMessage.CorrelationId,
            Subject = sbMessage.Subject
        };

        // Use the stored _client to send
        var sender = _client.CreateSender(_receiver.EntityPath);
        await sender.ScheduleMessageAsync(cloned, nextAvailableTime);

        // Complete the old one
        await _receiver.CompleteMessageAsync(sbMessage);
        _inFlightMessages.Remove(message.Id);
    }

    public async Task MoveToDeadLetter(EventMessage message)
    {
        if (!_inFlightMessages.TryGetValue(message.Id, out var sbMessage))
            throw new InvalidOperationException("Message not found in in-flight store.");

        await _receiver.DeadLetterMessageAsync(sbMessage);
        _inFlightMessages.Remove(message.Id);
    }
}
