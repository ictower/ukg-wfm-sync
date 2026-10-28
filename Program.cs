using Microsoft.Azure.Functions.Worker.Builder;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using ukg_wfm_sync.Services;

FunctionsApplicationBuilder builder = FunctionsApplication.CreateBuilder(args);

builder.ConfigureFunctionsWebApplication();

builder.Services.AddHttpClient();

builder.Services.AddScoped(_ => new KeyVaultService(Environment.GetEnvironmentVariable("KEYVAULT_URL")!));
builder.Services.AddScoped<AuthenticationService>();
builder.Services.AddScoped<GetPeopleService>();
builder.Services.AddScoped<GetTimecardsService>();
builder.Services.AddScoped<SqlService>();

builder.Build().Run();
