using KatzuoOgust.Promises;
using KatzuoOgust.Promises.FileSystem;
using Xunit;

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
	public async Task Create_WritesJsonFile()
	{
		Promise<string> promise = await _store.CreateAsync();

		string[] files = System.IO.Directory.GetFiles(_dir, "*.json");
		Assert.Single(files);
		Assert.Contains(promise.Id, files[0]);
	}

	[Fact]
	public async Task Create_ReturnsPendingPromise()
	{
		Promise<string> promise = await _store.CreateAsync();
		PromiseRecord<string> record = await promise.CheckAsync();

		Assert.Equal(PromiseStatus.Pending, record.Status);
		Assert.Null(record.Result);
	}

	[Fact]
	public async Task Resolve_SetsResolvedStatusAndResult()
	{
		Promise<string> promise = await _store.CreateAsync();
		await _store.ResolveAsync(promise.Id, "world");

		PromiseRecord<string> record = await promise.CheckAsync();

		Assert.Equal(PromiseStatus.Resolved, record.Status);
		Assert.Equal("world", record.Result);
	}

	[Fact]
	public async Task Fail_SetsFailedStatusAndError()
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
	public async Task GetAsync_PathTraversal_ThrowsArgumentException()
	{
		await Assert.ThrowsAsync<ArgumentException>(
			() => _store.GetAsync("../../etc/passwd"));
	}

	[Fact]
	public async Task ResolveAsync_PathTraversal_ThrowsArgumentException()
	{
		await Assert.ThrowsAsync<ArgumentException>(
			() => _store.ResolveAsync("../../etc/passwd", "val"));
	}
}
