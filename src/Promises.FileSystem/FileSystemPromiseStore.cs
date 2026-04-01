using System.Text.Json;
using System.Text.Json.Serialization;

namespace KatzuoOgust.Promises.FileSystem;

/// <summary>
/// File-system-backed implementation of <see cref="IPromiseStore{T}"/>.
/// Each promise is stored as a JSON file named <c>{id}.json</c> inside <see cref="Directory"/>.
/// Concurrent access within the same process is serialised per-promise via
/// <see cref="SemaphoreSlim"/>; cross-process safety relies on exclusive file locking.
/// </summary>
/// <typeparam name="T">The type of results stored in promises.</typeparam>
public sealed class FileSystemPromiseStore<T> : IPromiseStore<T>
{
	private static readonly JsonSerializerOptions s_jsonOptions = new()
	{
		WriteIndented = false,
		Converters = { new JsonStringEnumConverter() }
	};

	// One semaphore per promise id keeps per-promise writes serialised without a global lock.
	private readonly System.Collections.Concurrent.ConcurrentDictionary<string, SemaphoreSlim> _locks = new();

	/// <summary>The directory where promise records are stored.</summary>
	public string Directory { get; }

	/// <summary>
	/// Initializes a new file system promise store.
	/// </summary>
	/// <param name="directory">The directory to store promise files in. Will be created if it doesn't exist.</param>
	/// <exception cref="ArgumentException">Thrown when <paramref name="directory"/> is null or whitespace.</exception>
	public FileSystemPromiseStore(string directory)
	{
		ArgumentException.ThrowIfNullOrWhiteSpace(directory);
		Directory = Path.GetFullPath(directory);
		System.IO.Directory.CreateDirectory(Directory);
	}

	/// <inheritdoc/>
	public async Task<Promise<T>> CreateAsync(CancellationToken ct = default)
	{
		string id = Guid.NewGuid().ToString("N");
		var record = new PromiseRecord<T>(id, PromiseStatus.Pending, default, null, DateTimeOffset.UtcNow, null);
		await WriteLockedAsync(id, record, ct).ConfigureAwait(false);
		return new Promise<T>(id, this);
	}

	/// <inheritdoc/>
	public Task ResolveAsync(string id, T result, CancellationToken ct = default)
		=> UpdateLockedAsync(id, r => r with
		{
			Status = PromiseStatus.Resolved,
			Result = result,
			CompletedAt = DateTimeOffset.UtcNow
		}, ct);

	/// <inheritdoc/>
	public Task FailAsync(string id, string errorMessage, CancellationToken ct = default)
		=> UpdateLockedAsync(id, r => r with
		{
			Status = PromiseStatus.Failed,
			ErrorMessage = errorMessage,
			CompletedAt = DateTimeOffset.UtcNow
		}, ct);

	/// <inheritdoc/>
	public async Task<PromiseRecord<T>?> GetAsync(string id, CancellationToken ct = default)
	{
		string path = SafeFilePath(id);
		try
		{
			// FileShare.Read allows concurrent reads; FileShare.None on the write side
			// ensures readers see a complete file.
			await using var stream = new FileStream(path, FileMode.Open, FileAccess.Read, FileShare.Read,
				bufferSize: 4096, useAsync: true);
			return await JsonSerializer.DeserializeAsync<PromiseRecord<T>>(stream, s_jsonOptions, ct)
				.ConfigureAwait(false);
		}
		catch (FileNotFoundException)
		{
			return null;
		}
	}

	private async Task UpdateLockedAsync(string id, Func<PromiseRecord<T>, PromiseRecord<T>> updater,
		CancellationToken ct)
	{
		SemaphoreSlim sem = _locks.GetOrAdd(id, _ => new SemaphoreSlim(1, 1));
		await sem.WaitAsync(ct).ConfigureAwait(false);
		try
		{
			PromiseRecord<T> existing = await GetAsync(id, ct).ConfigureAwait(false)
										?? throw Error.NotFound(id);

			if (existing.Status != PromiseStatus.Pending)
				throw Error.AlreadySettled(id, existing.Status);

			await WriteLockedAsync(id, updater(existing), ct).ConfigureAwait(false);
		}
		finally
		{
			sem.Release();
			_locks.TryRemove(id, out _);
		}
	}

	private async Task WriteLockedAsync(string id, PromiseRecord<T> record, CancellationToken ct)
	{
		string path = SafeFilePath(id);
		// FileShare.None prevents readers from opening a partially-written file.
		await using var stream = new FileStream(path, FileMode.Create, FileAccess.Write, FileShare.None,
			bufferSize: 4096, useAsync: true);
		await JsonSerializer.SerializeAsync(stream, record, s_jsonOptions, ct).ConfigureAwait(false);
	}

	/// <summary>
	/// Returns the resolved file path and guards against path-traversal attacks by
	/// ensuring the result stays inside <see cref="Directory"/>.
	/// </summary>
	/// <param name="id">The promise ID (used to compute the file name).</param>
	/// <returns>The full path to the promise file.</returns>
	/// <exception cref="ArgumentException">Thrown when the <paramref name="id"/> resolves outside <see cref="Directory"/>.</exception>
	private string SafeFilePath(string id)
	{
		string path = Path.GetFullPath(Path.Combine(Directory, $"{id}.json"));
		if (!path.StartsWith(Directory + Path.DirectorySeparatorChar, StringComparison.Ordinal)
			&& path != Directory)
		{
			throw Error.InvalidId(id, nameof(id));
		}
		return path;
	}

	private static class Error
	{
		public static PromiseNotFoundException NotFound(string id) => new(id);

		public static InvalidOperationException AlreadySettled(string id, PromiseStatus status)
			=> new($"Promise '{id}' is already settled ({status}).");

		public static ArgumentException InvalidId(string id, string paramName)
			=> new($"Promise id '{id}' resolves outside the store directory.", paramName);
	}
}
