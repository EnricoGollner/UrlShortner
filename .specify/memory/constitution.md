<!--
SYNC IMPACT REPORT
==================
- Version change: Unversioned scaffold -> 1.0.0
- List of modified principles:
  - PRINCIPLE_1: Scaffold placeholder -> I. Separação Estrita de Camadas (Layer Isolation)
  - PRINCIPLE_2: Scaffold placeholder -> II. Justificativa Explícita de Dependências (Minimal Dependencies)
  - PRINCIPLE_3: Scaffold placeholder -> III. Testabilidade e Cobertura Obrigatória (Test-First & Quality Gates)
  - PRINCIPLE_4: Scaffold placeholder -> IV. Padronização de Erros e Contratos HTTP (ProblemDetails)
  - PRINCIPLE_5: Scaffold placeholder -> V. Convenções de Nomenclatura e Localização (Clean Code & Localization)
- Added sections:
  - Stack e Restrições Técnicas
  - Fluxo de Desenvolvimento e Padrões de Commit
- Removed sections:
  - Nenhum (placeholders do scaffold preenchidos com as regras do projeto)
- Follow-up TODOs:
  - Nenhum. Todas as definições foram ratificadas e consolidadas na constituição oficial do Spec Kit.
-->

# UrlShortener Constitution

## Core Principles

### I. Separação Estrita de Camadas (Layer Isolation)
Endpoints HTTP NUNCA devem acessar o banco de dados diretamente; todo acesso e persistência DEVE transitar obrigatoriamente através da camada de serviço de aplicação (`Application Services`). Nenhuma regra de negócio pode residir no endpoint ou no handler HTTP. O endpoint atua exclusivamente validando o contrato de entrada, delegando a execução ao serviço e traduzindo o resultado em resposta HTTP.
- **Racional**: Isola as regras de negócio de detalhes de transporte e infraestrutura, facilitando testes unitários e prevenindo vazamento de lógica de domínio para a camada web.

### II. Justificativa Explícita de Dependências (Minimal Dependencies)
Nenhuma dependência externa, pacote NuGet ou biblioteca de terceiros entra no projeto sem justificativa técnica explicitamente documentada no plano técnico da funcionalidade (`plan.md`).
- **Racional**: Evita dependências desnecessárias, minimiza a superfície de ataque, reduz conflitos de versão e mantém a base de código enxuta e fácil de manter.

### III. Testabilidade e Cobertura Obrigatória (Test-First & Quality Gates)
Toda regra de negócio implementada DEVE possuir cobertura correspondente por testes unitários automatizados. Todo endpoint HTTP DEVE possuir ao menos um teste de integração cobrindo o caminho feliz (happy path) de ponta a ponta.
- **Racional**: Garante a confiabilidade funcional contínua, evita regressões silenciosas durante refatorações e assegura que os contratos de API funcionem exatamente conforme especificado.

### IV. Padronização de Erros e Contratos HTTP (ProblemDetails)
Erros de validação e violações de contratos de entrada DEVEM sempre retornar status HTTP 400 (Bad Request) utilizando o padrão RFC 7807 (`ProblemDetails`). Recursos não encontrados DEVEM responder 404 (Not Found) e recursos expirados DEVEM responder 410 (Gone).
- **Racional**: Assegura consistência semântica nas respostas da API, oferecendo mensagens de erro compreensíveis e padronizadas para os consumidores da API.

### V. Convenções de Nomenclatura e Localização (Clean Code & Localization)
Nomes de classes, métodos, interfaces, propriedades e arquivos DEVEM ser escritos em inglês. Mensagens de erro de validação, payloads descritivos ao usuário e respostas de domínio DEVEM ser redigidos em português.
- **Racional**: Harmoniza a base de código com o ecossistema e padrões globais do .NET/C#, preservando a comunicação com os usuários e clientes no idioma nativo do projeto.

## Stack e Restrições Técnicas

A pilha tecnológica oficial do projeto é composta por:
- **Plataforma e Linguagem**: .NET 10 e C# moderno utilizando Minimal APIs.
- **Persistência**: SQLite através do Entity Framework Core (`Microsoft.EntityFrameworkCore.Sqlite`), com migrações automatizadas.
- **Cache**: Cache em memória com política cache-aside (`Microsoft.Extensions.Caching.Memory`) para otimização de leitura dos links mais acessados.
- **Qualidade e Testes**: xUnit com `Microsoft.AspNetCore.Mvc.Testing` para suítes de testes unitários, concorrentes e testes de integração.
- **Documentação da API**: OpenAPI / Swagger com suporte a autenticação por chave de API via cabeçalho (`X-Api-Key`).

## Fluxo de Desenvolvimento e Padrões de Commit

Para manter a rastreabilidade e histórico limpo do projeto:
- **Padrão de Commit**: Todas as mensagens de commit DEVEM seguir estritamente o formato `tipo(escopo): descrição (ID_DO_REQUISITO_OU_TAREFA)` (exemplo: `feat(links): criar link com codigo automatico (T-05)`).
- **Rastreabilidade**: Toda nova funcionalidade ou correção deve estar atrelada a uma especificação formal (`spec.md`), decisão de plano (`plan.md`) ou tarefa catalogada (`tasks.md`).

## Governance

A Constituição do Projeto é o documento de autoridade máxima de engenharia do repositório:
- **Supremacia**: Se qualquer especificação, plano técnico (`plan.md`) ou tarefa entrar em contradição com esta constituição, o plano ou especificação DEVE ser alterado para adequar-se à constituição.
- **Procedimento de Emenda**: Qualquer alteração, adição ou revogação de princípios nesta constituição DEVE ser realizada através de um commit dedicado e exclusivo, explicitando a motivação da emenda.
- **Versionamento Semântico da Constituição**:
  - **MAJOR**: Alterações que quebrem retrocompatibilidade, remoção ou redefinição substancial de princípios.
  - **MINOR**: Inclusão de novos princípios, seções ou expansão significativa de diretrizes.
  - **PATCH**: Correções textuais, clarificações semânticas e ajustes de formatação.
- **Revisão de Conformidade**: Todas as propostas, PRs e revisões de código devem validar ativamente o cumprimento de todos os princípios aqui instituídos.

**Version**: 1.0.0 | **Ratified**: 2026-10-14 | **Last Amended**: 2026-10-21
