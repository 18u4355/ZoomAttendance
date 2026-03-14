using ZoomAttendance.Repositories.Interfaces;

namespace ZoomAttendance.BackgroundJobs
{
    public class InviteSchedulerBackgroundJob : BackgroundService
    {
        private readonly IServiceProvider _serviceProvider;
        private readonly ILogger<InviteSchedulerBackgroundJob> _logger;

        public InviteSchedulerBackgroundJob(IServiceProvider serviceProvider, ILogger<InviteSchedulerBackgroundJob> logger)
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
                    var inviteRepo = scope.ServiceProvider.GetRequiredService<IMeetingInviteRepository>();

                    var dueMeetings = await meetingRepo.GetMeetingsDueForInviteSendAsync();

                    foreach (var meetingId in dueMeetings)
                    {
                        try
                        {
                            await meetingRepo.MarkInviteProcessingAsync(meetingId);
                            await inviteRepo.SendInvitesAsync(meetingId);
                            await meetingRepo.MarkInviteSentAsync(meetingId);
                        }
                        catch (Exception ex)
                        {
                            await meetingRepo.MarkInviteFailedAsync(meetingId);
                            _logger.LogError(ex, "Failed sending invites for meeting {MeetingId}", meetingId);
                        }
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error in InviteSchedulerBackgroundJob");
                }

                await Task.Delay(TimeSpan.FromMinutes(1), stoppingToken);
            }
        }
    }
}