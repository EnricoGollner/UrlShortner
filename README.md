# UrlShortener API 🚀

Encurtador de URLs desenvolvido em **.NET**, utilizando **Minimal APIs**, **Entity Framework Core com SQLite**, cache em memória e testes automatizados com **xUnit**.

---

## 📌 Funcionalidades

- **Encurtamento Automático**: Gera códigos únicos de 7 caracteres aleatórios e seguros (alfabeto `A-Z` e `1-9`, excluindo `0` e caracteres ambíguos).
- **Códigos Personalizados**: Suporte a códigos customizados de 4 a 20 caracteres com validação estrita de formato e garantia de unicidade.
- **Validação Anti-SSRF e Conectividade**: Validação estrutural de URLs, rejeição de redes locais/privadas (anti-SSRF) e teste ativo de rede via HTTP HEAD.
- **Redirecionamento Rápido (`302 Found`)**: Busca *case-insensitive* do código curto com cache em memória (*cache-aside*) para redução de latência.
- **Contador Atômico de Acessos**: Incremento direto no banco de dados através de `UPDATE` atômico, garantindo consistência sob acesso concorrente.
- **Expiração Configurável**: Suporte a data/hora em UTC (ISO 8601); links expirados respondem `410 Gone` e não redirecionam.
- **Estatísticas Protegidas por Chave**: Endpoint de métricas protegido por chave de API (`X-Api-Key`), permitindo consulta do histórico mesmo após expiração do link.

---

## 🛠️ Tecnologias Utilizadas

- **.NET (C#)** — Minimal APIs
- **Entity Framework Core** — SQLite como banco de dados persistente
- **Swashbuckle.AspNetCore** — Documentação interativa OpenAPI / Swagger UI com suporte a autenticação por chave
- **Microsoft.Extensions.Caching.Memory** — Cache em memória dos links mais acessados
- **xUnit & Microsoft.AspNetCore.Mvc.Testing** — Testes unitários, concorrentes e de integração

---

## 📋 Pré-requisitos

- [.NET 10 SDK](https://dotnet.microsoft.com/download)
- Git

---

## 🚀 Como Executar o Projeto

### 1. Clonar o repositório
```bash
git clone https://github.com/EnricoGollner/UrlShortner.git
cd UrlShortner
```

### 2. Restaurar dependências
```bash
dotnet restore
```

### 3. Aplicar migrações do banco de dados (SQLite)
Se você tiver a ferramenta `dotnet-ef` instalada:
```bash
dotnet ef database update --project src/UrlShortener.Api
```

> [!NOTE]
> A migração inicial cria automaticamente o arquivo de banco de dados `urlshortener.db` configurado no [appsettings.json](file:///c:/Estudos/UrlShortner/src/UrlShortener.Api/appsettings.json). A aplicação também garante a aplicação automática das migrações na inicialização.

### 4. Executar a API
```bash
dotnet run --project src/UrlShortener.Api
```

A API e a documentação interativa estarão acessíveis em:
- **Swagger UI**: `http://localhost:5228/swagger` (ou `https://localhost:7014/swagger`)
- **Especificação OpenAPI JSON**: `http://localhost:5228/swagger/v1/swagger.json`
- **API Base**: `http://localhost:5228` (ou `https://localhost:7014`)

---

## 📑 Documentação Interativa (Swagger UI)

A interface do Swagger permite testar todos os endpoints diretamente pelo navegador em `http://localhost:5228/swagger`.

Para testar endpoints protegidos como `GET /api/links/{code}/stats`:
1. Clique no botão **Authorize 🔓** no topo do Swagger UI.
2. No campo **Value**, digite a chave de API (padrão: `secret-stats-key`).
3. Clique em **Authorize** e feche a janela modal.
4. Execute as requisições com o header `X-Api-Key` injetado automaticamente!

---

## 🧪 Como Executar os Testes

O projeto conta com suíte completa de testes unitários, testes de concorrência atômica e testes de integração de ponta a ponta:

```bash
dotnet test src/UrlShortener.Tests
```

---

## 📖 Endpoints da API

### 1. Criar Link Encurtado
Cria um novo link com código automático ou personalizado.

- **Método**: `POST`
- **Rota**: `/api/links`
- **Headers**: `Content-Type: application/json`

**Exemplo de Corpo (Código Automático)**:
```json
{
  "targetUrl": "https://github.com/EnricoGollner",
  "expiresAtUtc": "2026-12-31T23:59:59Z"
}
```

**Exemplo de Corpo (Código Próprio)**:
```json
{
  "targetUrl": "https://github.com/EnricoGollner",
  "code": "MEUGITHUB"
}
```

**Resposta de Sucesso (`201 Created`)**:
```json
{
  "shortUrl": "http://localhost:5228/MEUGITHUB",
  "code": "MEUGITHUB"
}
```

---

### 2. Redirecionar para URL de Destino
Acessa o código encurtado e redireciona imediatamente ao destino.

- **Método**: `GET`
- **Rota**: `/{code}`

**Respostas**:
- `302 Found`: Redireciona para `targetUrl` no cabeçalho `Location`.
- `404 Not Found`: Link inexistente.
- `410 Gone`: Link expirado.

---

### 3. Consultar Estatísticas de Acessos
Consulta analítica do total de acessos de um código, acessível mesmo após expiração.

- **Método**: `GET`
- **Rota**: `/api/links/{code}/stats`
- **Headers**: `X-Api-Key: secret-stats-key` *(configurável em appsettings.json)*

**Resposta de Sucesso (`200 OK`)**:
```json
{
  "code": "MEUGITHUB",
  "targetUrl": "https://github.com/EnricoGollner",
  "accessCount": 42,
  "createdAtUtc": "2026-10-16T12:00:00Z",
  "expiresAtUtc": null,
  "isExpired": false
}
```

**Respostas de Erro**:
- `401 Unauthorized`: Chave de API ausente ou incorreta.
- `404 Not Found`: Código não encontrado.

---

## 📂 Estrutura do Projeto

```text
UrlShortener/
├── specs/                           # Especificações, constituição e tarefas
│   ├── constitution.md              # Princípios arquiteturais e regras
│   ├── plan.md                      # Plano técnico e decisões arquiteturais
│   ├── spec.md                      # Requisitos funcionais e regras de negócio
│   └── tasks.md                     # Lista e status das tarefas do projeto
├── src/
│   ├── UrlShortener.Api/            # Aplicação Web (Minimal API)
│   │   ├── Data/                    # Contexto EF Core e Repositórios
│   │   ├── Endpoints/               # Mapeamento das rotas HTTP
│   │   ├── Models/                  # Entidades de domínio e DTOs
│   │   └── Services/                # Regras de negócio, geração e validação
│   └── UrlShortener.Tests/          # Projeto de Testes
│       ├── IntegrationTests/        # Testes de integração de API
│       └── UnitTests/               # Testes unitários e concorrentes
└── README.md
```

---

## 📄 Licença

Este projeto é desenvolvido para fins educacionais e de estudo.
