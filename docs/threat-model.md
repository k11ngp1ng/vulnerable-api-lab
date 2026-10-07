# Modelo de ameaças — anotações privadas

**Estado em 2026-10-07:** análise e superfície mínima do Dia 1. A API tem rotas de anotações que recusam todas as requisições com 403 enquanto não há autenticação e autorização. O banco local contém dois usuários e duas anotações fictícias, mas a API ainda não o acessa. As regras de propriedade abaixo continuam hipóteses para experimentos futuros, não resultados de um teste de autorização.

## Pergunta

Se uma API permite que cada pessoa mantenha anotações privadas, em quais pontos ela precisa decidir quem pode ler ou alterar cada anotação?

## Conceitos aplicados ao exemplo

- **Confidencialidade:** o texto de uma anotação deve ser visto apenas por seu proprietário. **Integridade:** outra pessoa não deve alterar o texto nem o vínculo de propriedade. **Disponibilidade:** o serviço deve continuar utilizável para quem tem permissão.
- **Ativo:** conteúdo e vínculo de propriedade das anotações. **Ameaça:** alguém tenta ler ou alterar uma anotação alheia. **Vulnerabilidade hipotética:** a API confia em um `ownerId` enviado pelo cliente. **Risco:** essa decisão poderia expor ou modificar dados privados; a ocorrência e o impacto ainda não foram demonstrados.
- **Superfície de ataque:** rota, ID, parâmetros, corpo e cabeçalhos das requisições HTTP. **Limite de confiança:** esses valores entram na API vindos de um cliente que pode alterá-los.
- **Autenticação:** estabelecer de modo confiável quem faz a chamada. **Autorização:** decidir se essa identidade pode executar uma operação sobre uma anotação específica. **Menor privilégio:** conceder apenas as operações necessárias sobre as próprias anotações.

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

## Superfície implementada e observada

| Requisição | Resultado atual | Motivo |
| --- | --- | --- |
| `GET /health` | 200 | Endpoint público sem dados privados. |
| `GET /api/notes/{id}` | 403 | Leitura privada fechada até existir autenticação e autorização. |
| `POST /api/notes` | 403 | Criação privada fechada. |
| `PUT /api/notes/{id}` e `DELETE /api/notes/{id}` | 403 | Alteração e exclusão privadas fechadas. |

O cliente pode enviar `id`, `ownerId`, conteúdo e cabeçalhos como `X-Lab-User`, mas nenhum deles é aceito como prova de identidade. Sete testes em `Production` verificaram a resposta pública e as recusas. Em execução local, `GET /api/notes/1?ownerId=user-a` com `X-Lab-User: user-a` retornou 403. A API não consultou o banco nessa requisição.

O 403 é uma recusa temporária enquanto nenhum mecanismo de login existe. Ele não indica que uma decisão de autorização por proprietário foi tomada. Quando a autenticação for implementada, os códigos para pessoa anônima e pessoa autenticada sem permissão serão definidos e testados separadamente.

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

O comportamento atual prova apenas que a superfície de anotações está fechada nas condições testadas. Ele não prova que o proprietário conseguirá acesso nem que outra pessoa autenticada será recusada quando a funcionalidade existir. O próximo experimento pode consultar os dados fictícios no PostgreSQL sem registrar endpoints vulneráveis. A implementação e a demonstração controlada de autorização por objeto pertencem ao Dia 4, após autenticação mínima.
