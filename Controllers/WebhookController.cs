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

            // O primeiro valor é o ticketId
            string ticketId = lines[0].Trim();
            if (string.IsNullOrEmpty(ticketId) || !ticketId.All(char.IsDigit))
            {
                throw new Exception($"TicketId inválido ou não encontrado: '{ticketId}'");
            }

            // O segundo valor é o número de mensagens
            int numberOfMessages = int.Parse(lines[1].Trim());

            // Informações do ticket
            string assignees = lines[2].Trim(); // Usuário(s) responsável(is)
            string status = lines[3].Trim(); // Status do ticket
            string priority = lines[4].Trim(); // Prioridade do ticket

            // Processar as mensagens
            for (int i = 5; i < lines.Length; i++)
            {
                if (string.IsNullOrWhiteSpace(lines[i])) continue; // Ignorar linhas em branco

                // Verificar se a linha contém uma data
                if (lines[i].Trim().StartsWith("21-12-2024")) // Exemplo de data
                {
                    // A próxima linha é o autor
                    string author = lines[i + 1].Trim();

                    // A linha seguinte é a descrição
                    string description = System.Text.RegularExpressions.Regex.Replace(
                        lines[i + 2],
                        "<[^>]+>",
                        "").Trim();

                    // Verificar se é um acompanhamento ou uma tarefa
                    if (description.Contains("Novo Acompanhamento"))
                    {
                        // Adicionar acompanhamento
                        var webhookLog = new WebhookLog
                        {
                            TicketId = ticketId.TrimStart('0'),
                            FollowupDescription = $"{description} - Novo Acompanhamento",
                            NumberOfFollowups = numberOfMessages.ToString(),
                            Requesters = author,
                            AssignedToTechnician = assignees,
                            Status = status,
                            Priority = priority,
                            CreatedAt = DateTime.UtcNow
                        };

                        await LogDebugInfoAsync($"Dados extraídos - TicketId: {webhookLog.TicketId}, Mensagem: {webhookLog.FollowupDescription}");

                        _dbContext.WebhookLogs.Add(webhookLog);
                        await _dbContext.SaveChangesAsync();

                        await SaveFollowupDataToFile(webhookLog); // Salvar em acompanhamentos.txt
                        await _hubContext.Clients.All.SendAsync("ReceiveNotification", webhookLog.TicketId, webhookLog.FollowupDescription);
                    }
                    else if (description.Contains("Nova Tarefa"))
                    {
                        // Adicionar tarefa
                        var taskLog = new TaskLog
                        {
                            TaskId = ticketId.TrimStart('0'),
                            TaskDescription = $"{description} - Nova Tarefa",
                            CreatedAt = DateTime.UtcNow
                        };

                        await LogDebugInfoAsync($"Dados extraídos - TaskId: {taskLog.TaskId}, Descrição: {taskLog.TaskDescription}");

                        _dbContext.TaskLogs.Add(taskLog);
                        await _dbContext.SaveChangesAsync();

                        await SaveTaskDataToFile(taskLog); // Salvar em tarefas.txt
                        await _hubContext.Clients.All.SendAsync("ReceiveTaskNotification", taskLog.TaskId, taskLog.TaskDescription);
                    }
                }
            }

            return Ok(new { message = "Dados processados com sucesso." });
        }
        catch (Exception ex)
        {
            await LogDebugInfoAsync($"Erro ao processar o webhook: {ex.Message}", ex);
            return BadRequest(new { message = "Erro ao processar o webhook", error = ex.Message });
        }
    }

    private async Task SaveFollowupDataToFile(WebhookLog webhookLog)
    {
        try
        {
            string followupFilePath = Path.Combine(Directory.GetCurrentDirectory(), "acompanhamentos.txt");
            string followupData = $"[{DateTime.Now}] TicketId: {webhookLog.TicketId}, Descrição: {webhookLog.FollowupDescription}\n";
            await System.IO.File.AppendAllTextAsync(followupFilePath, followupData);
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Erro ao salvar os dados do acompanhamento: {ex.Message}");
        }
    }

    private async Task SaveTaskDataToFile(TaskLog taskLog)
    {
        try
        {
            string taskFilePath = Path.Combine(Directory.GetCurrentDirectory(), "tarefas.txt");
            string taskData = $"[{DateTime.Now}] TaskId: {taskLog.TaskId}, Descrição: {taskLog.TaskDescription}\n";
            await System.IO.File.AppendAllTextAsync(taskFilePath, taskData);
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Erro ao salvar os dados da tarefa: {ex.Message}");
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
