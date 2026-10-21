# Tasks: Encurtador de URLs (URL Shortener)

**Feature Branch**: `001-url-shortener`  
**Feature Spec**: [spec.md](spec.md)  
**Implementation Plan**: [plan.md](plan.md)  
**Origem das Tarefas**: Decomposição das histórias de usuário e requisitos técnicos segundo o fluxo do Spec Kit.

---

## Phase 1: Setup (Shared Infrastructure)

**Purpose**: Inicialização da infraestrutura do projeto, gerenciamento de dependências e configuração básica.

- [X] T001 Inicializar a estrutura da solução e projetos `UrlShortener.Api` e `UrlShortener.Tests` em `UrlShortener.slnx`
- [X] T002 [P] Configurar dependências e pacotes NuGet (`Microsoft.EntityFrameworkCore.Sqlite`, `Swashbuckle.AspNetCore`) em `src/UrlShortener.Api/UrlShortener.Api.csproj`
- [X] T003 [P] Configurar dependências e pacotes de teste (`xunit`, `Microsoft.AspNetCore.Mvc.Testing`, `coverlet.collector`) em `src/UrlShortener.Tests/UrlShortener.Tests.csproj`
- [X] T004 [P] Configurar string de conexão SQLite e chave de estatísticas em `src/UrlShortener.Api/appsettings.json`

---

## Phase 2: Foundational (Blocking Prerequisites)

**Purpose**: Infraestrutura central de dados e repositório que DEVE ser concluída antes de qualquer história de usuário.

**⚠️ CRITICAL**: Nenhuma história de usuário pode ser iniciada antes da conclusão desta fase.

- [X] T005 Criar a entidade de domínio `Link` com propriedades `Id`, `Code`, `TargetUrl` (máx 2048 chars), `CreatedAtUtc`, `ExpiresAtUtc` e `AccessCount` em `src/UrlShortener.Api/Models/Link.cs`
- [X] T006 Configurar o contexto `AppDbContext` com índice único `IX_Links_Code` e mapeamento SQLite em `src/UrlShortener.Api/Data/AppDbContext.cs`
- [X] T007 Criar e aplicar a migração inicial do banco de dados SQLite em `src/UrlShortener.Api/Data/Migrations/`
- [X] T008 Definir o contrato do repositório de persistência em `src/UrlShortener.Api/Data/ILinkRepository.cs`
- [X] T009 Implementar operações base do repositório com EF Core em `src/UrlShortener.Api/Data/LinkRepository.cs`
- [X] T010 Configurar injeção de dependência do `AppDbContext`, repositório e migração automática no startup em `src/UrlShortener.Api/Program.cs`

**Checkpoint**: Fundação concluída — a implementação das histórias de usuário pode prosseguir de forma independente.

---

## Phase 3: User Story 1 - Encurtamento com Código Automático e Redirecionamento (Priority: P1) 🎯 MVP

**Goal**: Permitir encurtar uma URL de destino pública e obter um link curto com código gerado de 7 caracteres (A-Z, 1-9), redirecionando visitantes via HTTP 302 direto. Submissões repetidas geram links independentes.

**Independent Test**: Submeter uma URL pública (ex: `https://www.google.com`), obter o link curto gerado, acessar o código (inclusive com minúsculas) e verificar redirecionamento 302 para o destino original.

### Tests for User Story 1
- [X] T011 [P] [US1] Criar testes unitários para geração de código de 7 caracteres com alfabeto `[A-Z1-9]` sem dígito 0 em `src/UrlShortener.Tests/UnitTests/CodeGeneratorTests.cs`
- [X] T012 [P] [US1] Criar testes unitários para validação de URL de destino, protocolo http/https, limite de 2048 caracteres e anti-SSRF em `src/UrlShortener.Tests/UnitTests/UrlValidatorTests.cs`
- [X] T013 [P] [US1] Criar testes de integração para o endpoint de criação e redirecionamento de links em `src/UrlShortener.Tests/IntegrationTests/CreateLinkIntegrationTests.cs` e `src/UrlShortener.Tests/IntegrationTests/RedirectLinkIntegrationTests.cs`

### Implementation for User Story 1
- [X] T014 [P] [US1] Implementar o gerador criptográfico de códigos aleatórios de 7 caracteres em `src/UrlShortener.Api/Services/CodeGenerator.cs`
- [X] T015 [P] [US1] Implementar o validador de URL com bloqueio anti-SSRF e teste ativo via HTTP HEAD com timeout de 3s em `src/UrlShortener.Api/Services/UrlValidator.cs`
- [X] T016 [US1] Definir DTOs de requisição e resposta de criação de links em `src/UrlShortener.Api/Models/CreateLinkDto.cs`
- [X] T017 [US1] Implementar a criação de links com código automático e tentativas de colisão (até 5x) em `src/UrlShortener.Api/Services/LinkService.cs`
- [X] T018 [US1] Implementar os endpoints `POST /api/links` e `GET /{code}` com redirecionamento 302 em `src/UrlShortener.Api/Endpoints/LinkEndpoints.cs`

**Checkpoint**: MVP funcional e testável de forma autônoma.

---

## Phase 4: User Story 2 - Definição de Código Personalizado com Unicidade (Priority: P2)

**Goal**: Permitir que o criador defina um código identificador legível customizado de 4 a 20 caracteres (`[A-Z1-9]`), rejeitando códigos já utilizados ou com caracteres inválidos.

**Independent Test**: Submeter criação de link com código `MYLINK1`, verificar que o link é criado com sucesso; tentar cadastrar o mesmo código novamente e validar recebimento de status `409 Conflict`.

### Tests for User Story 2
- [X] T019 [P] [US2] Criar testes unitários para validação de formato de códigos personalizados (4-20 chars `[A-Z1-9]`) e rejeição de minúsculas/espaços em `src/UrlShortener.Tests/UnitTests/LinkServiceTests.cs`
- [X] T020 [P] [US2] Criar testes de integração para criação de link com código personalizado e rejeição 409 em caso de duplicação em `src/UrlShortener.Tests/IntegrationTests/CreateLinkIntegrationTests.cs`

### Implementation for User Story 2
- [X] T021 [US2] Implementar a validação de regras de código personalizado no serviço em `src/UrlShortener.Api/Services/LinkService.cs`
- [X] T022 [US2] Implementar tratamento de violação de unicidade (`DbUpdateException`) retornando status 409 Conflict com ProblemDetails em `src/UrlShortener.Api/Endpoints/LinkEndpoints.cs`

**Checkpoint**: Histórias 1 e 2 operacionais e testadas de forma independente.

---

## Phase 5: User Story 3 - Contagem Atômica e Consulta de Métricas de Acesso (Priority: P3)

**Goal**: Garantir que cada clique válido incremente atomicamente o contador de acessos do link sem perda de contagem sob concorrência, disponibilizando a consulta de métricas de forma pública e irrestrita (FR-014).

**Independent Test**: Executar acessos concorrentes simultâneos ao link curto e verificar que a consulta pública a `GET /api/links/{code}/stats` (sem autenticação) reflete com exatidão 100% dos cliques registrados.

### Tests for User Story 3
- [X] T023 [P] [US3] Criar testes unitários e concorrentes de incremento de contador de acessos sem perda de contagem em `src/UrlShortener.Tests/UnitTests/ConcurrentAccessCountTests.cs`
- [X] T024 [P] [US3] Criar testes de integração para o endpoint de estatísticas `GET /api/links/{code}/stats` validando consulta pública sem autenticação em `src/UrlShortener.Tests/IntegrationTests/StatsLinkIntegrationTests.cs`

### Implementation for User Story 3
- [X] T025 [P] [US3] Criar DTOs de estatísticas `LinkStatsResponse` em `src/UrlShortener.Api/Models/LinkStatsDto.cs`
- [X] T026 [US3] Implementar o incremento atômico no banco de dados via `IncrementAccessCountAsync` em `src/UrlShortener.Api/Data/LinkRepository.cs`
- [X] T027 [US3] Implementar o método `GetStatsAsync` na camada de aplicação em `src/UrlShortener.Api/Services/LinkService.cs`
- [X] T028 [US3] Implementar o endpoint público `GET /api/links/{code}/stats` sem exigência de autenticação em `src/UrlShortener.Api/Endpoints/LinkEndpoints.cs`

**Checkpoint**: Histórias 1, 2 e 3 funcionais com persistência atômica e métricas auditáveis.

---

## Phase 6: User Story 4 - Controle de Vigência e Expiração de Links (Priority: P4)

**Goal**: Permitir que o criador defina um prazo de expiração temporal em UTC. Após o vencimento, o link não redireciona, responde explicitamente `410 Gone` e mantém a contagem de acessos congelada.

**Independent Test**: Criar um link com expiração, avançar o horário além da expiração, efetuar requisição e validar retorno `410 Gone` com ProblemDetails sem acréscimo no contador de acessos.

### Tests for User Story 4
- [X] T029 [P] [US4] Criar testes unitários para regras de expiração temporal em UTC, normalização para 23:59:59 e rejeição de datas passadas em `src/UrlShortener.Tests/UnitTests/LinkServiceTests.cs`
- [X] T030 [P] [US4] Criar testes de integração verificando retorno 410 Gone sem incremento de contador para link expirado em `src/UrlShortener.Tests/IntegrationTests/RedirectLinkIntegrationTests.cs`

### Implementation for User Story 4
- [X] T031 [US4] Implementar lógica de normalização de data e validação de expiração em `src/UrlShortener.Api/Services/LinkService.cs`
- [X] T032 [US4] Implementar resposta 410 Gone com ProblemDetails para links expirados em `src/UrlShortener.Api/Endpoints/LinkEndpoints.cs`

**Checkpoint**: Todas as 4 histórias de usuário concluídas e operacionais.

---

## Phase 7: Polish & Cross-Cutting Concerns

**Purpose**: Otimizações de desempenho e documentação interativa que abrangem todas as histórias de usuário.

- [X] T033 [P] Criar testes unitários para a camada de cache em memória em `src/UrlShortener.Tests/UnitTests/LinkCacheTests.cs`
- [X] T034 Implementar cache-aside em memória com `IMemoryCache` para links frequentes em `src/UrlShortener.Api/Services/LinkService.cs`
- [X] T035 [P] Configurar documentação interativa Swagger / OpenAPI com suporte ao cabeçalho `X-Api-Key` em `src/UrlShortener.Api/Program.cs`
- [X] T036 [P] Criar testes de integração validando a disponibilidade do Swagger UI em `src/UrlShortener.Tests/IntegrationTests/SwaggerIntegrationTests.cs`
- [X] T037 Executar a suíte completa de testes automatizados e validar o guia `specs/001-url-shortener/quickstart.md`

---

## Dependencies & Execution Order

### Phase Dependencies
- **Phase 1 (Setup)**: Sem dependências — execução imediata.
- **Phase 2 (Foundational)**: Depende da Fase 1 — **BLOQUEIA** todas as histórias de usuário.
- **Phase 3 a 6 (User Stories)**: Todas dependem da conclusão da Fase 2 (Foundational). Podem ser desenvolvidas de forma incremental na ordem de prioridade (P1 → P2 → P3 → P4).
- **Phase 7 (Polish)**: Depende da implementação das histórias de usuário.

### Parallel Opportunities
- As tarefas de configuração `T002`, `T003` e `T004` podem rodar em paralelo.
- Na Fase 3 (US1), os testes `T011`, `T012`, `T013` e serviços auxiliares `T014`, `T015` podem rodar em paralelo antes de `T017` e `T018`.
- As histórias de usuário US2, US3 e US4 possuem componentes independentes nos modelos e testes unitários.

---

## Parallel Example: User Story 1

```bash
# Executar em paralelo testes da História 1:
Task: "Criar testes unitários para geração de código em src/UrlShortener.Tests/UnitTests/CodeGeneratorTests.cs"
Task: "Criar testes unitários para validação de URL em src/UrlShortener.Tests/UnitTests/UrlValidatorTests.cs"

# Executar em paralelo serviços auxiliares da História 1:
Task: "Implementar o gerador de códigos em src/UrlShortener.Api/Services/CodeGenerator.cs"
Task: "Implementar o validador de URL em src/UrlShortener.Api/Services/UrlValidator.cs"
```

---

## Implementation Strategy

### MVP First (User Story 1 Only)
1. Concluir Fase 1: Setup
2. Concluir Fase 2: Foundational (Persistência e Entidade Link)
3. Concluir Fase 3: User Story 1 (Encurtamento com código automático e redirecionamento 302)
4. **Validar MVP**: Executar testes e validar redirecionamento ponta a ponta.

### Incremental Delivery
1. Setup + Foundational concluídos.
2. Adicionar US1: Geração automática e redirecionamento 302 (MVP!).
3. Adicionar US2: Código customizado e índice de unicidade (409 Conflict).
4. Adicionar US3: Incremento atômico concorrente e estatísticas com chave.
5. Adicionar US4: Vigência temporal UTC e interceptação 410 Gone.
6. Aplicar Polish: Cache em memória e documentação Swagger UI.
