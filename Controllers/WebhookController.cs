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

            var lines = body.Split(new[] { "\r\n", "\n" }, StringSplitOptions.RemoveEmptyEntries);
            
            var webhookRecive = new WebHookRecive();

            // Processar informações do ticket
            foreach (var line in lines)
            {
                if (line.StartsWith("- **ID do Ticket:**"))
                    webhookRecive.TicketId = ExtractValue(line, "**ID do Ticket:**").TrimStart('0');
                else if (line.StartsWith("- **Título:**"))
                    webhookRecive.Title = ExtractValue(line, "**Título:**");
                else if (line.StartsWith("- **Usuário(s) Responsável(is):**"))
                    webhookRecive.Assignees = ExtractValue(line, "**Usuário(s) Responsável(is):**");
                else if (line.StartsWith("- **Status do Ticket:**"))
                    webhookRecive.Status = ExtractValue(line, "**Status do Ticket:**");
                else if (line.StartsWith("- **Prioridade:**"))
                    webhookRecive.Priority = ExtractValue(line, "**Prioridade:**");
                else if (line.StartsWith("- **Autor:**"))
                    webhookRecive.Author = ExtractValue(line, "**Autor:**");
            }

            // Se não encontrou o TicketId nas informações do ticket, pegar da primeira linha
            if (string.IsNullOrEmpty(webhookRecive.TicketId))
            {
                webhookRecive.TicketId = lines[0].Trim().TrimStart('0');
            }

            // Determinar o tipo de notificação e processar dados específicos
            if (body.Contains("**Nova Tarefa**"))
            {
                webhookRecive.Type = "Tarefa";
                
                // Encontrar o primeiro bloco de tarefa (mais recente)
                int taskIndex = body.IndexOf("**Nova Tarefa**");
                string taskBlock = body.Substring(taskIndex);
                var taskLines = taskBlock.Split(new[] { "\r\n", "\n" }, StringSplitOptions.RemoveEmptyEntries);

                DateTime taskDate = DateTime.Now;
                foreach (var line in taskLines)
                {
                    if (line.StartsWith("- **Data da Tarefa:**"))
                    {
                        string dateStr = ExtractValue(line, "**Data da Tarefa:**");
                        if (DateTime.TryParseExact(dateStr, "dd-MM-yyyy HH:mm", null, System.Globalization.DateTimeStyles.None, out DateTime parsedDate))
                        {
                            taskDate = parsedDate;
                        }
                    }
                    else if (line.StartsWith("- **Autor da Tarefa:**"))
                        webhookRecive.Author = ExtractValue(line, "**Autor da Tarefa:**");
                    else if (line.StartsWith("- **Descrição:**"))
                        webhookRecive.Description = CleanHtmlTags(ExtractValue(line, "**Descrição:**"));
                    else if (line.StartsWith("- **Tempo Estimado:**"))
                        webhookRecive.EstimatedTime = ExtractValue(line, "**Tempo Estimado:**");
                    else if (line.StartsWith("- **Categoria da Tarefa:**"))
                        webhookRecive.TaskCategory = ExtractValue(line, "**Categoria da Tarefa:**");

                    if (!string.IsNullOrEmpty(webhookRecive.Description)) 
                        break; // Sair após encontrar a primeira descrição (mais recente)
                }

                webhookRecive.CreatedAt = taskDate;
                await _hubContext.Clients.All.SendAsync("ReceiveTaskNotification", webhookRecive.TicketId, webhookRecive.Description);
            }
            else if (body.Contains("**Novo Acompanhamento**"))
            {
                webhookRecive.Type = "Acompanhamento";
                
                // Encontrar o primeiro bloco de acompanhamento (mais recente)
                int followupIndex = body.IndexOf("**Novo Acompanhamento**");
                string followupBlock = body.Substring(followupIndex);
                var followupLines = followupBlock.Split(new[] { "\r\n", "\n" }, StringSplitOptions.RemoveEmptyEntries);

                DateTime followupDate = DateTime.Now;
                foreach (var line in followupLines)
                {
                    if (line.StartsWith("- **Data do Acompanhamento:**"))
                    {
                        string dateStr = ExtractValue(line, "**Data do Acompanhamento:**");
                        if (DateTime.TryParseExact(dateStr, "dd-MM-yyyy HH:mm", null, System.Globalization.DateTimeStyles.None, out DateTime parsedDate))
                        {
                            followupDate = parsedDate;
                        }
                    }
                    else if (line.StartsWith("- **Autor do Acompanhamento:**"))
                        webhookRecive.Author = ExtractValue(line, "**Autor do Acompanhamento:**");
                    else if (line.Contains("<p>"))
                    {
                        webhookRecive.Description = CleanHtmlTags(line);
                        break; // Sair após encontrar a primeira descrição (mais recente)
                    }
                }

                webhookRecive.CreatedAt = followupDate;
                await _hubContext.Clients.All.SendAsync("ReceiveNotification", webhookRecive.TicketId, webhookRecive.Description);
            }

            // Garantir que nenhum campo esteja nulo
            webhookRecive.Title ??= "";
            webhookRecive.Description ??= "";
            webhookRecive.Author ??= "";
            webhookRecive.Assignees ??= "";
            webhookRecive.Status ??= "";
            webhookRecive.Priority ??= "";
            webhookRecive.EstimatedTime ??= "";
            webhookRecive.TaskCategory ??= "";

            // Log antes de salvar
            await LogDebugInfoAsync($"Tentando salvar - TicketId: {webhookRecive.TicketId}, " +
                $"Tipo: {webhookRecive.Type}, " +
                $"Título: {webhookRecive.Title}, " +
                $"Autor: {webhookRecive.Author}, " +
                $"Status: {webhookRecive.Status}, " +
                $"Prioridade: {webhookRecive.Priority}, " +
                $"Data: {webhookRecive.CreatedAt}, " +
                $"Descrição: {webhookRecive.Description}");

            _dbContext.WebHookRecives.Add(webhookRecive);
            await _dbContext.SaveChangesAsync();

            return Ok(new { message = "Dados processados com sucesso." });
        }
        catch (Exception ex)
        {
            var innerException = ex.InnerException != null ? $" Inner Exception: {ex.InnerException.Message}" : "";
            await LogDebugInfoAsync($"Erro ao processar o webhook: {ex.Message}{innerException}", ex);
            return BadRequest(new { message = "Erro ao processar o webhook", error = ex.Message + innerException });
        }
    }

    private string ExtractValue(string line, string label)
    {
        return line.Replace("-", "").Replace(label, "").Trim();
    }

    private string CleanHtmlTags(string text)
    {
        if (string.IsNullOrEmpty(text))
            return "";
        return System.Text.RegularExpressions.Regex.Replace(text, "<[^>]+>", "").Trim();
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
