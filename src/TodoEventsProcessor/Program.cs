using Azure.Identity;
using Azure.Messaging.ServiceBus;
using Azure.Security.KeyVault.Secrets;
using Azure.Storage.Blobs;
using Microsoft.ApplicationInsights.AspNetCore.Extensions;
using Microsoft.ApplicationInsights.Extensibility;
using Microsoft.ApplicationInsights.Extensibility.PerfCounterCollector;
using Microsoft.ApplicationInsights.DependencyCollector;
using Microsoft.ApplicationInsights.Kubernetes;

var builder = Host.CreateApplicationBuilder(args);

// Application Insights
builder.Services.AddApplicationInsightsTelemetryWorkerService();
builder.Services.AddApplicationInsightsKubernetesEnricher();

// Key Vault configuration
var keyVaultUrl = $"https://{builder.Configuration["KeyVaultName"]}.vault.azure.net/";
var credential = new DefaultAzureCredential();
var secretClient = new SecretClient(new Uri(keyVaultUrl), credential);

// Storage configuration
var blobServiceClient = new BlobServiceClient(
    new Uri($"https://{builder.Configuration["StorageAccountName"]}.blob.core.windows.net"),
    credential);
builder.Services.AddSingleton(blobServiceClient);

// Service Bus configuration
var serviceBusConnection = secretClient.GetSecret("ServiceBusListen").Value.Value;
var serviceBusClient = new ServiceBusClient(serviceBusConnection);
builder.Services.AddSingleton(serviceBusClient);

builder.Services.AddHostedService<Worker>();

builder.Services.AddApplicationInsightsTelemetry(options =>
{
    options.EnableAdaptiveSampling = false; // Wyłączenie adaptacyjnego samplingu
    options.EnableDependencyTrackingTelemetryModule = true; // Distributed tracing
    options.EnablePerformanceCounterCollectionModule = true; // Metryki wydajności
    options.EnableAppServicesHeartbeatTelemetryModule = true; // Heartbeat
    options.EnableDebugLogger = true; // Debugowanie w konsoli (dla deweloperów)
});

// Dodanie Kubernetes Enricher
builder.Services.AddApplicationInsightsKubernetesEnricher();

var host = builder.Build();
host.Run();