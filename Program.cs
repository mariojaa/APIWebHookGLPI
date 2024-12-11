
var builder = WebApplication.CreateBuilder(args);

// Adiciona o SignalR ao contêiner de serviços
builder.Services.AddSignalR();  // IMPORTANTE: Adicionar o SignalR

// Adiciona serviços ao container
builder.Services.AddControllers(); // Adiciona suporte a controllers

// Configuração opcional: Adiciona suporte ao Swagger (documentação da API)
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

// Cria a aplicação
var app = builder.Build();

// Configura o pipeline de requisições HTTP
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseHttpsRedirection(); // Redireciona para HTTPS, se necessário

app.UseAuthorization(); // Configura a autorização (se aplicável)

app.MapControllers(); // Mapeia automaticamente as controllers da API

// Inicia o aplicativo
app.Run();