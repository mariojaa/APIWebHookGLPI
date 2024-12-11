
var builder = WebApplication.CreateBuilder(args);

// Adiciona o SignalR ao cont�iner de servi�os
builder.Services.AddSignalR();  // IMPORTANTE: Adicionar o SignalR

// Adiciona servi�os ao container
builder.Services.AddControllers(); // Adiciona suporte a controllers

// Configura��o opcional: Adiciona suporte ao Swagger (documenta��o da API)
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

// Cria a aplica��o
var app = builder.Build();

// Configura o pipeline de requisi��es HTTP
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseHttpsRedirection(); // Redireciona para HTTPS, se necess�rio

app.UseAuthorization(); // Configura a autoriza��o (se aplic�vel)

app.MapControllers(); // Mapeia automaticamente as controllers da API

// Inicia o aplicativo
app.Run();