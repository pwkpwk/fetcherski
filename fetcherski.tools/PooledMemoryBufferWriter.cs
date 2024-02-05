using System.Buffers;

namespace fetcherski.tools;

public class PooledMemoryBufferWriter(int capacity) : IBufferWriter<byte>, IDisposable
{
    private readonly IMemoryOwner<byte> _memoryOwner = MemoryPool<byte>.Shared.Rent(capacity);
    private int _position = 0;

    public ReadOnlySpan<byte> WrittenBytes => _memoryOwner.Memory.Span[.._position];
    
    public void Advance(int count)
    {
        if (_position + count > capacity)
        {
            throw new ArgumentOutOfRangeException(nameof(count));
        }

        _position += count;
    }

    public Memory<byte> GetMemory(int sizeHint) =>
        _memoryOwner.Memory.Slice(_position, Math.Min(sizeHint, capacity - _position));

    public Span<byte> GetSpan(int sizeHint) =>
        _memoryOwner.Memory.Span.Slice(_position, Math.Min(sizeHint, capacity - _position));

    public void Dispose()
    {
        _memoryOwner.Dispose();
    }
}