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

## 2026-10-08 — Dia 2: entrada como dado ou instrução SQL

- **Pergunta e hipótese:** um título com sintaxe SQL mudaria o filtro se fosse concatenado no comando? Hipótese: a concatenação permitiria ampliar o predicado; a parametrização manteria a entrada como um valor literal.
- **Conceito estudado e entendimento anterior:** entrada controlada pelo chamador atravessa a fronteira entre aplicação e banco. Validação de formato pode atender regras de negócio, mas não substitui a separação entre texto SQL e parâmetros. A proteção de um ORM depende de como a consulta é construída. Consulte a [investigação](experiments/sql-injection.md) e as fontes oficiais nela vinculadas.
- **Experimento e ambiente:** PostgreSQL 17 do Compose em `127.0.0.1:5433`, duas anotações fictícias do seed, Npgsql 10.0.3, uma consulta concatenada apenas no teste e uma busca parametrizada no código da API, ainda sem rota HTTP. A conexão de teste foi limitada ao banco local.
- **Resultado observado e evidência:** a entrada `' OR 1=1 --` devolveu IDs 1 e 2 na consulta concatenada e nenhum ID na parametrizada. O título `Lab note A` devolveu apenas o ID 1 nas duas. Nove testes passaram com a variável de conexão local configurada: sete testes de superfície e dois de integração. O banco retornou `1|Lab note A` e `2|Lab note B` antes do teste.
- **Causa-raiz e impacto:** a interpolação incluiu texto não confiável na gramática SQL; a aspa, o operador e o comentário alteraram o predicado. O impacto observado foi ampliar a leitura de IDs fictícios. Não houve exposição de conteúdo por HTTP.
- **Mitigação e verificação:** `NoteLookup.FindIdsByTitleAsync` mantém o comando fixo e envia o título como parâmetro posicional `$1`. O teste adversário verifica que a entrada não altera o filtro, e o teste normal verifica a busca exata. A API não registra essa busca como endpoint.
- **Limites do teste e riscos restantes:** a execução depende do banco local e dos dois registros do seed; sem `LAB_DB_CONNECTION_STRING`, os testes de integração são ignorados. A busca é só de leitura e cobre um predicado e uma entrada. Não há autenticação ou autorização por proprietário; todas as rotas privadas continuam fechadas.
- **O que mudou no meu entendimento:** o problema não é a presença de caracteres especiais em si, mas o ponto em que um valor controlado pelo chamador passa a ser analisado como parte da instrução. Parametrizar mantém esse limite explícito e preserva uma busca normal.
- **Dúvidas e próxima pergunta:** como demonstrar que a senha fictícia não precisa ser recuperável para permitir login e como testar o fluxo sem vazar senhas em banco ou logs? Essa é a pergunta do Dia 3.

### Backlog de conhecimento do Dia 2

| Pergunta | Por que importa | Entendimento atual | Fonte a consultar | Prioridade |
| --- | --- | --- | --- | --- |
| Como tratar identificadores dinâmicos quando parâmetros só representam valores? | Uma futura consulta pode escolher coluna ou ordenação. | Usar opções fixas permitidas no código; não concatenar um identificador arbitrário do cliente. | Documentação do Npgsql sobre parâmetros e um experimento local específico. | Média |
| Quais privilégios mínimos o usuário do banco da API deve ter? | Reduzir impacto caso uma consulta falhe. | O usuário atual é de laboratório; seus privilégios não foram avaliados nesta etapa. | Documentação oficial do PostgreSQL sobre roles e grants. | Média |

### Divulgação do Dia 2

- **Insight e público:** para desenvolvedores de APIs, o erro acontece quando um valor entra na gramática da consulta; o teste mostra a diferença entre concatenar e parametrizar.
- **Evidência compartilhável:** trecho de código, resultado 2 versus 0 IDs fictícios, teste normal e [investigação](experiments/sql-injection.md), sem senha ou conexão real no material.
- **Limite da conclusão:** uma busca local de leitura foi testada; nenhuma exposição HTTP nem segurança geral da API foi demonstrada.
- **Abertura e decisão:** [rascunho em inglês](posts/2026-10-08-day2-sql-injection-en.md) e [imagem](posts/assets/day2-input-as-data.png) preparados para revisão. A decisão de publicar é do autor.
