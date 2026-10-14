# Encurtador de links

## Problema
Links longos quebram em mensagens, e-mails e materiais impressos, e não dá para medir quantas pessoas clicaram neles.

## Objetivo
Permitir que uma pessoa transforme uma URL longa em um link curto, compartilhe esse link e acompanhe quantos acessos ele recebeu.

## Usuários
- Criador do link: gera links curtos e acompanha os acessos
- Visitante: clica no link curto e chega ao destino

## Histórias
- Como criador, quero encurtar uma URL para compartilhar um link menor
- Como criador, quero escolher o código do link para usar um nome legível
- Como criador, quero ver quantos acessos meu link recebeu
- Como visitante, quero ser redirecionado ao destino sem passar por tela intermediária

## Requisitos funcionais
- RF-01: o sistema recebe uma URL de destino e devolve um link curto; submissões repetidas da mesma URL de destino geram links distintos e independentes
- RF-02: o código do link tem 7 caracteres gerados automaticamente, usando apenas letras maiúsculas de A a Z e números de 1 a 9
- RF-03: o criador pode informar um código próprio de 4 a 20 caracteres, usando apenas letras maiúsculas de A a Z e números de 1 a 9, sem espaços
- RF-04: acessar um link curto existente redireciona para a URL de destino
- RF-05: cada acesso a um link válido incrementa o contador daquele link, incluindo acessos repetidos e automatizados
- RF-06: o criador pode definir uma data de expiração para o link; aceita data e horário em UTC (formato ISO 8601); se for informada apenas a data, o link permanece válido até o final daquele dia (23:59:59 UTC)
- RF-07: o sistema devolve o total de acessos de um link a qualquer pessoa que conheça seu código, mesmo após a expiração do link
- RF-08: os links e seus dados devem ser persistidos em banco de dados para que não se percam após reinício da aplicação

## Regras de negócio
- RN-01: o código do link é único em todo o sistema e nunca pode ser reutilizado, mesmo após a expiração do link
- RN-02: a URL de destino precisa usar http ou https, não pode apontar para endereços locais/privados (ex: localhost, faixas de IP privado) e deve responder com sucesso a uma verificação ativa de rede (com timeout curto) no momento da criação
- RN-03: link expirado não redireciona e não conta acesso; o visitante recebe mensagem explícita de que o link expirou
- RN-04: o contador de acessos nunca diminui
- RN-05: códigos automáticos e personalizados aceitam somente A-Z maiúsculo e os dígitos de 1 a 9 na criação; o dígito 0, minúsculas, espaços e caracteres especiais são recusados com erro de validação; no acesso pelo visitante, o código é tratado de forma insensível à caixa (case-insensitive)
- RN-06: se um código gerado automaticamente já estiver em uso, o sistema tenta gerar outro código por até 5 tentativas consecutivas; se todas colidirem, a criação é interrompida com aviso de indisponibilidade temporária

## Casos de borda
- Código já em uso: recusar a criação e explicar o motivo
- Código próprio com formato inválido (minúsculas, espaços, dígito 0 ou caracteres especiais): recusar na criação com erro de validação
- Código gerado automaticamente já em uso: tentar gerar outro código até 5 vezes; persistindo a colisão, recusar por indisponibilidade temporária
- Código inexistente no acesso: responder que o link não existe
- Código expirado no acesso: responder explicitamente que o link expirou (sem redirecionar e sem incrementar contador)
- URL de destino com mais de 2048 caracteres: recusar
- URL de destino local, privada ou inacessível: recusar na criação com erro explicativo
- Data de expiração no passado: recusar na criação

## Fora de escopo
- Cadastro e login de usuários
- Domínio personalizado
- Relatório com origem geográfica do acesso
- Edição do destino de um link já criado

## Critérios de aceite
- Criar um link e acessá-lo redireciona para o destino informado
- Acessar o link informando letras maiúsculas ou minúsculas redireciona com sucesso para o destino correto
- Dois links não podem ter o mesmo código, e códigos expirados não podem ser recriados
- Submissões repetidas da mesma URL de destino geram links independentes com contadores isolados
- Link com data de expiração vencida responde erro explícito de expiração e não incrementa o contador
- Um link ainda válido funciona até o término de sua data/hora de expiração em UTC
- Qualquer pessoa que conheça o código pode consultar o total de acessos, inclusive de links expirados
- URLs de destino direcionadas para redes locais/privadas ou que não respondam na verificação ativa são recusadas na criação
- Tentativas consecutivas de colisão automática que excedam o limite de 5 tentativas retornam erro amigável
- Após reiniciar a aplicação, os links persistidos no banco continuam funcionando