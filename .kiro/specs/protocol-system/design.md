# Design Document

## Overview

Este design especifica a **Fase 5 — Sistema de Protocolos**, conforme `requirements.md`, o roadmap `Docs/Tech-Guy_Gameplay_Rework.md` (§§18, 38, 42–43 e 48) e `Tech-Guy_Parecer_Requirements_Fase5.md`. Entrega Ignite, Frost, Conductor, Detonation, Reactor, Resonance e Overflow como modificadores universais que compõem efeitos de combate.

**Status: design aprovado pelo usuário; execução do plano iniciada, tasks 1–12 concluídas; 13 em implementação/validação, 14–16 pendentes; evidências em `validation.md`.** O parecer aprova os requirements sem ajustes obrigatórios. Decisões D1–D8 fecham os contratos necessários para derivar tasks. Valores de balanceamento e performance são propostas mensuráveis, não resultados observados. A elaboração documental não executou testes; as verificações posteriores da implementação estão registradas separadamente.

Preservar contratos causais da Manopla, técnicas/mutações, modos legados e lifecycle da run. A Manopla é o cenário principal. Não incluir Arco/Lança 2.0, novos inimigos, setores, geração procedural, meta progressão ou a proposta separada de reformulação de inimigos/boons.

## Architecture

### Autoridade e responsabilidades

`RunBoons` permanece o único aplicador de aquisições. `ProgressionDirector` permanece a autoridade de progressão. Um runtime local ao jogador resolve Protocolos; não introduzir singleton, bus global ou segundo diretor. Preservar hooks existentes mediante adapters com identidade e deduplicação, em vez de removê-los indiscriminadamente.

| Componente proposto | Responsabilidade |
| --- | --- |
| `ProtocolCatalogDefinition` (ScriptableObject) | Dados serializáveis dos sete IDs, ranks, coeficientes, relógios e limites |
| `ProtocolCatalogValidator` / `ProtocolCatalogSnapshot` | Validar dados brutos e criar cópia imutável; não escrever no asset durante gameplay |
| `ProtocolInventory` | Ranks por sessão e revisão; consulta de elegibilidade |
| `ProtocolRewardSelection` | Cartões determinísticos segundo oportunidade, inventário e regras D1 |
| `ProtocolRuntime` (MonoBehaviour local) | Coordenar adapters Unity, relógio, orçamento agregado e cleanup |
| `ProtocolResolver` (C#) | Resolver fila, status, mortes e prioridades sem recursão de chamadas |
| `ProtocolRootContext` / `ProtocolBudget` | Identidade, snapshot de ranks, contadores e fronteira da cascata |
| `ProtocolDamageAdapter` | Receipts de dano real, origem, autoria e integração com transações/feedback existentes |
| `ProtocolStatusAdapter` | Aplicar/consultar Burn e Chill com aplicação e geração do alvo |
| `ProtocolFeedbackPresenter` | Feedback limitado, independente da quantidade de impactos |

Novos helpers de combate em `Assets/_Project/Scripts/Characters/Combat/Protocols`; aquisição em `Core/Run`; apresentação em `UI`/`Effects`; catálogo em `Assets/_Project/ScriptableObjects/Protocols`. Evoluir `PlayerOnHitEffects`, `RunSynergyEffects`, `BurnStatus` e `ChillStatus` por adapters, mantendo fachadas necessárias aos consumidores atuais. Não mover arquivos existentes ou alterar GUIDs para organizar o sistema.

## Components and Interfaces

APIs abaixo são propostas, não declarações de implementação existente.

| Interface | Contrato |
| --- | --- |
| `TryAcquire(sessionId, protocolId, expectedRevision)` | Preflight de sessão, dependências e cap; commit de um rank, revisão única |
| `BeginRoot(sessionId, encounterId, producer, executionId)` | Reserva contexto e orçamento; recusa quando a capacidade estiver cheia |
| `SubmitConfirmedHit(root, receipt, targetHandle)` | Recebe dano confirmado, nunca recalcula o ataque nativo |
| `ReserveChild(root, producer, dueTime)` | Retém raiz com capacidade e identidade antes de agendar descendente |
| `ResolvePending(root)` | Drena FIFO limitado; reentrada adiciona trabalho ao contexto, não chama outro Resolve |
| `SealRoot(root)` | Impede novas submissões externas; finaliza quando produtores reservados e fila terminarem |
| `BeginBurnTick(applicationId, ordinal)` | Cria evento periódico autorizado, deduplicado e limitado por D3/D6 |
| `TryCaptureDeath(targetHandle, killingReceipt)` | Captura estado e posição antes de cleanup; reclama a morte uma vez |
| `CloseEncounter(encounterId)` | Fecha admissão futura e cancela reservas ainda não executadas após a fronteira D7 |
| `EndSession(sessionId)` | Invalida contexto, aplicações e inscrições pertencentes à sessão |

Adapters sem sessão de run devem conservar o comportamento suportado pelas fachadas existentes sem criar inventário global. Test rigs podem injetar sessão local explícita; não usar bypass de aquisição como evidência de progressão real.

## Data Models

### Autoria e identidade

Catálogo usa listas de entradas `[Serializable]` e campos `[SerializeField] private`. Não serializar dicionários de runtime. IDs estáveis: `ignite`, `frost`, `conductor`, `detonation`, `reactor`, `resonance`, `overflow`. Todos possuem **rank máximo 3**, para a fase manual e ofertas universais legadas desses IDs; esta limitação é mudança explícita no catálogo legado, sem alterar os demais boons. Sem persistência entre runs.

Validar unicidade, sete entradas, enums conhecidos, coeficientes finitos/não negativos, durações/intervalos positivos, chances em [0,1], ranks inteiros positivos e caps coerentes. Rejeitar erros; não usar clamp para esconder autoria inválida. Snapshot validado no começo da run; edição de asset durante Play não muda a sessão.

`ProtocolRootContext`: SessionId, EncounterId, RootId, ProducerKind, ExecutionId, ranks congelados, Budget, fila, conjunto de mortes e reservas. `ProtocolTargetHandle`: identidade estável do Actor + Generation. `ProtocolApplicationHandle`: SessionId + TargetHandle + ApplicationId + Revision. Não usar somente referência de componente, posição ou instance ID como identidade de efeito atrasado.

Receipts carregam dano de HP efetivamente confirmado, categoria existente, autor, alvo, ordem e origem. Dano solicitado e dano confirmado são campos distintos. Ranks adquiridos depois de BeginRoot não modificam seus descendentes; novas raízes usam o novo inventário. Burn captura DPS/rank na aplicação; renovar pela nova raiz pode aumentar intensidade, sem recalcular ticks retroativamente.

### D1 — Progressão e tickets: quatro aquisições normais, cinco no Elite

**Mudança explícita da Fase 4:** acrescentar uma escolha individual de Protocol depois da Technique de C1, C2 e C6. Manter a escolha existente em C5/E2, a Mutation de C4, a cura RP, a quantidade de Techniques de E1 e todos os nós, conexões, combates e HP persistente. Não aplicar pacotes de IDs sob uma única escolha.

| Percurso | Sequência de oportunidades de Protocol | Total |
| --- | --- | --- |
| RP → C5 ou C3 → C5 | C1, C2, C5, C6 | 4 |
| RP → E1/E2 ou C3 → E1/E2 | C1, C2, E2, E2, C6 | 5 |

E2 passa a conceder **duas escolhas individuais de Protocol**, preservando vantagem quantitativa do Elite. E1 mantém suas duas Techniques. Cada escolha possui ticket e ordinal próprios; não duplicar evento de conclusão do encontro.

Evoluir autoria do nó com `RewardSteps`, lista ordenada de categoria e quantidade. Migração: nós antigos sem lista conservam o par Reward/Choices atual; definição publicada é migrada explicitamente para lista. C1/C2/C6: `[Technique×1, Protocol×1]`; E2: `[Protocol×2]`; restantes conservam seus passos. Ordinal é único e crescente dentro do nó, atravessando categorias. O ticket registra categoria do passo, não a propriedade única antiga do nó. Nó permanece RewardPending até finalizar todos os passos, incluindo Mutation efetiva quando obrigatória.

Preservar snapshot/revisão, cartões congelados, alternativa definida, confirmação única e release gate entre tickets. Segundo ticket não pode confirmar por clique/tecla ainda pressionados. Inventário muda a elegibilidade do ticket seguinte, nunca os cartões já congelados sem o processo de revisão explícito existente. Chave de inventário inclui os sete ranks e revisão do catálogo.

Seleção manual determinística, sem reroll e sem inventário de run anterior: prioridade abaixo, remover inelegíveis/duplicatas, preencher até três cartões na ordem estável `ignite, frost, conductor, detonation, reactor, resonance, overflow` com entradas ainda elegíveis. Primeira oportunidade é estritamente limitada aos dois elementos; não preencher com outros IDs.

| Oportunidade global de Protocol | Prioridade de cartões |
| --- | --- |
| 1 (C1) | Ignite, Frost |
| 2 (C2) | Conductor, Detonation, Reactor se elegível; caso contrário Overflow |
| 3 (C5 ou primeiro E2) | Elemento ainda não adquirido; Overflow; Resonance se elegível, senão Detonation ainda não adquirido, senão Conductor ainda não adquirido |
| 4 (C6 normal ou segundo E2) | Resonance, Reactor, Detonation |
| 5 (C6 Elite) | Protocolos adquiridos abaixo do cap em ordem estável; preencher com elegíveis |

Pré-requisitos: Ignite/Frost/Conductor/Detonation nenhum; Reactor exige Ignite; Overflow exige Ignite ou Frost; Resonance exige Ignite ou Frost **e** Conductor ou Detonation. Remover ofertas no cap. A primeira oportunidade nunca produz alternativa por ausência de elemento elegível numa sessão nova válida. Catálogo legado conserva escolha genérica e esses pré-requisitos, com novos caps explícitos.

Provas construtivas no caminho normal, a executar pela UI/tickets reais:

| Composição | Escolhas nas oportunidades 1–4 |
| --- | --- |
| Ignite + Conductor | Ignite → Conductor → qualquer elegível → qualquer elegível |
| Ignite + Reactor + Detonation | Ignite → Reactor → Detonation → qualquer elegível |
| Frost + Detonation | Frost → Detonation → qualquer elegível → qualquer elegível |
| Dois elementos + Conductor + Resonance | Ignite → Conductor → Frost → Resonance |
| Elemento + Overflow | Frost → Conductor → Overflow → qualquer elegível |

Todos os sete IDs aparecem em pelo menos uma sequência válida. Os cartões de rota apresentam recompensas ordenadas, inclusive `1 Technique + 1 Protocol` e vantagem `2 Protocols` de E2. A expansão muda o ritmo de decisão/build e exige playtest; não presumir o tuning da Fase 4 válido para o novo poder.

### D2 — Contratos e fórmulas iniciais

`r` é rank congelado na raiz; `d` é dano de HP confirmado do produtor imediatamente anterior; `s` é quantidade de Burn/Chill ativos no snapshot relevante (0–2). Resistência/mitigação de Actor continua aplicada; Protocol não atravessa invulnerabilidade. Descendentes não rolam crítico adicional nem reaplicam multiplicadores diretos de arma.

| Protocol | Efeito inicial |
| --- | --- |
| Ignite | Burn por 4s; DPS base `6r`; ticks a cada 0,5s |
| Frost | Chance `min(0,35r,1)` por impacto elegível; aplicação nominal 0,9 por 2,5s, submetida à política de resistência abaixo |
| Conductor | Até `r` vizinhos por emissão, raio `3+0,5r` metros; dano base `0,45d` |
| Detonation | Uma explosão por morte elegível; raio `3+0,5r`; dano base por vítima `d×(0,75+0,25(r−1))` |
| Reactor | DPS de Burn na aplicação `6×rankIgnite + 0,12r×d`; não amplifica novamente o próprio tick |
| Resonance | Dano secundário elegível multiplicado uma vez por `1+0,4r×s` |
| Overflow | Um burst por receipt elegível sobre alvo vivo com elemento após aplicação; base `0,5r×d`; não consome/renova status |

Burn reaplicado mantém o maior DPS e renova tempo restante para pelo menos 4s; não soma DPS de impactos nem reinicia o relógio do tick. Primeiro tick ocorre após 0,5s, corrigindo explicitamente o timer inicial imediato/default mínimo observado no componente atual. Nenhum tick ocorre com delta escalado zero ou após expiração. Stall processa no máximo um tick por aplicação por frame, sem recuperar todos os ticks perdidos.

Frost usa sorteio injetável para testes, uma vez por receipt. Aplicação respeita controles da Fase 2: se congelamento não for permitido, converter a slow limitado pelo perfil. Perfil inicial proposto: Fragile/Grunt slow nominal e freeze até 2,5s; Heavy slow máximo 0,5 sem freeze; Elite 0,35 sem freeze; Boss 0,15 sem freeze. Não sobrescrever NavMeshAgent.isStopped ou restaurar speed ignorando outro proprietário de controle. Essa adaptação ao antigo Frost exige testes de rank/resistência e é mudança documentada, não reabertura geral da postura. Produzir Burn e Chill não causa, por si, dano de postura ou energia.

Choque térmico conserva dois gatilhos distintos: fogo aplicado sobre Chill prévio e frio aplicado sobre Burn prévio. Cada um pode gerar `1,5d` uma vez por receipt elegível; até dois quando ambos preexistem. Cada choque reserva impacto e recebe Resonance uma única vez antes de TakeDamage. Remover a sequência atual de causar choque e depois reportar o dano como outro impacto amplificado: são **um** choque e **um** receipt, sem segunda aplicação disfarçada de amplificação.

Detonation: snapshot com Chill tem precedência, inclusive Burn+Chill; Shatter atinge até seis vítimas, com 3–6 traços apenas cosméticos sujeitos ao cap visual. Burn sem Chill gera Combustion com raio ×1,3. Sem elemento, explosão comum. Regra de dano permanece única; variante não adiciona ataques invisíveis.

### D3 — Raiz e efeitos futuros

Uma execução de básico cria uma raiz; todos os alvos desse básico a compartilham. Uma execução de habilidade cria uma raiz, compartilhada por suas etapas e impactos. Descendentes de Technique/Mutation, ecos e zonas criados por essa execução retêm a mesma raiz, inclusive pulsos atrasados. IDs de RootExecution existentes devem ser transportados, não substituídos a cada alvo. Arco/Lança legados recebem adapter de identidade por execução/projétil: irmãos de uma mesma salva compartilham a origem; projéteis não relacionados possuem origens distintas.

Criar raiz antes do primeiro receipt e fechar admissão quando terminar a ação produtora. Reservar filhos antes de agendar; cada filho libera sua reserva em finally, ao completar/cancelar. Raiz só é reciclada após fila vazia, reservas zero e nenhum lote confirmado aberto. Dano de arma continua mesmo quando orçamento de Protocol acaba: suprimir apenas trabalho derivado.

Uma aplicação Burn é um produtor periódico autorizado, não uma raiz eterna. Cada ordinal de tick cria uma raiz legítima própria, ranks capturados na aplicação e autoria da sessão original. Esse tick não reaplica elementos, não ativa Conductor/Overflow/choque, mas morte confirmada pode iniciar Detonation. Descendentes da explosão usam o orçamento desse tick, nunca outro root. Renovação mantém um único relógio e ordinal crescente. Limites agregados continuam valendo para ticks de muitas aplicações.

Nenhum Protocol desta fase agenda explosão/cadeia atrasada: a fila é drenada sincronamente no lote. Efeitos visuais podem sobreviver ao fechamento, sem lease de combate. A arquitetura admite filhos atrasados existentes da arma com reserva e validação; não criar novas mecânicas assíncronas para justificar a arquitetura.

### D4 — Elegibilidade por produtor

`Sim` exige dano positivo confirmado, alvo/identidade válidos e orçamento. Detonation exige morte causada pelo receipt daquele produtor, com crédito preservado. Amplificação nunca torna um efeito elegível para energia.

| Produtor | Elementos | Conductor | Detonation na morte | Choque térmico | Overflow | Resonance | Energia Asura |
| --- | --- | --- | --- | --- | --- | --- | --- |
| Básico/etapa nativa de habilidade | Sim | Sim | Sim | Sim | Sim | Não | Somente política nativa da Fase 3 |
| Impacto derivado de Technique/Mutation | Sim | Sim | Sim | Sim | Não | Sim | Não |
| Eco de arma | Sim | Sim | Sim | Sim | Não | Sim | Não |
| Pulso de zona persistente | Sim | Sim | Sim | Sim | Não | Sim | Não |
| Conductor/Detonation | Sim | Sim | Sim | Sim | Não | Sim | Não |
| Choque térmico | Não | Não | Sim | Não | Não | Sim | Não |
| Overflow | Não | Não | Sim | Não | Não | Sim | Não |
| Tick Burn | Não | Não | Sim | Não | Não | Não | Não |
| Dano ambiental/autor desconhecido | Não | Não | Não | Não | Não | Não | Não |

Mutation que substitui uma etapa nativa preserva sua classificação real da Fase 3; o nome Mutation não converte automaticamente toda etapa em secundária. Técnica modificadora do golpe também não muda sua origem. Só novos impactos derivados usam a linha secundária. Guard Impact e demais produtores causais devem ser mapeados individualmente na auditoria; nenhuma origem fica elegível por simplesmente passar em `PlayerActor.ResolveAttackDamage`.

Visita de propagação: uma vítima recebe no máximo um impacto de cadeia/explosão por raiz, combinado entre ambos; receipts nativos diretos não gastam essa visita e podem receber os bursts locais permitidos. Marcar vítima antes de enviar dano para impedir retorno por reentrada. Deduplicar `(receiptId, efeito)` para choque/Overflow e `(SessionId, TargetId, Generation)` para morte. Alvos selecionados por distância, desempate por identidade estável; candidatos filtrados por equipe, vida, encontro e linha de visão. Fonte morta usa posição capturada, não consulta transform reciclado.

### D5 — Ordem de processamento e snapshots

1. Validar raiz/sessão/encounter/geração, produtor, números e deduplicação. Capturar status prévios e posição da vítima antes de dano/cleanup.
2. Para secundário elegível, aplicar Resonance uma única vez a partir dos status **anteriores** ao impacto. Para nativo, usar dano já calculado pela arma; não reaplicar críticos/multiplicadores.
3. Confirmar dano de HP pelo adapter e preservar notificações/feedback/energia nativa. Dano zero não ativa Protocolos.
4. Se vítima morreu, capturar DeathReceipt com status prévios e dano letal confirmado, e registrar morte. Não instalar status novos num cadáver.
5. Se viva e produtor permitir elementos, aplicar Burn e sorteio Frost; Reactor usa `d` dessa confirmação e ranks da raiz. Capturar status posteriores.
6. Enfileirar choques a partir de elementos efetivamente aplicados e status prévios. Em seguida, enfileirar Overflow elegível a partir dos status **posteriores**, incluindo elementos do próprio impacto; conservar timers.
7. Enfileirar emissão Conductor para receipt elegível e registrar Detonation se houve morte. Drenar FIFO: bursts locais precedem a emissão derivada do mesmo receipt. Se um burst matar o alvo, reclamar sua morte e cancelar apenas bursts ainda não confirmados para ele.
8. Para morte por burst, Detonation captura status anteriores ao burst que matou. DeathReceipt é imutável e publicado antes de destruir statuses/reutilizar Actor. O snapshot não é reconstruído consultando componentes após cleanup.

Cada efeito calcula base a partir de seu receipt pai; explosão usa dano confirmado do receipt letal (não vida máxima, overkill solicitado ou um dano anterior). HookBus recebe morte/explosão uma vez com origem/contexto; não chamar duas fachadas de TakeDamage para o mesmo efeito. Esta ordem altera explicitamente o caminho antigo de choque e elimina a aplicação dupla via ReportImpact, sujeita a regressões relevantes.

### D6 — Orçamento e contenção

Limites iniciais por runtime local; incluem todas as origens secundárias, com e sem Resonance. Os antigos 32/profundidade 4 permanecem como limites por **execução compartilhada**, não por chamada em cada alvo. Valores devem ser revistos apenas com evidência registrada.

| Recurso | Limite e regra |
| --- | --- |
| Impactos derivados por raiz | 32 tentativas reservadas antes do dano; dano zero/mitigado também gasta tentativa |
| Profundidade | 4 arestas: nativo/tick em 0; secundários em 1–4; profundidade 4 aplica dano, sem novos filhos |
| Fila de Protocol por raiz | 64 itens; reserva recusada quando cheia, sem alocação extra ou adiamento |
| Reservas de produtores atrasados | 32 por raiz; excedente continua contrato de dano da arma, mas não produz novos Protocolos |
| Raízes Protocol simultâneas | 64; root excedente não ativa efeitos, sem bloquear ataque nativo |
| Impactos derivados agregados | 256 tentativas por frame, compartilhadas por raízes/ticks |
| Consultas agregadas | 64 OverlapSphere por frame; até 128 colliders por consulta e 128 testes de linha de visão |
| Aplicações elementais owned | Até 256 por tipo por jogador/sessão; renovar existente antes de tentar nova reserva |
| Burn ticks | Até 64 por frame; round-robin por ApplicationId evita starvation; no máximo um por aplicação/frame |
| VFX Protocol | Até 24 ativos e 16 novos por frame; máximo 0,6s escalados por instância |
| Root lifetime | Até 10s escalados para admissão Protocol; expiração não cancela dano/recursos já confirmados da arma |

Excesso é descartado de forma previsível no ponto de reserva; não gerar dano posteriormente na sala seguinte e não fabricar outro root para contornar o cap. Tick sem capacidade descarta aquele ordinal e avança o relógio, sem dívida de dano. Percorrer round-robin a lista local para ticks elegíveis; aplicação removida libera capacidade. Buffer de overlap cheio não inicia busca ilimitada adicional; selecionar deterministicamente dentro do conjunto retornado e registrar saturação.

Relógios gameplay são escalados (`deltaTime`), incluindo Burn, Chill, filhos e TTL: pausa/hitstop congelam avanço. Frames com delta zero não processam ticks nem trabalho gameplay novo. Medição/telemetria e timeout de harness usam tempo real; orçamentos agregados por frame não são renovados por root ou reentrada. Não esperar tick cosmético para finalizar encontro. Telemetria bounded acumula contadores de impacto, fila máxima, drop por motivo, consultas, raízes, ticks e VFX; não logar por impacto.

### D7 — Fronteira de encontro e cleanup

Última morte abre candidato de encerramento conforme Fase 4. Durante o lote já iniciado, concluir receipts, mortes derivadas e notificações confirmadas; preservar leases de feedback relevantes. `ProtocolRuntime` participa da consulta de lote aberto ao diretor. Raiz viva com reserva futura não equivale a lote de dano confirmado aberto.

Após resolver o lote e confirmar zero inimigos, fechar admissão do encontro, cancelar filhos/ticks futuros daquele encounter, drenar somente notificações já confirmadas e liberar reservas. Preservar categorias, energia, epoch e hitstop existentes; não usar Configure(null), reset global ou destruição do jogador para acelerar o fechamento. Só então apresentar ticket. Se jogador morrer no mesmo lote, morte tem prioridade e não há aquisição.

Não esperar VFX, timer de Burn ou todo TTL de root; não executar futuras ondas de dano em sala fechada. Cleanup remove apenas aplicações owned daquele encounter/sessão e libera movimento conforme os proprietários de controle existentes. Outras fontes de status não são apagadas como efeito colateral.

Antes de cada tick/filho validar SessionId, EncounterId, TargetId, Generation e ApplicationId/Revision. Actor reutilizado na mesma sessão tem geração nova; callback antigo vira no-op. Incremento da geração pertence ao lifecycle explícito do Actor/adapter, com inicialização mesmo sem pooling atual. Finalização idempotente da run cancela filas/inscrições e invalida aplicações; próxima run começa com inventário zero. Mudança de sala conserva inventário e recursos, sem reaplicar aquisição.

### D8 — UX e aceitação mensurável

Cartões: nome familiar + rótulo Protocol, rank atual→novo, benefício numérico, gatilho, dependências e `não gera energia Asura`. Reactor informa DPS adicional por dano confirmado; Resonance informa leitura dos elementos prévios; Overflow diz explicitamente que considera elementos do próprio acerto e **não consome** status. Não anunciar freeze garantido com chance 35% ou freeze de Boss. Detonation apresenta variantes sem confundir traços cosméticos com golpes.

Choice screen mostra progresso do nó (`Technique 1/1 → Protocol 1/1`, `Protocol 1/2`), conserva M para fechar/reabrir e não faz reroll. Rota explica recompensas múltiplas e vantagem Elite antes da escolha. Cor é apoio: fogo quente, frio azul, cadeia ciano, explosão com expansão distinta; geometria/movimento também distinguem, sem depender apenas de cor. Priorizar telegraphs inimigos acima dos VFX de Protocol; overflow/choque usam burst curto no alvo sem flash de tela.

#### Performance reproduzível — alvo inicial

Implementar harness local com seed fixa e configuração salva, execução via Unity MCP. Duas medições: Editor para regressão funcional e Development Player quando suportado para aceitação de custo; Editor isolado não comprova custo de build. Perfil de referência mínimo proposto: Windows x64, CPU 6 núcleos/12 threads, 16GB RAM, GPU equivalente a GTX 1660, 1080p; registrar modelos reais, versão Unity, backend, build, qualidade e resolução. Hardware diferente gera resultado contextualizado, sem fingir equivalência.

Warm-up 10s, coleta 60s, três repetições por cenário. Baseline e combinação usam mesmos inimigos, seed, ações e duração; cenário sustentado repõe alvos pela geração controlada. Densidades 8, 32 e 64; combinação de stress todos os sete rank 3 via fixture permitida **somente para stress**, além de uma build alcançável rank 1. Testar limites separadamente com 256 aplicações e 65 raízes. Executar uma segunda série VFX desligados para separar custo lógico e apresentação.

Metas iniciais: custo CPU dos marcadores Protocol p95 ≤1ms e p99 ≤2ms nas densidades até 64; GC alocado pelos marcadores de resolução ≤1KB/frame p95 após warm-up, sem crescimento monotônico de memória/coleções após 60s; frame total p95 ≤16,7ms no hardware de referência e aumento p95 ≤2ms contra baseline. Separar falha do baseline de regressão Protocol: se baseline já exceder 16,7ms, meta absoluta fica pendente e ainda avaliar incremento. Registrar raw samples, percentis, caps e drops; médias não substituem p95/p99. Metas ainda não medidas e ajustáveis por decisão explícita, sem redefinir silenciosamente um resultado reprovado.

#### Gameplay humano

Executar pelo menos duas runs com a mesma Manopla e escolhas de Protocol diferentes. Registrar oportunidades/tickets, ramos, ranks, Mutation, HP, resultados e três situações concretas: mudança de posicionamento, prioridade de alvo ou sequência de habilidades causada pela combinação. Exemplo: preparar alvo central com Burn e eliminá-lo para Combustion; espalhar Chill antes de cadeia amplificada; perseguir nova aquisição em vez de aumentar rank. Dano maior sozinho não satisfaz composição emergente.

Em pelo menos três encontros por build, avaliador identifica a causa dominante da cadeia/explosão e consegue ler um telegraph inimigo durante a combinação; registrar acertos/erros e vídeos quando disponíveis. Qualquer ocultação recorrente de telegraph, freeze indevido ou perda de responsividade é pendência. Esse gate é avaliação humana com observações, não assertion de teste automatizado.

## Error Handling

- Catálogo inválido: diagnóstico com ID/campo; não iniciar runtime de Protocol nem normalizar erro. No modo manual, preservar ticket/estado de recuperação, sem conceder rank parcial ou deixar nó silenciosamente preso.
- Falha de preflight de aquisição: nenhuma mutação. Commit é operação local não reentrante; falha inesperada após início marca faulted, não repete efeitos. Notificar diretor/UI com diagnóstico existente de recuperação.
- Contexto/target/aplicação obsoleto: descartar e contabilizar motivo, sem alterar alvo reutilizado.
- Cap excedido: suprimir derivado, manter confirmação do dano pai e progresso; não gerar spam de Console ou recuperar dano num tick futuro.
- Dependência obrigatória ausente: validar em Awake/bootstrap; não buscar jogador por nome ou singleton. Interfaces de teste injetam dependências explícitas.
- Exceção em fila: liberar reservas em finally, registrar falha e não reaplicar receipt; impedir aquisição até a fronteira confirmada ficar consistente.

## Correctness Properties

| Propriedade | Contrato verificável | Requirements |
| --- | --- | --- |
| P1 — Catálogo válido | Sete IDs únicos, números válidos, save/reload fiel, rank ≤3 | R1, R9.6 |
| P2 — Aquisição única | Um ID/rank por ticket; ordinais/categorias distintos, fechamento sem reroll | R2.5–R2.7 |
| P3 — Alcance publicado | Sequências D1 obtêm os sete IDs/composições em todas as topologias pertinentes | R2.1–R2.4, R5.7, R10.4 |
| P4 — Status estável | Renovação não duplica tick; relógio escalado; rank/resistência respeitados | R3, R5.1 |
| P5 — Amplificação única | Resonance uma vez; choque sem segundo dano; Overflow conserva timers | R5, R6.8 |
| P6 — Mortes únicas | Snapshot antes de cleanup; uma detonação por identidade/geração | R4.3–R4.5, R9.4 |
| P7 — Orçamento compartilhado | Multialvo/etapas/filhos dividem root; todos os derivados contam | R6.2–R6.3, R7.1–R7.5 |
| P8 — Origem preservada | Protocols/DoT/eco não geram energia nativa; hooks mantêm autoria | R6.1, R6.4, R9.5 |
| P9 — Encerramento finito | Espera lote/feedback confirmado; cancela futuro, não espera VFX | R6.5–R6.7, R9.7 |
| P10 — Sessão e pooling | Callback antigo não afeta novo encounter/aplicação/geração/run | R9.1–R9.4 |
| P11 — Apresentação verdadeira | Cartões, pré-requisitos, ranks, tickets múltiplos e efeitos correspondem | R8 |
| P12 — Aceitação separada | Medições reproduzíveis e gameplay documentados, sem aprovação fictícia | R7.6–R7.7, R10 |

## Testing Strategy

**EditMode:** helpers de catálogo/validação bruta, elegibilidade e sequences D1, migração RewardSteps, ranks, seleção até três cartões, fórmulas, ordem de snapshots, visited/death dedup, root/reservas, NaN/infinito, caps/round-robin e identidade. Propriedades geradas com seed registrada quando útil; não espelhar loops internos como prova de comportamento.

**PlayMode:** dano real de todos os produtores D4; AttackDamageBatch/transação multialvo; golpes/etapas e ecos/zonas assíncronos; Burn e atualização de rank em curso; choque/Overflow com/sem Resonance; Detonation no dano direto, secundário e tick; Burn+Chill e morte com cleanup imediato; pause/hitstop; resistência dos cinco ranks; Actor reutilizado; root cheio e buffers saturados. Conferir dano efetivo, energia e feedback, não somente contadores.

**Integração publicada:** carregar Nexus/FirstSector pelo fluxo real, escolher os novos tickets sequencialmente, provar as sequências D1 sem injetar inventário e atravessar os quatro percursos. Setup determinístico de inimigos é permitido em testes, declarado como fixture; aquisição nunca usa ApplyOffer direto nessa prova. Verificar Mutation, HP, dois tickets E2, UI/release, segunda sala e última morte causada por cascata sem bloqueio. Modo legado de Bow/Spear deve continuar selecionado e seus produtores manter hooks/classificação, sem rework de gameplay.

Regressões dirigidas: famílias existentes `DetonationCascadeBoundsTests`, `GlobalCascadeBoundsTests`, `ReactorBurnScalingTests`, `IsolatedBurnTickTests`, testes de Overflow/choque/Resonance, feedback nativo e tickets manuais; confirmar nomes/disponibilidade ao elaborar tasks. Se assertion refletir contrato legado que este design altera explicitamente, registrar migração e manter cobertura do contrato novo; não enfraquecer teste correto para esconder implementação defeituosa.

Para cada alteração Unity: confirmar MCP conectado ao projeto correto, capturar Console inicial, importar/aguardar compilação, inspecionar Console sem limpar, executar somente testes pertinentes e runtime suportado. Para autoria, salvar/recarregar catálogo e definição, conferir GUIDs/referências. Registrar jobs/resultados reais e sair de Play preservando cena. Performance/humano são gates separados, conforme D8; indisponibilidade de MCP ou profiler deixa checks pendentes, não aprovados por inspeção estática.

## Migration and Handoff

Mudanças de contrato desta fase: D1 amplia aquisições mantendo topologia; RewardSteps generaliza tickets por categoria; D2 limita ranks, altera tick inicial e adapta Frost a resistências; D5 resolve choque com amplificação única; D3/D6 compartilham limites e classificação de todas as origens. Cada mudança deve constar na task de migração correspondente e em `validation.md` futuro com resultado observado. Não atualizar retroativamente a Fase 4 como se esses contratos já estivessem implementados/aceitos.

Preservar GUID da FirstSector, definição/prefab/NavMesh publicados, IDs dos boons e caminho dos Resources. Não editar terceiros. Mudanças de campos serializados usam fallback/migração verificável; migração não altera assets em OnValidate. A implementação deve atualizar UI e referências somente no escopo afetado.

**Próxima etapa:** executar o plano incremental em `tasks.md`, começando pela auditoria de produtores e baseline. Este documento serve como registro das decisões e sua justificativa; a elaboração do design/plano não executou implementação nem validou os contratos em Unity.

