public class WebhookLog
{
    public int Id { get; set; }
    public string TicketId { get; set; }
    public string FollowupDescription { get; set; }
    public string NumberOfFollowups { get; set; }
    public string Requesters { get; set; }
    public string AssignedToTechnician { get; set; }
    public string Status { get; set; }
    public string Priority { get; set; }
    public DateTime CreatedAt { get; set; }
}