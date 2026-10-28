# Gameplay Rework — desenho técnico mínimo

27/10/2026. **GPR-C01.3a concluída tecnicamente, aguardando revisão.** C01.2c aceita no escopo técnico. LMB direcional por clique implementado; hold do básico somente com ChargedShot por decisão explícita; Asura no RMB da Manopla incluída por autorização adicional.102/102 testes normais; aceite físico pendente. C01.3b/GPR-01 não iniciadas. [Relatório C01.3a](../decisions/GPR-C01.3a_RELATORIO.md).

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
