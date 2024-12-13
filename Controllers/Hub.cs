using Microsoft.AspNetCore.SignalR;

public class NotificationHub : Hub
{
    // Método para enviar mensagens para todos os clientes conectados
    public async Task SendNotification(string ticketId, string description)
    {
        await Clients.All.SendAsync("ReceiveNotification", ticketId, description);
    }
}