<img width="100%" alt="Halloween 2026" src="https://baltaio.blob.core.windows.net/static/images/v4/challenges/halloween-2026/banner.jpg" />

## 🎃 Halloween - Desafio 2

Oi, eu sou o Igor e este é o espaço onde compartilho minha jornada de aprendizado durante o desafio **Halloween 2026**, realizado pelo [balta.io](https://balta.io). 👻

Aqui você vai encontrar projetos, exercícios e códigos que estou desenvolvendo durante o desafio.

### Sobre este desafio
Neste desafio o objetivo é criar um encurtador de URLs completo utilizando os conceitos de Spec-Driven Development

#### O que foi implementado
Foi implementado um encurtador de links em .NET 8 com Minimal API, seguindo Spec-Driven Development: especificação, constituição, plano e tarefas.

A API faz quatro coisas:
- **Cria links curtos.** O código pode ser gerado automaticamente, com 7 caracteres, ou escolhido pelo criador.
- **Redireciona o visitante** para a URL de destino.
- **Conta os acessos** de cada link.
- **Permite definir uma data de expiração.** Depois dela, o link responde 410 e para de contar acessos.

Os dados ficam em SQLite via EF Core, então nada se perde quando a aplicação reinicia. A URL de destino só é aceita se usar http ou https e se o host resolver para um IP público. Os códigos são únicos no sistema, garantidos também por um índice no banco. O contador usa um UPDATE atômico e não perde acessos simultâneos. Os links acessados recentemente ficam em cache, sem prejudicar a contagem nem a expiração. A documentação OpenAPI fica disponível em `/api/docs` em todos os ambientes.

O projeto tem 140 testes xUnit, entre unitários e de integração. Eles cobrem todas as regras de negócio, os casos de borda e os endpoints, com relógio controlado para testar a expiração.

Neste processo eu aprendi:
* ✅ Aprendi mais a fundo como construir os arquivos de constituição, especificação, plano técnico e tarefas, para desenvolver projetos utilizando SDD (Spec-Driven Development).

## Bagde
<img src="https://baltaio.blob.core.windows.net/static/images/v4/challenges/halloween-2026/02.png" width="200" />

## Sobre o Halloween 2026
O desafio **Halloween 2026** consiste em implementar implementar o modelo Spec Driven Development de ponta a ponta, criando apps completas com IA.

### Veja meu progresso no desafio
https://github.com/igorvazf
