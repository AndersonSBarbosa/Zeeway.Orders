# Zeeway Pedidos

API de pedidos em ASP.NET Core 8 com PostgreSQL, EF Core e integracao resiliente a um gateway ficticio.

## Estrutura

- `src/Zeeway.Pedidos.Api` - endpoints HTTP e configuracao da aplicacao.
- `src/Zeeway.Pedidos.Service` - casos de uso e orquestracao de pedidos.
- `src/Zeeway.Pedidos.Domain` - entidades e regras de dominio.
- `src/Zeeway.Pedidos.Infrastructure` - persistencia e integracao com gateway.
- `tests/Zeeway.Pedidos.UnitTests` - testes unitarios.
- `tests/Zeeway.Pedidos.IntegrationTests` - testes da API.

Os nomes internos foram padronizados para `Pedidos` e `Pagamentos`; rotas, JSON,
configuracoes e nomes de tabelas/colunas existentes foram mantidos para preservar compatibilidade.

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

- `POST /orders` - requer `Idempotency-Key`
- `GET /orders/{id}`
- `GET /orders?status=Pending&page=1&pageSize=20`
- `POST /webhooks/payments`
- `GET /health/live`
- `GET /health/ready`

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
