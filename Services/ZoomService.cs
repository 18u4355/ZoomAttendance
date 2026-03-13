using System.Net.Http.Headers;
using System.Text;
using Microsoft.Extensions.Options;
using Newtonsoft.Json;
using ZoomAttendance.Models;

namespace ZoomAttendance.Services
{
    public class ZoomService : IZoomService
    {
        private readonly ZoomSettings _zoomSettings;
        private readonly IHttpClientFactory _httpClientFactory;

        public ZoomService(IOptions<ZoomSettings> zoomSettings, IConfiguration configuration, IHttpClientFactory httpClientFactory)
        {
            _zoomSettings = zoomSettings.Value;
            _httpClientFactory = httpClientFactory;
        }

        private async Task<string> GetAccessTokenAsync()
        {
            var client = _httpClientFactory.CreateClient();

            var request = new HttpRequestMessage(
                HttpMethod.Post,
                $"https://zoom.us/oauth/token?grant_type=account_credentials&account_id={_zoomSettings.AccountId}");

            var credentials = Convert.ToBase64String(
                Encoding.UTF8.GetBytes($"{_zoomSettings.ClientId}:{_zoomSettings.ClientSecret}"));

            request.Headers.Authorization = new AuthenticationHeaderValue("Basic", credentials);

            var response = await client.SendAsync(request);
            response.EnsureSuccessStatusCode();

            var json = await response.Content.ReadAsStringAsync();
            dynamic tokenObj = JsonConvert.DeserializeObject(json)!;

            return tokenObj.access_token;
        }

        public async Task<(string MeetingId, string JoinUrl, string StartUrl)> CreateMeetingAsync(
            string title,
            DateTime startDatetime,
            int durationMinutes)
        {
            var token = await GetAccessTokenAsync();

            var client = _httpClientFactory.CreateClient();
            client.DefaultRequestHeaders.Authorization =
                new AuthenticationHeaderValue("Bearer", token);
            var utcStart = startDatetime.ToUniversalTime();

            var payload = new
            {
                topic = title,
                type = 2,
                start_time = utcStart.ToString("yyyy-MM-ddTHH:mm:ssZ"),
                duration = durationMinutes
            };

            var content = new StringContent(
                JsonConvert.SerializeObject(payload),
                Encoding.UTF8,
                "application/json");

            var response = await client.PostAsync("https://api.zoom.us/v2/users/me/meetings", content);
            var responseBody = await response.Content.ReadAsStringAsync();
            if (!response.IsSuccessStatusCode)
            {
                throw new Exception($"Zoom Create Meeting failed. Status:{(int)response.StatusCode}, Body:{responseBody}");
            }

            //var json = await response.Content.ReadAsStringAsync();
            dynamic zoom = JsonConvert.DeserializeObject(responseBody)!;

            return (
                zoom.id.ToString(),
                zoom.join_url.ToString(),
                zoom.start_url.ToString()
            );
        }

        public async Task UpdateMeetingAsync(
            string zoomMeetingId,
            string title,
            DateTime startDatetime,
            int durationMinutes)
        {
            var token = await GetAccessTokenAsync();

            var client = _httpClientFactory.CreateClient();
            client.DefaultRequestHeaders.Authorization =
                new AuthenticationHeaderValue("Bearer", token);

            var payload = new
            {
                topic = title,
                start_time = startDatetime.ToString("yyyy-MM-ddTHH:mm:ss"),
                duration = durationMinutes,
                timezone = "Africa/Lagos"
            };

            var content = new StringContent(
                JsonConvert.SerializeObject(payload),
                Encoding.UTF8,
                "application/json");

            var response = await client.PatchAsync(
                $"https://api.zoom.us/v2/meetings/{zoomMeetingId}",
                content);

            response.EnsureSuccessStatusCode();
        }
    }
}