# Matriz de competências

Níveis: **0** desconheço; **1** reconheço; **2** explico; **3** implemento ou demonstro; **4** investigo variações de forma independente; **5** aplico repetidamente em contextos diferentes. Os níveis abaixo descrevem apenas a evidência deste laboratório, não experiência profissional.

| Competência | Nível sustentado | Evidência | Limite / próximo passo |
| --- | --- | --- | --- |
| Modelar ativos, entradas e limites de confiança de uma API | 2 | [Modelo de ameaças](threat-model.md) e [testes da superfície](../tests/VulnerableApiLab.Api.Tests/SurfaceTests.cs) | Matriz de autorização por proprietário ainda não foi implementada. |
| Explicar e demonstrar SQL injection em uma consulta de leitura | 3 | [Investigação do Dia 2](experiments/sql-injection.md) e [teste de integração](../tests/VulnerableApiLab.Api.Tests/SqlInjectionTests.cs) | Uma entrada e um predicado sobre dados fictícios; outras formas não foram investigadas. |
| Implementar uma consulta parametrizada no PostgreSQL | 3 | [NoteLookup.cs](../src/VulnerableApiLab.Api/NoteLookup.cs) e [testes de entrada adversária e normal](../tests/VulnerableApiLab.Api.Tests/SqlInjectionTests.cs) | Ainda sem uso em rota autenticada e sem autorização por proprietário. |
| Armazenar e verificar senhas de forma apropriada | 0 | Nenhuma implementação ou experimento ainda. | Pergunta do Dia 3. |
