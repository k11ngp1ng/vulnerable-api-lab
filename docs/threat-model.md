# Modelo de ameaças — anotações privadas (rascunho)

**Estado em 2026-10-07:** análise de desenho do Dia 1. A API mínima, o banco local com dois usuários e duas anotações fictícias, e testes de superfície existem, mas ainda não há rota de anotações nem vínculo da API com o banco. As decisões e riscos abaixo são hipóteses a verificar em experimentos futuros, não resultados de um teste de autorização.

## Pergunta

Se uma API permite que cada pessoa mantenha anotações privadas, em quais pontos ela precisa decidir quem pode ler ou alterar cada anotação?

## Escopo e fluxo planejado

```text
Pessoa usando cliente HTTP
    │ requisição: identidade apresentada, ID da anotação, conteúdo
    ▼
API ASP.NET Core
    │ autentica a pessoa e autoriza a operação sobre o objeto
    ▼
PostgreSQL local
    │ dados fictícios: usuários e anotações com proprietário
    └── resposta volta pela API ao cliente
```

O cliente controla a rota, o ID pedido e o conteúdo enviado. A identidade declarada pelo cliente também não é confiável até ser autenticada. O banco armazena o vínculo entre anotação e proprietário; a API deve usar esse vínculo para decidir o acesso. Um ID conhecido ou uma sessão válida, isoladamente, não demonstram autorização sobre uma anotação específica.

## Ativos, atores e decisões

| Item | O que precisa ser protegido ou decidido |
| --- | --- |
| Conteúdo das anotações | Confidencialidade: apenas o proprietário deve ler. Integridade: apenas o proprietário deve alterar ou excluir. |
| Vínculo anotação–proprietário | Não pode ser definido ou trocado livremente pelo cliente. |
| Disponibilidade da API | Requisições abusivas não devem impedir o uso normal; este controle será estudado em etapa posterior. |
| Pessoa anônima | Não deve acessar anotações privadas. |
| Proprietário autenticado | Deve conseguir operar somente sobre as próprias anotações. |
| Outra pessoa autenticada | Não deve ganhar acesso por conhecer o ID de uma anotação. |

O primeiro limite de confiança é entre cliente e API. O segundo é entre a decisão da API e a leitura ou escrita no banco. Para cada operação sobre uma anotação, a API precisa associar a identidade autenticada ao proprietário do registro antes de devolver ou modificar o conteúdo.

## Matriz de acesso esperada

Esta é uma regra de desenho para os futuros endpoints, ainda não um comportamento implementado.

| Ator | Ler anotação própria | Alterar anotação própria | Ler ou alterar anotação alheia | Definir `ownerId` no pedido |
| --- | --- | --- | --- | --- |
| Proprietário autenticado | Permitir | Permitir | Negar | Ignorar valor informado; usar identidade autenticada |
| Outra pessoa autenticada | Não se aplica | Não se aplica | Negar | Ignorar valor informado; usar identidade autenticada |
| Pessoa anônima | Negar | Negar | Negar | Negar criação |

## Riscos priorizados e hipóteses de controle

| Prioridade | Hipótese de abuso | Controle planejado | Evidência necessária |
| --- | --- | --- | --- |
| Alta | Pessoa autenticada solicita o ID de uma anotação alheia e recebe seu conteúdo. | Verificar propriedade no servidor para cada leitura. | Testes com proprietário, outra pessoa e usuário anônimo. |
| Alta | Pessoa autenticada altera ou exclui anotação alheia. | Restringir escrita pelo proprietário derivado da identidade autenticada. | Testes que confirmem recusa e ausência de alteração no banco. |
| Média | Cliente informa um `ownerId` e cria ou transfere anotações para outra pessoa. | Ignorar proprietário fornecido pelo cliente; derivá-lo da identidade autenticada. | Teste com `ownerId` divergente e conferência do registro salvo. |

## Limites e próximo experimento

Este desenho ainda não prova que a API implementa os controles de acesso. A Etapa 0 criou e verificou a API local sem expor rotas privadas. O próximo experimento pode preparar dados fictícios e consultas no PostgreSQL sem registrar endpoints vulneráveis. A implementação e a demonstração controlada de autorização por objeto pertencem ao Dia 4, após autenticação mínima.
