using System.Threading;
using System.Threading.Tasks;
using AdvertApi.Services;
using Microsoft.Extensions.Diagnostics.HealthChecks;

namespace AdvertApi.HealthChecks
{
    public class StorageHealthCheck : IHealthCheck
    {
        private readonly IAdvertStorageService _advertStorageService;

        public StorageHealthCheck(IAdvertStorageService advertStorageService)
        {
            _advertStorageService = advertStorageService;
        }

        public async Task<HealthCheckResult> CheckHealthAsync(HealthCheckContext context, CancellationToken cancellationToken = default)
        {
            var isOk = await _advertStorageService.CheckHealthAsync();
            return isOk ? HealthCheckResult.Healthy("A healthy result.") : HealthCheckResult.Unhealthy("An unhealthy result.");
        }
    }
}
