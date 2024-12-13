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

            var lines = body.Split(new[] { "\r\n", "\n" }, StringSplitOptions.None);

            string ticketId = lines[0].Trim();
            if (string.IsNullOrEmpty(ticketId) || !ticketId.All(char.IsDigit))
            {
                throw new Exception($"TicketId inválido ou não encontrado: '{ticketId}'");
            }

            string lastMessage = "Sem descrição";
            bool foundDate = false;
            for (int i = 0; i < lines.Length; i++)
            {
                if (lines[i].Trim().StartsWith("13-12-2024"))
                {
                    foundDate = true;

                    if (i + 2 < lines.Length)
                    {

                        lastMessage = System.Text.RegularExpressions.Regex.Replace(
                            lines[i + 2],
                            "<[^>]+>",
                            "").Trim();
                        break;
                    }
                }
            }

            var webhookLog = new WebhookLog
            {
                TicketId = ticketId.TrimStart('0'),
                FollowupDescription = lastMessage,
                NumberOfFollowups = "1",
                Requesters = "Mario Araujo",
                AssignedToTechnician = "Não especificado",
                Status = "Atualizado",
                Priority = "Normal",
                CreatedAt = DateTime.UtcNow
            };

            await LogDebugInfoAsync($"Dados extraídos - TicketId: {webhookLog.TicketId}, Mensagem: {webhookLog.FollowupDescription}");

            _dbContext.WebhookLogs.Add(webhookLog);
            await _dbContext.SaveChangesAsync();

            await _hubContext.Clients.All.SendAsync("ReceiveNotification", webhookLog.TicketId, webhookLog.FollowupDescription);

            return Ok(webhookLog);
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
