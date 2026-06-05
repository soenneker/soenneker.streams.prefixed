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

    /// <summary>
    /// Gets or sets a value indicating whether read is allowed.
    /// </summary>
    public override bool CanRead => true;
    /// <summary>
    /// Gets or sets a value indicating whether seek is allowed.
    /// </summary>
    public override bool CanSeek => false;
    /// <summary>
    /// Gets or sets a value indicating whether write is allowed.
    /// </summary>
    public override bool CanWrite => false;
    /// <summary>
    /// Gets or sets length.
    /// </summary>
    public override long Length => throw new NotSupportedException();

    /// <summary>
    /// Gets or sets position.
    /// </summary>
    public override long Position
    {
        get => throw new NotSupportedException();
        set => throw new NotSupportedException();
    }

    /// <summary>
    /// Executes the read operation.
    /// </summary>
    /// <param name="buffer">The buffer.</param>
    /// <param name="offset">The offset.</param>
    /// <param name="count">The count.</param>
    /// <returns>The result of the operation.</returns>
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

    /// <summary>
    /// Reads async.
    /// </summary>
    /// <param name="buffer">The buffer.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>A task containing the result of the operation.</returns>
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

    /// <summary>
    /// Reads async.
    /// </summary>
    /// <param name="buffer">The buffer.</param>
    /// <param name="offset">The offset.</param>
    /// <param name="count">The count.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>A task containing the result of the operation.</returns>
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

    /// <summary>
    /// Asynchronously releases resources used by the current instance.
    /// </summary>
    /// <returns>A task that represents the asynchronous operation.</returns>
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

    /// <summary>
    /// Executes the flush operation.
    /// </summary>
    public override void Flush() => throw new NotSupportedException();
    /// <summary>
    /// Executes the seek operation.
    /// </summary>
    /// <param name="offset">The offset.</param>
    /// <param name="origin">The origin.</param>
    /// <returns>The result of the operation.</returns>
    public override long Seek(long offset, SeekOrigin origin) => throw new NotSupportedException();
    /// <summary>
    /// Sets length.
    /// </summary>
    /// <param name="value">The value.</param>
    public override void SetLength(long value) => throw new NotSupportedException();
    /// <summary>
    /// Executes the write operation.
    /// </summary>
    /// <param name="buffer">The buffer.</param>
    /// <param name="offset">The offset.</param>
    /// <param name="count">The count.</param>
    public override void Write(byte[] buffer, int offset, int count) => throw new NotSupportedException();
}