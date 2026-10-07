# Diário de segurança

## 2026-10-07 — Começo do laboratório e modelo de ameaças

- **Pergunta e hipótese:** onde uma API de anotações privadas deve decidir se alguém pode acessar uma anotação? Hipótese: autenticar a pessoa não basta; a API precisa verificar a propriedade do objeto em cada operação.
- **Conceito estudado e entendimento anterior:** distingui identidade apresentada, autenticação e autorização por objeto. Registrei quais dados são controlados pelo cliente em [threat-model.md](threat-model.md).
- **Experimento e ambiente:** implementei a API mínima e executei testes de superfície no ambiente `Production`, uma chamada HTTP real pelo loopback e PostgreSQL 17 local pelo Docker Compose. O banco contém apenas dois usuários e duas anotações fictícias; não há dados reais.
- **Resultado observado e evidência:** SDK .NET 10.0.401 e Docker Engine 29.6.2 funcionaram. `GET /health` retornou 200 e `{"status":"ok"}`; `GET /api/notes/1` retornou 404. Quatro testes passaram. `pg_isready` aceitou conexões, uma consulta autenticada `SELECT 1` retornou `1`, e a consulta das anotações mostrou `1/user-a` e `2/user-b`; a porta do banco ficou em `127.0.0.1:5433`. O remoto `origin` aponta para `https://github.com/k11ngp1ng/vulnerable-api-lab.git`. O cliente `psql` não está no PATH do host, mas foi usado dentro do contêiner.
- **Causa-raiz e impacto:** nenhuma falha de código foi observada. A análise aponta decisões que poderiam causar exposição ou alteração indevida se fossem omitidas.
- **Mitigação e verificação:** nenhum endpoint privado ou deliberadamente vulnerável foi registrado na API. Os quatro testes confirmam a resposta de saúde e a ausência das rotas de anotações no ambiente `Production`. A estratégia de manter demonstrações inseguras apenas em testes está registrada em [lab-safety.md](lab-safety.md).
- **Limites do teste e riscos restantes:** a API ainda não usa o PostgreSQL; não há autenticação, autorização ou consulta de anotação para testar. A auditoria NuGet não obteve dados de vulnerabilidade por indisponibilidade de `api.nuget.org` (`NU1900`). `dotnet --info` teve erro de permissão ao consultar serviços do Windows, mas `dotnet test` e `dotnet run` funcionaram. Os testes não provam segurança geral.
- **O que mudou no meu entendimento:** um login válido responde quem está chamando; a permissão depende também do recurso solicitado e da operação.
- **Dúvidas e próxima pergunta:** como preparar a menor consulta a dados fictícios no PostgreSQL e demonstrar que entrada do cliente permanece dado, sem alterar a estrutura SQL? A verificação de propriedade virá depois da autenticação mínima.

### Backlog de conhecimento

| Pergunta | Por que importa | Entendimento atual | Fonte a consultar | Prioridade |
| --- | --- | --- | --- | --- |
| Onde concentrar a checagem de propriedade sem deixar endpoints descobertos? | Evitar decisões inconsistentes entre leitura e escrita. | A decisão deve ocorrer no servidor em toda operação sobre o objeto. | Documentação oficial de autorização do ASP.NET Core e experimento local. | Alta |
| Como distinguir falha de autenticação de falha de autorização nos testes? | Tornar a evidência específica. | São condições distintas que exigem usuários e respostas diferentes. | Documentação HTTP/ASP.NET Core e testes locais. | Média |

### Divulgação

- **Insight:** conhecer o ID e estar autenticado não bastam para autorizar acesso a uma anotação privada.
- **Evidência compartilhável:** modelo de fluxo e tabela de riscos em [threat-model.md](threat-model.md); baseline executável e testes de superfície sem dados sensíveis.
- **Público:** desenvolvedores que estão começando a pensar em segurança de APIs.
- **Decisão:** rascunho conceitual em inglês para revisão; não apresentar como vulnerabilidade reproduzida.

## 2026-10-07 — Dia 1: superfície de anotações fechada

- **Pergunta e hipótese:** o que a API deve fazer quando recebe um pedido de anotação privada antes de existir um mecanismo confiável de autenticação? Hipótese: recusar todas as operações e não interpretar `ownerId` ou cabeçalhos fornecidos pelo cliente como identidade.
- **Conceito estudado e entendimento anterior:** a rota e o ID fazem parte da superfície de ataque, mas não conferem direito de acesso. A matriz desejada por proprietário continua distinta do comportamento temporário de recusa total.
- **Experimento e ambiente:** registrei `GET`, `POST`, `PUT` e `DELETE` para anotações com resposta 403. Executei testes em memória no ambiente `Production` e uma requisição HTTP pelo loopback local com `ownerId=user-a` e `X-Lab-User: user-a`.
- **Resultado observado e evidência:** sete testes passaram. `/health` retornou 200 e o pedido de leitura com identidade informada pelo cliente retornou 403. Nenhuma rota consultou o banco ou devolveu conteúdo de anotação.
- **Causa-raiz e impacto:** nenhuma falha foi reproduzida. Se o servidor aceitasse `ownerId` ou um cabeçalho arbitrário como prova de identidade, uma pessoa poderia escolher o proprietário aparente do pedido; esta é uma hipótese de risco, ainda não um comportamento observado.
- **Mitigação e verificação:** as rotas privadas permanecem fechadas até haver autenticação e autorização verificáveis. Os testes cobrem leitura anônima e tentativas de informar identidade em leitura, criação, alteração e exclusão.
- **Limites do teste e riscos restantes:** o 403 atual não prova autorização por proprietário. Não há fluxo de login, consulta do banco pela API, nem teste de proprietário versus outra pessoa autenticada. A auditoria de pacotes continua indisponível neste ambiente (`NU1900`).
- **O que preciso explicar com minhas palavras:** por que o ID e o `ownerId` são entradas não confiáveis e em qual ponto a API deve comparar a identidade autenticada com o proprietário persistido.
- **Dúvidas e próxima pergunta:** no futuro, qual resposta deve diferenciar pessoa anônima de pessoa autenticada sem permissão? Antes disso, como a consulta SQL pode manter entrada do cliente separada da estrutura da consulta?
