# Implementation Plan: Encurtador de URLs (URL Shortener)

**Branch**: `001-url-shortener` | **Date**: 2026-10-21 | **Spec**: [spec.md](spec.md)

**Input**: Feature specification from [spec.md](spec.md) e decisões consolidadas em [research.md](research.md)

## Summary

Implementação da API Minimal de encurtador de links em .NET 10 / C#, utilizando arquitetura em camadas com separação estrita (Minimal APIs para endpoints, LinkService para regras de negócio e EF Core com SQLite para persistência). A solução suporta criação de links com código aleatório seguro de 7 caracteres ou código customizado de 4 a 20 caracteres, validação anti-SSRF com teste ativo de conectividade HTTP, redirecionamento 302 direto com busca case-insensitive, contagem atômica de acessos no SQLite, suporte a expiração temporal em UTC, cache em memória dos links mais acessados e documentação interativa com Swagger/OpenAPI.

## Technical Context

**Language/Version**: C# / .NET 10 (`net10.0`)

**Primary Dependencies**:
- `Microsoft.EntityFrameworkCore.Sqlite` (10.0.13) - Persistência relacional em SQLite com suporte a transações e operações atômicas
- `Microsoft.EntityFrameworkCore.Design` (10.0.13) - Suporte a migrações em tempo de design
- `Microsoft.Extensions.Caching.Memory` - Cache-aside em memória para resolução rápida de redirects
- `Swashbuckle.AspNetCore` (10.3.0) - OpenAPI / Swagger com suporte a autenticação por `X-Api-Key`

**Storage**: SQLite (`UrlShortener.db`) via EF Core com tabela `Links` e índice único em `Code`

**Testing**:
- `xUnit` (2.9.3) com `Microsoft.NET.Test.Sdk`
- `Microsoft.AspNetCore.Mvc.Testing` (10.0.13) para testes de integração end-to-end com WebApplicationFactory
- `coverlet.collector` (6.0.4) para cobertura de testes

**Target Platform**: Servidor multiplataforma .NET 10 (Windows, Linux, macOS, contêineres Docker)

**Project Type**: Web Service (Minimal API RESTful)

**Performance Goals**:
- Resolução e redirecionamento de link curto em menos de 500ms (p95 < 50ms quando em cache)
- Criação de links em menos de 2 segundos (incluindo validação ativa de conectividade)
- Incremento atômico concorrente sem perda de contagem sob alta carga

**Constraints**:
- Tratamento estrito de RFC 7807 (`ProblemDetails`) para todas as respostas de erro HTTP
- Anti-SSRF: Bloqueio estrito de IPs de loopback, faixas privadas (RFC 1918) e IPs reservados
- Código gerado automaticamente de 7 caracteres com alfabeto sem caracteres ambíguos (A-Z, 1-9, sem '0')
- Código do link com unicidade perpétua no banco (índice único)
- Consulta de métricas de acesso pública e irrestrita por código, sem exigência de autenticação (FR-014)

**Scale/Scope**:
- Serviço leve, autossuficiente em arquivo único de banco de dados SQLite
- 3 endpoints principais: criação (`POST /api/links`), redirecionamento (`GET /{code}`) e estatísticas (`GET /api/links/{code}/stats`)

## Constitution Check

*GATE: Must pass before Phase 0 research. Re-check after Phase 1 design.*

| Princípio Constitucional | Status | Avaliação / Justificativa |
|---|:---:|---|
| **I. Separação Estrita de Camadas (Layer Isolation)** | PASSED | Endpoints em `LinkEndpoints.cs` apenas validam contratos de entrada, delegam a `ILinkService` e traduzem resultados para `Results.Problem` ou respostas de sucesso. Não há acesso direto ao `AppDbContext` nos endpoints. |
| **II. Justificativa Explícita de Dependências (Minimal Dependencies)** | PASSED | Apenas pacotes essenciais são utilizados: EF Core SQLite para persistência durável (RF-08), MemoryCache nativo para cache-aside e Swashbuckle para OpenAPI/Swagger. Todas as dependências possuem justificativa técnica documentada. |
| **III. Testabilidade e Cobertura Obrigatória (Test-First & Quality Gates)** | PASSED | Todas as regras de negócio possuem cobertura por testes unitários em `UrlShortener.Tests/UnitTests`, e todos os endpoints possuem testes de integração via `WebApplicationFactory` cobrindo o caminho feliz e cenários de erro. |
| **IV. Padronização de Erros e Contratos HTTP (ProblemDetails)** | PASSED | Respostas de erro retornam ProblemDetails (RFC 7807): 400 (Bad Request) para validação, 404 (Not Found) para link inexistente, 409 (Conflict) para colisão de código, 410 (Gone) para link expirado e 503 para indisponibilidade após colisões. |
| **V. Convenções de Nomenclatura e Localização (Clean Code & Localization)** | PASSED | Código-fonte, classes, métodos e contratos em inglês (`Link`, `LinkService`, `CreateLinkRequest`). Mensagens de erro de validação, títulos de ProblemDetails e respostas de domínio em português. |

## Project Structure

### Documentation (this feature)

```text
specs/001-url-shortener/
├── plan.md              # Este plano técnico de implementação (/speckit-plan)
├── research.md          # Fase 0: Pesquisa técnica, decisões e mitigação de riscos (/speckit-plan)
├── data-model.md        # Fase 1: Entidades, modelo de dados, validações e ciclo de vida (/speckit-plan)
├── quickstart.md        # Fase 1: Guia rápido de execução e validação ponta a ponta (/speckit-plan)
├── contracts/           # Fase 1: Especificação formal dos contratos de API HTTP (/speckit-plan)
│   └── api-contracts.md # Contratos RESTful dos endpoints
└── checklists/
    └── requirements.md  # Checklist de qualidade da especificação
```

### Source Code (repository root)

```text
src/
├── UrlShortener.Api/
│   ├── Data/
│   │   ├── AppDbContext.cs            # Contexto EF Core e mapeamento da entidade Link
│   │   ├── ILinkRepository.cs         # Contrato do repositório de persistência
│   │   ├── LinkRepository.cs          # Implementação do repositório com operações atômicas
│   │   └── Migrations/                # Migrações versionadas do SQLite
│   ├── Endpoints/
│   │   └── LinkEndpoints.cs           # Mapeamento Minimal API dos endpoints HTTP
│   ├── Models/
│   │   ├── CreateLinkDto.cs           # DTOs de requisição e resposta de criação
│   │   ├── Link.cs                    # Entidade de domínio do Link
│   │   └── LinkStatsDto.cs            # DTOs para estatísticas de acesso
│   ├── Services/
│   │   ├── CodeGenerator.cs           # Geração de código seguro (A-Z, 1-9)
│   │   ├── ICodeGenerator.cs          # Interface de geração de código
│   │   ├── ILinkService.cs            # Interface da camada de aplicação
│   │   ├── IUrlValidator.cs           # Interface de validação de URL e conectividade
│   │   ├── LinkService.cs             # Orquestrador de aplicação e regras RN-01 a RN-06
│   │   └── UrlValidator.cs            # Validador de formato, anti-SSRF e HTTP HEAD ativo
│   ├── appsettings.json               # Configurações de conexão e chaves
│   └── Program.cs                     # Inicialização da Minimal API, DI, Swagger e migrações
└── UrlShortener.Tests/
    ├── IntegrationTests/              # Testes end-to-end com WebApplicationFactory
    │   ├── CreateLinkEndpointTests.cs
    │   ├── RedirectEndpointTests.cs
    │   ├── StatsEndpointTests.cs
    │   └── SwaggerEndpointTests.cs
    └── UnitTests/                     # Testes unitários das regras de negócio
        ├── CodeGeneratorTests.cs
        ├── ConcurrentAccessCountTests.cs
        ├── LinkCacheTests.cs
        ├── LinkModelTests.cs
        ├── LinkServiceTests.cs
        ├── ProjectSetupTests.cs
        └── UrlValidatorTests.cs
```

**Structure Decision**: Padrão em duas camadas sob `src/`: `UrlShortener.Api` (aplicação Minimal API com separação interna em Endpoints, Services, Models e Data) e `UrlShortener.Tests` (suíte xUnit contendo testes unitários e testes de integração end-to-end).

## Complexity Tracking

> Nenhuma violação aos princípios constitucionais. Todos os portões foram satisfeitos integralmente sem necessidade de justificativas de complexidade adicional.
