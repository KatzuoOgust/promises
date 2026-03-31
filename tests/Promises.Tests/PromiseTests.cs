using KatzuoOgust.Promises;
using KatzuoOgust.Promises.InMemory;
using Xunit;

namespace KatzuoOgust.Promises;

public class PromiseTests
{
	private readonly InMemoryPromiseStore<int> _store = new();

	[Fact]
	public async Task GetResultAsync_WhenResolved_ReturnsResult()
	{
		Promise<int> promise = await _store.CreateAsync();
		await _store.ResolveAsync(promise.Id, 42);

		int result = await promise.GetResultAsync();

		Assert.Equal(42, result);
	}

	[Fact]
	public async Task GetResultAsync_WhenPending_ThrowsPromiseNotResolvedException()
	{
		Promise<int> promise = await _store.CreateAsync();

		await Assert.ThrowsAsync<PromiseNotResolvedException>(
			() => promise.GetResultAsync());
	}

	[Fact]
	public async Task GetResultAsync_WhenFailed_ThrowsPromiseFailedException()
	{
		Promise<int> promise = await _store.CreateAsync();
		await _store.FailAsync(promise.Id, "task exploded");

		PromiseFailedException ex = await Assert.ThrowsAsync<PromiseFailedException>(
			() => promise.GetResultAsync());

		Assert.Contains("task exploded", ex.Message);
	}

	[Fact]
	public async Task CheckAsync_ReturnsLatestRecord_OnEachCall()
	{
		Promise<int> promise = await _store.CreateAsync();

		PromiseRecord<int> first = await promise.CheckAsync();
		Assert.Equal(PromiseStatus.Pending, first.Status);

		await _store.ResolveAsync(promise.Id, 7);

		PromiseRecord<int> second = await promise.CheckAsync();
		Assert.Equal(PromiseStatus.Resolved, second.Status);
		Assert.Equal(7, second.Result);
	}

	[Fact]
	public async Task CheckAsync_UnknownId_ThrowsPromiseNotFoundException()
	{
		// Build a detached promise (id not in store)
		var store = new InMemoryPromiseStore<int>();
		var ghost = new Promise<int>("ghost-id", store);

		await Assert.ThrowsAsync<PromiseNotFoundException>(
			() => ghost.CheckAsync());
	}

	[Theory]
	[InlineData(null)]
	[InlineData("")]
	[InlineData("   ")]
	public void Constructor_NullOrWhitespaceId_ThrowsArgumentException(string? id)
	{
		Assert.ThrowsAny<ArgumentException>(() => new Promise<int>(id!, _store));
	}

	[Fact]
	public void Constructor_NullStore_ThrowsArgumentNullException()
	{
		Assert.Throws<ArgumentNullException>(() => new Promise<int>("id", null!));
	}

	[Theory]
	[InlineData(null)]
	[InlineData("")]
	[InlineData("   ")]
	public void PromiseRecord_NullOrWhitespaceId_ThrowsArgumentException(string? id)
	{
		Assert.ThrowsAny<ArgumentException>(() =>
			new PromiseRecord<int>(id!, PromiseStatus.Pending, default, null, DateTimeOffset.UtcNow, null));
	}

	[Fact]
	public async Task WaitAsync_ResolvesBeforePolling_ReturnsResult()
	{
		Promise<int> promise = await _store.CreateAsync();
		await _store.ResolveAsync(promise.Id, 99);

		Promise<int> settled = await promise.WaitAsync();
		int result = await settled.GetResultAsync();

		Assert.Equal(99, result);
	}

	[Fact]
	public async Task WaitAsync_ResolvesAfterDelay_ReturnsResult()
	{
		Promise<int> promise = await _store.CreateAsync();

		// Resolve after a short delay on a background task
		_ = Task.Run(async () =>
		{
			await Task.Delay(80);
			await _store.ResolveAsync(promise.Id, 77);
		});

		Promise<int> settled = await promise.WaitAsync();
		int result = await settled.GetResultAsync();

		Assert.Equal(77, result);
	}

	[Fact]
	public async Task WaitAsync_FailsAfterDelay_ThrowsPromiseFailedException()
	{
		Promise<int> promise = await _store.CreateAsync();

		_ = Task.Run(async () =>
		{
			await Task.Delay(80);
			await _store.FailAsync(promise.Id, "background failure");
		});

		await Assert.ThrowsAsync<PromiseFailedException>(
			() => promise.WaitAsync());
	}

	[Fact]
	public async Task WaitAsync_Timeout_ThrowsTimeoutException()
	{
		Promise<int> promise = await _store.CreateAsync(); // stays pending

		await Assert.ThrowsAsync<TimeoutException>(
			() => promise.WaitAsync(TimeSpan.FromMilliseconds(150)));
	}

	[Fact]
	public async Task WaitAsync_CancellationRequested_ThrowsOperationCanceledException()
	{
		Promise<int> promise = await _store.CreateAsync(); // stays pending
		using var cts = new CancellationTokenSource(TimeSpan.FromMilliseconds(150));

		await Assert.ThrowsAnyAsync<OperationCanceledException>(
			() => promise.WaitAsync(ct: cts.Token));
	}

	[Fact]
	public async Task WaitForResultAsync_ResolvesAfterDelay_ReturnsResult()
	{
		Promise<int> promise = await _store.CreateAsync();

		_ = Task.Run(async () =>
		{
			await Task.Delay(80);
			await _store.ResolveAsync(promise.Id, 42);
		});

		int result = await promise.WaitForResultAsync();

		Assert.Equal(42, result);
	}

	[Fact]
	public async Task WaitForResultAsync_Timeout_ThrowsTimeoutException()
	{
		Promise<int> promise = await _store.CreateAsync(); // stays pending

		await Assert.ThrowsAsync<TimeoutException>(
			() => promise.WaitForResultAsync(TimeSpan.FromMilliseconds(150)));
	}
}
