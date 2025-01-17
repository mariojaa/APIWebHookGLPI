using System;
using System.ComponentModel.DataAnnotations;

public class WebHookRecive
{
    public int Id { get; set; }
    
    [MaxLength(50)]
    public string TicketId { get; set; }
    
    [MaxLength(20)]
    public string Type { get; set; }  // "Chamado", "Acompanhamento" ou "Tarefa"
    
    [MaxLength(200)]
    public string Title { get; set; }
    
    [MaxLength(1000)]
    public string Description { get; set; }
    
    [MaxLength(100)]
    public string Author { get; set; }
    
    [MaxLength(200)]
    public string Assignees { get; set; }
    
    [MaxLength(50)]
    public string Status { get; set; }
    
    [MaxLength(20)]
    public string Priority { get; set; }
    
    public DateTime CreatedAt { get; set; }
    
    [MaxLength(50)]
    public string EstimatedTime { get; set; }
    
    [MaxLength(100)]
    public string TaskCategory { get; set; }
} 