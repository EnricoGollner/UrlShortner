# Plano técnico

## Contexto
Implementa a especificação do encurtador de links, respeitando a constituição do projeto.

## Arquitetura
- API HTTP em Minimal API, um arquivo de endpoints por recurso
- Camada de aplicação com LinkService, dona das regras RN-01 a RN-06
- Camada de dados com EF Core e um repositório por agregado
- Endpoint só valida entrada, chama o serviço e traduz o resultado em resposta

## Decisões
- D-01: SQLite como armazenamento, porque o RF-08 exige persistência
  entre reinícios
- D-02: contador incrementado por UPDATE atômico no banco, e não lido e
  reescrito na aplicação, para atender a RN-04 sob acesso concorrente
- D-03: código do link gerado com alfabeto sem caracteres ambíguos, para
  reduzir erro de digitação em link lido em voz alta
- D-04: unicidade do código garantida por índice único no banco, além da
  checagem no serviço, porque a checagem sozinha tem corrida
- D-05: verificação de URL de destino em duas etapas para atender a RN-02:
  rejeição de endereços locais/privados (anti-SSRF) e teste ativo de conectividade
  (HTTP HEAD com timeout curto)
- D-06: geração de código automático com até 5 tentativas consecutivas de sorteio
  em caso de colisão, para atender a RN-06

## Modelo de dados
Link
- Id, Code (único), TargetUrl, CreatedAtUtc, ExpiresAtUtc (nulo), AccessCount

## Contratos
- POST /api/links entrada: targetUrl, code opcional, expiresAtUtc opcional
- POST /api/links saída: shortUrl, code
- GET /{code} redireciona com 302 (busca case-insensitive), ou 404 se não existe, ou 410 se expirou
- GET /api/links/{code}/stats consulta pública: retorna 200 com total de acessos (mesmo se expirado), ou 404 se código não existe

## Riscos
- Concorrência na criação de código próprio, mitigada por D-04
- Crescimento da tabela de links sem política de limpeza, fora de escopo por ora