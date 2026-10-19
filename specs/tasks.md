# Tarefas

| ID | Tarefa | Origem | Depende de | Concluída quando |
|---|---|---|---|---|
| T-01 | Criar projeto e estrutura de pastas | Constituição | | Projeto compila |
| T-02 | Modelar a entidade Link e a migração | RF-01, RF-08, D-01 | T-01 | Migração aplica no banco com os campos do modelo de dados do plano |
| T-03 | Gerar código aleatório de 7 caracteres | RF-02, RN-05, D-03 | T-01 | Teste unitário garante 7 caracteres, somente o alfabeto definido e nunca o dígito 0 |
| T-04 | Validar URL de destino | RN-02, CB-04, CB-05, CB-06 | T-01 | Testes unitários: http e https aceitos; outro esquema, mais de 2048 caracteres, host que resolve para IP privado, loopback, link-local ou reservado, e host que não resolve no DNS são recusados |
| T-05 | Criar link com código automático | RF-01 | T-02, T-03, T-04 | Teste de integração: caminho feliz retorna 201; URL inválida retorna 400 com ProblemDetails |
| T-06 | Aceitar código próprio | RF-03, RN-01, RN-05, CB-01, D-04 | T-05 | Testes unitários: 4 e 20 caracteres aceitos; 3 e 21 caracteres, minúscula, dígito 0, espaço e caractere especial recusados; regra de unicidade coberta. Teste de integração: código duplicado retorna 409 com ProblemDetails |
| T-07 | Redirecionar por código | RF-04, CB-03 | T-05 | Teste de integração: código existente retorna 302 com header Location; código inexistente retorna 404 |
| T-08 | Incrementar contador de forma atômica | RF-05, RN-04, D-02 | T-07 | Teste unitário da regra; teste concorrente não perde contagem |
| T-09 | Endpoint de estatísticas | RF-07 | T-08 | Teste de integração: retorna 200 com o total de acessos sem exigir header; código inexistente retorna 404; link expirado também retorna o total |
| T-10 | Cache em memória da resolução de código | RNF-01 | T-07, T-08, T-13 | Teste unitário: a segunda resolução do mesmo código não consulta o repositório. Teste de integração: N acessos seguidos resultam em contador N; link em cache deixa de redirecionar a partir de 00:00 UTC do dia seguinte à data de expiração (relógio controlado) |
| T-11 | Gerar outro código em caso de colisão | RN-06, CB-02, D-04 | T-05 | Teste unitário com gerador forçado a colidir: gera outro código; após 5 colisões, a criação falha com erro e nenhum link é criado |
| T-12 | Aceitar e validar data de expiração na criação | RF-06, CB-07 | T-02, T-05 | Testes unitários com relógio controlado: data anterior à data atual em UTC recusada com 400 e ProblemDetails; data atual aceita |
| T-13 | Bloquear link expirado | RN-03 | T-07, T-08, T-12 | Teste unitário da regra. Teste de integração com relógio controlado: link válido às 23:59:59 UTC da data de expiração; às 00:00 UTC do dia seguinte retorna 410 e o contador não muda |
| T-14 | Garantir persistência após reinício | RF-08 | T-07 | Teste de integração: após reiniciar a aplicação sobre o mesmo arquivo SQLite, o link continua redirecionando |
| T-15 | Documentação OpenAPI | RNF-02, D-05 | T-05, T-07, T-09 | Teste de integração: /api/docs responde 200 fora do ambiente de desenvolvimento; o documento em /api/docs/v1/swagger.json lista os três endpoints com todas as respostas de sucesso e de erro; um link com código próprio SWAGGER redireciona normalmente |
