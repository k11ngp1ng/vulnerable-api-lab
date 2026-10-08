# Dia 2 — SQL injection e a fronteira entre dados e consulta

## Pergunta, hipótese e pré-condições

**Pergunta:** o que muda quando um título fornecido pelo cliente é inserido no texto SQL, em vez de enviado como valor de um parâmetro?

**Hipótese antes do teste:** a busca por título deveria devolver apenas a anotação cujo título é exatamente igual à entrada. Se o título for concatenado entre aspas no comando SQL, uma entrada com aspas e operadores poderá alterar o predicado. Se for passada como parâmetro, o banco deverá compará-la como texto literal.

O experimento usa somente o PostgreSQL 17 do Docker Compose, publicado em `127.0.0.1:5433`, e as anotações fictícias `1 / Lab note A` e `2 / Lab note B` de `db/seed.sql`. A variável `LAB_DB_CONNECTION_STRING` é exigida pelos testes e validada para apontar para esse banco local. A demonstração insegura está exclusivamente em `SqlInjectionTests.cs`; nenhuma rota HTTP a executa. As rotas privadas continuam fechadas com 403.

## Experimento e resultado observado

A função de teste constrói o comando assim:

```csharp
var sql = $"SELECT id FROM notes WHERE title = '{title}' ORDER BY id";
```

Para a entrada fictícia `' OR 1=1 --`, o comando resultante contém `WHERE title = '' OR 1=1 --' ORDER BY id`. A primeira aspa fecha o valor vazio, `OR 1=1` torna o predicado verdadeiro para todas as linhas e `--` comenta o restante da linha. **Previsão:** a busca insegura devolveria os IDs 1 e 2, embora não exista uma anotação com esse título. **Observação:** devolveu os IDs 1 e 2.

A implementação em `NoteLookup.cs` usa SQL fixo e parâmetro posicional:

```csharp
const string sql = "SELECT id FROM notes WHERE title = $1 ORDER BY id";
command.Parameters.Add(new NpgsqlParameter { Value = title });
```

Com a mesma entrada, a consulta parametrizada devolveu zero linhas. Com o título normal `Lab note A`, ambas devolveram somente o ID 1. Os dois testes de integração passaram junto com os sete testes de superfície existentes: **9 aprovados em 2026-10-08**.

## Causa-raiz, impacto e correção

O limite de confiança é a passagem do texto controlado pelo chamador para o comando enviado ao banco. Na versão insegura, a aplicação incorpora o texto à gramática SQL antes de o PostgreSQL interpretar o comando. O banco não tem como saber que os operadores vieram de um suposto título. Neste conjunto fictício, o impacto observado foi ampliar uma busca de uma anotação para duas. Em uma rota real que devolvesse conteúdo privado, a mesma classe de falha poderia expor registros fora do filtro pretendido; **essa exposição HTTP não foi demonstrada aqui**.

Na versão corrigida, o texto do comando permanece fixo e o valor é enviado separadamente por Npgsql. O banco o trata como dado para a comparação `title = $1`, mesmo que contenha aspas, operadores ou comentários. Uma validação de formato ou tamanho pode ser útil para regras de negócio e recursos, mas não substitui a parametrização: aceitar só certos títulos neste teste não corrigiria a mistura entre entrada e código em outras consultas. Um ORM também só oferece essa separação quando a consulta concreta usa seus mecanismos de parâmetros; SQL construído por concatenação continua perigoso.

## Reproduzir e verificar

Na raiz do repositório, com `.env` local configurado conforme o README:

```powershell
docker compose up -d --wait db
docker compose exec -T db psql -U lab_user -d vulnerable_api_lab -c 'SELECT id, title FROM notes ORDER BY id'
$passwordLine = Get-Content .env | Where-Object { $_ -match '^LAB_DB_PASSWORD=' } | Select-Object -First 1
$labPassword = $passwordLine.Substring('LAB_DB_PASSWORD='.Length)
$env:LAB_DB_CONNECTION_STRING = "Host=127.0.0.1;Port=5433;Database=vulnerable_api_lab;Username=lab_user;Password=$labPassword"
dotnet test tests/VulnerableApiLab.Api.Tests/VulnerableApiLab.Api.Tests.csproj
Remove-Item Env:LAB_DB_CONNECTION_STRING
```

Os testes de integração são marcados como **ignorados** quando `LAB_DB_CONNECTION_STRING` não está definida. Nesse caso, a execução da suíte não verifica o Dia 2. O teste de regressão falharia se a consulta parametrizada voltasse a incorporar o título ao texto SQL; o teste de comportamento normal confirma que a mitigação preserva a busca exata pelos dados de exemplo.

## Limites e próxima pergunta

O teste usa duas linhas conhecidas, uma busca somente de leitura e um banco local com usuário de laboratório. Ele verifica esse predicado e essa entrada; não cobre todas as formas de SQL injection, consultas dinâmicas com identificadores, privilégios do banco nem autorização por proprietário. A classe `NoteLookup` ainda não é usada pela API HTTP, pois falta autenticação e autorização. A próxima pergunta é como armazenar e verificar credenciais fictícias sem guardar senhas recuperáveis, antes de permitir acesso privado às anotações.

**Fontes consultadas:** [Npgsql — Basic Usage: Parameters](https://www.npgsql.org/doc/basic-usage.html#parameters) e [PostgreSQL 17 — PREPARE](https://www.postgresql.org/docs/17/sql-prepare.html).
