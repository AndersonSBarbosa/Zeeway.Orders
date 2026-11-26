# Zeeway Pedidos

API de pedidos em ASP.NET Core 8 com PostgreSQL, EF Core e integracao resiliente a um gateway ficticio.

## Estrutura

- `src/Zeeway.Pedidos.Api` - endpoints HTTP e configuracao da aplicacao.
- `src/Zeeway.Pedidos.Service` - casos de uso e orquestracao de pedidos.
- `src/Zeeway.Pedidos.Domain` - entidades e regras de dominio.
- `src/Zeeway.Pedidos.Infrastructure` - persistencia e integracao com gateway.
- `tests/Zeeway.Pedidos.UnitTests` - testes unitarios.
- `tests/Zeeway.Pedidos.IntegrationTests` - testes da API.

Os nomes internos foram padronizados para `Pedidos` e `Pagamentos`; os contratos
JSON e os nomes de tabelas/colunas existentes foram mantidos para preservar compatibilidade.

## Como executar

Pre-requisitos: .NET 8 SDK e Docker.

```powershell
docker compose up --build
```

A API fica em `http://localhost:8080` e o Swagger em `http://localhost:8080/swagger`.

Execucao local:

```powershell
docker compose up postgres
dotnet run --project src/Zeeway.Pedidos.Api
```

Testes:

```powershell
dotnet test Zeeway.Pedidos.sln
```

## Endpoints

- `POST /pedidos` - requer `Idempotency-Key`
- `GET /pedidos/{id}`
- `GET /pedidos?status=Pending&page=1&pageSize=20`
- `POST /webhooks/payments`
- `GET /health/live`
- `GET /health/ready`

O filtro de status aceita `Pending`, `Paid`, `Failed` e os nomes localizados
`Pendente`, `Pago`, `Falha`. As respostas continuam usando os valores legados.

Exemplo:

```json
{
  "customerEmail": "cliente@example.com",
  "items": [
    { "name": "Teclado", "quantity": 1, "unitPrice": 250.00 }
  ]
}
```

O webhook usa `X-Signature-SHA256` contendo o HMAC-SHA256 hexadecimal do corpo bruto. O segredo vem de `PaymentGateway:WebhookSecret`.

Payload aceito:

```json
{
  "event_id": "evt-123",
  "order_id": "GUID-DO-PEDIDO",
  "type": "payment.approved"
}
```

Tipos: `payment.approved` e `payment.failed`.

### Executar o webhook localmente

Use um `order_id` de um pedido existente (retornado por `POST /pedidos`). O segredo abaixo e o valor local de desenvolvimento em `appsettings.json`; em outros ambientes, use o segredo configurado para `PaymentGateway:WebhookSecret`.

```powershell
$base = "http://localhost:5038"
$pedidoId = "GUID-RETORNADO-PELO-POST-PEDIDOS"
$secret = "development-webhook-secret"

$body = @{
    event_id = "evt_$([guid]::NewGuid().ToString('N'))"
    order_id = $pedidoId
    type = "payment.approved"
} | ConvertTo-Json -Compress

$hmac = [System.Security.Cryptography.HMACSHA256]::new(
    [Text.Encoding]::UTF8.GetBytes($secret)
)
try {
    $signature = [BitConverter]::ToString(
        $hmac.ComputeHash([Text.Encoding]::UTF8.GetBytes($body))
    ).Replace("-", "").ToLowerInvariant()
}
finally {
    $hmac.Dispose()
}

curl.exe -X POST "$base/webhooks/payments" `
    -H "Content-Type: application/json" `
    -H "X-Signature-SHA256: $signature" `
    --data-raw $body

Invoke-RestMethod "$base/pedidos/$pedidoId" |
    Select-Object id, status, paymentId
```

O corpo usado para calcular a assinatura precisa ser exatamente o mesmo enviado. O esperado e HTTP 200 e status `Paid`. Para testar falha, troque `payment.approved` por `payment.failed`; para testar duplicidade, reenvie o mesmo corpo com a mesma assinatura.

### Enviar pelo Swagger

1. Use `POST /pedidos` no Swagger para criar um pedido e copie o `id` retornado.
2. Gere a assinatura do corpo exato que sera enviado. No PowerShell, substitua o GUID por esse `id`:

```powershell
$secret = "development-webhook-secret"
$body = '{"event_id":"evt-swagger-001","order_id":"GUID-DO-PEDIDO","type":"payment.approved"}'

$hmac = [System.Security.Cryptography.HMACSHA256]::new(
    [Text.Encoding]::UTF8.GetBytes($secret)
)
try {
    $signature = [BitConverter]::ToString(
        $hmac.ComputeHash([Text.Encoding]::UTF8.GetBytes($body))
    ).Replace("-", "").ToLowerInvariant()
}
finally {
    $hmac.Dispose()
}

$signature
```

3. No `POST /webhooks/payments`, clique em **Try it out**, cole o mesmo JSON de `$body` em uma linha e cole o valor de `$signature` no header `X-Signature-SHA256`.
4. Execute. O esperado e HTTP 200; consulte `GET /pedidos/{id}` para confirmar `Paid`.

Se alterar qualquer caractere do corpo ou usar outro `event_id`, gere uma nova assinatura. O Swagger mostra o campo, mas nao calcula o HMAC automaticamente.

## Decisoes

- PostgreSQL foi escolhido por transacoes, indices unicos e concorrencia.
- A chave de idempotencia e persistida junto ao pedido em transacao unica; repeticoes nao criam outro pedido nem outra cobranca.
- A maquina de estados permite somente `Pending -> Paid` e `Pending -> Failed`. Webhooks duplicados sao tratados por `event_id`; eventos fora de ordem nao alteram estado final.
- PostgreSQL `xmin` funciona como token de concorrencia otimista em `Pedido`.
- O gateway usa timeout, retry com backoff e circuit breaker via `Microsoft.Extensions.Http.Resilience`.
- Um handler HTTP fake permite demonstrar o fluxo sem dependencia externa.
- Erros usam `ProblemDetails`. Todas as respostas incluem `X-Correlation-ID`, tambem registrado em logs estruturados.
- Migrações EF Core são aplicadas na inicialização.

## Trade-offs

Nao implementei Outbox, reconciliação assíncrona ou OpenTelemetry. Uma chamada de cobrança falha deixa o pedido `Pending`, pois a execução atual é síncrona e não possui worker de recuperação.

## Próximos passos

- Outbox para `PedidoPago`.
- Reconciliação de cobranças pendentes.
- OpenTelemetry e métricas de resiliencia.
- Testcontainers para integração real com PostgreSQL.
- Mais testes de HMAC e transicoes fora de ordem.

## Uso de IA

O projeto foi montado com auxílio do Copilot para o esqueleto da solução, entidades, EF Core, endpoints, testes e documentação. O autor revisou e corrigiu a implementação, especialmente tipos de retorno, configuracao EF, idempotencia e migracoes.


4bf5e806-12ba-4165-bd71-217a8b674f13
