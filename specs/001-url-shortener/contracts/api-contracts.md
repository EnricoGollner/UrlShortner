# HTTP API Contracts: Encurtador de URLs

**Feature**: [Encurtador de URLs (URL Shortener)](../spec.md)
**Status**: Stable / Implemented

Esta especificação define os contratos de entrada e saída, padrões de erro RFC 7807 (`ProblemDetails`) e códigos de status HTTP dos endpoints expostos pela Minimal API.

---

## 1. Criar Link Encurtado

Cria um novo link encurtado com código gerado aleatoriamente ou personalizado.

- **Método**: `POST`
- **Rota**: `/api/links`
- **Content-Type**: `application/json`

### Requisição (Payload JSON)

```json
{
  "targetUrl": "https://example.com/artigos/eng-software-2026",
  "code": "MYLINK1",
  "expiresAtUtc": "2026-12-31T23:59:59Z"
}
```

| Campo | Tipo | Obrigatório | Descrição / Restrição |
|---|---|:---:|---|
| `targetUrl` | `string` | Sim | URL completa de destino (http/https, $\le 2048$ chars, acessível, não-privada). |
| `code` | `string` | Não | Código personalizado (4 a 20 caracteres `[A-Z1-9]`). Se omitido, gera 7 caracteres aleatórios. |
| `expiresAtUtc` | `string` | Não | Data/hora limite de expiração em ISO 8601 UTC. Se omitido, o link é perpétuo. |

### Respostas

#### `201 Created`
Retornado quando o link é criado com sucesso.

- **Header**: `Location: /{code}`
- **Corpo JSON**:
```json
{
  "shortUrl": "https://meudominio.com/MYLINK1",
  "code": "MYLINK1"
}
```

#### `400 Bad Request` (Erro de Validação - RFC 7807)
Retornado quando a URL de destino é inválida, inacessível, privada, ou o código personalizado não atende às restrições.
```json
{
  "type": "https://tools.ietf.org/html/rfc7807",
  "title": "Erro de validação",
  "status": 400,
  "detail": "A URL de destino informada é inacessível ou inválida."
}
```

#### `409 Conflict` (Código já existente)
Retornado quando o código solicitado já está em uso por outro link.
```json
{
  "type": "https://tools.ietf.org/html/rfc7807",
  "title": "Conflito de código",
  "status": 409,
  "detail": "O código informado já está em uso."
}
```

#### `503 Service Unavailable` (Colisão contínua de código automático)
Retornado quando todas as 5 tentativas de geração aleatória colidem.
```json
{
  "type": "https://tools.ietf.org/html/rfc7807",
  "title": "Serviço indisponível",
  "status": 503,
  "detail": "Não foi possível gerar um código único após múltiplas tentativas. Tente novamente mais tarde."
}
```

---

## 2. Redirecionar Visitante

Resolve o código e redireciona o visitante para a URL de destino correspondente.

- **Método**: `GET`
- **Rota**: `/{code}`
- **Parâmetros de Rota**: `code` (string identificadora, insensível à caixa)

### Respostas

#### `302 Found` (Redirecionamento Temporário)
Retornado quando o link existe e está dentro do prazo de validade. O contador de acessos é incrementado atomicamente.
- **Header**: `Location: https://example.com/artigos/eng-software-2026`
- **Corpo**: Vazio

#### `404 Not Found` (Código Inexistente - RFC 7807)
```json
{
  "type": "https://tools.ietf.org/html/rfc7807",
  "title": "Link não encontrado",
  "status": 404,
  "detail": "O link informado não foi encontrado."
}
```

#### `410 Gone` (Link Expirado - RFC 7807)
Retornado quando a data de expiração do link foi ultrapassada. O contador não é incrementado.
```json
{
  "type": "https://tools.ietf.org/html/rfc7807",
  "title": "Link expirado",
  "status": 410,
  "detail": "Este link expirou e não está mais disponível para redirecionamento."
}
```

---

## 3. Consultar Estatísticas de Acesso

Consulta o total acumulado de acessos e metadados de um link. Endpoint público e irrestrito (FR-014), não exigindo chave de API ou autenticação.

- **Método**: `GET`
- **Rota**: `/api/links/{code}/stats`
- **Parâmetros de Rota**: `code` (string identificadora do link, insensível à caixa)

### Respostas

#### `200 OK`
Retornado com sucesso para qualquer link existente, mesmo que já esteja expirado.
```json
{
  "code": "MYLINK1",
  "targetUrl": "https://example.com/artigos/eng-software-2026",
  "accessCount": 42,
  "createdAtUtc": "2026-10-21T00:00:00Z",
  "expiresAtUtc": "2026-12-31T23:59:59Z",
  "isExpired": false
}
```

#### `404 Not Found` (Código inexistente - RFC 7807)
```json
{
  "type": "https://tools.ietf.org/html/rfc7807",
  "title": "Link não encontrado",
  "status": 404,
  "detail": "O link informado não foi encontrado."
}
```
