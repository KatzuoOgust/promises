using KatzuoOgust.Promises.InMemory;

namespace KatzuoOgust.Promises;

public class InMemoryPromiseStoreTests
{
	private readonly InMemoryPromiseStore<string> _store = new();

	[Fact]
	public async Task Create_ReturnsPendingPromise()
	{
		Promise<string> promise = await _store.CreateAsync();
		PromiseRecord<string> record = await promise.CheckAsync();

		Assert.Equal(PromiseStatus.Pending, record.Status);
		Assert.Null(record.Result);
		Assert.Null(record.ErrorMessage);
		Assert.Null(record.CompletedAt);
	}

	[Fact]
	public async Task ResolveAsync_SetsResolvedStatusAndResult()
	{
		Promise<string> promise = await _store.CreateAsync();
		await _store.ResolveAsync(promise.Id, "hello");

		PromiseRecord<string> record = await promise.CheckAsync();

		Assert.Equal(PromiseStatus.Resolved, record.Status);
		Assert.Equal("hello", record.Result);
		Assert.NotNull(record.CompletedAt);
	}

	[Fact]
	public async Task FailAsync_SetsFailedStatusAndError()
	{
		Promise<string> promise = await _store.CreateAsync();
		await _store.FailAsync(promise.Id, "boom");

		PromiseRecord<string> record = await promise.CheckAsync();

		Assert.Equal(PromiseStatus.Failed, record.Status);
		Assert.Equal("boom", record.ErrorMessage);
		Assert.NotNull(record.CompletedAt);
	}

	[Fact]
	public async Task GetAsync_UnknownId_ReturnsNull()
	{
		PromiseRecord<string>? result = await _store.GetAsync("does-not-exist");
		Assert.Null(result);
	}

	[Fact]
	public async Task ResolveAsync_UnknownId_ThrowsPromiseNotFoundException()
	{
		await Assert.ThrowsAsync<PromiseNotFoundException>(
			() => _store.ResolveAsync("does-not-exist", "value"));
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
}
