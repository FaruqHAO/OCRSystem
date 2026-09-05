using System.Threading.Channels;

namespace IdentityDocument.Api.Processing;

/// <summary>
/// In-process async job queue. The MVP uses an unbounded Channel; swapping to
/// RabbitMQ/Kafka later only changes this type and the worker's message source.
/// </summary>
public sealed class ProcessingQueue
{
    private readonly Channel<Guid> _channel = Channel.CreateUnbounded<Guid>();

    public void Enqueue(Guid documentId) => _channel.Writer.TryWrite(documentId);

    public ChannelReader<Guid> Reader => _channel.Reader;
}