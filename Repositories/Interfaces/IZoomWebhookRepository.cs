using ZoomAttendance.Models;

namespace ZoomAttendance.Repositories.Interfaces
{
    public interface IZoomWebhookRepository
    {
        Task ProcessParticipantEventAsync(ZoomParticipantWebhookEvent webhookEvent, string rawPayload);
    }
}
