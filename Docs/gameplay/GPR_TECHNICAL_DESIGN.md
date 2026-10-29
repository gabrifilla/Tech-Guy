# Gameplay Rework — desenho técnico mínimo

## Estado vigente — GPR-01.2b, 28/10/2026

O desenvolvedor testou fisicamente a onda instantânea anterior: funcionalidade técnica aceita, resultado de gameplay considerado pouco transformador e rejeitado como design final. A revisão autorizada substitui essa onda por um **tornado de vento móvel**, mantendo o ID da Mutation, aquisição, controles e combate nativo. A evidência de GPR-01.2 abaixo permanece histórica.

Cada terminal realmente executado no contato do scheduler, com A no snapshot aceito, autoriza no máximo um tornado **inclusive em whiff**. Pedido/cancelamento pré-contato e estágios intermediários não emitem. Contrato específico de emissão separado de `ReserveConfirmedBasicChild`, cujo requisito de HP confirmado permanece intacto. Emissão não produz HP/estatística/recurso nem nova raiz; impactos posteriores usam a fila e os receipts existentes, mesma raiz/parent e caps. Política do ramo: `ProtocolBasicEffects.None`.

Definição experimental única: deslocamento de 5,5 m a 3,5 m/s, vida máxima escalada de 2 s, raio 0,65 m, altura 1,8 m e offset inicial 0,75 m. Direção comprometida não acompanha mouse/rotação posterior. Dano = 0,60 × `CaptureAttackDamage` não crítico capturado na aceitação do terminal (dano authored + stats/Momentum elegíveis naquela aceitação); não depende de receipt, não sorteia crítico nem reaplica multiplicadores do atacante. Mitigação/HP seguem o alvo. Uma vítima por handle+generation, mantendo a exclusão das vítimas nativas já confirmadas.

Overlap inicial + capsule sweep por passo; sólidos e LOS bloqueiam. Reservas negadas/saturação encerram com segurança. Pausa/hitstop congelam; troca de arma/contexto, fim da run, disable/morte e cena invalidam/limpam reservas e apresentação. VFX reversível, pool limitado e leases por geração, sem Rigidbody/segundo executor/alteração permanente de prefab.

Implementação e validação desta revisão registradas no [relatório GPR-01.2b](../decisions/GPR-01.2b_RELATORIO.md). **Aceite físico do tornado novo pendente. STOP antes de GPR-01.3**: somente recomendar revisão da composição com Conductor após avaliação do desenvolvedor; nenhum grant novo de Conductor/Detonation, swap modular, tuning nativo ou quarta skill implementado.

### Fechamento técnico da GPR-01.2b

**CONCLUÍDA TECNICAMENTE NO ESCOPO TESTADO; HUMANO PENDENTE.**29/29 focados (28 da Mutation+1 interação),90/90 EditMode e187/187 regressões:305 nomes distintos finais, zero ignorados; não suíte global. Focados joba0ca7eec1c9f48668c0bcf4601e6b9ed; EditMode4b30bf2757f3424eb46e2d268dd40f82; regressão02090688af2f415ea00dbd3b27cb386f. Falhas intermediárias, correções de fixtures e intermitências históricas registradas no relatório, sem relaxar asserts/clocks.

- [x] Separar contato terminal/whiff de receipt positivo sem relaxar reserva confirmada global.
- [x] Tornado progressivo, direção capturada, pacote não crítico estável, sweep/LOS, caps/dedupe e cleanup.
- [x] Pool/VFX reversíveis; captura Game View em6 quadros eCSV; aquisição por ticket preservada.
- [x] Regressões de controles/GI/armas e integridade de assets/evidência histórica no alcance declarado.
- [ ] Revisão física do tornado pelo desenvolvedor; sensação/arte/balanceamento e carga em swarms pendentes.
- [ ] Autorizar separadamente GPR-01.3 antes de compor Conductor; nenhuma composição nova habilitada.

## Histórico — estado GPR-01.2 anterior à revisão

### Registro histórico — GPR-01.2, 28/10/2026

GPR-01.1 aceita tecnicamente pelo desenvolvedor; GPR-C01 continua encerrada. Autorizada somente a primeira Mutation jogável **Punhos Titânicos**. Implementados catálogo Basic da Manopla, onda na raiz nativa com HP confirmado e grants `ProtocolBasicEffects.None`, sobrepeça reversível e testes focados. Implementação concluída com ressalva de regressão:18/18 focados e90/90 EditMode;187/187 na última bateria ampliada.295 casos distintos finais, com ocorrências intermitentes anteriores de hitstop/burn registradas e não declaradas corrigidas. Zero ignorados; não confundir automação com aceite humano. [Relatório da atividade](../decisions/GPR-01.2_RELATORIO.md).

Parâmetros experimentais: caixa frontal de 1,5× o alcance/largura efetivos do soco, altura preservada; 0,60× o maior dano pré-mitigação entre receipts nativos positivos do terminal, coeficiente aplicado uma vez. Sem tuning do básico/Asura/QER, nova ação, recurso, postura ou grant Conductor/Detonation. Nova aquisição usa os tickets/catálogos existentes; nenhuma política de recompensa ampliada.

**STOP após GPR-01.2. GPR-01.3 e etapas seguintes não autorizadas.** Revisão humana da mutação e de seu visual permanece pendente; moonwalk/cadência e swap modular continuam diferidos. As seções anteriores preservadas abaixo são históricas onde divergirem deste estado.



## Execução vigente de Punhos Titânicos

SnapshotBasic no commit → batch nativo/HP positivo → reserva única None na raiz atual → espera por clock válido → query frontal1,5× → QueueDamage/DrainDamage existente com token verificado em cada commit → HP/analytics existentes. SourceDamage registra o pacote pré-mitigação; a onda usa0,60× do máximo confirmado, sem nova rolagem/modificador do atacante. Native victims e gerações repetidas são excluídas; sem postura/recurso. A consulta de capacidade é somente leitura: uma onda autorizada sem nova vítima pode ser mostrada sem consumir quota de dano; orçamento/reserva negados não mostram a onda.

Overlay local à mão e um renderer reutilizado; nenhuma referência de cena/prefab ou animação modificada. Troca/cleanup revogam a fila, inclusive em callbacks reentrantes. Uma raiz mantida por queue items não substitui uma reserva válida. Composição futura precisa propagar a autorização/cancelamento aos descendentes; não ativada em01.2.

## Histórico da entrega GPR-01.1 — superado pelo estado vigente acima

GPR-C01 permanece **COMPLETA e aceita pelo desenvolvedor**; GPR-01.P1 aceita como planejamento. LMB básico, RMB especial permanente e exatamente três modulares Q/E/R, sem X. Moonwalk/cadência continuam refinamentos futuros não bloqueadores, sem tuning nesta etapa.

Direção experimental A→A+B→A+B+C aceita, parâmetros1,5×/0,60× provisórios; autorizada **exclusivamente GPR-01.1**. [Relatório da fundação](../decisions/GPR-01.1_RELATORIO.md) / [fatia e próximo escopo](GPR_01_VERTICAL_SLICE.md). Sem asset/boonTitanic Fists habilitado, onda ou transformação visual de produção. STOP para revisão antes de01.2;01.3–6 também exigem autorização posterior.

Q/Avanço para aproximação é compatibilidade de ações, não sinergia mecânica nova. SwapAvanço→Uppercut adiado por redução aprovada de escopo; aquisição/substituição modular Q/E/R permanece requisito **NÃO IMPLEMENTADO/NÃO VALIDADO**, rastreado em GPR-03.1/RF-TROCA. Gates e decisões históricos abaixo não sobrepõem esta autorização.

## Desenho proposto da fatia atual

Contrato detalhado, seams, parâmetros e gates em [GPR_01_VERTICAL_SLICE.md](GPR_01_VERTICAL_SLICE.md). Reusar BasicImpact/scheduler/ledger/PlayerActor/BreakerGauntletCombat/ProtocolRuntime/RunBoons. Os gaps concretos são Basic no grafo, autorização de filho por receipt, whitelist por linhagem e aquisição sem reset global de timers. Nenhum novo pipeline/manager de combate, mutação em assets originais ou mudança em Asura.

A recomendação de Basic=4/flag16 é extensão de alvo de boon, com mapping explícito para GauntletActionSlot.Basic=-1; Any15 legado permanece. O desenho não cria mais uma skill modular. Índices0/1/2 são Q/E/R físicas e3 é especial. ProtocolEligibility admite cascatas no código atual: restringir somente o novo ramo A requer decisão/implementação específica, não pressuposto falso de que Conductor já pare sozinho.

Troca Avanço→Uppercut, transação de swap e visual de eco descritos no histórico abaixo deixam de ser obrigação desta primeira fatia proposta. O refresh de aquisição existente ainda exige um ajuste limitado para preservar timers sem implementar swap universal.

## Desenhos históricos — aplicar somente onde compatíveis

## Histórico aprovado C01.4 — três skills + especial,28/10/2026

A decisão direta mais recente **substitui todo modelo de quatro slots modulares Q/E/R/X**: exatamente três slots modulares Q/E/R e um especial permanente separado no RMB. X não tem ação/placeholder; index3 e arrays de quatro referências permanecem compatibilidade interna, nunca uma quarta skill modular. Asura mantém executor, custos, duração, cooldown, animação, danos e continuação; F11 não é alias direto. Bow/Spear reutilizam sua quarta ação existente no canal especial, sem mecânica/asset novo. A futura aquisição/substituição é restrita aos três slots e continua fora de C01.4.

WASD e demais ações configuráveis no Settings; InteractG dedicado e finisherF distintos por padrão, sem sobrescrever personalizações. Dash padrãoCursor; opçãoMovimento usa vetor configurado/câmera normalizado e **facing atual quando não há direção pressionada**, conforme esclarecimento posterior do desenvolvedor. A sugestão histórica de fallbackcursor no modoMovimento está superada. Escape reservado à pausa/cancel seguro. Migração só lê/repara em memória, Apply explícito grava nomes de ações; sem reset de progresso.

Registro histórico de C01.4: implementação e116 casos cumulativos concluídos antes do aceite físico. A pendência humana foi encerrada por C01.5; o estado vigente e o novo STOP de GPR-01 estão no início deste documento.



## Histórico C01.3b — fechamento na data,28/10/2026

**CONCLUÍDA TECNICAMENTE:105/105 casos distintos finais, zero ignorados**, via Unity MCP no checkout correto/Editor6000.5.10f1. Aceite humano anterior das últimas correções recebido para WASD, básicos, Q/E/R e Asura/RMB; básicos rápidos ficam como refinamento futuro, sem mudar cadência. [Relatório](../decisions/GPR-C01.3b_RELATORIO.md).

Corrigidas fixtures que criavam dispositivos virtuais antes da política de foco, e o gate de soltura que descartava o primeiro comando novo no frame seguinte. Dois testes falharam0/2 antes da correção de produção;22 básicos/input+23 movimento+7 contextos+30 combate+8 dash+15 EditMode passaram após as correções. Ajustado teste Cadenciada para agir no frame de recovery observado, mantendo asserções/tempos. Outputs e falhas anteriores preservados em c01-3b/correcao e gate; não suíte global. Runtime publicado com input virtual:3 básicos0/1/2 e1 Asura por RMB,4 PNG; não novo teste físico.

F11 deixou de ativar Asura direta por decisão explícita; binding salva/executor/custos/estágios preservados. Frame de soltura continua bloqueado; held não atravessa UI/foco/pausa. Editor final NexusLobby limpa/Edit Mode; permanece erro de pacote Tripo3D preexistente. Causa física do incidente RMB antigo não confirmada retrospectivamente; aceite humano posterior inclui RMB funcional.

**Gate de C01.4 satisfeito;4a–4d já autorizadas, ainda não implementadas.** Não iniciar C01.5/GPR-01. Interact separado precisa respeitar F/finisher; X pode ficar vazio, nunca duplicar Asura. Autorizações e evidências3a abaixo são históricas onde conflitam com este fechamento.

## Delta C01.3a — básico direcional e adapter Asura

CharControlScript.DirectBasicInput lê o edge de Primary, exclui UI gráfica/HUD e projeta cursor no plano horizontal. Não consulta Actor/WorldClickResolver. Holder tenta TryDirectionalBasicAttack; impedimento temporal usa o InputBuffer genérico já existente, CommandKind.Basic com direção capturada,120ms/single slot. Isso substitui a hipótese de usar o tap legado sem TTL do BasicAttackDriver para o novo caminho, sem criar outro buffer/driver/executor. Skill/dash/cancel/swap/contexto descartam pendente apropriado. Hitstop preserva validade nos clocks existentes.

Execução e geometria permanecem PerformAttack/BasicImpact/ArsenalProjectile. Cada básico aceito avança somente um estágio; locks de facing e impacto autorado continuam. Bow primário repete por cadência apenas quando ChargedShot tem rank, conforme escolha expressa; não introduzir release-to-fire genérico.

WeaponSpecialBinding usa a binding secundária existente, Mouse1 no perfil observado. Holder identifica AsuraBurst de Manopla no modo direto, combina edges secundário/Skill4 legado numa única entrega a TryUseAbility(3), mantém cooldown/mana/energia/continuações. RMB tem prioridade sobre LMB simultâneo; F conserva prioridade sobre continuação. Controlador exclui o secundário da Manopla do caminho de básico legado; UI impede press e replay. Não remove referência histórica de WeaponScript nem coloca Asura em X. Separação completa de slots é outra etapa.

Controller real inspecionado usa Walk_N/Run_N e punches, sem conjunto completo strafe/backpedal nessa lista. Follow-up mínimo: velocidade efetiva em coordenadas locais de facing e design de blends/clips direcionais, respeitando locks durante ataque. Não girar o corpo para WASD durante o golpe. Nenhuma animação/rig/arte foi substituída.

## Delta implementado C01.2c

`PlayerNavigationMotor.Move` permite até três segmentos apenas em Locomotion: sweep de capsule e Agent.Raycast delimitam avanço; normal horizontal projeta o resíduo sem renormalização. Action/Dash/ExternalPull/Reaction/Boon mantêm truncamento direcional. Mesmo agente/writer, sem CC concorrente, SamplePosition através de paredes ou alteração de valores autorados.

`CharControlScript.GameplayInputReady` é barreira local de entrada no modo direto: contexto bloqueado/foco perdido/disable/init exigem release de WASD e bindings efetivas, depois um frame liberado. Holder verifica antes de aceitar/bufferizar comandos. `DiscardUncommittedInput` elimina intenções, não cast/cooldown; BasicAttackDriver descarta tap/hold mantendo sua cadência. Open de pausa/reward/interação invalida explicitamente para cobrir transição no mesmo frame; os closes existentes reutilizam RequireAttackRelease. Não usar esse gate para bloquear o motor de pull ao desabilitar o controlador.

Animação segue o delta efetivo existente. O feedback físico relata estranheza de caminhar orientado ao cursor: refino de blends/facing não implementado. Default dash para cursor preservado; pedido de opção mouse/WASD registrado para Settings, sem tuning.

Recovery legado mantém lifetime0,35s e clocks atuais. Hitch medido0,516s fez clock0,926 ultrapassar expiração0,760 enquanto delta de ação foi limitado0,333s; intenção vencida não é executada. Caso original falha nesse diagnóstico e permanece inalterado, com trace test-only. Não afirmar correção definitiva da intermitência histórica.

## Implementação C01.2b

CharControlScript parcial mantém o único coordenador. PlayerNavigationMotor recebe intenção dos produtores reais, faz capsule sweep/raycast e Move; fontes enum não são outro executor de combate. Prioridades consultam estados existentes: pull toma dash; root/cast impedem teclado; Bow usa a velocidade reduzida já aplicada; pausa/hitstop/world input suspendem. Body traversal sincroniza apenas a pose validada do agente dentro desse writer para preservar contato imediato. Fallback legado/NPC só executa com modo direto desligado; reposicionamentos de spawn/transição/ragdoll permanecem próprios.

LateUpdate existente de BasicImpact chama amostragem da posição final e conserva a pose de contato. Walk/Idle lê velocidade planar real. CurrentMoveDirection conserva a amostra locomotora anterior no frame de aceitar o básico, permitindo Kiting Step sem confundir impulso de cast/dash com locomoção.

GamePreferences mantém enum e chaves antigas; perfil ativo direto resolve migração pura/customização e cache por frame. Apply validado grava DirectBinding.v2, sem escrita parcial ou regravação do legado. Q/E/R para três skills e F11 do especial no baseline; sem AsuraX/RMB ou slot novo. Title/pausa continuam no mesmo painel; WASD fixas/reservadas aguardam remap completo. Quatro cenas só receberam a flag e builders passam a preservá-la; não houve rebuild de mapa.

## Histórico: resultado experimental C01.2a antes da integração

[Relatório e limites](../decisions/GPR-C01.2a_RELATORIO.md). Assembly exclusiva UNITY_INCLUDE_TESTS, nenhum componente no player publicado. Move cru atravessou parede sem bake; variante candidata faz CapsuleCastNonAlloc e Agent.Raycast antes do único Agent.Move, sem destino de clique, sem Warp durante movimento, sem avoidance autônomo. Rotação independente e vetor câmera/mundo; velocidade5/dash20 por0,25 preservados. Agent.velocity=0 no experimento: parâmetro Animator e direção vêm de deslocamento real pós-movimento. Não transplantar o árbitro sintético como arquitetura paralela: em C01.2b autorizada, adaptar os estados/executores existentes ao único writer e substituir consumidores de velocity onde preciso.

Root/cast, hitstop, pausa/foco e deslocamento forçado foram injetados na fixture; não são integração de combate. Pausa/hitstop congelam dash; foco/disable cancelam o dash da fixture e exigem soltura. Direção de dash cursor é padrão comparativo; variante WASD não adotada. CharacterController isolado bloqueou parede, mas atravessou limite do NavMesh; não recomendado para migração imediata. Overlap inicial/moving geometry/crowd/mapa real/rig e feel permanecem pendentes. Asura/X/especiais Bow-Spear não são dependências deste experimento. C01.2b proposta começa por cópia temporária do player e prova real de ownership, sem novos bindings ou integração publicada antes da aprovação.

## Fronteiras e alternativas

InputAction/maps sobre Input System1.20, com GamePreferences como fachada de compatibilidade, é recomendado. Alternativa: estender KeyCode[] com componentes direcionais/contexts próprios. O segundo produz diff menor, porém exige implementar captura, composites, conflitos e device lifecycle manualmente. Um único leitor fica ativo; nada de callbacks mais polling consumindo o mesmo press. Não adicionar PlayerInput apenas por exemplo de skill; ownership do consumo continua no controlador/holder atuais.

Movimento: o candidato protegido foi integrado em C01.2b após autorização, com medição real própria além da baseline legada. CharacterController continua alternativa não adotada, que exigiria migrar autoridade dash/pull/CC/solo/limites; não deve coexistir como segundo motor. Crowd/obstáculos móveis/overlap inicial e físico permanecem pendentes. Nenhum bake/rebuild de mapa foi realizado.

Input actions são dados, sem dano/custo/timer. CharControlScript/BasicAttackDriver normalizam intenção; AbilityHolder decide habilidade/dash/cancel; scheduler e executores permanecem autoridades da execução. Separar deslocamento e facing: movimento usa vetor da câmera, aim usa cursor no plano navegável; compromisso de swing captura direção e não acompanha mouse após início. Durante casts, executores conservam rotação/deslocamento e devolvem ownership ao fim. Sem zerar ou sobrescrever savedStopped/savedRotation dos executores.

## EspecialAsura separado dos quatro slots — delta de input proposto

O plano de execução detalhado está em GPR_CONTROL_MIGRATION. Preservar executor/scheduler/contextos/energia/duração/cooldown/continuações e F; não chamarTryActivate direto nem duplicarTryUseAbility em RMB/X. Asura é index3 no holder e no Array.IndexOf do executor, com boons/causaR. Proposta: uma referência e um registro do especial no próprio holder; visão modular com quatro entradas separada da referência autorada antiga, identidadeR resolvida por ação. X inicialmente vazio é propostaD15, nunca substituição implícita. PreencherX depois exige identidade/capabilities próprias. Esse adapter é parte de C01.3, após decisão; não sistema de habilidades novo em C01.1.

LMB usa edge→um Request no driver existente, buffer limitado e clocks atuais. Hold legado foi demonstrado e requer política de compatibilidade/ChargedShot explícita; não aprovado como default. RMB sóespecial da arma. Bow/Spear não ganham alias de básico nem novo kit sem design. Rebind inclui piloto de overrides em memória antes de trocarbackend; nenhumSettings implementado nesta etapa.

## Estender composição do básico — GPR-01 futuro, não autorizado

Usar Basic=-1 de GauntletActionSlot como identidade existente; não atribuir uma Mutation do básico ao slot Q ou acrescentar quinto slot ativo. O grafo de boons orientado QWER precisará de escopo Basic explícito nos filtros/authoring, preservando IDs/enums/limites atuais. Primeiro recorte não é motor genérico para dezenas de formas.

Em BeginGauntletBasicImpact capturar também o estado aprovado de A e o estágio do combo; no impacto, HP confirmado autoriza filho da mesma execução/raiz. A onda captura geometria/origem/direção e coleção de vítimas já atingidas. Reservar antes de overlap/HP, validar geração/LOS/encontro e deduplicar. Não gerar a onda por BasicAttackPerformed ou overlap sem recibo. ComboNova existente continua com sua regra por emissão e não pode ser reinterpretada silenciosamente como A.

Reaproveitar contexto causal/budget de BreakerGauntletCombat/ProtocolRuntime para o filho; se o produtor de Basic ainda não oferece a reserva necessária, estender o owner existente. Não gerar raiz nova por alvo ou filho para contornar cap. Somente onda autorizada pode disparar Conductor conforme D08; budget negado suprime HP/postura/recursos/efeitos mecânicos do filho. Profile snapshots impedem aquisição tardia de mudar ataque em andamento.

Ordem estável: identificar conteúdo/arma/capabilities → resolver exclusão da forma → aditivos por prioridade+ID → snapshot → intenção/commit → ImpactEvent → HP → filhos autorizados → apresentação/telemetria. Repetir A+B/B+A deve resultar no mesmo snapshot normalizado. Não escalar arbitrariamente speed/animation/recovery para acomodar módulo.

## Transação de swap no owner atual

Recomendação de primeiro adaptador: Q/Avanço→Uppercut Eclipse existente no index0. Não é implementação de qualquer habilidade em qualquer slot. RunBoons continua owner dos clones e inventário; AbilityHolder recebe operação seletiva a desenhar/implementar depois, sem chamar RefreshLoadout global.

1. Na recompensa pendente, resolver ID estável do candidato e revisar ticket/inventory key; confirmar player vivo, encontro parado, sem cast/buffer/feedback/filho pendente. TryStopEncounterOffense não é prova única de segurança; suas mutações devem ocorrer apenas depois do preflight, ou ser reversíveis.
2. Preparar candidato em clone fora do loadout; validar capacidades/grafo/perfil/recursos/boons. Inspecionar investimentos: incompatível bloqueia oferta ou confirmação com explicação, sem consumo do ticket.
3. Capturar snapshot mínimo: habilidade/variante/clones do slot, timers/estado, inventário, índice de ofertas/ticket/revisão, visual derivado. Outros slots não mudam.
4. Commit síncrono sem notificar consumidores no meio: instalar variante, inventário e timer conservador D05; manter mana/HP/Asura existentes e não cobrar custo de cast pela troca.
5. Reconciliar HUD/visual e publicar evento de mudança existente ou extensão local. Só então concluir ticket/ordinal e liberar input após soltura.
6. Falha antes de publicação restaura snapshot seletivo e descarta clone candidato; se uma exceção atravessar efeitos irreversíveis, Faulted com diagnóstico/abort pelo owner atual, sem repetir concessão. Testes devem provar rollback de todo o delta aprovado; ticket sozinho não basta.

Recarga nova usa max(restante antigo, duração efetiva nova); slots não substituídos mantêm seus relógios. Cooldown progride pelos relógios atuais, não pelo menu de swap. Sem charges nesse candidato; skills futuras com charges entram vazias sem mint e exigem contrato específico. Nenhuma transferência automática de ranks de conteúdo incompatível. Remoção/undo técnico reconstitui estado derivado; não inclui menu de descarte universal.

## Run, apresentação e resultados

Integrar ofertas aos tickets existentes por tipo/ordinal. Primeiro garantir em simulação uma sequência legal A→B→swap sem remover gate de primeira Mutation; registrar seed/decisões para o percurso real. Troca não pode consumir o ordinal da Mutation e depois conceder outra skill extra fora do ticket. Se conflito de tipos impedir o recorte mínimo, obter aprovação do delta de oferta antes da implementação.

Visual mínimo usa peças/manoplas compatíveis com rig/renderers atuais, sem alterar prefab original. Reconciliador local recebe inventário atual, resolve prioridades e instancia/remove apenas seus attachments/efeitos. A é forma maior+onda; B é assinatura elétrica condizente com emissão real. Temporários/CC não apagam aparência permanente da run. Cleanup em troca/morte/disable/pooling/fim/new run é idempotente. Arte final/rig novo não entram.

RunDamageSource mantém Basic e slots históricos Q/W/E/R como identidade, com bindings como apresentação separada. O filho tem produtor/Mutation/parentes; novo label não gera bucket adicional. RunStatistics totaliza somente receipts; HUD/hooks não recontam. Não mudar schema de RunSummary sem necessidade demonstrada e aprovação separada. A lista de skills no resultado deve consumir identidade/variante disponível e rotular ausência de informação antiga, sem inventar histórico.

## Delta permitido numa futura aprovação

GPR-C01: GamePreferences, ControlRemappingPanel, CharControlScript, fronteira de leitura do AbilityHolder/DashScript, prompts em PlayerHUD/MainMenuUI/PauseMenuUI/LobbyInteraction/LobbyArsenal/tutorial/rewards/results, testes e um asset/map de ações caso aprovado. Campos/GUIDs atuais preservados; sem mudança de balanceamento/perfis de impacto.

GPR-01 posterior: RunBoons/WeaponRunModifiers/grafo/catálogos, BasicImpact/BreakerGauntletCombat/ProtocolRuntime somente no seam aprovado, operação seletiva do holder, UI reward/HUD/visual e fontes autoradas mínimas de A/adaptador. Não reescrever Actor/RunStatistics, mapas, inimigos ou três armas.

Rollback futuro: commits pequenos por tarefa, registrar hashes/assets/meta; reverter apenas commits do rework por revisão explícita, sem git reset sobre alterações anteriores e sem restaurar prefs humanas antigas. Nova configuração de bindings deve guardar versão e fallback legível, sem apagar chaves antigas; rollback não apaga progresso.

## Contratos implementados em GPR-01.1

BoonSlot.Basic4/flag16/AllTargets31 anexados; Any15 preservado. Grafo aceita as quatro configurações legadas e Basic opcional, com mapeamento explícito para GauntletActionSlot.Basic=-1. AlvoBasic limitado à Manopla; nenhuma configuração de cast/HUD acrescentada. Snapshots/records incluem Basic sem alterar referências/GUIDs existentes.

PlayerActor registra ConfirmedHpReceipt no callback de HP efetivo da transação; ProtocolRootContext registra apenas terminal direto elegível e vítimas/generation. ReserveConfirmedBasicChild rejeita callbacks/batches ainda abertos, permite uma reserva e mantém parent/root existentes. ProtocolExecution/lineage carregam a proveniência e a lista de exclusão. Não há chamado desse contrato no input de produção nem onda criada.

ProtocolBasicEffects.None é o padrão. Grants explícitos Conductor/Detonation habilitam só as arestas permitidas, com uma emissão de cada, sem recursão e sem elementos/Overflow/Resonance ou hooks de recurso herdados. Fila, LOS, dedupe, geração, lifecycle e caps são os existentes. Nenhuma política nativa global ou limite autorado foi mudado.

RefreshAfterBoonAcquisition conserva estados/timers de referências de habilidade idênticas; equip/reset explícito continua separado. Identidade substituída mantém reset legado; isso não é transação de swap. ID desconhecido é rejeitado antes de completar uma aquisição. Evidência e limites no relatório01.1.
