using System.Collections.Concurrent;

namespace KatzuoOgust.Promises.InMemory;

/// <summary>
/// Thread-safe in-memory implementation of <see cref="IPromiseStore{T}"/>.
/// Suitable for testing and single-process scenarios.
/// </summary>
/// <typeparam name="T">The type of results stored in promises.</typeparam>
public sealed class InMemoryPromiseStore<T> : IPromiseStore<T>
{
	private readonly ConcurrentDictionary<string, PromiseRecord<T>> _store = new();

	/// <inheritdoc/>
	public Task<Promise<T>> CreateAsync(CancellationToken ct = default)
	{
		string id = Guid.NewGuid().ToString("N");
		var record = new PromiseRecord<T>(id, PromiseStatus.Pending, default, null, DateTimeOffset.UtcNow, null);
		_store[id] = record;
		return Task.FromResult(new Promise<T>(id, this));
	}

	/// <inheritdoc/>
	public Task ResolveAsync(string id, T result, CancellationToken ct = default)
	{
		Update(id, r => r with
		{
			Status = PromiseStatus.Resolved,
			Result = result,
			CompletedAt = DateTimeOffset.UtcNow
		});
		return Task.CompletedTask;
	}

	/// <inheritdoc/>
	public Task FailAsync(string id, string errorMessage, CancellationToken ct = default)
	{
		Update(id, r => r with
		{
			Status = PromiseStatus.Failed,
			ErrorMessage = errorMessage,
			CompletedAt = DateTimeOffset.UtcNow
		});
		return Task.CompletedTask;
	}

	/// <inheritdoc/>
	public Task<PromiseRecord<T>?> GetAsync(string id, CancellationToken ct = default)
	{
		_store.TryGetValue(id, out PromiseRecord<T>? record);
		return Task.FromResult(record);
	}

	private void Update(string id, Func<PromiseRecord<T>, PromiseRecord<T>> updater)
	{
		while (true)
		{
			if (!_store.TryGetValue(id, out PromiseRecord<T>? existing))
				throw Error.NotFound(id);

			if (existing.Status != PromiseStatus.Pending)
				throw Error.AlreadySettled(id, existing.Status);

			PromiseRecord<T> updated = updater(existing);
			if (_store.TryUpdate(id, updated, existing))
				return;
			// Another thread beat us; retry with the fresh value.
		}
	}

	private static class Error
	{
		public static PromiseNotFoundException NotFound(string id) => new(id);

		public static InvalidOperationException AlreadySettled(string id, PromiseStatus status)
			=> new($"Promise '{id}' is already settled ({status}).");
	}
}
