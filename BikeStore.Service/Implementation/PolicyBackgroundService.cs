using BikeStore.Repository.Models; 
using BikeStore.Common.Enums;
using BikeStore.Common.Helpers;
using BikeStore.Repository.Contract;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace BikeStore.Service.BackgroundJobs
{
    public class PolicyBackgroundService : BackgroundService
    {
        private readonly IServiceProvider _serviceProvider;
        private readonly ILogger<PolicyBackgroundService> _logger;
        private readonly TimeSpan _delay = TimeSpan.FromMinutes(1); // Kiểm tra mỗi phút một lần

        public PolicyBackgroundService(
            IServiceProvider serviceProvider,
            ILogger<PolicyBackgroundService> logger)
        {
            _serviceProvider = serviceProvider;
            _logger = logger;
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            _logger.LogInformation("PolicyBackgroundService started.");

            while (!stoppingToken.IsCancellationRequested)
            {
                try
                {
                    // Trong vòng lặp while của BackgroundService
                    using var scope = _serviceProvider.CreateScope();
                    var policyRepo = scope.ServiceProvider.GetRequiredService<IGenericRepository<Policy>>();
                    var unitOfWork = scope.ServiceProvider.GetRequiredService<IUnitOfWork>();
                    var nowVn = DateTimeHelper.NowVN();

                    // 1. Tìm chính sách mớI nhất đã đến giờ áp dụng
                    var nextResult = await policyRepo.GetAllDataByExpression(
                        filter: p => p.Status == PolicyStatusEnum.Inactive && p.IsDeleted == false && p.AppliedDate <= nowVn,
                        pageNumber: 1, pageSize: 1,
                        orderBy: p => p.AppliedDate, isAscending: false
                    );

                    var nextPolicy = nextResult.Items.FirstOrDefault();

                    if (nextPolicy != null)
                    {
                        // 2. Gom tất cả các chính sách đang Active cũ
                        var currentActives = await policyRepo.GetAllDataByExpression(
                            filter: p => p.Status == PolicyStatusEnum.Active && p.IsDeleted == false,
                            pageNumber: 1, pageSize: 50
                        );

                        // 3. Chuyển cũ -> Expired
                        foreach (var old in currentActives.Items)
                        {
                            old.Status = PolicyStatusEnum.Expired;
                            old.UpdatedAt = nowVn;
                            await policyRepo.Update(old);
                        }

                        // 4. Chuyển mới -> Active
                        nextPolicy.Status = PolicyStatusEnum.Active;
                        nextPolicy.UpdatedAt = nowVn;
                        await policyRepo.Update(nextPolicy);

                        await unitOfWork.SaveChangeAsync();
                        _logger.LogInformation("Đã đảo trạng thái chính sách phí thành công lúc {time}", nowVn);
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Lỗi khi chạy PolicyBackgroundService.");
                }

                try
                {
                    await Task.Delay(_delay, stoppingToken);
                }
                catch (TaskCanceledException) {}
            }

            _logger.LogInformation("PolicyBackgroundService stopped.");
        }
    }
}