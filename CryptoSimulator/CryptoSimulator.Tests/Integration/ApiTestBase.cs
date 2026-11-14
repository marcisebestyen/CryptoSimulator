namespace CryptoSimulator.Tests.Integration;

[Collection(PostgresCollection.Name)]
public abstract class ApiTestBase : IAsyncLifetime
{
    private static readonly SemaphoreSlim MigrationLock = new(1, 1);
    private static bool _migrated;

    private readonly PostgresFixture _postgres;

    protected ApiTestBase(PostgresFixture postgres)
    {
        _postgres = postgres;
        Factory = new ApiFactory(postgres.ConnectionString);
    }

    protected ApiFactory Factory { get; }

    public async Task InitializeAsync()
    {
        await MigrationLock.WaitAsync();
        try
        {
            if (!_migrated)
            {
                await Factory.MigrateAsync();
                _migrated = true;
            }
        }
        finally
        {
            MigrationLock.Release();
        }
    }

    public async Task DisposeAsync() => await Factory.DisposeAsync();
}
