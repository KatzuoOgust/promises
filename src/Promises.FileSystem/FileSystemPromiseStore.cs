using System.Text.Json;
using System.Text.Json.Serialization;
using KatzuoOgust.Promises;

namespace KatzuoOgust.Promises.FileSystem;

/// <summary>
/// File-system-backed implementation of <see cref="IPromiseStore{T}"/>.
/// Each promise is stored as a JSON file named <c>{id}.json</c> inside <see cref="Directory"/>.
/// Concurrent access within the same process is serialised per-promise via
/// <see cref="SemaphoreSlim"/>; cross-process safety relies on exclusive file locking.
/// </summary>
public sealed class FileSystemPromiseStore<T> : IPromiseStore<T>
{
	private static readonly JsonSerializerOptions JsonOptions = new()
	{
		WriteIndented = false,
		Converters = { new JsonStringEnumConverter() }
	};

	// One semaphore per promise id keeps per-promise writes serialised without a global lock.
	private readonly System.Collections.Concurrent.ConcurrentDictionary<string, SemaphoreSlim> _locks = new();

	public string Directory { get; }

	public FileSystemPromiseStore(string directory)
	{
		ArgumentException.ThrowIfNullOrWhiteSpace(directory);
		Directory = Path.GetFullPath(directory);
		System.IO.Directory.CreateDirectory(Directory);
	}

	public async Task<Promise<T>> CreateAsync(CancellationToken ct = default)
	{
		var id = Guid.NewGuid().ToString("N");
		var record = new PromiseRecord<T>(id, PromiseStatus.Pending, default, null, DateTimeOffset.UtcNow, null);
		await WriteLockedAsync(id, record, ct).ConfigureAwait(false);
		return new Promise<T>(id, this);
	}

	public Task ResolveAsync(string id, T result, CancellationToken ct = default)
		=> UpdateLockedAsync(id, r => r with
		{
			Status = PromiseStatus.Resolved,
			Result = result,
			CompletedAt = DateTimeOffset.UtcNow
		}, ct);

	public Task FailAsync(string id, string errorMessage, CancellationToken ct = default)
		=> UpdateLockedAsync(id, r => r with
		{
			Status = PromiseStatus.Failed,
			ErrorMessage = errorMessage,
			CompletedAt = DateTimeOffset.UtcNow
		}, ct);

	public async Task<PromiseRecord<T>?> GetAsync(string id, CancellationToken ct = default)
	{
		var path = SafeFilePath(id);
		try
		{
			// FileShare.Read allows concurrent reads; FileShare.None on the write side
			// ensures readers see a complete file.
			await using var stream = new FileStream(path, FileMode.Open, FileAccess.Read, FileShare.Read,
				bufferSize: 4096, useAsync: true);
			return await JsonSerializer.DeserializeAsync<PromiseRecord<T>>(stream, JsonOptions, ct)
				.ConfigureAwait(false);
		}
		catch (FileNotFoundException)
		{
			return null;
		}
	}

	private async Task UpdateLockedAsync(string id, Func<PromiseRecord<T>, PromiseRecord<T>> updater, CancellationToken ct)
	{
		var sem = _locks.GetOrAdd(id, _ => new SemaphoreSlim(1, 1));
		await sem.WaitAsync(ct).ConfigureAwait(false);
		try
		{
			var existing = await GetAsync(id, ct).ConfigureAwait(false)
				?? throw new PromiseNotFoundException(id);

			if (existing.Status != PromiseStatus.Pending)
				throw new InvalidOperationException($"Promise '{id}' is already settled ({existing.Status}).");

			await WriteLockedAsync(id, updater(existing), ct).ConfigureAwait(false);
		}
		finally
		{
			sem.Release();
		}
	}

	private async Task WriteLockedAsync(string id, PromiseRecord<T> record, CancellationToken ct)
	{
		var path = SafeFilePath(id);
		// FileShare.None prevents readers from opening a partially-written file.
		await using var stream = new FileStream(path, FileMode.Create, FileAccess.Write, FileShare.None,
			bufferSize: 4096, useAsync: true);
		await JsonSerializer.SerializeAsync(stream, record, JsonOptions, ct).ConfigureAwait(false);
	}

	/// <summary>
	/// Returns the resolved file path and guards against path-traversal attacks by
	/// ensuring the result stays inside <see cref="Directory"/>.
	/// </summary>
	private string SafeFilePath(string id)
	{
		var path = Path.GetFullPath(Path.Combine(Directory, $"{id}.json"));
		if (!path.StartsWith(Directory + Path.DirectorySeparatorChar, StringComparison.Ordinal)
			&& path != Directory)
		{
			throw new ArgumentException($"Promise id '{id}' resolves outside the store directory.", nameof(id));
		}
		return path;
	}
}

