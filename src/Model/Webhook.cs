using System.Text.Json.Serialization;
using Microsoft.EntityFrameworkCore;

namespace WebConsultaCnpjReceita.Models
{
    public class WebhookPayload
    {
        [JsonPropertyName("webhookId")]
        public string WebhookId { get; set; }

        [JsonPropertyName("jobId")]
        public string JobId { get; set; }

        [JsonPropertyName("eventType")]
        public string EventType { get; set; }

        [JsonPropertyName("data")]
        public WebhookData Data { get; set; }
    }

    public class WebhookData
    {
        [JsonPropertyName("fileLink")]
        public string FileLink { get; set; }
    }

    [PrimaryKey("WebhookId")]
    public class WebhookModel
    {
        public string WebhookId { get; set; }
        public string JobId { get; set; }
        public string EventType { get; set; }
        public string FileLink { get; set; }
    }
}
