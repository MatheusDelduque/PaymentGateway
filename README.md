# PaymentGateway

Mini gateway de pagamentos em C#/.NET 10, criado como projeto de estudo de arquitetura de software, Domain-Driven Design (DDD) e testes. Opera somente em reais (BRL).

> Projeto em desenvolvimento: hoje, o que está implementado é o núcleo do domínio. A API ainda é apenas um esqueleto, e não há integração com adquirentes nem persistência.

## O que já foi feito

- **`Money`:** value object para valores em reais. Rejeita valores negativos e mais de 2 casas decimais.
- **`Payment`:** agregado imutável, em que cada transição devolve um novo objeto. Controla autorização, captura, estorno, conclusão e cancelamento.
- **Regras de valor:**
  - O valor do pagamento não muda depois da criação.
  - A autorização é sempre do valor integral.
  - Cada pagamento tem uma única captura, total ou parcial, limitada ao valor do pagamento.
  - Estornos parciais se acumulam até o valor capturado.
- **Testes:** testes unitários do domínio com xUnit.

## Ciclo de vida do pagamento

| De | Operação | Para |
| --- | --- | --- |
| `Created` | `Authorize()` | `Authorized` |
| `Created` ou `Authorized` | `Cancel()` | `Canceled` |
| `Authorized` | `Capture(valor)` | `Captured` |
| `Captured` | `Complete()` | `Completed` |
| `Captured` ou `Completed` | `Refund(valor)` parcial | estado não muda |
| `Captured` ou `Completed` | `Refund(valor)` que completa o valor capturado | `Refunded` |

## Arquitetura

Monolito modular com arquitetura hexagonal. As dependências apontam para o domínio:

```text
Api ──────────> Application ──> Domain
 └─> Infrastructure ──┘
```

| Projeto | Responsabilidade |
| --- | --- |
| `PaymentGateway.Domain` | Entidades, value objects e regras de negócio, sem dependências externas |
| `PaymentGateway.Application` | Casos de uso e portas (a implementar) |
| `PaymentGateway.Infrastructure` | Adaptadores externos, como banco e adquirentes (a implementar) |
| `PaymentGateway.Api` | Ponto de entrada HTTP (a implementar) |

## Como executar os testes

É necessário ter o [SDK do .NET 10](https://dotnet.microsoft.com/download).

```bash
dotnet test
```

## Próximos passos

- [ ] Concluir as regras de criação e os testes de transição de estado
- [ ] Autorizar e capturar por meio de uma porta para o adquirente
- [ ] Tratar recusa, resposta desconhecida e expiração da autorização
- [ ] Adicionar idempotência e resiliência
- [ ] Registrar o histórico de cada pagamento
- [ ] Expor os endpoints da API
