using System.Text.Json.Serialization;

public class WebhookLog
{
    public int Id { get; set; }

    [JsonPropertyName("TicketId")]
    public string? TicketId { get; set; }

    [JsonPropertyName("FollowupDescription")]
    public string FollowupDescription { get; set; }

    [JsonPropertyName("NumberOfFollowups")]
    public string NumberOfFollowups { get; set; }

    [JsonPropertyName("Requesters")]
    public string Requesters { get; set; }

    [JsonPropertyName("AssignedToTechnician")]
    public string AssignedToTechnician { get; set; }

    [JsonPropertyName("Status")]
    public string Status { get; set; }

    [JsonPropertyName("Priority")]
    public string Priority { get; set; }

    public DateTime CreatedAt { get; set; }
}
