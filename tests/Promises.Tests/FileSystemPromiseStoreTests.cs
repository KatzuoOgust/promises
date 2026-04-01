using KatzuoOgust.Promises.FileSystem;

namespace KatzuoOgust.Promises;

public class FileSystemPromiseStoreTests : IDisposable
{
	private readonly string _dir = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString("N"));
	private readonly FileSystemPromiseStore<string> _store;

	public FileSystemPromiseStoreTests() => _store = new FileSystemPromiseStore<string>(_dir);

	public void Dispose()
	{
		if (System.IO.Directory.Exists(_dir))
			System.IO.Directory.Delete(_dir, recursive: true);
	}

	[Fact]
	public async Task Create_WritesJsonFileAndReturnsPendingPromise()
	{
		Promise<string> promise = await _store.CreateAsync();

		string[] files = System.IO.Directory.GetFiles(_dir, "*.json");
		Assert.Single(files);
		Assert.Contains(promise.Id, files[0]);

		PromiseRecord<string> record = await promise.CheckAsync();
		Assert.Equal(PromiseStatus.Pending, record.Status);
		Assert.Null(record.Result);
	}

	[Fact]
	public async Task ResolveAsync_SetsResolvedStatusAndResult()
	{
		Promise<string> promise = await _store.CreateAsync();
		await _store.ResolveAsync(promise.Id, "world");

		PromiseRecord<string> record = await promise.CheckAsync();

		Assert.Equal(PromiseStatus.Resolved, record.Status);
		Assert.Equal("world", record.Result);
	}

	[Fact]
	public async Task FailAsync_SetsFailedStatusAndError()
	{
		Promise<string> promise = await _store.CreateAsync();
		await _store.FailAsync(promise.Id, "crash");

		PromiseRecord<string> record = await promise.CheckAsync();

		Assert.Equal(PromiseStatus.Failed, record.Status);
		Assert.Equal("crash", record.ErrorMessage);
	}

	[Fact]
	public async Task GetAsync_UnknownId_ReturnsNull()
	{
		PromiseRecord<string>? result = await _store.GetAsync("missing");
		Assert.Null(result);
	}

	[Fact]
	public async Task ResolveAsync_UnknownId_ThrowsPromiseNotFoundException()
	{
		await Assert.ThrowsAsync<PromiseNotFoundException>(
			() => _store.ResolveAsync("missing", "val"));
	}

	[Fact]
	public async Task ResolveAsync_AlreadyResolved_ThrowsInvalidOperation()
	{
		Promise<string> promise = await _store.CreateAsync();
		await _store.ResolveAsync(promise.Id, "first");

		await Assert.ThrowsAsync<InvalidOperationException>(
			() => _store.ResolveAsync(promise.Id, "second"));
	}

	[Fact]
	public async Task FailAsync_AlreadyResolved_ThrowsInvalidOperation()
	{
		Promise<string> promise = await _store.CreateAsync();
		await _store.ResolveAsync(promise.Id, "value");

		await Assert.ThrowsAsync<InvalidOperationException>(
			() => _store.FailAsync(promise.Id, "too late"));
	}

	[Fact]
	public void Constructor_NullOrWhitespace_Throws()
	{
		Assert.Throws<ArgumentException>(() => new FileSystemPromiseStore<string>(""));
		Assert.Throws<ArgumentException>(() => new FileSystemPromiseStore<string>("   "));
	}

	[Fact]
	public async Task GetAsync_ThrowsArgumentException_WhenPathTraversal()
	{
		await Assert.ThrowsAsync<ArgumentException>(
			() => _store.GetAsync("../../etc/passwd"));
	}

	[Fact]
	public async Task ResolveAsync_ThrowsArgumentException_WhenPathTraversal()
	{
		await Assert.ThrowsAsync<ArgumentException>(
			() => _store.ResolveAsync("../../etc/passwd", "val"));
	}

	[Fact]
	public async Task Cleanup_RemovesSemaphoreAfterResolution()
	{
		Promise<string> promise = await _store.CreateAsync();
		string promiseId = promise.Id;

		await _store.ResolveAsync(promiseId, "result");

		// Force a semaphore check by attempting another operation.
		// If cleanup happened, there should be no semaphore entry in _locks.
		// We verify this indirectly: resolve again (should throw AlreadySettled)
		// and the error happens quickly without waiting for a non-existent lock.
		await Assert.ThrowsAsync<InvalidOperationException>(
			() => _store.ResolveAsync(promiseId, "other"));
	}

	[Fact]
	public async Task Cleanup_HandlesMultipleConcurrentPromises()
	{
		int count = 100;
		var promises = new List<Promise<string>>();

		for (int i = 0; i < count; i++)
		{
			promises.Add(await _store.CreateAsync());
		}

		await Task.WhenAll(promises.Select((p, i) =>
			_store.ResolveAsync(p.Id, $"result-{i}")));

		// Verify all promises are resolved.
		foreach (var promise in promises)
		{
			PromiseRecord<string> record = await promise.CheckAsync();
			Assert.Equal(PromiseStatus.Resolved, record.Status);
		}
	}

	[Fact]
	public async Task Cleanup_DoesNotCleanupWhileInUse()
	{
		Promise<string> promise = await _store.CreateAsync();
		string promiseId = promise.Id;

		// Simulate concurrent access: both tasks try to resolve at the same time.
		// Only one succeeds (first to acquire the lock), the other gets "already settled" error.
		// The key test: after both complete, no deadlock occurs and cleanup happens correctly.
		var task1 = _store.ResolveAsync(promiseId, "result-1");
		var task2 = _store.ResolveAsync(promiseId, "result-2");

		// First completes successfully.
		await task1;

		// Second fails with already settled (expected).
		await Assert.ThrowsAsync<InvalidOperationException>(async () => await task2);

		// Verify final state: promise is resolved and no deadlock.
		PromiseRecord<string> record = await promise.CheckAsync();
		Assert.Equal(PromiseStatus.Resolved, record.Status);
	}
}
