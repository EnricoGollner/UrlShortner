# Feature Specification: Encurtador de URLs (URL Shortener)

**Feature Branch**: `001-url-shortener`

**Created**: 2026-10-21

**Status**: Draft

**Input**: User description: "Encurtador de URLs (URL Shortener)"

## Clarifications

### Session 2026-10-21
- Q: Qual deve ser a política de controle de acesso para a consulta de métricas e estatísticas dos links encurtados? → A: A consulta de métricas e estatísticas é pública e irrestrita, permitindo que qualquer pessoa que conheça o código do link consulte o total de acessos e a data de criação sem exigência de chave de API ou autenticação.

## User Scenarios & Testing *(mandatory)*

### User Story 1 - Encurtamento de URL com código automático e redirecionamento (Priority: P1)

Como criador, quero informar uma URL longa e receber um link curto com código gerado automaticamente de 7 caracteres, e como visitante, quero clicar no link encurtado e ser redirecionado imediatamente para o destino original sem telas intermediárias. Submissões repetidas da mesma URL geram links distintos e independentes.

**Why this priority**: Constitui a funcionalidade central e o MVP (Minimum Viable Product) do produto. Sem o encurtamento básico e o redirecionamento para o destino, o serviço não entrega seu valor primordial.

**Independent Test**: Pode ser testado de ponta a ponta fornecendo uma URL de destino pública válida, obtendo o link curto gerado com 7 caracteres, acessando o link curto e confirmando o redirecionamento direto para o destino original.

**Acceptance Scenarios**:

1. **Given** uma URL de destino válida e pública (HTTP ou HTTPS), **When** o criador solicita o encurtamento sem especificar código, **Then** o sistema gera um código aleatório exclusivo de 7 caracteres (utilizando apenas A-Z maiúsculo e dígitos de 1 a 9), cria o link e retorna a URL encurtada.
2. **Given** um link curto ativo existente no sistema, **When** um visitante acessa o link utilizando o código (seja com letras maiúsculas ou minúsculas), **Then** o visitante é redirecionado diretamente à URL de destino correspondente sem passar por telas intermediárias.
3. **Given** uma URL de destino que já foi encurtada anteriormente, **When** o criador submete novamente essa mesma URL para encurtamento, **Then** o sistema gera um novo link curto com um código diferente e métricas independentes.

---

### User Story 2 - Definição de código personalizado para o link (Priority: P2)

Como criador, quero escolher um código customizado legível (de 4 a 20 caracteres com letras e números) para o meu link, facilitando a memorização, a leitura em voz alta e o compartilhamento em materiais de divulgação.

**Why this priority**: Agrega alto valor de personalização e confiabilidade de marca para campanhas de marketing e comunicação direta, permitindo links intuitivos e fáceis de digitar.

**Independent Test**: Pode ser testado fornecendo uma URL de destino acompanhada de um código personalizado válido e disponível; em seguida, verifica-se que o link é criado com o código solicitado, rejeitando códigos duplicados ou fora do padrão.

**Acceptance Scenarios**:

1. **Given** um código personalizado entre 4 e 20 caracteres (utilizando apenas A-Z maiúsculo e dígitos de 1 a 9, sem espaços ou símbolos) que não esteja em uso, **When** o criador solicita o encurtamento informando esse código, **Then** o link é criado com sucesso com o código especificado.
2. **Given** um código que já foi utilizado previamente por qualquer link no sistema, **When** o criador tenta criar um link com esse mesmo código, **Then** a criação é recusada informando que o código já está em uso.
3. **Given** um código com formato inválido (contendo espaços, minúsculas, o dígito 0, caracteres especiais ou tamanho menor que 4 ou maior que 20 caracteres), **When** o criador submete a criação, **Then** o sistema recusa a solicitação com erro explicativo de validação.

---

### User Story 3 - Contagem cumulativa e acompanhamento de acessos (Priority: P3)

Como criador ou interessado, quero consultar a quantidade total de acessos que um link curto recebeu, sabendo que cada clique válido é contabilizado de forma atômica e cumulativa, inclusive sob acessos simultâneos ou repetidos.

**Why this priority**: Permite mensurar a eficácia de links compartilhados e o alcance das campanhas, atendendo à necessidade essencial de medir cliques.

**Independent Test**: Pode ser testado acessando o link curto repetidas vezes (inclusive com acessos simultâneos) e em seguida consultando as métricas do link pelo seu código, comprovando que o total de acessos reflete fielmente todas as visitas sem perdas.

**Acceptance Scenarios**:

1. **Given** um link curto ativo, **When** visitantes ou processos automatizados acessam o link, **Then** cada acesso bem-sucedido incrementa o contador de acessos do link em exatamente 1 unidade de forma cumulativa, garantindo que o contador nunca decresça.
2. **Given** um código de link cadastrado no sistema (ativo ou expirado), **When** qualquer pessoa que conheça o código consulta suas métricas de acesso de forma pública (sem chave de API ou autenticação), **Then** o sistema retorna o total de acessos acumulados e a data de criação do link.

---

### User Story 4 - Controle de vigência e expiração de links (Priority: P4)

Como criador, quero definir opcionalmente um prazo de expiração para o meu link curto, para que após essa data ou horário o link deixe de redirecionar e informe explicitamente ao visitante que a vigência encerrou.

**Why this priority**: Essencial para ofertas por tempo limitado, inscrições com prazo determinado e privacidade de conteúdos sazonais, prevenindo acessos indesejados após o encerramento da ação.

**Independent Test**: Pode ser testado criando um link com data/hora de expiração específica; antes do prazo, o link redireciona normalmente; após o prazo, o acesso resulta em aviso de link expirado sem redirecionar e sem incrementar o contador de cliques.

**Acceptance Scenarios**:

1. **Given** uma data e horário de expiração futura em UTC (formato ISO 8601), **When** o criador define esse valor na criação do link, **Then** o link é registrado com esse limite de vigência. Se for fornecida apenas a data, o link permanece válido até as 23:59:59 UTC daquele dia.
2. **Given** um link curto cuja data de expiração já expirou, **When** um visitante tenta acessá-lo, **Then** o sistema informa explicitamente que o link expirou, impede o redirecionamento e não contabiliza novo acesso no contador.
3. **Given** uma solicitação de criação de link com data de expiração no passado, **When** a solicitação é processada, **Then** o sistema recusa a criação informando que a data de expiração deve ser futura.

---

### Edge Cases

- **Código já em uso**: A criação de links com código manual já existente deve ser prontamente recusada com mensagem clara de conflito, garantindo que códigos nunca sejam reciclados ou sobrepostos, mesmo se o link original estiver expirado.
- **Colisão consecutiva na geração automática**: Ao gerar códigos automáticos, se o código sorteado já existir, o sistema deve realizar até 5 tentativas consecutivas de sorteio. Se todas as 5 tentativas colidirem, a criação é cancelada com aviso amigável de indisponibilidade temporária.
- **Código próprio com formato inválido**: Qualquer código personalizado que contenha o dígito 0, letras minúsculas na criação, caracteres de pontuação/especiais, espaços ou tamanho inferior a 4 ou superior a 20 caracteres deve ser rejeitado no ato da validação.
- **Código inexistente**: O acesso a um código que nunca foi registrado no sistema deve retornar informação clara de que o link não foi encontrado.
- **Código expirado no momento do acesso**: O visitante que tentar acessar um link com validade vencida não é redirecionado e recebe notificação explícita de expiração. O contador de cliques permanece congelado. No entanto, a consulta de estatísticas continua acessível para qualquer pessoa com o código.
- **URL de destino com mais de 2048 caracteres**: Solicitações contendo URLs que excedam 2048 caracteres devem ser recusadas com erro de validação de comprimento.
- **URL de destino local, privada ou inacessível**: URLs que apontem para endereços de loopback, faixas de IP locais/privadas (ex: localhost, 127.0.0.1, 10.x.x.x, 192.168.x.x) ou que falhem na verificação ativa de rede (com timeout curto) devem ser rejeitadas no momento da criação para proteger o ambiente e garantir que o destino existe.
- **Tratamento de caixa (case-insensitivity) no acesso**: Embora a criação exija apenas maiúsculas (A-Z) para uniformidade dos códigos, o acesso por visitantes deve tratar o código de maneira insensível à caixa (maiúsculas e minúsculas resolvem para o mesmo link).

## Requirements *(mandatory)*

### Functional Requirements

- **FR-001**: O sistema DEVE permitir que um usuário informe uma URL de destino pública e receba um link encurtado correspondente.
- **FR-002**: O sistema DEVE tratar submissões repetidas de uma mesma URL de destino como criações totalmente independentes, gerando links distintos com identificadores e contadores isolados.
- **FR-003**: O sistema DEVE gerar automaticamente um código identificador exclusivo de 7 caracteres para links criados sem código personalizado, utilizando apenas letras maiúsculas (A-Z) e dígitos de 1 a 9 (excluindo o dígito 0).
- **FR-004**: O sistema DEVE permitir a definição opcional de um código identificador personalizado pelo criador, com comprimento entre 4 e 20 caracteres, restrito a letras maiúsculas (A-Z) e dígitos de 1 a 9, sem espaços ou símbolos.
- **FR-005**: O sistema DEVE assegurar a unicidade perpétua de cada código de link no sistema, proibindo estritamente a reutilização de códigos pré-existentes, mesmo após a expiração do link.
- **FR-006**: Na geração automática de código, o sistema DEVE efetuar até 5 tentativas consecutivas de sorteio em caso de colisão de código antes de interromper a criação com aviso de indisponibilidade temporária.
- **FR-007**: O sistema DEVE validar a URL de destino, exigindo protocolo HTTP ou HTTPS, limite máximo de 2048 caracteres, rejeitando faixas de rede local/privada e exigindo resposta positiva em verificação ativa de conectividade de rede com timeout curto.
- **FR-008**: O sistema DEVE redirecionar o visitante imediatamente para a URL de destino ao receber uma requisição para um link curto existente e válido, sem exibir página intermediária.
- **FR-009**: O sistema DEVE resolver os códigos de links de forma insensível à caixa (case-insensitive) no momento do acesso pelo visitante.
- **FR-010**: O sistema DEVE incrementar atomicamente em exatamente 1 unidade o contador de acessos a cada visita legítima a um link curto válido, incluindo acessos repetidos e automatizados, assegurando que a contagem nunca decresça nem se perca em acessos concorrentes.
- **FR-011**: O sistema DEVE permitir ao criador definir opcionalmente um prazo de expiração em data e horário UTC (formato ISO 8601). Caso seja informada apenas a data, o link deve permanecer ativo até as 23:59:59 UTC do dia informado.
- **FR-012**: O sistema DEVE recusar a criação de links cuja data de expiração informada seja anterior ao momento atual da criação.
- **FR-013**: O sistema DEVE impedir o redirecionamento e suspender o incremento do contador de acessos para links que tenham atingido a data/hora de expiração, notificando explicitamente o visitante sobre a expiração do link.
- **FR-014**: O sistema DEVE disponibilizar para qualquer pessoa que informe o código do link a consulta pública e irrestrita de métricas de acesso contendo o total de acessos acumulados e a data de criação, sem exigência de autenticação ou chave de API, independentemente de o link estar ativo ou expirado.
- **FR-015**: O sistema DEVE persistir de forma durável todas as entidades de links, metadados e contadores de acessos, assegurando a integridade e disponibilidade das informações após reinicializações do sistema.

### Key Entities

- **Link**: Representa o link encurtado gerenciado pelo sistema. Contém o código identificador único (público, alfanumérico e perpétuo), a URL de destino original validada, o registro de data e horário de criação em UTC, o registro opcional de data e horário de expiração em UTC e o contador acumulado de acessos.
- **Métricas do Link**: Representa as estatísticas e informações de status consultáveis publicamente associadas ao código do link, incluindo total acumulado de acessos, data/hora de criação, status de validade (ativo ou expirado) e data/hora de expiração (quando aplicável).

## Success Criteria *(mandatory)*

### Measurable Outcomes

- **SC-001**: Usuários conseguem encurtar uma URL e obter o link curto pronto para compartilhamento em menos de 2 segundos.
- **SC-002**: Visitantes são redirecionados à URL de destino em menos de 500 milissegundos a partir da requisição ao link curto em condições normais de rede.
- **SC-003**: 100% dos cliques recebidos por links ativos são registrados e acumulados de forma exata, sem perdas de contagem sob acessos concorrentes.
- **SC-004**: 100% das tentativas de submissão de códigos duplicados ou de URLs de destino inválidas, locais ou inacessíveis são bloqueadas na entrada com mensagem clara.
- **SC-005**: 100% das tentativas de acesso a links com prazo de expiração ultrapassado são interceptadas com aviso explícito de expiração sem redirecionamento do visitante.
- **SC-006**: 100% dos links e contadores de acessos permanecem operacionais e inalterados após reinicializações completas do ambiente de execução.

## Assumptions

- Os usuários possuem conectividade de rede estável para submeter URLs e acessar links redirecionados.
- As URLs de destino apontam para serviços públicos disponíveis na Web e respondem a requisições de verificação de rede.
- O relógio do sistema opera sincronizado com o padrão UTC para cálculo preciso e determinístico dos prazos de expiração.
- A versão inicial opera com acesso público direto para criação de links e consulta de estatísticas, sem exigência de autenticação ou criação prévia de contas de usuário.
- A edição do destino de um link previamente criado está fora do escopo desta versão (links são imutáveis após a criação).
