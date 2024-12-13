using Azure.Core;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.SignalR;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Text;
using System.IO;

[ApiController]
[Route("api/[controller]")]
public class WebhookController : ControllerBase
{
    private readonly IHubContext<NotificationHub> _hubContext;
    private readonly AppDbContext _dbContext;

    public WebhookController(IHubContext<NotificationHub> hubContext, AppDbContext dbContext)
    {
        _hubContext = hubContext;
        _dbContext = dbContext;
    }

    [HttpPost]
    public async Task<IActionResult> ReceiveWebhook()
    {
        try
        {
            using var reader = new StreamReader(Request.Body, Encoding.UTF8);
            string body = await reader.ReadToEndAsync();

            await LogDebugInfoAsync($"Corpo da solicitação recebido (raw): {body}");
            await SaveRawWebhookData(body);

            // Tenta extrair informações do formato de log
            var lines = body.Split(new[] { "\r\n", "\n" }, StringSplitOptions.RemoveEmptyEntries);

            // Cria um objeto WebhookData com as informações disponíveis
            var webhookData = new WebhookData
            {
                TicketId = DateTime.Now.Ticks.ToString(), // ID temporário
                FollowupDescription = string.Join(" ", lines), // Todo o conteúdo como descrição
                NumberOfFollowups = "1",
                Requesters = lines.FirstOrDefault(l => l.Contains("Mario Araujo")) ?? "Desconhecido",
                AssignedToTechnician = "Não especificado",
                Status = "Atualizado",
                Priority = "Normal"
            };

            var webhookLog = new WebhookLog
            {
                TicketId = webhookData.TicketId,
                FollowupDescription = webhookData.FollowupDescription,
                NumberOfFollowups = webhookData.NumberOfFollowups,
                Requesters = webhookData.Requesters,
                AssignedToTechnician = webhookData.AssignedToTechnician,
                Status = webhookData.Status,
                Priority = webhookData.Priority,
                CreatedAt = DateTime.UtcNow
            };

            _dbContext.WebhookLogs.Add(webhookLog);
            await _dbContext.SaveChangesAsync();

            await _hubContext.Clients.All.SendAsync("ReceiveNotification", webhookData.TicketId, webhookData.FollowupDescription);

            return Ok(webhookData);
        }
        catch (Exception ex)
        {
            await LogDebugInfoAsync($"Erro ao processar o webhook: {ex.Message}", ex);
            return BadRequest(new { message = "Erro ao processar o webhook", error = ex.Message });
        }
    }

    private async Task SaveRawWebhookData(string data)
    {
        try
        {
            string rawFilePath = Path.Combine(Directory.GetCurrentDirectory(), "raw_webhook_data.txt");
            await System.IO.File.AppendAllTextAsync(rawFilePath, $"[{DateTime.Now}] {data}\n---------------------------------------------------\n");
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Erro ao salvar os dados brutos do webhook: {ex.Message}");
        }
    }

    private async Task LogDebugInfoAsync(string message, Exception ex = null)
    {
        try
        {
            string debugFilePath = Path.Combine(Directory.GetCurrentDirectory(), "debug.txt");
            string debugMessage = $"[{DateTime.Now}] {message}\n";
            if (ex != null)
            {
                debugMessage += $"Exception: {ex.Message}\nStackTrace: {ex.StackTrace}\n";
            }
            debugMessage += "---------------------------------------------------\n";
            await System.IO.File.AppendAllTextAsync(debugFilePath, debugMessage);
        }
        catch (Exception debugEx)
        {
            Console.WriteLine($"Erro ao salvar no arquivo de debug: {debugEx.Message}");
        }
    }
}

public class WebhookData
{
    public string TicketId { get; set; }
    public string FollowupDescription { get; set; }
    public string NumberOfFollowups { get; set; }
    public string Requesters { get; set; }
    public string AssignedToTechnician { get; set; }
    public string Status { get; set; }
    public string Priority { get; set; }
}
