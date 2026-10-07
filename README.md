# Vulnerable API Lab

Laboratório de segurança de aplicações em C# / ASP.NET Core e PostgreSQL. O ciclo de trabalho é estudar uma decisão de engenharia, formular uma hipótese, reproduzir uma falha **somente em ambiente local**, corrigi-la, testar a correção e explicar seus limites.

**Estado atual:** a API oferece apenas `GET /health`. Não existem endpoints de anotações nem exemplos vulneráveis em execução. O PostgreSQL recebe dois usuários e duas anotações fictícias na primeira criação do volume pelo Docker Compose, mas a API ainda não se conecta ao banco.

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
```

A resposta esperada é `status: ok`. A consulta SQL deve mostrar as anotações `1/user-a` e `2/user-b`. O PostgreSQL fica acessível apenas em `127.0.0.1:5433`; a API fica em `127.0.0.1:5080` pelo perfil `local`. Para encerrar o banco, execute `docker compose down`. O volume de dados local permanece para próximas sessões.

O script `db/seed.sql` é executado automaticamente apenas quando o volume do PostgreSQL é criado. Se o volume já existia antes de o script ser adicionado, aplique-o uma vez sem apagar os dados existentes:

```powershell
Get-Content -Raw db/seed.sql | docker compose exec -T db psql -v ON_ERROR_STOP=1 -U lab_user -d vulnerable_api_lab
```

O arquivo `.env` é ignorado pelo Git. Use somente dados e credenciais fictícios. Não altere o vínculo dos serviços para uma interface pública. Antes de qualquer commit ou publicação, revise `git status`, o diff e os arquivos incluídos para evitar segredos e dados reais.

## O que os testes verificam agora

Os testes sobem a API em memória no ambiente `Production`, confirmam a resposta de `/health` e verificam que rotas de anotações privadas ainda não estão disponíveis. Eles não demonstram autenticação, autorização, acesso ao PostgreSQL nem segurança geral da API.

## Organização e próximos passos

- [Roadmap](ROADMAP.md): sequência de estudo e critérios de saída.
- [Modelo de ameaças](docs/threat-model.md): ativo, fluxo, limites de confiança e riscos priorizados para anotações privadas.
- [Estratégia de isolamento](docs/lab-safety.md): onde ficarão demonstrações intencionalmente inseguras.
- [Diário de segurança](docs/security-journal.md): evidências, dúvidas e limites por sessão.

O próximo passo técnico é conectar um recurso mínimo de dados fictícios ao PostgreSQL para o experimento local de consulta parametrizada, mantendo qualquer versão insegura exclusivamente no código de testes. A implementação de anotações privadas depende de autenticação e autorização verificadas em etapas posteriores.
