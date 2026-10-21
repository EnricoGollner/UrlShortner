# Research & Technical Decisions: Encurtador de URLs

**Feature**: [Encurtador de URLs (URL Shortener)](spec.md)
**Status**: Completed

Este documento consolida a pesquisa de engenharia, análise de alternativas e decisões arquiteturais adotadas no desenvolvimento do encurtador de links, fundamentando a conformidade com a constituição do projeto e os requisitos funcionais e não funcionais.

---

## 1. Mecanismo de Persistência e Armazenamento (D-01)

- **Decision**: Adotar o **SQLite** gerenciado pelo **Entity Framework Core** (`Microsoft.EntityFrameworkCore.Sqlite`) com migrações em tempo de inicialização.
- **Rationale**:
  - O requisito RF-08 / FR-015 exige persistência confiável entre reinicializações da aplicação.
  - O SQLite é autocontido, não necessita de provisionamento de servidor externo de banco de dados, possui pegada de memória desprezível e oferece suporte integral a transações ACID e comandos SQL relacionais.
  - Compatibilidade nativa com EF Core no .NET 10 permite migrações automáticas de esquema e suporte a consultas LINQ fortemente tipadas.
- **Alternatives considered**:
  - *Armazenamento em memória (In-Memory Database / ConcurrentDictionary)*: Rejeitado por violar o RF-08 (perda de dados ao reiniciar).
  - *PostgreSQL / SQL Server*: Rejeitados por violar o princípio de dependências mínimas (Princípio II) e complexidade desnecessária para uma aplicação leve autocontida.
  - *Arquivos JSON planos*: Rejeitado devido à ausência de garantias de concorrência atômica e risco de corrupção sob escrita simultânea.

---

## 2. Incremento Atômico de Contadores sob Concorrência (D-02)

- **Decision**: Executar o incremento do contador de acessos através de operação SQL atômica no banco de dados (`ExecuteUpdateAsync` / `UPDATE Links SET AccessCount = AccessCount + 1 WHERE Id = ...`).
- **Rationale**:
  - Atende estritamente à RN-04 / FR-010 e SC-003, assegurando que o contador seja estritamente não-decrescente e nunca perca contagens sob acessos simultâneos massivos.
  - Abordagens tradicionais de ORM (leitura em memória -> modificação de propriedade -> `SaveChanges`) introduzem corridas críticas (lost updates) sob tráfego concorrente.
  - A operação direta no banco de dados desonera a camada de aplicação e delega a serialização atômica ao mecanismo de locking de linha/tabela do SQLite.
- **Alternatives considered**:
  - *Leitura e regravação via EF Core ChangeTracker*: Rejeitado por falhar no teste de concorrência atômica.
  - *Bloqueio de aplicação via `lock` ou `SemaphoreSlim`*: Rejeitado por não escalar além de uma instância única e onerar o processamento assíncrono de threads.

---

## 3. Composição do Alfabeto e Geração de Códigos Aleatórios (D-03)

- **Decision**: Utilizar alfabeto de 35 caracteres composto por letras maiúsculas de `A` a `Z` e dígitos de `1` a `9`, excluindo explicitamente o dígito `0` e gerando códigos automáticos com 7 caracteres via gerador criptograficamente seguro (`RandomNumberGenerator`).
- **Rationale**:
  - O dígito `0` é frequentemente confundido visualmente e foneticamente com a letra `O`. A exclusão do `0` e o uso exclusivo de maiúsculas na criação eliminam ambiguidades quando o link é lido em voz alta ou impresso.
  - Com 35 caracteres e comprimento 7, o espaço amostral é de $35^7 \approx 64,3$ bilhões de combinações possíveis, resultando em probabilidade desprezível de colisões no uso regular.
- **Alternatives considered**:
  - *Base64 convencional (com letras minúsculas, maiúsculas e símbolos '+/' ou '-_')*: Rejeitado por introduzir caracteres ambíguos (ex: `l`, `1`, `I`, `0`, `O`) e dificultar ditado de links.
  - *Identificadores sequenciais inteiros (IDs)*: Rejeitados por permitirem adivinhação trivial (insegurança por enumeração) e gerarem URLs previsíveis.

---

## 4. Garantia de Unicidade e Mitigação de Corridas (D-04)

- **Decision**: Impor restrição de unicidade em duas camadas: checagem prévia na camada de aplicação (`ILinkService`) e índice exclusivo rígido no banco de dados (`UNIQUE INDEX IX_Links_Code`).
- **Rationale**:
  - Checagens apenas em nível de aplicação sofrem de condições de corrida quando duas requisições com o mesmo código personalizado são submetidas no mesmo milissegundo.
  - O índice único garante integridade relacional perpétua. Em caso de colisão concorrente no banco, a violação de constraint (`DbUpdateException`) é capturada e traduzida amigavelmente em resposta HTTP 409 Conflict.
- **Alternatives considered**:
  - *Validação exclusiva em software*: Rejeitada pela vulnerabilidade inerente a corridas de leitura/escrita simultâneas.

---

## 5. Validação de URLs em Duas Etapas e Prevenção Anti-SSRF (D-05)

- **Decision**: Processo de validação rigoroso composto por validação estrutural + anti-SSRF + verificação ativa de rede via `HttpClient`:
  1. *Estrutural*: Protocolo estritamente `http` ou `https`, comprimento $\le 2048$ caracteres, formato de URI bem formado.
  2. *Anti-SSRF (Server-Side Request Forgery)*: Resolução de DNS e rejeição imediata de endereços IP de loopback (`127.0.0.0/8`, `::1`, `localhost`), endereços de rede privada (RFC 1918: `10.0.0.0/8`, `172.16.0.0/12`, `192.168.0.0/16`), link-local e endereços reservados.
  3. *Verificação Ativa*: Envio de requisição HTTP `HEAD` (ou `GET` caso `HEAD` não seja suportado) com timeout agressivo de 3 segundos para confirmar que o destino responde com sucesso.
- **Rationale**:
  - Previne que o encurtador seja utilizado como vetor de ataque interno para escanear portas ou acessar recursos restritos da infraestrutura interna da organização.
  - Garante que apenas links genuinamente operacionais e acessíveis sejam cadastrados no serviço.
- **Alternatives considered**:
  - *Apenas validação de regex de URL*: Rejeitado por não proteger contra SSRF e permitir links quebrados ou maliciosos.

---

## 6. Tratamento de Colisão na Geração Automática (D-06)

- **Decision**: Adotar laço de tentativa de até 5 sorteios consecutivos em caso de colisão com código existente. Se todas as 5 tentativas colidirem, interromper a operação e retornar HTTP 503 (Service Unavailable).
- **Rationale**:
  - Atende à RN-06 / FR-006. Dada a imensidão do espaço amostral ($35^7$), 5 tentativas são mais do que suficientes para contornar qualquer colisão acidental.
  - Evita loops infinitos ou degradação de CPU caso o banco atinja alta densidade.
- **Alternatives considered**:
  - *Retentar indefinidamente*: Rejeitado por risco de travamento de threads em cenários de exaustão de chaves.

---

## 7. Documentação Interativa e Autenticação por Chave de API (D-07)

- **Decision**: Integração do Swashbuckle (`Swashbuckle.AspNetCore`) com suporte à definição de segurança OpenAPI para cabeçalho `X-Api-Key` no endpoint de estatísticas.
- **Rationale**:
  - Facilita a exploração e testes manuais da API via interface Swagger UI em ambiente de desenvolvimento.
  - Padroniza a segurança do endpoint administrativo através de cabeçalho padrão de mercado.
- **Alternatives considered**:
  - *OAuth2 / JWT Bearer*: Rejeitado nesta fase por complexidade excessiva desnecessária (fora de escopo segundo a especificação).

---

## 8. Estratégia de Cache em Memória (D-08)

- **Decision**: Utilizar `IMemoryCache` com política Cache-Aside para redirecionamentos frequentes (`GET /{code}`).
- **Rationale**:
  - Reduz a latência de leitura para menos de 10ms nos links com alto volume de acessos.
  - O cache armazena o mapeamento `code -> (targetUrl, expiresAtUtc)`. No acesso, valida a expiração em memória e executa o incremento atômico assincronamente no banco de dados.
- **Alternatives considered**:
  - *Redis distribuído*: Rejeitado por violar a premissa de dependências mínimas e autocontenção local.
