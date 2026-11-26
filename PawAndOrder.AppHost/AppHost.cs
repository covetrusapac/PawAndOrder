
var builder = DistributedApplication.CreateBuilder(args);

var sql = builder.AddSqlServer("sql")
                 .WithDataVolume()
                 .AddDatabase("SqlServer", "PawAndOrder");

var api = builder.AddProject<Projects.PawAndOrder_Api>("pawandorder-api")
                 .WithReference(sql)
                 .WaitFor(sql)
                 .WithHttpHealthCheck("/health");

var frontend = builder.AddViteApp("frontend", "../frontend")
                      .WithReference(api)
                      .WaitFor(api);

api.WithEnvironment("Cors__AllowedOrigins__0", frontend.GetEndpoint("http"));

builder.Build().Run();
