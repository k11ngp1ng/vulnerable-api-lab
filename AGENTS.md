# Vulnerable API Lab — instruções de trabalho

## Identidade e objetivo

Este repositório é um laboratório de aprendizado em segurança de aplicações, com C# / ASP.NET Core e PostgreSQL como stack inicial. O repositório oficial informado pelo autor é `https://github.com/k11ngp1ng/vulnerable-api-lab`. O primeiro ciclo está descrito em [ROADMAP.md](ROADMAP.md); sete dias representam uma sequência de estudo, não uma promessa de domínio do assunto nem um prazo rígido.

O objetivo é construir competência verificável em cibersegurança a partir da experiência prévia em desenvolvimento de software. O ciclo de trabalho é: **estudar → formular uma hipótese → construir → reproduzir localmente → investigar a causa → corrigir → verificar → documentar → explicar**.

## Regras para qualquer pessoa ou agente que trabalhe aqui

1. **Use somente ambientes e contas autorizados.** Experimentos ofensivos ficam no próprio laboratório local. Nunca teste sistemas de terceiros, mesmo que pareçam vulneráveis.
2. **Não publique uma API intencionalmente vulnerável.** Exemplos inseguros devem ser isolados, claramente identificados e impossíveis de habilitar por acidente fora do ambiente local. A documentação pode mostrar a causa e a correção sem expor segredos ou alvos reais.
3. **Não use dados ou credenciais reais.** Empregue usuários, senhas e registros fictícios; mantenha segredos fora do Git. Antes de cada commit, confira arquivos, configuração e histórico da alteração.
4. **Preserve a comparação entre falha e correção.** Para cada experimento, registre a hipótese, a pré-condição, o comportamento observado, a causa-raiz, o impacto, a mitigação e o teste de regressão. Uma demonstração isolada não é evidência suficiente de compreensão.
5. **Escolha o menor exemplo que ensina o conceito.** Evite novas tecnologias, abstrações ou funcionalidades sem relação direta com a pergunta de segurança atual. Quando a etapa ficar grande, divida-a em tarefas verificáveis.
6. **Não reivindique segurança total nem experiência profissional que o projeto não prove.** Testes mostram propriedades específicas nas condições testadas. Registre limites, dúvidas e riscos restantes.
7. **Mantenha o histórico no mesmo repositório.** Código, testes, notas, decisões e retrospectivas do laboratório pertencem aqui. Atualize o roadmap quando a evidência mudar a prioridade.

## Estrutura pretendida

Crie diretórios à medida que forem necessários, sem gerar pastas vazias apenas para seguir este desenho:

```text
/
├── AGENTS.md
├── README.md
├── ROADMAP.md
├── src/                    # API e exemplos controlados
├── tests/                  # regressões de comportamento e segurança
└── docs/
    ├── threat-model.md
    ├── security-journal.md
    ├── competency-matrix.md
    ├── security-checklist.md
    ├── retrospective.md
    └── experiments/        # uma investigação por falha
```

## Como executar cada etapa

1. Leia a etapa correspondente em `ROADMAP.md` e defina uma pergunta concreta. Verifique as pré-condições antes de implementar.
2. Estude o mínimo necessário para prever o comportamento esperado. Anote hipóteses e dúvidas.
3. Implemente o caso mínimo e reproduza-o **somente no laboratório local**. Salve evidências que possam ser compartilhadas sem dados sensíveis.
4. Explique a causa-raiz em termos de fluxo de dados, limites de confiança e decisão de código. Corrija o problema e acrescente um teste que falharia sem a correção.
5. Atualize a documentação e marque a etapa como concluída apenas quando o critério de saída do roadmap estiver demonstrado. Se algo não foi verificado, deixe explícito.
6. Ao final da sessão, entregue um resumo curto do que mudou, como foi verificado, o que permanece incerto e qual é a próxima pergunta.

## Registro de aprendizado

Mantenha `docs/security-journal.md` por sessão relevante, com data e os seguintes campos, ajustando a profundidade à investigação:

```markdown
## AAAA-MM-DD — Tema

- Pergunta e hipótese:
- Conceito estudado e entendimento anterior:
- Experimento e ambiente:
- Resultado observado e evidência:
- Causa-raiz e impacto:
- Mitigação e verificação:
- Limites do teste e riscos restantes:
- O que mudou no meu entendimento:
- Dúvidas e próxima pergunta:
```

Registre dúvidas como backlog de conhecimento, com pergunta, por que importa, entendimento atual, fonte a consultar e prioridade. Em `docs/competency-matrix.md`, atribua níveis apenas com evidência vinculada: **0** desconheço; **1** reconheço; **2** explico; **3** implemento ou demonstro; **4** investigo variações de forma independente; **5** aplico repetidamente em contextos diferentes. Completar um tutorial não eleva o nível por si só.

## Comunicação pública

O projeto também documenta uma transição profissional: **desenvolvedor de software construindo especialização em cibersegurança por meio de laboratórios e engenharia de segurança**. Use linguagem proporcional às evidências; não se apresente como especialista por ter concluído a primeira semana.

Depois de uma sessão, avalie se há material para divulgação: **insight real, comportamento observado, causa-raiz, correção, evidência compartilhável, limite da conclusão e possível público**. Um post só merece ser escrito quando ensina algo específico. A cadência não é diária por obrigação. Prepare rascunhos e revise precisão técnica e exposição de dados antes de publicar; a decisão de publicar é do autor.

## Definição de pronto

Uma etapa só está pronta quando há: implementação ou análise prometida, reprodução controlada quando aplicável, correção, verificação proporcional ao risco, documentação da causa e dos limites, além de um próximo passo claro. Se a etapa for exclusivamente de estudo, substitua a implementação por uma explicação própria e um exemplo verificável.

Comece por **Etapa 0 e Dia 1** em `ROADMAP.md`. Não avance por calendário se as evidências da etapa anterior ainda faltarem.
