# Plano técnico

## Contexto
Implementa a especificação do encurtador de links, respeitando a constituição
do projeto.

## Arquitetura
- API HTTP em Minimal API, um arquivo de endpoints por recurso
- Camada de aplicação com LinkService, dona das regras RN-01 a RN-06
- Camada de dados com EF Core e um repositório por agregado
- O endpoint faz apenas a leitura da requisição (campos presentes e tipos corretos),
  chama o serviço e traduz o resultado em resposta. Nenhuma RN é verificada no endpoint
- O relógio é injetado por meio da abstração de tempo nativa do .NET 8.

## Decisões
- D-01: SQLite como armazenamento, porque o RF-08 exige persistência
  entre reinícios
- D-02: contador incrementado por UPDATE atômico no banco, e não lido e
  reescrito na aplicação, para atender a RN-04 sob acesso concorrente
- D-03: código do link gerado com alfabeto sem caracteres ambíguos, para
  reduzir erro de digitação em link lido em voz alta
- D-04: unicidade do código garantida por índice único no banco, além da
  checagem no serviço, porque a checagem sozinha tem corrida
- D-05: dependência Swashbuckle.AspNetCore gera a documentação OpenAPI e
  a interface Swagger UI, que é o template padrão do .NET 8 para Minimal APIs


## Modelo de dados
Link
- Id, Code (único), TargetUrl, CreatedAtUtc, ExpiresAtUtc (nulo), AccessCount

## Contratos
- POST /api/links entrada: targetUrl, code opcional, expiresAtUtc opcional
- POST /api/links saída: shortUrl, code, managementKey
- GET /{code} redireciona com 302, ou 404 se não existe, ou 410 se expirou
- GET /api/links/{code}/stats responde 200 com o total de acessos, ou 404
- GET /api/docs: interface da documentação; documento OpenAPI em /api/docs/v1/swagger.json

## Riscos
- Concorrência na criação de código próprio, mitigada por D-04
- Crescimento da tabela de links sem política de limpeza, fora de escopo por ora