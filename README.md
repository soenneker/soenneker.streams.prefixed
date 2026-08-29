[![](https://img.shields.io/nuget/v/soenneker.streams.prefixed.svg?style=for-the-badge)](https://www.nuget.org/packages/soenneker.streams.prefixed/)
[![](https://img.shields.io/github/actions/workflow/status/soenneker/soenneker.streams.prefixed/publish-package.yml?style=for-the-badge)](https://github.com/soenneker/soenneker.streams.prefixed/actions/workflows/publish-package.yml)
[![](https://img.shields.io/nuget/dt/soenneker.streams.prefixed.svg?style=for-the-badge)](https://www.nuget.org/packages/soenneker.streams.prefixed/)
[![](https://img.shields.io/github/actions/workflow/status/soenneker/soenneker.streams.prefixed/codeql.yml?label=CodeQL&style=for-the-badge)](https://github.com/soenneker/soenneker.streams.prefixed/actions/workflows/codeql.yml)

# Soenneker.Streams.Prefixed

Stream wrapper that replays a prefetched head buffer first, then continues with the underlying stream. Owns the head buffer (returns it to ArrayPool on Dispose).

## Install

```bash
dotnet add package Soenneker.Streams.Prefixed
```

## What you get

- `PrefixedStream` — Stream wrapper that replays a prefetched head buffer first, then continues with the underlying stream. Owns the head buffer (returns it to ArrayPool on Dispose).

## API at a glance

| API | What it does | Result / important behavior |
| --- | --- | --- |
| `PrefixedStream.CanRead` | Gets or sets a value indicating whether read is allowed. | Gets or sets a value indicating whether read is allowed. |
| `PrefixedStream.CanSeek` | Gets or sets a value indicating whether seek is allowed. | Gets or sets a value indicating whether seek is allowed. |
| `PrefixedStream.CanWrite` | Gets or sets a value indicating whether write is allowed. | Gets or sets a value indicating whether write is allowed. |
| `PrefixedStream.Length` | Gets or sets length. | Gets or sets length. |
| `PrefixedStream.Position` | Gets or sets position. | Gets or sets position. |
| `PrefixedStream.Read(buffer, offset, count)` | Executes the read operation. | The result of the operation. |
| `PrefixedStream.ReadAsync(buffer, cancellationToken)` | Reads async. | A task containing the result of the operation. |
| `PrefixedStream.ReadAsync(buffer, offset, count, cancellationToken)` | Reads async. | A task containing the result of the operation. |
| `PrefixedStream.DisposeAsync()` | Asynchronously releases resources used by the current instance. | A task that represents the asynchronous operation. |
| `PrefixedStream.Seek(offset, origin)` | Executes the seek operation. | The result of the operation. |

## Practical notes

- Cancellation stops pending work; it does not undo work that has already completed.
- Dispose instances you own when their scope ends so held resources can be released.
