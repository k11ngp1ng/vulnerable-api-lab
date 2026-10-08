# Vulnerable API Lab

Laboratório de segurança de aplicações em C# / ASP.NET Core e PostgreSQL. O ciclo de trabalho é estudar uma decisão de engenharia, formular uma hipótese, reproduzir uma falha **somente em ambiente local**, corrigi-la, testar a correção e explicar seus limites.

**Estado atual:** a API oferece `GET /health` e rotas mínimas de anotações privadas que recusam todas as requisições com 403. Não há autenticação nem autorização por proprietário. O Dia 2 acrescentou uma busca parametrizada no código da API, sem rota HTTP, e um exemplo deliberadamente inseguro apenas no projeto de testes. O PostgreSQL contém dois usuários e duas anotações fictícias.

## Pré-requisitos

- SDK .NET 10 (projeto `net10.0`).
- Docker Desktop com contêineres Linux e Docker Compose.
- PowerShell para os comandos abaixo. O cliente `psql` no host não é necessário; a verificação do banco usa o cliente dentro do contêiner.

## Execução local

Execute na raiz do repositório:

```powershell
$localPassword = [guid]::NewGuid().ToString("N")
"LAB_DB_PASSWORD=$localPassword" | Set-Content -NoNewline .env
docker compose config
docker compose up -d --wait db
docker compose exec -T db pg_isready -U lab_user -d vulnerable_api_lab
docker compose exec -T db psql -U lab_user -d vulnerable_api_lab -c 'SELECT id, owner_id FROM notes ORDER BY id'
dotnet test tests/VulnerableApiLab.Api.Tests/VulnerableApiLab.Api.Tests.csproj
dotnet run --project src/VulnerableApiLab.Api/VulnerableApiLab.Api.csproj --launch-profile local
```

Em outro terminal, com a API em execução:

```powershell
Invoke-RestMethod http://127.0.0.1:5080/health
try { Invoke-WebRequest http://127.0.0.1:5080/api/notes/1 -ErrorAction Stop } catch { [int]$_.Exception.Response.StatusCode }
```

A resposta esperada é `status: ok` para `/health` e `403` para `/api/notes/1`. A consulta SQL deve mostrar as anotações `1/user-a` e `2/user-b`. O PostgreSQL fica acessível apenas em `127.0.0.1:5433`; a API fica em `127.0.0.1:5080` pelo perfil `local`. Para encerrar o banco, execute `docker compose down`. O volume de dados local permanece para próximas sessões.

O script `db/seed.sql` é executado automaticamente apenas quando o volume do PostgreSQL é criado. Se o volume já existia antes de o script ser adicionado, aplique-o uma vez sem apagar os dados existentes:

```powershell
Get-Content -Raw db/seed.sql | docker compose exec -T db psql -v ON_ERROR_STOP=1 -U lab_user -d vulnerable_api_lab
```

O arquivo `.env` é ignorado pelo Git. Use somente dados e credenciais fictícios. Não altere o vínculo dos serviços para uma interface pública. Antes de qualquer commit ou publicação, revise `git status`, o diff e os arquivos incluídos para evitar segredos e dados reais.

### Experimento do Dia 2 no PostgreSQL local

Os dois testes de SQL injection precisam de `LAB_DB_CONNECTION_STRING`. Com o banco iniciado e as duas anotações de `db/seed.sql` presentes, execute:

```powershell
$passwordLine = Get-Content .env | Where-Object { $_ -match '^LAB_DB_PASSWORD=' } | Select-Object -First 1
$labPassword = $passwordLine.Substring('LAB_DB_PASSWORD='.Length)
$env:LAB_DB_CONNECTION_STRING = "Host=127.0.0.1;Port=5433;Database=vulnerable_api_lab;Username=lab_user;Password=$labPassword"
dotnet test tests/VulnerableApiLab.Api.Tests/VulnerableApiLab.Api.Tests.csproj
Remove-Item Env:LAB_DB_CONNECTION_STRING
```

A conexão é validada para o banco local do laboratório. Sem essa variável, os testes de integração são ignorados; uma execução da suíte nessa condição não verifica o Dia 2. Consulte [a investigação](docs/experiments/sql-injection.md) para a hipótese, o resultado e os limites.

## O que os testes verificam agora

Os testes de superfície sobem a API em memória no ambiente `Production`, confirmam a resposta de `/health` e verificam que leitura, criação, alteração e exclusão de anotações são recusadas mesmo quando o cliente envia um ID de usuário, `ownerId` ou conteúdo. Dois testes adicionais usam o PostgreSQL local para comparar uma busca concatenada com a consulta parametrizada e verificar um título normal. A suíte passou com nove testes quando a conexão local foi fornecida. Ela não demonstra autenticação, autorização por proprietário nem segurança geral da API.

## Organização e próximos passos

- [Roadmap](ROADMAP.md): sequência de estudo e critérios de saída.
- [Modelo de ameaças](docs/threat-model.md): ativo, fluxo, limites de confiança e riscos priorizados para anotações privadas.
- [Estratégia de isolamento](docs/lab-safety.md): onde ficarão demonstrações intencionalmente inseguras.
- [Diário de segurança](docs/security-journal.md): evidências, dúvidas e limites por sessão.
- [SQL injection — Dia 2](docs/experiments/sql-injection.md): hipótese, reprodução local, correção e regressão.
- [Matriz de competências](docs/competency-matrix.md): níveis vinculados a evidências do repositório.

O próximo passo técnico é o Dia 3: investigar armazenamento e verificação de senhas fictícias para uma autenticação mínima. As rotas de anotações privadas continuam fechadas até que a autenticação e a autorização por objeto sejam verificadas.
