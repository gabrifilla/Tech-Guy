# Design Document

## Overview

Este design especifica a **Fase 3 — Manopla 2.0**, seguindo `requirements.md`, `Docs/Tech-Guy_Gameplay_Rework.md` (§§12, 16, 17, 39 e 46) e o parecer `Tech-Guy_Parecer_Requirements_Fase3.md` de 03/10/2026. A identidade é **pressão → postura → Asura**: básico sustenta contato, Q aproxima, W mantém pressão, E favorece quebra e R entrega o pico ofensivo.

As decisões D1–D7 têm soluções concretas neste documento. O catálogo selecionado contém **10 Techniques e 4 Mutations**. Valores novos são **parâmetros iniciais de design**, não balanceamento aprovado por playtest. As observações originais sobre código representam o baseline estático da revisão. Atualização de execução em 04/10/2026: tasks 1–16 concluídas tecnicamente via Unity MCP, incluindo conteúdo, aquisição, HUD e autoria. Evidências e cobertura P1–P16 em contract-audit.md. Aceitação humana da Fase 3 registrada em 04/10/2026; tasks 17–18 concluídas e encerramento em validation.md.

Revisão consolidada conforme `Tech-Guy_Parecer_Design_Fase3.md`: disponibilidade da espera manual, escopo do bloqueio de descendentes e R2.3 sincronizados. O plano incremental está em `tasks.md`; nenhuma implementação é declarada concluída por essa revisão.

O kit base funciona sem boons ou `RunBoons`. Conteúdo da fase funciona sem Protocols universais. Mana, cooldowns, fundação de ações da Fase 1 e autoridade de postura/reação da Fase 2 permanecem. Não há momentum nativo adicional, perfect dodge novo, infraestrutura global de raridade, Mutation rooms ou alteração de Arco/Lança.

O usuário confirmou a aceitação da Fase 2 em 03/10/2026 e autorizou a continuidade. As tasks 17–18 e o handoff da fase anterior foram atualizados; a dependência de início está liberada. Os gates de validação e aceitação próprios da Fase 3 continuam obrigatórios.

## Architecture

### Responsabilidades

| Camada | Responsabilidade e integração |
| --- | --- |
| Autoria | Configuração serializável de taxas, limites e definições de conteúdo. Reutilizar arma, habilidade e perfis existentes; novos assets apenas quando necessários, sob `Assets/_Project`. |
| Estado do recurso | Evoluir `AsuraMomentum` como proprietário único da energia e categoria anterior. `GauntletEnergyPolicy`, classe C# proposta, decide ganhos e deduplicação; não cria uma segunda barra. |
| Coordenação | `BreakerGauntletCombat` coordena execução, snapshots, confirmação, R e cleanup. `AbilityHolder` continua autorizando recursos, cooldowns, buffer e compromisso. |
| Confirmação | Pipeline de `PlayerActor` entrega recibos causais de HP e postura. `CombatReactionController` permanece a única autoridade de quebra, resistência, deslocamento e abertura. |
| Conteúdo | `WeaponRunModifiers` resolve ranks; builder proposto produz snapshot imutável. Executor de Mutation local transforma os passos selecionados sem editar assets ou duplicar scheduler. |
| Aquisição | Adaptar catálogo e apresentação no fluxo existente de `RunBoons.OfferReward` e `RunRewardUI`, com filtro Gauntlet e metadados de Technique/Mutation. |
| Apresentação | HUD e feedback recebem resultados já decididos. VFX, áudio e popups não publicam novos gatilhos mecânicos. |

Helpers de execução/recurso ficam em `Abilities/Weapon`; catálogo em `Weapons`; recibos de combate em `Characters/Combat`; apresentação em `UI`/`Effects`. Dependências são locais, serializadas ou injetadas. Nenhum singleton novo é necessário.

HUD observa energia atual/capacidade e disponibilidade composta de R. Exibir estados separados de carregando, energia cheia mas bloqueada (mana/cooldown/compromisso) e pronto; a indicação de pronto exige todos os gates. Desequipar oculta/desvincula Asura. Feedback de alternância, quebra e Technique usa resultado de ganho efetivo, agrupado pelo impacto, sem novo hit-stop ou evento de dano. Em save/reload, verificar referências serializadas da apresentação e da configuração, além dos valores numéricos.

### Fluxo causal

1. Input e disponibilidade aceitam uma execução. Capturar família da arma, proprietário, epoch de lifecycle, ranks, Mutation e passos em um snapshot.
2. Resolver cada impacto pelo pipeline existente: HP e postura são canais independentes. Capturar HP efetivamente retirado e eventual recibo de quebra antes de callbacks externos.
3. A política identifica fontes elegíveis e prepara todas as contribuições do impacto (nativas e Techniques), marca chaves consumidas e aplica o resultado completo antes de notificações reentrantes. Nenhum ganho decorre apenas do input ou do início de Q/W/E.
4. Publicar o resultado já consolidado, discriminando parcelas e respeitando teto/limites da raiz. Um observador não pode ativar R entre as parcelas de um ganho confirmado. Preservar contagem e ordem dos eventos legados; integrar o commit antes da publicação no pipeline, sem suprimir eventos ou reordená-los globalmente. Se um callback tentar iniciar ação enquanto o impacto ainda está em resolução, revalidar a solicitação somente após esse commit.
5. Reação física e feedback usam o mesmo caminho da Fase 2. Imunidade física não apaga confirmação de HP ou postura.

`OnBasicHit(target, damage)` e `OnStanceBreak(target)` isoladamente não identificam proprietário, execução ou origem. Acrescentar recibos contextualizados ao pipeline, mantendo esses eventos públicos. HookBus pode transportar os novos sinais se estiver disponível independentemente de boons; alternativamente, o coordenador recebe um observador local injetado. Não inferir origem consultando a arma equipada depois do impacto, nem republicar eventos antigos para alimentar energia.

Auditoria inicial em `contract-audit.md`: o básico real de CharControlScript resolve por área e não publica o canal básico do hitbox legado. Integrar emissão explícita desse canal na rota direta da Manopla, por alvo confirmado, é adaptação deliberada de contagem desse caminho; não duplicar o hitbox/dano ou estender canal básico às skills/descendentes. Consumidores privados passam a granularidade por execução. O pipeline contextualizado deve permitir postura confirmada sem HP, pois o caminho atual por área condiciona reação a HP positivo. Preservar comportamento de outras famílias e testar a rota real.

## Components and Interfaces

### Contexto e confirmação

`GauntletImpactContext` é um modelo proposto, imutável, contendo:

- Proprietário/atacante e família capturados na execução; `LifecycleEpoch` para invalidar callbacks antigos.
- `ExecutionId`, `RootExecutionId`, índice de impacto e identificador do alvo com geração de pooling.
- Slot básico/Q/W/E/R, categoria stamina/shock, origem (`Direct`, `Technique`, `Echo`, `Persistent`, `External`) e identificador do conteúdo produtor.
- Snapshot da elegibilidade de geração, vínculo com R e limites compartilhados pela raiz.

`ConfirmedHpReceipt` contém dano efetivo positivo; `ConfirmedBreakReceipt` contém identidade da quebra fornecida pela autoridade de postura. Alvo morto após HP letal não invalida o recibo de HP já confirmado. Não inventar quebra de um alvo morto se a autoridade não a confirmou.

Echoes, zonas e procs herdam raiz e proprietário, mas não são promovidos a golpes diretos. GuardBreaker herda a raiz do básico que causou seu dano de postura. Uma quebra posterior após recuperação de postura tem outra identidade causal; callbacks repetidos da mesma quebra têm a mesma identidade.

### Recurso e fachada

Evoluir a API interna de `AsuraMomentum` para aceitar confirmações e resultado de consumo, preservando fachadas necessárias de `BreakerGauntletCombat`, inclusive leitura de energia, disponibilidade, `IsAsuraActive` e adição explícita. `RegisterSkill` deixa de ser chamado no início de Q/W/E: sua adaptação deve ser registrada junto aos consumidores/testes existentes.

Adição explícita de energia deve declarar origem e não pode representar uma segunda assinatura para o mesmo ganho. Preservar a assinatura legada `AddAsuraEnergy(int)` como adaptador de bônus externo, auditando consumidores; rejeitar valores negativos e aplicar teto. Chamadas desse adaptador durante R também obedecem ao bloqueio, exceto a reserva interna autorizada.

`AsuraSurge` deixa de conceder AsuraFist por cada callback de alvo e passa a usar o recibo elegível de básico, uma vez por execução. Reconfigurar ranks substitui a configuração anterior, sem somar handlers. Energia nativa permanece funcional na ausência desse consumidor.

## Data Models

### Semântica dos overrides de autoria — integração da task 15

Os valores abaixo substituem os defaults de D3/D4 somente quando sua flag está habilitada e são capturados na aceitação da execução. Ausência conserva o default; zero não é ausência. O perfil é copiado da Ability referenciada, sem escrita no asset fonte.

| Conteúdo | Duration | Interval | Profile |
| --- | --- | --- | --- |
| HungryCombo | Redução de cooldown em segundos por rank | Sem efeito definido: rejeitar flag habilitada | Sem efeito definido: rejeitar |
| FlurryEcho / AsuraEcho | Atraso até primeiro echo | Espaçamento entre echoes | Sem efeito definido: rejeitar |
| Campo de Pressão | Duração dos quatro intervalos; sozinho define intervalo = duração / 4 | Intervalo entre os quatro pulsos | Perfil da execução direta de W |
| Asura Cadenciada | Duração de cada golpe, estritamente positiva na execução | Limite inclusivo da espera | Um impacto direto no passo 0, dentro da fase active, commitment e movimento travado; remapear o passo para cada golpe |
| Avanço Demolidor / Epicentro | Sem efeito definido: rejeitar | Sem efeito definido: rejeitar | Perfil da execução direta de Q/E |
| Demais Techniques | Sem efeito definido: rejeitar | Sem efeito definido: rejeitar | Sem efeito definido: rejeitar |

Se Duration e Interval estiverem habilitados no Campo de Pressão, exigir Duration = 4 × Interval. Atrasos zero de filhos continuam depois da publicação do produtor, no próximo frame; não antecipam HP nem geram energia. Perfis explícitos exigem índices de impacto distintos, passos existentes e impactos dentro da fase active. Para Cadenciada, início de recovery e instante do impacto derivam do perfil capturado e da duração; os defaults continuam exatamente 0,22 s e 0,12 s. Configuração inválida rejeita a ação antes de cancelar/debitar. Valores brutos serializáveis e valores executáveis são validações distintas: duration zero pode ser salvo, mas não executa Cadenciada.

### Autoria e snapshots

Definições de Technique incluem ID estável, família, tipo, rank máximo, gatilhos, parâmetros por rank, efeito, apresentação e regras de compatibilidade. Mutations incluem ID, slot, perfil substituto, transformação de passos, exclusividades e apresentação de antes/depois.

Usar campos serializáveis compatíveis com Inspector, listas de entradas e flags explícitas para overrides opcionais. Dicionários runtime não são a fonte serializada. Validar valores brutos antes de normalizar: energia/ranks inteiros, floats finitos, tempos não negativos, sequência válida e referências obrigatórias. Na configuração inicial, capacidade é **100**, preservando o contrato legado; não oferecer capacidade arbitrária sem adaptar seus consumidores.

Snapshot ordena conteúdo por prioridade fixa e ID, independentemente da ordem de aquisição: perfil base → Mutation do slot → Techniques aplicáveis → validação de limites. Copiar passos e dados mutáveis. Uma aquisição durante execução afeta somente execuções futuras; custos já debitados, impactos e reserva daquela execução permanecem os do snapshot.

Parâmetros/ranks do snapshot não são contadores runtime. Para MomentumStrike, capturar as pilhas imediatamente antes de cada lote de impacto; aplicar seu multiplicador uma vez ao dano desse lote, igualmente a todos os alvos. Depois de resolver seus HPs, incrementar uma pilha por confirmação causal direta até o teto; as novas pilhas beneficiam somente lotes seguintes. Não incrementar por reemissão de callback nem recalcular o dano do lote conforme a ordem dos alvos. Aquisição muda parâmetros apenas na próxima execução; o contador local evolui pelos gatilhos declarados.

### D1 — Fontes de Asura

| Fonte | Confirmação e unidade | Valor inicial / limite | Origem e categoria anterior |
| --- | --- | --- | --- |
| Básico nativo | Primeiro HP positivo do básico, uma vez por execução | 3; máximo 3 por execução, independentemente de alvos/passos | Básico direto; não altera categoria anterior. |
| Q/W/E nativo | Primeiro HP positivo **ou** quebra elegível da execução | 10 uma vez por execução | Ação direta, inclusive transformada por Mutation. Atualiza categoria somente nesta confirmação. |
| Alternância | Categoria Q/W/E confirmada diferente da anterior confirmada | +15; total da habilidade 25 | Primeiro uso ou repetição recebe apenas 10. Básico e casts vazios não mudam o histórico. |
| Quebra nativa | Quebra causal elegível; alvos distintos têm identidades distintas | +20 por quebra; até 2 quebras por raiz, máximo 40 | Básico/Q/W/E direto ou postura de GuardBreaker vinculada ao básico. Não altera categoria por si, exceto ao confirmar Q/W/E ainda não confirmado. |
| Momentum | Mesma confirmação elegível de Q/W/E | +10 por rank; uma vez por execução | Bônus separado do ganho nativo e da alternância. |
| AsuraFist | Mesmo primeiro HP positivo do básico | +2 por rank; uma vez por execução | Benefício adicional ao básico nativo; não altera categoria. |
| AsuraReserve | Consumo aceito de R | +20 por rank, ranks 1–3, máximo 60 | Aplicada uma vez após consumo, como reserva; não é acerto nem altera categoria. |
| R e descendentes | Podem causar HP/postura normalmente | 0 geração | Não alteram categoria. Não produzem reserva adicional. |
| Echoes, zona persistente e procs sem origem energética autorizada | HP/postura conforme conteúdo | 0 geração | Não confirmam ganho direto nem atualizam categoria; não reproduzem produtores locais de echoes/zonas da Fase 3. Outros consumidores mantêm seu contrato. |
| Bônus externo explícito | Chamada validada com origem identificável | Teto geral 100 | Sem alternância ou break bonus implícito; respeita bloqueio durante R. |

**Durante R ativo, toda geração por combate fica bloqueada**, inclusive impactos atrasados de ações anteriores. A única exceção é AsuraReserve na transação de consumo. Um recibo bloqueado fica consumido para deduplicação; não é reaplicado ao terminar R. R pode quebrar postura e produzir feedback sem ser uma origem elegível de energia. Estes limites explicitam R2.3–R2.4; as demais quebras continuam resolvidas mecanicamente.

No mesmo impacto que confirma HP e quebra, Q/W/E recebe sua contribuição uma vez e o bônus causal de quebra separadamente. HP totalmente impedido bloqueia básico/AsuraFist, mas uma quebra elegível ainda confirma Q/W/E e seu bônus. Resistência a movimento ou stagger não bloqueia nenhum recibo válido.

Chaves de deduplicação: `(epoch, execution, source)` para básico/habilidade/alternância/Technique; `(epoch, breakId)` para quebra; `(epoch, RExecution)` para consumo/reserva. Marcar antes de callbacks para impedir reentrância. Manter chaves enquanto existirem filhos daquela raiz; liberar ao encerrar todos os filhos ou invalidar epoch. Nenhum cache global cresce indefinidamente.

Recibo também exige contexto registrado e ainda válido. Ao encerrar a raiz e todos os filhos, fechar/remover o contexto; callbacks posteriores não podem recriá-lo ou ganhar elegibilidade após liberar suas chaves. IDs de execução são monotônicos por proprietário/epoch e não são reciclados dentro do epoch. Um filho autorizado mantém sua raiz registrada até terminar; encerramento da ação produtora não encerra antecipadamente filhos legítimos. Morte/troca/disable invalidam todos esses contextos.

O bloqueio de descendentes é **local**: echoes/zonas não geram Asura, não atualizam alternância e não reproduzem produtores locais de echoes/zonas desta fase. Mantêm origem descendente. Eventos públicos de dano, acerto e quebra seguem a granularidade e ordem do pipeline existente; outros consumidores continuam elegíveis segundo seus contratos, salvo adaptação específica registrada em D7. Não suprimir HookBus ou `StanceBroken` globalmente. A exclusão de echoes/zonas das pilhas de MomentumStrike é uma adaptação específica desta Technique, distinta dessa regra geral.

Cancelar antes do gatilho não gera energia; cancelar depois preserva ganho confirmado. Com energia no teto, consumir o gatilho mesmo que o ganho efetivo seja zero. O resultado registra fonte, ganho solicitado, ganho efetivo e motivo de bloqueio para testes/feedback.

### D2 — Pressão sem momentum nativo

**Não adicionar momentum nativo nesta fase.** Contato, alternância, quebra e retorno de R já recompensam pressão. Um segundo recurso repetiria o incentivo de Asura e de MomentumStrike, aumentando HUD e cleanup sem necessidade demonstrada. Momentum continua uma Technique de energia; MomentumStrike continua uma Technique de pilhas por-run. Perfect dodge não recebe comportamento novo.

A comparação de gameplay deve observar abandono de contato, utilidade do básico e vontade de alternar Q/W/E. Caso o loop falhe, revisar primeiro taxas, alcance e timing; medidor novo requer decisão de escopo posterior.

### D3 — Catálogo de 10 Techniques

Todas são específicas de Gauntlet, usam IDs de `WeaponBoon` existentes e ranks 1–3. Ranks aumentam parâmetros do mesmo comportamento; não são entradas distintas para a contagem.

| ID / estilo | Gatilho e comportamento selecionado | Rank / energia / interação com R |
| --- | --- | --- |
| Momentum / alternância | Confirmação de Q/W/E acelera a recarga ofensiva de Asura; cast vazio deixa de beneficiar. | +10 energia/rank por execução; não atua em R. |
| AsuraReserve / ciclo | R aceito deixa uma reserva para reiniciar pressão. | +20/rank após consumo; máximo 60, sem refund ou repetição. |
| AsuraFist / básico | Básico com HP confirmado torna o combo uma rota adicional de recarga. | +2 energia/rank por execução; não atua em R. |
| GuardBreaker / postura | A cada 3 execuções de básico com HP confirmado, aplicar postura extra ao primeiro alvo confirmado da terceira; usar a autoridade de quebra. Q/W/E aceito reinicia contador. | Postura 24 × (1 + 0,5 × rank); solicita Knockback na quebra conforme resistência, sem Push adicional. Sem energia própria; pode produzir bônus nativo de quebra. |
| HungryCombo / combo | Básico confirmado reduz cooldowns ativos e favorece intercalar combo/habilidades. | 0,3 s/rank por execução, limitado a zero; não afeta R em execução nem reduz duas vezes por multi-alvo. |
| MomentumStrike / contato | Acertos diretos confirmados acumulam pilhas; dano de HP recebido encerra o benefício, incentivando manter contato sem trocar hits. | Até 10 pilhas, uma por impacto causal; +2% dano/rank/pilha. Echoes/zonas não empilham; R direto pode empilhar, mas não gera Asura. |
| FlurryEcho / W | Último impacto direto efetivamente executado de W produz repetições locais, prolongando a sequência de pressão. | +2 echoes/rank, 55% do HP/postura do passo final cada; sem energia nem reprodução local de echoes/zonas; demais consumidores seguem seu contrato. |
| AsuraEcho / R | Último impacto produtor efetivamente executado de R produz réplicas locais; cancelamento anterior não fabrica o impacto final. | +1 réplica/rank, 65% HP/postura do passo final cada; sem energia, reprodução local de echoes/zonas ou extensão de reserva/proteção recebida. |
| RocketAdvance / entrada | Amplia o avanço autorado de Q e permite iniciar pressão de mais longe; alcance exige posicionamento e validação de terreno. | +70% avanço/rank, limitado a 8 m totais; não atravessa sólidos, não amplia hitbox nem aplica dano ao longo do percurso. |
| SeismicFist / quebra | Amplia deslocamento de Knockback já autorizado, favorecendo separar alvos após quebra. | +30% distância/rank, dentro dos limites da Fase 2; não converte Launch/Knockdown nem afeta R. |

Feedback identifica contribuição de energia, contador de GuardBreaker, pilhas de MomentumStrike ou transformação de impacto/movimento conforme a entrada. Ao retirar conteúdo, desativar consumidor e limpar seus contadores/modificadores; ranks adquiridos continuam no inventário da run até seu fim. Não retirar energia já concedida ao perder uma Technique.

GuardBreaker e HungryCombo passam deliberadamente de granularidade por evento/alvo para **por execução de básico**, evitando benefício escalado por callback multi-alvo. MomentumStrike mantém impacto causal como unidade: confirmar em vários alvos pode acumular várias pilhas, até 10. HP recebido só limpa suas pilhas quando efetivamente positivo, uma vez por impacto causal; imunidade/absorção total não limpa.

SeismicFist também muda deliberadamente: o modificador atual pode substituir o efeito de quebra do passo por Knockback; nesta fase ele amplia apenas Knockback já autorizado. Isso preserva Launch do uppercut e demais efeitos escolhidos pela autoria/Mutation. Atualizar descrição e testes desse contrato específico, preservando os testes corretos de resistência e limites.

### D4 — Catálogo de 4 Mutations

Mutations têm rank único e uma seleção por slot. Custos de mana e cooldowns são os atuais do slot, debitados uma vez por execução. Perfis transformados continuam no controlador da Fase 1. Os números abaixo são iniciais.

| Mutation / ação | Antes → depois e execução | Asura, postura e combinação |
| --- | --- | --- |
| Avanço Demolidor / Q | Avanço frontal → travessia limitada de corpos inimigos, terminando em uppercut. Um input; compromisso Committed. Preservar duração total e startup/active/recovery autorados de Q. Sem colisão com corpos durante deslocamento validado; sólidos/terreno continuam bloqueando. Apenas o impacto final causa dano, com HP/postura totais do Q base. | Confirmação direta normal. Solicitar Launch **somente na quebra** do uppercut; imunidade usa a política da Fase 2. RocketAdvance amplia trajeto até 8 m; SeismicFist não converte Launch em Knockback. Não conceder invulnerabilidade. |
| Campo de Pressão / W | Sequência de W → mesma sequência, seguida de zona no ponto do último impacto direto. Um input; sequência Committed e cancelamento originais. Zona dura 2 s mecânicos, 4 pulsos a cada 0,5 s, cada um com 25% HP/postura do passo final base, sem deslocamento físico explícito. | Impactos diretos confirmam energia; pulsos persistentes não. Cancelar antes do último impacto não cria zona; depois da criação, zona pode persistir até expirar, salvo cleanup de proprietário. FlurryEcho repete o impacto direto final, sem criar zonas adicionais. |
| Epicentro / E | Golpe direcionado à frente → impacto radial centrado no jogador, substituindo geometria e direção da área. Um input; mesmas fases, compromisso, HP/postura totais por alvo e cancelamento do E base. Raio inicial usa o raio autorado do ShockRing legado no rank 1. | Confirma uma execução mesmo atingindo vários alvos; bônus de quebra limitado pela raiz. Reação respeita cada alvo. SeismicFist amplia apenas Knockback elegível. É transformação de geometria, não oferta de raio adicional. |
| Asura Cadenciada / R | Sequência automática interna → três golpes comandados individualmente por input R. Consumir energia/mana e iniciar cooldown somente no primeiro input aceito. Cada golpe tem startup 0,12 s, active 0,10 s e recovery 0,18 s; input seguinte é aceito na recovery por buffer vigente. Após recovery, aguardar até 0,8 s mecânicos por novo R; timeout encerra. Compromisso Committed por golpe, sem controle obrigatório durante espera. | HP/postura total do R base distribuído 25%/25%/50% entre golpes; nenhum ganho de Asura. AsuraEcho somente no terceiro golpe realizado. Reserva aplicada uma vez no início. Interrupção/timeout abandona os golpes restantes, sem refund. |

Para Asura Cadenciada, o buffer não fabrica inputs: cada golpe exige novo pressionamento; segurar R não solicita o próximo. Continuação pertence à mesma execução e passa por estado/fases/cancelamento, sem uma segunda cobrança/gate de energia ou cooldown. `AbilityHolder` roteia continuação antes do gate de **nova ativação**, somente quando esse R possui a continuação armada. Inputs R fora desse estado seguem o gate normal. Dash autorizado encerra a sequência; não a suspende para retomar depois.

#### Disponibilidade durante a espera de Asura Cadenciada

| Entrada/estado na espera | Resultado |
| --- | --- |
| Movimento | Permitido; preserva a espera. |
| Novo pressionamento de R válido | Continua a mesma execução vigente, sem novo débito. |
| Básico/Q/W/E disponível e aceito | Encerra golpes restantes de R e inicia a nova ação em transição coordenada. |
| Básico/Q/W/E rejeitado por disponibilidade | Preserva espera, energia, mana e cooldown; não cancela R nem deixa pedido indisponível aguardando indefinidamente. |
| Dash autorizado | Encerra a sequência e aplica o dash segundo a fundação. |
| Timeout, morte ou desequipamento | Encerra sequência e descarta continuações pendentes vinculadas a esse R. |

Validar destino e viabilidade **antes** de encerrar R; aplicar encerramento e início da nova ação como uma transição, sem duas execuções ofensivas coexistentes. A continuação verifica identidade da execução vigente: não pode interromper outra ação Committed que já iniciou. Essa disponibilidade vale somente na espera; startup/active/recovery conservam janelas do perfil, custo, buffer e compromisso.

Durante a espera, R continua ativo para o bloqueio de energia. Depois de uma nova ação aceita encerrar R, confirmações dessa nova ação seguem a geração normal. Na fronteira temporal, seguir a convenção inclusiva de validade da fundação: espera válida enquanto `agora - inicioEspera <= 0,8 s`; timeout somente quando maior. No mesmo tick exatamente no limite, processar o input válido antes de timeout. Após o limite, encerrar primeiro e rejeitar continuação, mesmo se seu input tiver sido emitido anteriormente; não reviver R com buffer atrasado. Pedidos concorrentes usam a sequência determinística do buffer existente, sem criar uma segunda fila.

A espera da Cadenciada permite movimentação normal e não mantém a redução de dano recebido. Durante cada golpe, reutilizar o modificador de R autorado, com ownership e liberação ao fim/cancelamento. Recovery autoriza dash apenas na janela do perfil (proposta: último 50%); startup/active não ganham cancelamento novo. Os ecos finais não prolongam essa proteção. Nenhum golpe extra ocorre após timeout.

Apenas um impacto direto por passo/alvo, mais descendentes explicitamente definidos. Cada pulso de zona tem identidade distinta para HP/postura, sem geração de Asura nem reprodução local de echoes/zonas; eventos e outros consumidores seguem seus contratos. Todas as Mutations respeitam teto físico, resistência binária, abertura e retomada de inimigos. Movimentos terminam em posição válida; se não houver percurso válido, resolver avanço parcial ou zero, nunca teletransportar através de sólidos.

Travessia de Q ignora somente bloqueio locomotor pelos corpos inimigos durante o avanço. Não desabilitar hurtboxes, recebimento de dano, colisões de terreno ou componentes de combate do alvo/jogador. Restaurar alterações de colisão próprias em conclusão/cancelamento/morte/disable, sem restaurar configurações que outro proprietário ainda mantém.

Echoes só são agendados quando seu passo produtor direto realmente executa; atingir um alvo não é condição adicional para criar a réplica espacial. Cancelar antes do produtor não cria echoes. Após o produtor, réplicas já agendadas seguem até finalizar, mesmo com cancelamento ofensivo ordinário; morte/troca/disable/fim de run removem descendentes próprios. Na Cadenciada, somente o terceiro golpe realizado produz AsuraEcho. Replicas não mantêm locks ou proteção do R.

O raio de Epicentro segue inicialmente `max(raioEsféricoBase, larguraBoxBase / 2) × 1,2`; loadouts ShockRing legados usam `× (1 + 0,2 × rankLegado)`. GuardBreaker solicita um único efeito de quebra, cujo Knockback pode receber SeismicFist uma vez; não executar dois deslocamentos. Técnicas de echoes operam sobre o último passo direto do slot transformado, antes de expandir réplicas: zona, eco ou réplica não pode ser selecionado como novo produtor. Aquisição de conteúdo durante R não altera reserva ou continuação já capturadas.

### Aquisição, exclusividade e compatibilidade de conteúdo

Oferecer as 10 Techniques e 4 Mutations pelo fluxo existente de recompensas Gauntlet. Oferta mostra tipo, slot, ranks ou rank único, mudança e compatibilidades. Mutation já selecionada não reaparece como rank adicional; se futuramente houver outra do mesmo slot, a seleção deve oferecer substituição explícita, nunca composição silenciosa. Não criar catálogo global de Protocols/raridade.

| Entrada anterior | Destino da Fase 3 | Compatibilidade planejada |
| --- | --- | --- |
| 10 Techniques selecionadas | Manter ID e adaptar efeitos conforme tabela | Preservar ranks adquiridos; atualizar descrições e consumidores dos contratos alterados. |
| ShockRing | Reclassificar como Epicentro, Mutation de E | Preservar enum/ID legado por adaptador. Rank antigo 1–3 seleciona Epicentro com raio legado correspondente até fim da run; novas aquisições têm rank único. Não contar ShockRing também como Technique. |
| ComboNova e Shockwave | Fora das novas ofertas deste catálogo | Preservar efeitos e ranks em loadouts antigos por consumidor de compatibilidade; não contar para as 10 Techniques. Evitar redundância de novas ofertas de área com Epicentro/Campo de Pressão. |
| LongFists, StanceCrusher e Berserker retirados anteriormente | Continuar compatibilidade existente | Não reutilizar IDs ou reintroduzir nas ofertas sem requisito. |
| Transformação genérica de Q existente | Auditoria confirmou substituição por ArsenalAbility, sem equivalência com Avanço Demolidor | Preservar Q legado; não converter automaticamente em travessia. Nova Mutation Q oferece substituição explícita se conflitar. Remover oferta genérica nova apenas de Gauntlet, preservando registro legado e outras famílias. |

Novas Mutations sem ID legado recebem identificadores estáveis novos, sem alterar valores existentes de enum. Não mudar GUIDs, nomes de assets ou chaves de Resources por consequência dessa classificação. A migração precisa ser idempotente, manter backup lógico dos dados de origem e registrar entradas desconhecidas; nenhuma troca automática perde ranks.

### D5 — R e ritmo de recarga

Capacidade **100**, gate cheio, consumo no início da ação aceita e **sem reembolso** após cancelamento/interrupção. Validar disponibilidade e snapshot antes de consumir; se a aceitação falhar, preservar energia, mana e cooldown. Consumo, reserva e publicação pertencem à mesma transação e não podem ser repetidos por reentrância.

R base conserva seu perfil de compromisso e duração; sua eventual transformação usa o contrato da Cadenciada. Ao encerrar, liberar locks, movimento, proteção, animação e feedback próprios e devolver disponibilidade à Fase 1. Reserva máxima 60 não habilita R sozinha. R cheio continua dependendo de mana, cooldown e estado; HUD distingue esses gates.

Referências aritméticas sem boons: básico isolado exige 34 execuções confirmadas para chegar a 100; Q/W/E com categorias stamina/stamina/shock gera 45 antes das quebras; Q/W/E/Q nessa ordem gera 70. Dez básicos somam os 30 restantes. Duas quebras elegíveis na mesma raiz somam até 40. Não são medições de duração ou garantia de gameplay.

No playtest, registrar tempo mecânico e número de ações confirmadas até primeiro/segundo R, casts vazios, oportunidades de alternância e contribuição de cada fonte. Comparar grupo comum com alvo resistente sem quebras frequentes. Hipótese inicial: primeiro R em 15–30 s de ofensiva efetiva; segundo R deve exigir retomada observável da pressão. Esses intervalos orientam tuning, não são critérios fechados de aprovação nem justificam mudar mana silenciosamente.

### D6 — Mana, lifecycle e relógios

Mana/cooldown seguem os dados atuais do slot e a autorização existente. Mutations não tornam ações gratuitas; Cadenciada paga uma vez pela execução inteira. Não modificar a unidade ou conversão de custo da arma nesta fase.

| Transição | Energia / categoria / contextos | Conteúdo e efeitos |
| --- | --- | --- |
| Sala seguinte com mesmo personagem/arma | Preservar energia e categoria; contextos terminados são liberados | Preservar ranks/seleções da run. Não exigir novo sistema de salas. |
| Troca/desequipamento de arma | Cancelar execução, zerar energia/categoria, invalidar epoch | Desativar consumidores e zonas/echoes próprios; manter aquisições no inventário da run. Reequipar aplica ranks, sem restaurar energia antiga. |
| Morte/disable/reuso | Zerar energia/categoria e invalidar epoch | Liberar efeitos próprios e subscriptions; reuso configura uma única assinatura nova. Não confundir disable com apagar inventário da run. |
| Fim/reset de run | Zerar energia/categoria/contextos | Limpar ranks, seleções, contadores e efeitos da run. |
| Aquisição durante uma ação | Preservar recurso e snapshot em execução | Alterar apenas próxima execução. Contadores de consumidor não recebem assinaturas cumulativas. |

Execução, buffers, espera de Cadenciada, pulsos e duração da zona usam o relógio mecânico da fundação, congelado em pausa/hit-stop. Não avançar um relógio paralelo com `unscaledDeltaTime` para compensar congelamento. Apresentação pode usar tempo não escalado, sem gerar ações/energia. O conteúdo não introduz decadência de energia ou de categoria por tempo.

Ao trocar arma, remover somente alterações que pertencem àquela execução/conteúdo. Descendentes atrasados carregam epoch e são descartados. Integração deve preservar a política de outros consumidores de tempo e não remover locks de outro sistema.

## Error Handling

- Configuração inválida (NaN/infinito, tempo negativo, rank inválido, perfil/referência faltante) produz diagnóstico de autoria e bloqueia a entrada afetada; não iniciar execução parcialmente configurada.
- Recibo sem proprietário/origem verificável não gera energia. Eventos antigos continuam funcionais, mas não podem ser usados para adivinhar causalidade.
- Colisão/posição inválida limita deslocamento; reação física recusada não invalida HP/quebra. Nunca contornar imunidade de Boss para cumprir a apresentação da Mutation.
- ID legado desconhecido é reportado e preservado no dado de origem para migração, sem remapear arbitrariamente.
- Falha após consumo ou cancelamento legítimo não reembolsa R. Cleanup deve ser idempotente mesmo quando morte, disable e desequipamento ocorrem juntos.

## Correctness Properties

| Propriedade | Invariante verificável |
| --- | --- |
| P1 — Confirmação independente | HP positivo gera fontes de HP mesmo sob imunidade física; quebra elegível sem HP gera somente suas fontes autorizadas. |
| P2 — Identidade causal | Duplicação/reentrância não muda o ganho; quebra distinta de outro alvo pode contribuir dentro do limite da raiz. |
| P3 — Alternância | Apenas Q/W/E confirmado muda histórico; primeiro/repetido = 10, alternado = 25 antes de Techniques/quebras. |
| P4 — Básico e limite | Um básico em N alvos recebe 3 nativo e até um bônus AsuraFist; HP letal conta, callbacks adicionais não. |
| P5 — Cancelamento | Cancelar antes do gatilho gera zero; depois não retira energia confirmada. |
| P6 — Segurança numérica | Energia sempre finita em [0, 100]; entrada inválida não altera estado. |
| P7 — R atômico | Rejeição preserva recursos; aceitação consome uma vez, reserva uma vez, sem geração por combate durante R. |
| P8 — Descendentes | Echoes/zonas não alimentam Asura, alternância ou reprodução local de echoes/zonas; eventos e outros consumidores mantêm seus contratos, salvo adaptação registrada. |
| P9 — Conteúdo determinístico | Mesmo conjunto de ranks/seleções produz mesmo snapshot independentemente da ordem de aquisição. |
| P10 — Imutabilidade | Aquisição/migração runtime não altera assets; ação iniciada mantém snapshot e custos. |
| P11 — Movimento e postura | Um único decisor da Fase 2; limites, imunidades e retomada preservados em todos os passos/procs. |
| P12 — Lifecycle | Epoch antigo ou contexto encerrado não contribui, mesmo após liberar chaves; troca/disable não deixa efeitos, subscriptions ou barra na outra arma. |
| P13 — Relógios | Pausa/hit-stop congela pulsos, espera e execução; feedback cosmético não altera estado mecânico. |
| P14 — Continuação manual | Cadenciada executa no máximo três golpes, um input por golpe e um débito total; destino rejeitado preserva espera, destino aceito encerra R sem concorrência e não há continuação após timeout/cancelamento/troca de execução. |
| P15 — Compatibilidade | Eventos legados preservam assinatura/ordem/contagem; aliases e ranks antigos não se tornam duas ofertas/effects equivalentes. |
| P16 — Independência | Kit base funciona sem RunBoons; builds de conteúdo funcionam sem Protocols universais. |

## Testing Strategy

Durante implementação, verificar inicialmente disponibilidade do Unity MCP e projeto/Editor conectado. Registrar Console antes das mudanças; importar/compilar e inspecionar depois, sem limpar erros. Executar **somente testes relevantes** à alteração e suas dependências. Mudança compartilhada em pipeline de confirmação justifica regressões direcionadas de Arco/Lança; não implica rodar toda suíte indiscriminadamente.

| Nível | Evidência necessária |
| --- | --- |
| EditMode | Política P1–P8, limites, confirmações HP/postura independentes, categoria, reentrância, snapshot, ordem de ranks, validação bruta e migração idempotente. |
| PlayMode | Pipeline real de básico/Q/W/E/R; multi-alvo, alvo letal, break sem HP, reação imune, limite por raiz, filhos atrasados, custos/buffer, GuardBreaker/HungryCombo por execução, cleanup e relógios. |
| Integração de conteúdo | Cada uma das 10 Techniques e 4 Mutations; FlurryEcho + zona, RocketAdvance + uppercut, GuardBreaker + SeismicFist, Epicentro multi-alvo e AsuraReserve/AsuraEcho + Cadenciada. |
| Editor/serialização | Inspector e save/reload das definições; referências de arma/prefab/HUD, aliases legados, IDs e Resources; nenhuma mutação dos assets durante run. |
| Runtime representativo | Com MCP quando suportado: cenário base sem RunBoons, grupo comum, Heavy/Elite/Boss, parede/terreno inválido, pausa/hit-stop, troca de arma e morte. Sair de Play Mode e preservar cena do usuário. |
| Aceitação humana | Aproximadamente 10 min com kit base sem boons; depois dois builds sem Protocols: pressão (HungryCombo/FlurryEcho/Campo de Pressão) e quebra/ciclo (GuardBreaker/SeismicFist/AsuraReserve/Epicentro). Registrar contato, decisões, clareza, recarga e oportunidades reais de ação dos inimigos. |

Separar compilação, testes, runtime e aceitação humana. Se MCP falhar, registrar tentativa e limitação concreta; continuar inspeção independente, mantendo verificações Unity pendentes. Atualizar asserts somente para contratos deliberadamente substituídos; não enfraquecer testes de postura, imunidade ou cancelamento para esconder defeitos.

## Requirements Coverage Map

| Requisito | Seções / propriedades |
| --- | --- |
| R1 — Kit e pressão | Architecture, catálogo de Mutations, D2; P11/P14/P16. |
| R2 — Geração | Contexto, D1; P1–P6/P8. |
| R3 — Ciclo R | D1/D5, Cadenciada, lifecycle; P7/P12/P14. |
| R4 — Momentum | D2; decisão negativa fundamentada, sem entrega condicional de novo medidor. |
| R5 — Techniques | D3, aquisição/compatibilidade, snapshots; P9/P10/P15/P16. |
| R6 — Mutations | D4, composição, aquisição, relógios; P8–P14/P16. |
| R7 — Feedback/autoria/lifecycle | Architecture, Data Models, D6, Error Handling; P6/P10/P12/P13. |
| R8 — Integração | Contexto e fachada, tabela de compatibilidade, D7; P11/P15/P16. |
| R9 — Validação | Testing Strategy e handoff; evidências técnicas e humanas separadas. |

## Decisões e handoff

| ID | Decisão deste design | Próxima ação / escopo afetado |
| --- | --- | --- |
| D1 | Confirmar básico/QWE/quebra; taxas e limites da tabela; sem energia de R/descendentes. | Implementar contexto e política, substituir ganho no cast; validar contra resistência, multi-alvo e reentrância. |
| D2 | Sem momentum nativo ou perfect dodge novo. | Avaliar pressão no playtest antes de propor outro recurso. |
| D3 | 10 Techniques selecionadas, com granularidades explícitas. | Adaptar catálogo/consumidores e dados legados; números permanecem iniciais. |
| D4 | 4 Mutations: Q travessia/uppercut, W zona, E Epicentro e R Cadenciada. | Integrar ao fluxo existente e snapshots; não criar Mutation rooms. |
| D5 | Capacidade 100, full gate, consumo inicial, reserva até 60 e sem refund. | Medir ações/tempo para primeiro e segundo R; preservar cooldown/mana. |
| D6 | Mana preservada; energia persiste entre salas, zera em morte/disable/troca/fim de run; aquisições persistem na run. | Validar teardown e referências; relógio mecânico da Fase 1. |
| D7 | Adaptações locais documentadas antes da implementação; aceitação da Fase 2 confirmada pelo usuário em 03/10/2026. | Executar tasks por etapas e reconciliar contratos abaixo no estágio afetado, preservando a evidência histórica. |

### Reconciliação planejada entre specs

| Spec / contrato existente | Adaptação deliberada a registrar na implementação |
| --- | --- |
| `combat-foundation-rework` | Manter controlador de fases/buffer; acrescentar roteamento de continuação manual de R sem segundo débito. Não generalizar a outras armas. |
| `posture-enemy-interaction` | Acrescentar contexto/recibo causal compatível; não mudar resistência, abertura, retomada ou canais. Aceitação 17 confirmada pelo usuário e handoff/fechamento 18 consolidados em 03/10/2026. |
| `impactful-weapon-boons` | Momentum deixa de gerar no uso; AsuraFist/GuardBreaker/HungryCombo usam execução elegível; preservar demais payloads e efeitos fora do catálogo afetado. |
| `gauntlet-boon-playstyle-overhaul` | Registrar os 10 selecionados, ShockRing → Mutation e compatibilidade de ComboNova/Shockwave; não contar aliases em duplicidade. |
| `modifier-synergies-theme17` | Snapshot/ranks e teardown continuam compartilhados; adaptar AsuraSurge e excluir descendentes da recarga e da reprodução local de echoes/zonas. MomentumStrike passa a excluir echoes/zonas de suas pilhas, com captura por lote antes do dano. Auditar consumidores legados afetados; nenhuma supressão genérica de eventos/sinergias ou alteração de outras famílias. |

Este documento é o handoff das decisões arquiteturais e do escopo da fase. Não altera retroativamente arquivos dessas specs nem marca tasks existentes como concluídas. `tasks.md` separa contexto/recurso, ciclo de R, Techniques, Mutations, aquisição/HUD, compatibilidade e validação/aceitação, com os gates da Fase 2 explícitos.

**Verificação desta entrega:** revisão documental e inspeção estática, sem alteração de scripts/assets, compilação, testes ou playtest. Design especificado não equivale a implementação ou balanceamento aceito.

