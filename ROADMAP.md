# Roadmap — Vulnerable API Lab

## Norte do projeto

O laboratório conecta decisões de engenharia de software a falhas e defesas de segurança. A primeira semana cobre fundamentos de **Application Security** em uma API própria. A meta é explicar e demonstrar cada resultado, não apenas executar ferramentas ou colecionar vulnerabilidades.

O repositório indicado para todo o trabalho é `https://github.com/k11ngp1ng/vulnerable-api-lab`. O remoto local `origin` foi configurado para esse endereço em 2026-10-07, e o baseline foi enviado ao branch `master`.

### Resultado esperado do primeiro ciclo

Ao final, outra pessoa deve conseguir entender quais ativos a API protege, onde a confiança muda, como cada falha foi reproduzida localmente, por que ocorreu, qual controle a corrige e o que os testes de regressão realmente verificam. O README deve trazer instruções de execução local e deixar claro que se trata de um laboratório intencionalmente vulnerável.

### Regras de ritmo

- **Dias são etapas, não prazos.** Se um conceito exigir mais tempo, permaneça nele.
- **Uma falha bem investigada vale mais que várias demonstrações superficiais.**
- **Cada dia termina com evidência e uma explicação própria.** Consulte o modelo de registro em `AGENTS.md`.
- **Conteúdo público nasce do resultado, não do calendário.** Prepare apenas posts sustentados pelo experimento.

## Ciclo 1 — fundamentos de segurança de aplicações

### Estado verificado em 2026-10-07

- **Etapa 0 concluída no ambiente local testado:** SDK .NET 10.0.401; Docker Engine 29.6.2; PostgreSQL 17 pelo Compose, vinculado a `127.0.0.1:5433`, com dois usuários e duas anotações fictícias; API mínima em `127.0.0.1:5080`; `GET /health` retornou 200; consulta autenticada `SELECT 1` no banco retornou `1` e a consulta de anotações retornou `1/user-a` e `2/user-b`. O README contém os comandos de reprodução. A API ainda não se conecta ao banco.
- **Dia 1 — superfície mínima implementada e verificada:** [modelo de ameaças](docs/threat-model.md) com ativos, atores, entradas controladas pelo cliente, limites de confiança, matriz de acesso esperada e três riscos priorizados. As rotas de anotações existem, mas recusam todas as requisições com 403 até que autenticação e autorização sejam implementadas. Sete testes passaram no ambiente `Production`; uma requisição local com identidade informada pelo cliente também retornou 403. A matriz de propriedade continua hipótese para o Dia 4.
- **Limite da verificação:** a auditoria de vulnerabilidades dos pacotes NuGet não conseguiu consultar `api.nuget.org` neste ambiente (`NU1900`). Os testes não demonstram segurança geral nem os controles planejados para as próximas etapas.

### Etapa 0 — preparar o laboratório

**Objetivo:** deixar o ambiente reproduzível antes de adicionar comportamento inseguro.

1. Confirmar ferramentas disponíveis, versões escolhidas de .NET, PostgreSQL e Docker, e registrar comandos de instalação/execução no README.
2. Configurar o remoto Git oficial informado pelo autor e conferir o destino antes de qualquer `push`.
3. Criar a API, testes e `docker compose` com configuração local e dados fictícios. Adicionar `.gitignore` e exemplo de configuração sem segredos.
4. Definir a estratégia para manter exercícios vulneráveis isolados e desabilitados por padrão.

**Saída:** uma pessoa consegue subir e testar a API localmente seguindo o README; nenhuma credencial real ou endpoint vulnerável está exposto. Esta etapa pode ser feita junto com o Dia 1.

### Dia 1 — modelo de ameaças e superfície de ataque

**Estudar:** tríade confidencialidade/integridade/disponibilidade, ativo, ameaça, vulnerabilidade, risco, superfície de ataque, limite de confiança, autenticação, autorização e menor privilégio.

**Fazer:** escolher um recurso simples da API (por exemplo, anotações privadas), desenhar o fluxo cliente → API → banco, listar ativos, entradas, atores, decisões de confiança e hipóteses de abuso em `docs/threat-model.md`. Criar apenas os endpoints mínimos para os próximos experimentos.

**Verificar:** explicar quem pode acessar cada recurso, quais dados são controlados pelo cliente e onde a autorização deve ocorrer. Registrar ao menos três riscos priorizados e a hipótese de controle para cada um.

**Evidência/divulgação:** diagrama simples e uma observação concreta sobre o que precisa ser protegido. Possível ângulo: “Antes de tentar quebrar minha API, precisei definir o que ela protege.”

### Dia 2 — SQL injection e fronteira entre dados e consulta

**Estudar:** entrada não confiável, construção de consultas, parâmetros, limite do ORM e diferença entre validação e parametrização.

**Fazer:** em um exercício local isolado, criar uma consulta deliberadamente vulnerável sobre dados fictícios; prever e observar o desvio de comportamento; implementar a versão parametrizada. Documentar em `docs/experiments/sql-injection.md`.

**Verificar:** teste de regressão para entrada que alterava o significado da consulta; teste de comportamento normal; explicação da causa-raiz sem depender apenas de um payload decorado.

**Evidência/divulgação:** comparação da consulta insegura com a segura e resultado do teste. Possível ângulo: “O que fez o banco interpretar entrada como instrução?”

### Dia 3 — senhas e autenticação

**Estudar:** armazenamento de senha, hash adaptativo com sal, verificação, fluxo de login, tentativas repetidas e limites da autenticação.

**Fazer:** comparar um exemplo inseguro isolado com armazenamento e verificação adequados; implementar registro/login mínimos para usuários fictícios. Não inventar algoritmo criptográfico próprio nem registrar senhas em logs.

**Verificar:** a senha em texto não aparece no banco nem nos logs da versão corrigida; senha incorreta é recusada; fluxo válido funciona. Documentar o que os testes não provam.

**Evidência/divulgação:** explicação baseada no comportamento observado. Possível ângulo: “O que mudou quando deixei de tratar senha como dado recuperável?”

### Dia 4 — autorização por objeto, BOLA/IDOR e tokens

**Pré-condição:** autenticação mínima do Dia 3 funcionando.

**Estudar:** diferença entre identidade e permissão, propriedade de recurso, políticas, claims, assinatura e expiração de token. JWT é uma opção de implementação, não o objetivo principal da etapa.

**Fazer:** criar dois usuários locais e recursos privados. Demonstrar, em modo de exercício isolado, o acesso indevido de um usuário autenticado ao recurso do outro. Corrigir a verificação de propriedade no servidor.

**Verificar:** testes para dono, outro usuário e usuário anônimo; registrar qual decisão de autorização foi omitida e onde passou a ocorrer. Documentar em `docs/experiments/bola-idor.md`.

**Evidência/divulgação:** matriz pequena de requisição × usuário × resultado. Possível ângulo: “Meu token era válido, mas isso não autorizava acessar aquele recurso.”

### Dia 5 — abuso de API e limitação de tentativas

**Estudar:** força bruta, enumeração, disponibilidade, limites por identidade/origem, falsos positivos e recuperação após bloqueio.

**Fazer:** medir tentativas repetidas somente no ambiente local, aplicar limitação ao fluxo escolhido e observar a resposta antes e depois.

**Verificar:** testes ou medições para uso normal, excesso de tentativas e retomada; explicar que rate limiting reduz abuso, mas não substitui autenticação e monitoramento. Documentar em `docs/experiments/api-abuse.md`.

**Evidência/divulgação:** números e condições da medição. Possível ângulo: “Rejeitar a senha errada não bastou para controlar tentativas.”

### Dia 6 — camadas de defesa

**Estudar:** validação de entrada, erros, configuração, segredos, logs, dependências, privilégios do banco e observabilidade. Avaliar cabeçalhos de segurança conforme o modo de consumo da API, sem adicioná-los por checklist cego.

**Fazer:** revisar o laboratório completo, selecionar os riscos remanescentes mais relevantes, aplicar melhorias justificadas e criar `docs/security-checklist.md` com estado, evidência e limites de cada controle.

**Verificar:** nenhuma alteração quebra os casos seguros existentes; erros e logs não revelam segredos; configuração do exercício vulnerável continua restrita ao ambiente local.

**Evidência/divulgação:** uma descoberta real da revisão, caso exista. Não publicar por obrigação.

### Dia 7 — revisão e retrospectiva

**Fazer:** sem grandes funcionalidades novas, revisar modelo de ameaças, autenticação, autorização, consultas, dados sensíveis, abuso, testes, configuração Docker, documentação e instruções de execução. Corrigir discrepâncias encontradas e criar `docs/retrospective.md`.

**Responder na retrospectiva:** o que eu esperava; o que os experimentos mudaram; qual falha mais ensinou; qual suposição estava errada; o que os testes demonstram e não demonstram; o que ainda não entendo; como construiria uma API comum de outra forma; qual será a próxima pergunta.

**Saída:** outro desenvolvedor consegue reproduzir localmente o caminho falha → causa → correção → teste. O README aponta para as investigações e explicita riscos restantes.

**Evidência/divulgação:** retrospectiva honesta com links para código, testes e notas. Possível ângulo: “Sete etapas quebrando e corrigindo minha própria API mudaram estas decisões de engenharia.”

## Fluxo de divulgação

Ao fim de cada investigação, registre em `docs/security-journal.md`: tema, insight mais forte, mudança de entendimento, evidência compartilhável, público, possível abertura e decisão **publicar / amadurecer / não publicar**. Prepare um rascunho apenas quando a conclusão puder ser demonstrada. Prefira 3–5 publicações fortes ao longo do ciclo a sete posts repetitivos.

Estrutura sugerida para um post: **pergunta inicial → hipótese → experimento local → resultado inesperado → causa-raiz → mitigação → limite do teste → aprendizado**. Use capturas e trechos sem senhas, tokens ou dados pessoais. O repositório sustenta as afirmações; ele não precisa virar um tutorial de exploração de sistemas alheios.

## Próximos ciclos — direção, não compromisso de calendário

| Fase | Pergunta central | Evidência para avançar |
| --- | --- | --- |
| 2. Segurança web | Como navegador, sessão, cookies, CORS, CSRF, XSS, CSP, SSRF, uploads e caminhos alteram o modelo de confiança? | Laboratórios locais com falha, mitigação e explicação dos limites. |
| 3. Redes e sistemas | Como tráfego, TLS, DNS, processos, permissões e serviços definem a superfície de ataque? | Diagnóstico próprio de um ambiente Linux/rede controlado. |
| 4. Ferramentas de análise | O que interceptação HTTP, análise estática e varredura de dependências revelam ou deixam passar? | Achados reproduzidos e triados manualmente. |
| 5. Fundamentos ofensivos | Como descobrir e encadear falhas em ambientes autorizados? | Investigações controladas com escopo e relatório claros. |
| 6. Defesa e resposta | Como logs, telemetria e alertas ajudam a detectar e investigar uma falha? | Simulação local com trilha de eventos e resposta documentada. |
| 7. Cloud e DevSecOps | Como IAM, segredos, CI/CD, containers e cadeia de suprimentos mudam o risco? | Pipeline e infraestrutura de laboratório com controles verificáveis. |
| 8. Especialização | Em que área há interesse, aptidão e oportunidades sustentados por prática? | Portfólio e retrospectiva comparando experiências reais. |

Esses temas permanecem no mesmo repositório enquanto fizer sentido para a evolução do laboratório. A ordem pode mudar depois da retrospectiva, de lacunas registradas e de oportunidades concretas. A matriz de competência deve acompanhar **evidência**, nunca apenas tempo estudado.

## Próxima ação imediata

Iniciar a **Etapa 0**: conferir ferramentas instaladas, escolher versões, preparar a estrutura mínima da API e dos testes, e validar a execução local. Em seguida, escrever o primeiro modelo de ameaças antes de implementar as vulnerabilidades.
