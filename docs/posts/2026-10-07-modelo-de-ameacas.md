# Rascunho de post — 2026-10-07

Antes de tentar encontrar falhas na minha API, precisei responder uma pergunta mais básica: **o que ela deve proteger?**

Escolhi um exemplo pequeno para o meu laboratório de segurança: anotações privadas. O cliente poderá enviar o ID de uma anotação, mas esse ID é apenas um pedido. Mesmo que a pessoa esteja autenticada, a API ainda precisará conferir se ela pode ler ou alterar **aquela** anotação.

No primeiro modelo de ameaças, separei três casos: o proprietário, outra pessoa autenticada e alguém sem autenticação. Também anotei o que vem do cliente — ID, conteúdo e identidade apresentada — e onde a decisão de acesso precisa acontecer: no servidor, usando o vínculo entre anotação e proprietário.

Daí saíram três hipóteses para testar: leitura indevida, alteração indevida e criação com proprietário escolhido pelo cliente. Ainda não reproduzi nenhuma falha. A API local mantém as rotas privadas fechadas, e sete testes iniciais verificam que uma identidade informada pelo cliente não as abre. O próximo passo é verificar as hipóteses localmente com dados fictícios à medida que a API evoluir.

Minha primeira lição nesta etapa: **identificar quem faz a requisição e autorizar o que essa pessoa pode fazer são decisões diferentes.**

---

**Nota editorial (não incluir no post):** o texto descreve análise de desenho, não um teste de segurança concluído. Adicionar link do repositório apenas depois de conferir o remoto e o conteúdo publicado.
