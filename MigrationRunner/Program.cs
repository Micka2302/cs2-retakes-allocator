using RetakesAllocatorCore.Config;
using RetakesAllocatorCore.Db;

var config = new ConfigData
{
    DatabaseProvider = DatabaseProvider.MySql,
    DatabaseConnectionString = "Server=127.0.0.1;Port=3307;Database=cs2allocator_test;Uid=root;Pwd=;",
    MigrateOnStartup = true,
};

Configs.OverrideConfigDataForTests(config);

try
{
    Console.WriteLine("Running migrations...");
    Queries.Migrate();
    Console.WriteLine("Migrations completed.");
}
catch (Exception ex)
{
    Console.WriteLine("Migration failed:");
    Console.WriteLine(ex);
}
finally
{
    Queries.Disconnect();
}
