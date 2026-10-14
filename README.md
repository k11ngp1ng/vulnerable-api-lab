# Vulnerable API Lab

Este é meu laboratório de segurança de APIs em ASP.NET Core e PostgreSQL. Cada exercício usa dados fictícios e fica restrito ao computador local. Os exemplos deliberadamente inseguros existem apenas nos testes; a API executável não registra essas rotas.

## Executar

É necessário ter .NET 10, Docker Desktop com contêineres Linux e PowerShell. Na raiz do repositório:

```powershell
if (-not (Test-Path .env)) {
    "LAB_DB_PASSWORD=$([guid]::NewGuid().ToString('N'))" | Set-Content -NoNewline .env
}
docker compose up -d --wait db
$passwordLine = Get-Content .env | Where-Object { $_ -match '^LAB_DB_PASSWORD=' } | Select-Object -First 1
$labPassword = $passwordLine.Substring('LAB_DB_PASSWORD='.Length)
$env:LAB_DB_CONNECTION_STRING = "Host=127.0.0.1;Port=5433;Database=vulnerable_api_lab;Username=lab_user;Password=$labPassword"
dotnet test tests/VulnerableApiLab.Api.Tests/VulnerableApiLab.Api.Tests.csproj
dotnet run --project src/VulnerableApiLab.Api/VulnerableApiLab.Api.csproj --launch-profile local
```

Em outro terminal, `Invoke-RestMethod http://127.0.0.1:5080/health` deve retornar `status: ok`. Uma leitura anônima em `/api/notes/1` deve retornar 401 no perfil local. O banco fica vinculado a `127.0.0.1:5433`, e a API a `127.0.0.1:5080`.

O script [db/seed.sql](db/seed.sql) roda quando o volume é criado. Para atualizar um volume antigo sem apagá-lo:

```powershell
Get-Content -Raw db/seed.sql | docker compose exec -T db psql -v ON_ERROR_STOP=1 -U lab_user -d vulnerable_api_lab
```

O arquivo `.env` fica fora do Git. Use somente contas e senhas fictícias. Não exponha este laboratório à rede pública.

## O que está implementado

- [SQL injection](tests/VulnerableApiLab.Api.Tests/SqlInjectionTests.cs): o teste compara uma consulta concatenada sobre anotações fictícias com a busca parametrizada em [NoteLookup.cs](src/VulnerableApiLab.Api/NoteLookup.cs). A busca não tem rota HTTP.
- [Autenticação](tests/VulnerableApiLab.Api.Tests/AuthenticationTests.cs): cadastro e login locais guardam hash de senha com sal. Uma senha errada recebe 401; uma sessão válida é verificada em `/api/auth/me`.
- [Autorização por objeto](tests/VulnerableApiLab.Api.Tests/ObjectAuthorizationTests.cs): uma leitura apenas por ID encontra uma anotação de outro usuário no teste. A rota corrigida usa a identidade da sessão e o proprietário no banco: dono recebe 200, outro usuário 404, anônimo 401.
- [Limite de tentativas](tests/VulnerableApiLab.Api.Tests/ApiAbuseTests.cs): o login local aceita três pedidos por IP em cinco segundos; o excesso recebe 429 e o acesso volta após a janela.
- [Tratamento de erros](tests/VulnerableApiLab.Api.Tests/SecurityLayersTests.cs): uma falha simulada retorna 500 genérico, sem copiar o diagnóstico para a resposta ou os logs capturados.

As rotas de autenticação e leitura existem apenas em `Development` e aceitam somente loopback. Em `Production`, não há rotas de autenticação e a leitura de anotações fica fechada. Criação, edição e exclusão de anotações continuam em 403. Os testes com PostgreSQL exigem `LAB_DB_CONNECTION_STRING`; sem ela, esses testes são ignorados.

## Limites conhecidos

O usuário `lab_user` do banco ainda tem privilégios de superusuário; separar os papéis de instalação e execução é uma pendência. O limite de login fica em memória, agrupa pessoas pelo IP e não cobre cadastro. Não há teste de revogação de sessão nem autorização de escrita. A auditoria de dependências não foi concluída porque a fonte de advisories do NuGet estava inacessível. Os testes demonstram os comportamentos descritos nas condições locais, não segurança geral da API.
