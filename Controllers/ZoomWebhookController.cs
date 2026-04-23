using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;
using ZoomAttendance.Models;
using ZoomAttendance.Repositories.Interfaces;

namespace ZoomAttendance.Controllers
{
    /// <summary>
    /// Receives Zoom webhook events used for webhook URL validation and automatic virtual attendance processing.
    /// </summary>
    [ApiController]
    [Route("api/v1/zoom/webhook")]
    [AllowAnonymous]
    public class ZoomWebhookController : ControllerBase
    {
        private readonly IZoomWebhookRepository _repo;
        private readonly ZoomWebhookSettings _settings;
        private readonly ILogger<ZoomWebhookController> _logger;

        public ZoomWebhookController(
            IZoomWebhookRepository repo,
            IOptions<ZoomWebhookSettings> settings,
            ILogger<ZoomWebhookController> logger)
        {
            _repo = repo;
            _settings = settings.Value;
            _logger = logger;
        }

        /// <summary>
        /// Processes Zoom webhook callbacks, validates request authenticity, stores raw webhook events, and updates participant attendance sessions.
        /// </summary>
        /// <returns>
        /// An acknowledgement response for supported Zoom events, including URL validation responses when Zoom verifies the endpoint.
        /// </returns>
        [HttpPost]
        public async Task<IActionResult> Receive()
        {
            string rawBody;
            using (var reader = new StreamReader(Request.Body, Encoding.UTF8))
            {
                rawBody = await reader.ReadToEndAsync();
            }

            using var document = JsonDocument.Parse(rawBody);
            var root = document.RootElement;
            var eventName = GetString(root, "event");

            if (string.Equals(eventName, "endpoint.url_validation", StringComparison.OrdinalIgnoreCase))
            {
                var plainToken = GetNestedString(root, "payload", "plainToken") ?? GetNestedString(root, "payload", "plain_token");
                if (string.IsNullOrWhiteSpace(plainToken))
                    return BadRequest();

                return Ok(new
                {
                    plainToken,
                    encryptedToken = ComputeHexHash(_settings.SecretToken, plainToken)
                });
            }

            if (!IsRequestSignatureValid(rawBody))
                return Unauthorized();

            if (!string.Equals(eventName, "meeting.participant_joined", StringComparison.OrdinalIgnoreCase) &&
                !string.Equals(eventName, "meeting.participant_left", StringComparison.OrdinalIgnoreCase))
            {
                _logger.LogInformation("Ignoring unsupported Zoom webhook event {EventName}", eventName);
                return Ok();
            }

            var normalizedEvent = new ZoomParticipantWebhookEvent
            {
                EventName = eventName ?? string.Empty,
                EventTimeUtc = GetUnixMilliseconds(root, "event_ts"),
                ZoomMeetingId = GetNestedString(root, "payload", "object", "id") ?? string.Empty,
                ZoomMeetingUuid = GetNestedString(root, "payload", "object", "uuid"),
                ParticipantUserId = GetNestedString(root, "payload", "object", "participant", "user_id")
                    ?? GetNestedString(root, "payload", "object", "participant", "id"),
                ParticipantUuid = GetNestedString(root, "payload", "object", "participant", "participant_uuid")
                    ?? GetNestedString(root, "payload", "object", "participant", "uuid"),
                RegistrantId = GetNestedString(root, "payload", "object", "participant", "registrant_id"),
                ParticipantEmail = GetNestedString(root, "payload", "object", "participant", "email")
                    ?? GetNestedString(root, "payload", "object", "participant", "user_email"),
                ParticipantName = GetNestedString(root, "payload", "object", "participant", "user_name")
                    ?? GetNestedString(root, "payload", "object", "participant", "name"),
                OccurredAtUtc = GetNestedDateTime(root, "payload", "object", "participant", "join_time")
                    ?? GetNestedDateTime(root, "payload", "object", "participant", "leave_time")
            };

            if (string.IsNullOrWhiteSpace(normalizedEvent.ZoomMeetingId))
                return BadRequest("Zoom meeting id is required.");

            await _repo.ProcessParticipantEventAsync(normalizedEvent, rawBody);
            return Ok();
        }

        private bool IsRequestSignatureValid(string rawBody)
        {
            var timestamp = Request.Headers["x-zm-request-timestamp"].ToString();
            var signature = Request.Headers["x-zm-signature"].ToString();

            if (string.IsNullOrWhiteSpace(timestamp) || string.IsNullOrWhiteSpace(signature))
                return false;

            if (!long.TryParse(timestamp, out var timestampSeconds))
                return false;

            var requestTime = DateTimeOffset.FromUnixTimeSeconds(timestampSeconds);
            if (Math.Abs((DateTimeOffset.UtcNow - requestTime).TotalMinutes) > 5)
                return false;

            var message = $"v0:{timestamp}:{rawBody}";
            var expectedSignature = $"v0={ComputeHexHash(_settings.SecretToken, message)}";
            return CryptographicOperations.FixedTimeEquals(
                Encoding.UTF8.GetBytes(expectedSignature),
                Encoding.UTF8.GetBytes(signature));
        }

        private static string ComputeHexHash(string secret, string message)
        {
            using var hmac = new HMACSHA256(Encoding.UTF8.GetBytes(secret));
            var hashBytes = hmac.ComputeHash(Encoding.UTF8.GetBytes(message));
            return Convert.ToHexString(hashBytes).ToLowerInvariant();
        }

        private static string? GetString(JsonElement element, string propertyName)
        {
            if (element.ValueKind != JsonValueKind.Object)
                return null;

            foreach (var property in element.EnumerateObject())
            {
                if (string.Equals(property.Name, propertyName, StringComparison.OrdinalIgnoreCase))
                    return property.Value.ValueKind == JsonValueKind.String ? property.Value.GetString() : property.Value.ToString();
            }

            return null;
        }

        private static string? GetNestedString(JsonElement element, params string[] path)
        {
            var current = element;
            foreach (var segment in path)
            {
                if (!TryGetProperty(current, segment, out current))
                    return null;
            }

            return current.ValueKind == JsonValueKind.String ? current.GetString() : current.ToString();
        }

        private static DateTime? GetNestedDateTime(JsonElement element, params string[] path)
        {
            var value = GetNestedString(element, path);
            return DateTime.TryParse(value, out var parsed)
                ? DateTime.SpecifyKind(parsed, parsed.Kind == DateTimeKind.Unspecified ? DateTimeKind.Utc : parsed.Kind).ToUniversalTime()
                : null;
        }

        private static DateTime? GetUnixMilliseconds(JsonElement element, string propertyName)
        {
            var value = GetString(element, propertyName);
            return long.TryParse(value, out var milliseconds)
                ? DateTimeOffset.FromUnixTimeMilliseconds(milliseconds).UtcDateTime
                : null;
        }

        private static bool TryGetProperty(JsonElement element, string propertyName, out JsonElement value)
        {
            if (element.ValueKind != JsonValueKind.Object)
            {
                value = default;
                return false;
            }

            foreach (var property in element.EnumerateObject())
            {
                if (string.Equals(property.Name, propertyName, StringComparison.OrdinalIgnoreCase))
                {
                    value = property.Value;
                    return true;
                }
            }

            value = default;
            return false;
        }
    }
}
