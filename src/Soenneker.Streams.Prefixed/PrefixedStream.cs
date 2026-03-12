using System;
using System.Buffers;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using Soenneker.Extensions.ValueTask;

namespace Soenneker.Streams.Prefixed;

/// <summary>
/// Stream wrapper that replays a prefetched head buffer first, then continues with the underlying stream.
/// Owns the head buffer (returns it to ArrayPool on Dispose).
/// </summary>
public sealed class PrefixedStream : Stream
{
    private readonly Stream _inner;
    private byte[]? _prefix;
    private readonly int _prefixLength;
    private int _prefixPos;

    public PrefixedStream(Stream inner, byte[] prefix, int prefixLength)
    {
        _inner = inner;
        _prefix = prefix;
        _prefixLength = prefixLength;
    }

    public override bool CanRead => true;
    public override bool CanSeek => false;
    public override bool CanWrite => false;
    public override long Length => throw new NotSupportedException();

    public override long Position
    {
        get => throw new NotSupportedException();
        set => throw new NotSupportedException();
    }

    public override int Read(byte[] buffer, int offset, int count)
    {
        if (_prefix is not null)
        {
            int remaining = _prefixLength - _prefixPos;
            if (remaining > 0)
            {
                int take = Math.Min(count, remaining);
                Buffer.BlockCopy(_prefix, _prefixPos, buffer, offset, take);
                _prefixPos += take;
                return take;
            }

            ReturnPrefix();
        }

        return _inner.Read(buffer, offset, count);
    }

    public override async ValueTask<int> ReadAsync(Memory<byte> buffer, CancellationToken cancellationToken = default)
    {
        if (_prefix is not null)
        {
            int remaining = _prefixLength - _prefixPos;
            if (remaining > 0)
            {
                int take = Math.Min(buffer.Length, remaining);
                _prefix.AsSpan(_prefixPos, take)
                       .CopyTo(buffer.Span);
                _prefixPos += take;
                return take;
            }

            ReturnPrefix();
        }

        return await _inner.ReadAsync(buffer, cancellationToken).NoSync();
    }

    public override Task<int> ReadAsync(byte[] buffer, int offset, int count, CancellationToken cancellationToken)
    {
        // let Stream base route to Memory<byte> override for newer runtimes,
        // but keep this for completeness.
        return base.ReadAsync(buffer, offset, count, cancellationToken);
    }

    protected override void Dispose(bool disposing)
    {
        if (disposing)
        {
            ReturnPrefix();
            _inner.Dispose();
        }

        base.Dispose(disposing);
    }

    public override async ValueTask DisposeAsync()
    {
        ReturnPrefix();
        await _inner.DisposeAsync()
                    .NoSync();
    }

    private void ReturnPrefix()
    {
        byte[]? p = _prefix;
        if (p is null)
            return;

        _prefix = null;
        ArrayPool<byte>.Shared.Return(p);
    }

    public override void Flush() => throw new NotSupportedException();
    public override long Seek(long offset, SeekOrigin origin) => throw new NotSupportedException();
    public override void SetLength(long value) => throw new NotSupportedException();
    public override void Write(byte[] buffer, int offset, int count) => throw new NotSupportedException();
}