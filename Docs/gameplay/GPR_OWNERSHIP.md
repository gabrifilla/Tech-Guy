# GPR-00.2 — Ownership, dependências e lacunas

## Estado vigente — GPR-01.1,28/10/2026

GPR-C01 permanece **COMPLETA e aceita pelo desenvolvedor**; GPR-01.P1 aceita como planejamento. LMB básico, RMB especial permanente e exatamente três modulares Q/E/R, sem X. Moonwalk/cadência continuam refinamentos futuros não bloqueadores, sem tuning nesta etapa.

Direção experimental A→A+B→A+B+C aceita, parâmetros1,5×/0,60× provisórios; autorizada **exclusivamente GPR-01.1**. [Relatório da fundação](../decisions/GPR-01.1_RELATORIO.md) / [fatia e próximo escopo](GPR_01_VERTICAL_SLICE.md). Sem asset/boonTitanic Fists habilitado, onda ou transformação visual de produção. STOP para revisão antes de01.2;01.3–6 também exigem autorização posterior.

Q/Avanço para aproximação é compatibilidade de ações, não sinergia mecânica nova. SwapAvanço→Uppercut adiado por redução aprovada de escopo; aquisição/substituição modular Q/E/R permanece requisito **NÃO IMPLEMENTADO/NÃO VALIDADO**, rastreado em GPR-03.1/RF-TROCA. Gates e decisões históricos abaixo não sobrepõem esta autorização.

## Delta de ownership após fechamento C01

Movimento/input/rebinding/contextos agora são entregues e aceitos no escopoC01; linhas antigas classificadas EXTEND/UNDECIDED sobre esses controles são baseline histórica. C01 não implementou InputAction asset/PlayerInput; a fachada GamePreferences/Input System existente continua a solução atual, sem segundo backend.

GPR-01 proposta: BasicImpact→receipt de PlayerActor→reserva em BreakerGauntletCombat/ProtocolRuntime; RunBoons/WeaponRunModifiers/grafo continuam inventário. As extensões pendentes são alvoBasic, política restrita do novo ramo e aquisição sem refresh destrutivo de timers. Swap genérico não é parte desta fatia proposta. [Seams detalhados](GPR_01_VERTICAL_SLICE.md).


## Histórico aprovado C01.4 — três skills + especial,28/10/2026

A decisão direta mais recente **substitui todo modelo de quatro slots modulares Q/E/R/X**: exatamente três slots modulares Q/E/R e um especial permanente separado no RMB. X não tem ação/placeholder; index3 e arrays de quatro referências permanecem compatibilidade interna, nunca uma quarta skill modular. Asura mantém executor, custos, duração, cooldown, animação, danos e continuação; F11 não é alias direto. Bow/Spear reutilizam sua quarta ação existente no canal especial, sem mecânica/asset novo. A futura aquisição/substituição é restrita aos três slots e continua fora de C01.4.

WASD e demais ações configuráveis no Settings; InteractG dedicado e finisherF distintos por padrão, sem sobrescrever personalizações. Dash padrãoCursor; opçãoMovimento usa vetor configurado/câmera normalizado e **facing atual quando não há direção pressionada**, conforme esclarecimento posterior do desenvolvedor. A sugestão histórica de fallbackcursor no modoMovimento está superada. Escape reservado à pausa/cancel seguro. Migração só lê/repara em memória, Apply explícito grava nomes de ações; sem reset de progresso.

Registro histórico de C01.4: implementação e116 casos cumulativos concluídos antes do aceite físico. A pendência humana foi encerrada por C01.5; o estado vigente e o novo STOP de GPR-01 estão no início deste documento.



26/10/2026. Inspeção estática do checkout D:, HEAD3a3f0ee + alterações locais; evidência Unity limitada aos relatórios. Fontes principais: [arquitetura canônica](../04_ARQUITETURA_E_INTEGRIDADE.md), [combate](../systems/01_COMBATE.md), [boons](../systems/04_BOONS.md). Caminhos abaixo são relativos a `Assets/_Project/Scripts/`; métodos foram localizados no código atual.

**Revisão C01.1:** mapa histórico de owners preservado. Política atual éLMB clique→um pedido eRMB→especialAsura; a linha histórica de dois canais básicos não autorizaRMB alternativo. Ver [migração vigente](GPR_CONTROL_MIGRATION.md) para o acoplamento index3/snapshot/boonsR e separação proposta dos quatro slots.

## Caminho do comando até o resultado

1. `Core/GamePreferences.cs`: `WasPressed`/`IsHeld` resolve KeyCode salvo para botões do Input System. `Characters/Player/Actions/CharControlScript.cs`: `HandlePointerInput`/`ClickToMove` seleciona alvo, aproxima e autoriza básico; Shift solicita ataque direcional. `BasicAttackDriver` separa tap/hold de seleção. `CanStartBasicAttack` conserva busy, dash, cooldown, HP e bloqueios do holder.
2. `CharControlScript.PerformAttack` inicia animação/ocupação; `CharControlScript.BasicImpact.cs:BeginGauntletBasicImpact` copia BasicProfile e captura arma, dano, alcance, geometria e contexto. Não há HP na antecipação. `ActionImpactScheduler` + `ExecutionImpactLedger` emitem ImpactEvent uma vez por execução/impacto. FixedUpdate avança contatos; LateUpdate reaplica pose.
3. `EmitGauntletBasicImpact`: contato visual, BeginAction Basic, BeginImpactFeedback e TryApplyGauntletAreaDamage. `PlayerActor` agrupa vítimas elegíveis/LOS e resolve cada transação; `GauntletImpactTransaction` segura confirmações/notificações durante reentrância. Cancelamento antes do contato revoga impacto; HP já comprometido não é revogado por callback.
4. `Characters/Player/PlayerActor.cs:ResolveAttackDamage` obtém HP efetivo por `Actor.CaptureNextDamage`/TakeDamage. Reserva negada por budget retorna antes do dano derivado. HP/postura/controle pertencem a Actor/CombatReactionController; flinch não implica interrupção. `GauntletEnergyPolicy` recebe confirmações deduplicadas, não overlap bruto.
5. ProtocolRuntime recebe SubmitConfirmedImpact; ProtocolBudget e ProtocolRootContext limitam raiz/frame/profundidade/reservas/fila. HookBus por run recebe notificações elegíveis; não é fonte de totalização. Filhos preservam sessão/encontro/geração do alvo/produtor e ancestry.
6. SkillAnimationPlayer usa PlayableGraph manual, desliga root motion temporariamente e restaura ao liberar. GauntletImpactVfx/feedback usam contato confirmado; não criar segundo dano em AnimationEvent. GI1.0–1.4 dependem dessa ordem; GI1.6 reaplica pose inimiga, GI1.7 preserva entrada melee em alcance, GI1.8 fixa linha anunciada do Sniper.
7. `Core/Run/RunStatistics.cs:OnDamage` lê PublishedDamageTransaction, deduplica TargetId+Generation+Sequence e particiona HP efetivo. BeginAction e SourceFor preservam slot/execução/Mutation/parentesco. `RunSummaryStore` persiste o resumo validado; UI consulta o modelo existente. Q/W/E/R aqui também são IDs históricos de atribuição, não teclas a renomear em massa.

## Aquisição, substituição e limpeza

`ManualRoutePolicy/ProgressionDirector` → pedido `(SessionId, NodeId, Ordinal)` → `RunRewardPolicy.Open` valida fase/ordinal, congela IDs sorteados por seed derivada → `ManualRewardCatalog`/`BoonOfferCompatibility`/`BoonCapabilityGraph` filtram → `RunBoons.Manual.ChooseManual` revalida revisão/inventory key → ApplyOffer → CompleteOffer → RefreshLoadout → ticket resolvido/avanço + RequireRelease.

RefreshLoadout força `AbilityHolder.RefreshWeaponAbilities`: recria arrays de activeTimers/cooldownTimers/states **para todos os slots**. Portanto não é uma API de swap que preserva cooldown. O ticket evita concessão duplicada, mas Faulted após callback não garante rollback de mutações parciais. Esses são pontos de extensão necessários, sem corrigir agora.

RunBoons.Start clona arma e cada habilidade em runtime e mantém `_owned`; estado de run não deve escrever no asset original. Inventário/compatibilidade pertence a WeaponRunModifiers/Expansion, não a uma segunda lista de skills. `EndManualSession`/OnDestroy encerram Protocols, removem modificadores, limpam inventário e destroem clones. BreakerGauntletCombat invalida contextos/filhos/energia em morte/disable/troca. ProgressionDirector finaliza RunStatistics antes do teardown; não reinstalar resumo antigo após uma partida humana.

## Matriz de capacidades

| Capacidade | Classificação | Owner real | Risco e regressão obrigatória |
|---|---|---|---|
| Básico/timing/impactos | REUSE | CharControlScript.BasicImpact, CombatActionProfile, ActionImpactScheduler, ledger | Dano antecipado/fantasma; contato/cancel/reentrância |
| Custo/cancel/buffer | REUSE | AbilityHolder, InputBuffer, CancelResolver, PlayerActor | Cobrança dupla, janela alargada; dash/skill durante recovery |
| Asura/finisher | EXISTS/REUSE | slot3 BreakerAsura, TryFinishSequence, GauntletEnergyPolicy | F11 salvo não é ação nova; F também retorna órbita da lança |
| WASD e mira independente | EXTEND | CharControlScript + NavMeshAgent/câmera | Caminho antigo/rotação disputando direção; paredes/bordas/dash/pull |
| Ações InputAction/rebind composto | NEW na fronteira de input | Evoluir GamePreferences/ControlRemappingPanel | Nenhum .inputactions encontrado; não existe migração pronta |
| Dois canais de básico | EXTEND + UNDECIDED produto | BasicAttackDriver/CharControlScript/perfis | Política RMB ainda não existe; sem alterar clocks/cadência silenciosamente |
| Mutation/compatibilidade | REUSE/EXTEND | WeaponRunModifiers, BoonCapabilityGraph | Grafo hoje orientado a slots QWER; básico precisa alvo explícito sem consumir slot ativo |
| Derivados limitados | REUSE | ProtocolRuntime/ProtocolBudget/ProtocolRootContext | Raiz nova não pode renovar limite do frame; filho negado não dá HP/postura/recurso |
| Troca arbitrária de skill por run | EXTEND | RunBoons clones + AbilityHolder | Falta commit seletivo/rollback preservando timers e boons dependentes |
| Recompensa de skill | EXTEND | Tickets/catálogos/RunRewardUI | Não alterar cardinalidade/Mutation obrigatória/portão boss |
| Visuais cumulativos reversíveis | EXTEND/NEW local de apresentação | SkillAnimationPlayer, GauntletImpactVfx + estado de RunBoons | Reconciliador visual por owner, sem manager global; rig/attachments precisam validação Unity |
| Atribuição e resultados | REUSE/EXTEND mínimo | RunStatistics/RunDamageSource/RunSummary | Manter IDs históricos; label QERX não muda origem de HP |
| Save/resume de run | NOT VERIFIED como funcionalidade; fora do recorte | Store guarda último resultado, loadout guarda escolha persistente | Não inventar schema de save de run |
| Gamepad integral | NOT VERIFIED | StarterAssetsInputs residual não prova integração | Escopo inicial teclado/mouse; não anunciar suporte |

## Lacunas e restrições

Dados de ProtocolLimits têm defaults 32 impactos/raiz, depth4, 256 impactos/frame etc., mas não são novo orçamento aprovado nem medição de desempenho. Ler snapshot efetivo antes de qualquer ajuste. Catálogo habilitado não garante oferta; testes de seeds não provam sobrevivência. Não criar AttackDefinition/SkillDefinition/RunBuildState/VisualMutationProfile literais da visão se owners existentes bastarem.

Básico usa Push por default e contribuição de interrupção específica; preservar overrides reais da arma. A frase histórica “E finisher” protege a função de Choque/Stagger e não autoriza fundi-la com FinishSequence. Riscos de ranged sem HP na GI1.9 seguem incidentes sem executor confirmado, fora da implementação autorizada.
