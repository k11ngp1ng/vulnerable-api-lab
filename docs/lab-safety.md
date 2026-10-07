# Isolamento dos exercícios

Os exemplos deliberadamente inseguros deste laboratório devem ficar em projetos de teste ou funções de teste executadas apenas durante o experimento local. Eles não serão registrados como rotas da API executável nem incluídos em imagens de publicação. Assim, uma configuração de ambiente ou uma variável esquecida não habilita por acidente um endpoint vulnerável.

Para cada falha, o teste deve usar dados fictícios, comparar o comportamento inseguro com o corrigido e registrar a hipótese, causa, impacto, mitigação e limite da verificação em `docs/experiments/`. A API principal conterá apenas o comportamento corrigido quando a funcionalidade correspondente for adicionada.

O Docker Compose vincula PostgreSQL a `127.0.0.1`, exige uma senha local em `.env` e não expõe dados de produção. O perfil de execução da API também vincula a `127.0.0.1`. Nenhum serviço deste laboratório deve ser publicado enquanto contém demonstrações inseguras.
