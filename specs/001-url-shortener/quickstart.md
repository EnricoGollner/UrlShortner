# Quickstart & Validation Guide: Encurtador de URLs

**Feature**: [Encurtador de URLs (URL Shortener)](spec.md)
**Status**: Ready

Este guia apresenta o procedimento para executar o serviço e validar cenários ponta a ponta na API HTTP utilizando `curl` ou terminal PowerShell.

---

## 1. Pré-requisitos e Execução

- **SDK**: .NET 10 instalado (`dotnet --version` $\ge$ 10.0)
- **Porta padrão**: `http://localhost:5000` (ou porta configurada em `launchSettings.json`)

### Compilação e Execução dos Testes Automatizados

```bash
# Na raiz do repositório
dotnet build
dotnet test
```

### Inicialização do Servidor de Desenvolvimento

```bash
cd src/UrlShortener.Api
dotnet run
```

A interface Swagger UI estará disponível em: `http://localhost:5000/swagger` (ou porta HTTP atribuída).

---

## 2. Cenários de Validação Ponta a Ponta

### Cenário 1: Criar Link com Código Automático
**Ação**:
```bash
curl -X POST http://localhost:5000/api/links \
  -H "Content-Type: application/json" \
  -d '{"targetUrl": "https://www.google.com"}'
```
**Resultado Esperado**:
- Status: `201 Created`
- Header `Location`: `/{code}`
- Body contendo `{"shortUrl":"http://localhost:5000/{code}","code":"{code}"}`, onde `{code}` possui 7 caracteres maiúsculos/números.

---

### Cenário 2: Criar Link com Código Personalizado
**Ação**:
```bash
curl -X POST http://localhost:5000/api/links \
  -H "Content-Type: application/json" \
  -d '{"targetUrl": "https://github.com", "code": "GITHUB1"}'
```
**Resultado Esperado**:
- Status: `201 Created`
- Body: `{"shortUrl":"http://localhost:5000/GITHUB1","code":"GITHUB1"}`

---

### Cenário 3: Redirecionar Visitante (Busca Case-Insensitive)
**Ação**:
```bash
# Acessando com minúsculas
curl -i http://localhost:5000/github1
```
**Resultado Esperado**:
- Status: `302 Found`
- Header `Location`: `https://github.com`
- O contador de acessos do link é incrementado atomicamente no banco.

---

### Cenário 4: Consultar Estatísticas de Acesso (Consulta Pública)
**Ação**:
```bash
curl -X GET http://localhost:5000/api/links/GITHUB1/stats
```
**Resultado Esperado**:
- Status: `200 OK`
- Body contendo `accessCount >= 1`, datas de criação em UTC e `isExpired: false`.

---

### Cenário 5: Prevenção Anti-SSRF (Rejeição de Endereço Local)
**Ação**:
```bash
curl -X POST http://localhost:5000/api/links \
  -H "Content-Type: application/json" \
  -d '{"targetUrl": "http://localhost:8080/admin"}'
```
**Resultado Esperado**:
- Status: `400 Bad Request` (RFC 7807 `ProblemDetails`)
- Detail indicando que endereços locais ou privados são proibidos.

---

### Cenário 6: Acesso a Link Expirado
**Ação**:
1. Criar link com expiração de curto prazo.
2. Acessar o link após o vencimento:
```bash
curl -i http://localhost:5000/{codigoExpirado}
```
**Resultado Esperado**:
- Status: `410 Gone` (RFC 7807 `ProblemDetails`)
- Não redireciona e não incrementa o contador de acessos.
