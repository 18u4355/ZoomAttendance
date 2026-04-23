using ZoomAttendance.Repositories.Interfaces;

namespace ZoomAttendance.BackgroundJobs
{
    public class MeetingStatusBackgroundJob : BackgroundService
    {
        private readonly IServiceProvider _serviceProvider;
        private readonly ILogger<MeetingStatusBackgroundJob> _logger;

        public MeetingStatusBackgroundJob(IServiceProvider serviceProvider, ILogger<MeetingStatusBackgroundJob> logger)
        {
            _serviceProvider = serviceProvider;
            _logger = logger;
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            while (!stoppingToken.IsCancellationRequested)
            {
                try
                {
                    using var scope = _serviceProvider.CreateScope();
                    var meetingRepo = scope.ServiceProvider.GetRequiredService<IMeetingRepository>();
                    await meetingRepo.UpdateMeetingStatusesAsync();
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error in MeetingStatusBackgroundJob");
                }

                await Task.Delay(TimeSpan.FromMinutes(1), stoppingToken);
            }
        }
    }
}
