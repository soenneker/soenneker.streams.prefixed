[![](https://img.shields.io/nuget/v/soenneker.streams.prefixed.svg?style=for-the-badge)](https://www.nuget.org/packages/soenneker.streams.prefixed/)
[![](https://img.shields.io/github/actions/workflow/status/soenneker/soenneker.streams.prefixed/publish-package.yml?style=for-the-badge)](https://github.com/soenneker/soenneker.streams.prefixed/actions/workflows/publish-package.yml)
[![](https://img.shields.io/nuget/dt/soenneker.streams.prefixed.svg?style=for-the-badge)](https://www.nuget.org/packages/soenneker.streams.prefixed/)
[![](https://img.shields.io/github/actions/workflow/status/soenneker/soenneker.streams.prefixed/codeql.yml?label=CodeQL&style=for-the-badge)](https://github.com/soenneker/soenneker.streams.prefixed/actions/workflows/codeql.yml)

# Soenneker.Streams.Prefixed

A forward-only stream that replays prefetched bytes before continuing from the current position of another stream.

## Installation

```bash
dotnet add package Soenneker.Streams.Prefixed
```

## Usage

Use it when inspecting the beginning of a non-seekable stream without losing those bytes for the next consumer:

```csharp
using System.Buffers;
using Soenneker.Streams.Prefixed;

Stream source = await OpenInputStream(cancellationToken);
byte[] prefix = ArrayPool<byte>.Shared.Rent(512);

int prefixLength = await source.ReadAsync(
    prefix.AsMemory(0, 512),
    cancellationToken);

InspectHeader(prefix.AsSpan(0, prefixLength));

// Ownership of both source and prefix transfers to replayStream.
await using var replayStream = new PrefixedStream(source, prefix, prefixLength);
await ConsumeFromBeginning(replayStream, cancellationToken);
```

The first reads return bytes from `prefix[0..prefixLength]`. Once those bytes are exhausted, reads continue from `source` at its existing position. A read may return only the remaining prefix even when the caller's buffer has more space; consumers must follow normal `Stream` semantics and continue reading until zero is returned.

## Ownership and constraints

`prefix` must have been rented from `ArrayPool<byte>.Shared`, and `prefixLength` must be between zero and the buffer length. The wrapper returns that array to the shared pool when the prefix is exhausted or the wrapper is disposed. Do not read, modify, or return the array after constructing the wrapper.

Disposing the wrapper also disposes the inner stream; there is no `leaveOpen` mode. The wrapper does not support seeking, writing, length, position, resizing, or flushing.

Returned buffers are not cleared. Do not use the prefix for sensitive material that must be erased before pooled reuse.
