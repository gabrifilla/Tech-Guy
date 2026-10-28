# Combate — contrato de comportamento

## Estado vigente — GPR-01.1,28/10/2026

GPR-C01 permanece **COMPLETA e aceita pelo desenvolvedor**; GPR-01.P1 aceita como planejamento. LMB básico, RMB especial permanente e exatamente três modulares Q/E/R, sem X. Moonwalk/cadência continuam refinamentos futuros não bloqueadores, sem tuning nesta etapa.

Direção experimental A→A+B→A+B+C aceita, parâmetros1,5×/0,60× provisórios; autorizada **exclusivamente GPR-01.1**. [Relatório da fundação](../decisions/GPR-01.1_RELATORIO.md) / [fatia e próximo escopo](../gameplay/GPR_01_VERTICAL_SLICE.md). Sem asset/boonTitanic Fists habilitado, onda ou transformação visual de produção. STOP para revisão antes de01.2;01.3–6 também exigem autorização posterior.

Q/Avanço para aproximação é compatibilidade de ações, não sinergia mecânica nova. SwapAvanço→Uppercut adiado por redução aprovada de escopo; aquisição/substituição modular Q/E/R permanece requisito **NÃO IMPLEMENTADO/NÃO VALIDADO**, rastreado em GPR-03.1/RF-TROCA. Gates e decisões históricos abaixo não sobrepõem esta autorização.

## Input vigente — C01.4,28/10/2026

Decisão mais recente: **três skills Q/E/R + especial permanente RMB**, sem X/quarta modular. Binding é apresentação/entrada, não identidade de execução. No modo direto, índices0/1/2 correspondem às três skills e index3 ao especial; o array autorado de quatro entradas não foi alterado. Identidades legadas Q/W/E/R de boons/analytics permanecem internas. Asura e quarta ação nativa Bow/Spear mantêm os executores existentes, sem redesenho. Settings remapeia ações; InteractG e finisherF são distintos por padrão. DashCursor padrão ou Movimento; neste último, sem vetor, usa o facing atual. As tabelas autoradas/históricas abaixo não sobrepõem esses controles. Ver [consolidado4](../decisions/GPR-C01.4_RELATORIO.md).



**Direção de input aprovada em26/10, ainda não implementada:** WASD/mira independente; cada cliqueLMB pede um básico; RMB é especial da arma/Asura naManopla; quatro slotsQ/E/R/X. Hold não é padrão final aprovado. A descrição abaixo de cliques/QWER documenta o runtime legado. SomenteC01.1a/b (baseline/design) foram autorizadas; [migração vigente](../gameplay/GPR_CONTROL_MIGRATION.md) / [prontidão](../decisions/GPR-C01.1_BASELINE_E_PRONTIDAO.md). Preservar scheduler/dano/reação/cancel, sem executorAsura duplicado.

**Direção de design vigente:** combate ativo e responsivo com reação contextual de inimigos, inspirado principalmente em Lost Ark, usando leitura e clareza de Diablo. Não assumir valores de frames ou aceitação de gameplay a partir de código.

**Base técnica:** inspeção estática em 18/10/2026, `gameplayOverhaul`, HEAD `3a3f0ee` com alterações locais. Os comportamentos abaixo estão `IMPLEMENTADO_NAO_VALIDADO` nesta consolidação. Nenhum teste, Editor ou playtest foi executado aqui. Evidências históricas e gates ficam na [matriz de validação](../validation/01_MATRIZ_DE_VALIDACAO.md); somente o desenvolvedor aprova feel/diversão.

## Vocabulário obrigatório

| Evento | Efeito | Não significa |
|---|---|---|
| hit confirmed | impacto elegível resolvido | dano aplicado em todas as situações |
| hit reaction visual | animação/VFX/SFX curto | interrupção mecânica obrigatória |
| micro-stagger | interrupção breve configurada | stun duradouro |
| stun | incapacidade temporária explícita | push ou launch |
| stance damage | progressão para quebra de postura | quebrar stance em cada golpe |
| stance break | limiar/política de ruptura atingida | lançar todo tipo de inimigo |
| launch / air juggle | alvo elegível elevado e em janela aérea | knockback horizontal genérico |
| push | deslocamento controlado de curto alcance | knockback grande |
| knockback | deslocamento significativo quando a skill autoriza | hit básico normal |
| hitstop | pausa perceptual por impacto | cooldown, proc ou custo adicional |
| telegraph | aviso temporal/espacial de ataque | dano imediato na marcação |

## Regras comportamentais de produto

- Golpe básico da manopla em mob comum pode produzir micro-stagger e interromper windup, de acordo com janelas e perfis; não deve lançar e empurrar todos indiscriminadamente.
- Skills de launch consultam capacidade, estado e resistência do alvo. Elite e boss recebem redução/fallback explícitos; preservar o padrão de ameaça e evitar controle permanente.
- Projétil, área persistente e efeito derivado mantêm causa, dono, validade e tempo de vida. Cancelar uma animação não autoriza apagar projétil já emitido sem contrato específico.
- Energia, custo e cooldown são consumidos uma vez por execução; somente eventos elegíveis disparam procs. Não criar ciclos `OnHit → effect → OnHit` sem orçamento.
- Pausa, hitstop, morte, fim de encontro e disable têm semânticas próprias de bloqueio/cleanup. Não substituir todos por um único timer ou regra global.

## Implementação encontrada e fontes

| Responsabilidade | Caminho atual e comportamento observado |
|---|---|
| Bindings e input | [GamePreferences](../../Assets/_Project/Scripts/Core/GamePreferences.cs) mantém Primary, Move, Dash, Skill1–4 e FinishSequence, incluindo compatibilidade para bindings F antigos/colidentes. [CharControlScript](../../Assets/_Project/Scripts/Characters/Player/Actions/CharControlScript.cs) resolve clique com alvo/LOS, repetição segurada e Shift direcional. |
| Execução e recarga | [AbilityHolder](../../Assets/_Project/Scripts/Abilities/AbilityHolder.cs) carrega os quatro slots de `WeaponScript`, valida disponibilidade e recursos, ativa o executor e mantém `Ready → Active → Cooldown`. Recarga nominal e duração ativa são campos diferentes; redução de recarga é aplicada em runtime. |
| Cancelamento/buffer | O mesmo holder consulta [CancelResolver](../../Assets/_Project/Scripts/Characters/Combat/Core/CancelResolver.cs) com regras do `CombatActionProfile` ativo. Dash tem preflight de disponibilidade e buffer quando a janela temporal está fechada; skills também consultam a janela. Ausência de regra não libera cancelamento irrestrito. |
| Movimento durante ação | `MovementAllowedWhileCasting` libera casts nativos do arco e a espera da Asura Cadenciada. O R transformado de carga do arco pode prender o movimento. `GroundMovementBuffer` guarda destino navegável durante recovery quando não há intenção ofensiva pendente; iniciar outra ação descarta o destino. |
| Dano confirmado | [PlayerActor.ResolveAttackDamage](../../Assets/_Project/Scripts/Characters/Player/PlayerActor.cs) usa `Actor.CaptureNextDamage/TakeDamage` para obter dano efetivamente aplicado antes de publicar hooks/protocolos. Não inferir hit confirmado de overlap ou animação. `Actor.HealthChanged` é a notificação de recurso do alvo. |
| Impactos da manopla | [BreakerGauntletCombat](../../Assets/_Project/Scripts/Abilities/Weapon/BreakerGauntletCombat.cs) usa scheduler/ledger de impactos, snapshot por execução e contexto causal; [GauntletEnergyPolicy](../../Assets/_Project/Scripts/Abilities/Weapon/GauntletEnergyPolicy.cs) consome confirmações deduplicadas. |
| Postura e CC | [CombatReactionController](../../Assets/_Project/Scripts/Characters/Combat/CombatReactionController.cs) coordena [PostureReactionState](../../Assets/_Project/Scripts/Characters/Combat/PostureReactionState.cs), apresentação e navegação. Postura, efeito físico, janela de vulnerabilidade e interrupção têm estados separados; detalhes por alvo em [inimigos](03_INIMIGOS.md). |
| Efeitos derivados | [ProtocolRuntime](../../Assets/_Project/Scripts/Characters/Combat/Protocols/ProtocolRuntime.cs) mantém contexto/limites de execução e ciclo de encontro; contratos de compatibilidade e proc em [boons](04_BOONS.md). |

Os executores especializados da manopla, do arsenal nativo e de transformações de arco/lança são encaminhados pelo holder. Nomes semelhantes não comprovam aplicações duplicadas de dano; investigar o caminho ativo antes de consolidar código.

## Recursos e ciclo de vida observados

- `PlayerActor` captura a mana base serializada e recompõe máximos pelos stats. As cenas publicadas com jogador estão serializadas com **1500 de mana**, regeneração de **4% do máximo/s** após **1,25 s** do último gasto. Regeneração usa tempo escalado e para com a morte. São valores da configuração inspecionada, não uma autorização de rebalanceamento.
- `TrySpendMana` verifica/debita no dono do recurso; HUD, preflight e ativação devem consumir o custo efetivo exposto pela habilidade. A manopla tem multiplicador central de custo; valores e origem estão somente em [armas](02_ARMAS.md).
- Básicos não passam pelo débito de skill. O [asset de dash](../../Assets/_Project/ScriptableObjects/Abilities/Dash/Dash.asset) tem custo zero; disponibilidade, morte, pausa, câmera e cancelamento ainda são gates.
- `AbilityHolder.TryStopEncounterOffense` espera impactos/feedback da manopla terminarem quando necessário e interrompe executores/buffers pelo caminho existente. Aquisição de recompensa sozinha não representa reset geral de timers.
- Hitstop congela o relógio de postura e impactos que explicitamente o consultam; não assumir que ele equivale à pausa global. Cada efeito persistente exige verificar sua integração real.

## Validação e divergências

Há testes fontes para dano confirmado ([DamageConfirmationPlayModeTests](../../Assets/_Project/Scripts/Tests/PlayMode/DamageConfirmationPlayModeTests.cs)), cancelamento ([CancelNoDuplicationPlayModeTests](../../Assets/_Project/Scripts/Tests/PlayMode/CancelNoDuplicationPlayModeTests.cs), [DashCancelResolverPlayModeTests](../../Assets/_Project/Scripts/Tests/PlayMode/DashCancelResolverPlayModeTests.cs)), movimento ([BasicAttackR1MovementFluidityTests](../../Assets/_Project/Scripts/Tests/PlayMode/BasicAttackR1MovementFluidityTests.cs)) e postura ([PostureReactionPlayModeTests](../../Assets/_Project/Scripts/Tests/PlayMode/PostureReactionPlayModeTests.cs)). Existência do teste é cobertura pretendida, não resultado de execução atual.

O contrato observado usa janelas de cancelamento e exceções explícitas de movimento. Permanecem questões de geometria/identidade em [armas — conflitos](02_ARMAS.md#conflitos-e-lacunas-conhecidos); nenhuma direção de design foi trocada para acomodar o runtime.

Cenários para aceite humano, ainda pendentes nesta consolidação:

1. Comum durante windup: golpe apropriado interrompe; observar retomada.
2. Elite e boss: resistência/fallback perceptíveis, ameaça preservada, sem launch/stunlock indevidos.
3. Dash + skill e cancel: nenhuma cobrança duplicada; conferir projétil emitido antes do cancelamento.
4. Swarm de 10–20 inimigos como cenário sugerido: ameaça, jogador e efeitos legíveis; número final depende do baseline real.

Testes automatizados não aprovam impacto subjetivo. Registrar vídeo, telemetria e observação do desenvolvedor antes de marcar gameplay aprovada.

## Sincronização visual de impactos sucessivos

Na primeira iteração Gameplay Intelligence, o flinch de `CombatReactionController.ApplyReaction` passou a reiniciar explicitamente no tempo normalizado zero para cada reação elegível. Antes, `Animator.Play` sem tempo preservava GetHit quando esse já era o estado atual, perdendo o início visual do segundo impacto. O gate de reação, postura/interrupção e as chamadas de stun mantêm seus contratos; reiniciar animação não concede stun, push ou launch. A mudança usa o controller/clip existentes e vale para impactos recebidos das três armas.

Teste com Grunt real reproduziu a falha e passou após a correção; captura equivalente da Manopla confirmou o reinício sem mudar 30/20/10 de HP, mana/energia finais ou contagem de hitstop. A saída visual após o clip foi tratada separadamente na iteração 1.1, descrita abaixo. Evidências, regressões de Arco/Lança e limites temporais estão na [matriz](../validation/01_MATRIZ_DE_VALIDACAO.md). Isso não constitui aprovação humana de game feel.

## Recuperação visual de GetHit (Gameplay Intelligence 1.1)

CombatReactionController encerra somente o GetHit que iniciou, após o Animator concluir o clip. Novo impacto elegível reinicia a reprodução; o fim anterior não dispara uma saída atrasada. O retorno usa o blend Idle/Speed ou Idle/Walk e a histerese existentes. Controle, congelamento, execução/recovery ofensiva e apresentação procedural têm prioridade; a saída espera pausa/hitstop. Outro estado/transition ou RuntimeAnimatorController abandona a apresentação própria, sem sobrescrever a nova ação. Morte/disable/reconfiguração limpam ownership.

Esse ciclo é visual: não concede ou prolonga stagger/stun, não interrompe ataques adicionais e não muda resistência, dano, postura, recursos ou armas. O fallback binário de EnemyAI respeita o flinch até o dono liberá-lo. Capturas equivalentes e testes de Normal/Elite/Boss/prioridades estão na [matriz](../validation/01_MATRIZ_DE_VALIDACAO.md); aprovação humana permanece pendente. O ajuste de números/HUD P3 não faz parte dessa correção.


## Apresentação corporal da Manopla (Gameplay Intelligence 1.2)

A biblioteca no caminho já previsto `Combat/SkillAnimations` referencia PunchLeft/PunchRight/Slam e seus contatos existentes em EnemyMotions. O executor da Manopla reaplica no LateUpdate a última amostra do PlayableGraph manual, após a avaliação automática do AnimatorController. Isso não avança tempo nem emite outro impacto; cleanup continua destruindo o graph existente. Apenas o executor da Manopla usa essa reaplicação. Arco/Lança não receberam motions novas; inimigos preservam suas bibliotecas e apresentação.

A recuperação de GetHit 1.1 é independente dessa apresentação do atacante. Na 1.2, o básico ainda resolvia dano ao iniciar o clip; essa divergência foi tratada na 1.4 abaixo. As skills também misturavam tempos dos steps e dos ImpactEvents; Q/W/E nativas foram reconciliadas na 1.3 abaixo. R e Mutations conservam sua cronologia anterior. Buffer geral de skills também permanece ausente: não confundir o buffer de dash/Cadenciada com aceitação de qualquer skill fora da janela. Evidências, prioridades e limites da verificação estão na [matriz](../validation/01_MATRIZ_DE_VALIDACAO.md).


## Contato sincronizado de Q/W/E nativas (Gameplay Intelligence 1.3)

A apresentação de Q/W/E sem Mutation lê o profile/snapshot já aceito e os respectivos ImpactEvents. Antecipação e follow-through usam `At × ExecutionDuration`, preservando a convenção curta de follow-through existente. O callback do scheduler identifica o último evento efetivamente emitido; o LateUpdate reapresenta a pose de contato desse evento no mesmo frame e enquanto houver hitstop. Não se entra no follow-through de um evento ainda não emitido. Pausa conserva a última amostra e cleanup/novo cast limpam a identidade visual.

SkillAnimationPlayer, clips, scheduler, ledger e relógio mecânico existentes foram reutilizados. A apresentação não avança nem sinaliza o scheduler, não emite dano e não altera custo, cooldown nominal, hitstop, resistência, interrupção ou cancelamento. R/Asura Cadenciada, Mutations e fallback sem profile mantêm o sampling anterior; ataques básicos não foram alterados nessa iteração. Resultado/limites de timing no W, testes e falhas herdadas estão na [matriz](../validation/01_MATRIZ_DE_VALIDACAO.md). Contato temporal de clip não certifica alinhamento geométrico do punho com cada alvo nem aprovação humana de peso/fluidez.

## Contato do básico da Manopla (Gameplay Intelligence 1.4)

O comando aceito continua iniciando imediatamente o ataque e publicando `BasicAttackPerformed`. `CharControlScript.BasicImpact` mantém o owner da ação em CharControlScript, copia o BasicProfile e abre o contexto causal existente. O `ActionImpactScheduler`/ledger emite o contato autorado; somente então o pipeline existente de PlayerActor resolve a área atual, confirma HP, reação, Protocols e Asura. Não reserva um alvo nem redireciona o soco para uma seleção posterior. Um alvo que sai da área pode ser perdido. Arco mantém emissão imediata de projétil; Lança mantém a área imediata. O helper instantâneo de PlayerActor continua disponível para seus outros consumidores.

Decisão explícita do desenvolvedor: preservar cadência e ajustar apresentação. A janela mecânica de 0,20 s continua igual; o evento a 40% corresponde a 0,08 s, sujeito ao próximo FixedUpdate. `SkillAnimationPlayer`, já usado em Q/W/E, encaixa o clip existente nessa janela, amostra seu contato e o reaplica no LateUpdate. Não há edição de clip, velocidade de ataque, dano ou assets. Recuperação termina pelo busy existente; pausa e hitstop suspendem o relógio escalado. O frame emitido/hitstop conserva a pose de contato.

CancelCombo, movimento, troca de arma, disable, dash/skill aceitos e teardown continuam passando pelas autoridades existentes. Antes do contato, fechar a ação remove emissão pendente sem HP/Asura; após confirmar HP, o cancelamento não reverte o recibo nem emite novamente. Cancelamento reentrante durante a notificação deixa a área já comprometida concluir e só então fecha sua raiz. Não foram ativadas novas regras de cancelamento do BasicProfile nem adicionado buffer geral. Falhas e testes executados estão na [matriz](../validation/01_MATRIZ_DE_VALIDACAO.md).

## Apresentação ofensiva dos inimigos — GI1.6

EnemyCombatActions reaplica em LateUpdate a última pose do SkillAnimationPlayer enquanto conserva ownership `_busy`. O graph manual podia perder a pose antes do render, embora contato/dano ocorressem corretamente. Reaplicar não avança tempo, reemite impacto ou modifica reação, postura, cancelamento, dano ou resistência. O executor continua liberando o graph ao terminar/cancelar; GetHit/controle mantêm seus gates. Regressão vermelha→verde com Grunt/Shooter reais e pausa/cancel/retomada consta na [matriz](../validation/01_MATRIZ_DE_VALIDACAO.md). Conflitos de aproximação melee e aviso/mira do Sniper estão em [inimigos](03_INIMIGOS.md), pendentes de decisão e sem correção mecânica nesta etapa.

## Compromisso de trajetória anunciado — GI1.8

O Sniper agora compromete origem/direção ao criar seu aviso visível e reutiliza esses dados na emissão após o windup existente. A opção explícita lockDirection do EnemyProjectile impede uma segunda aquisição durante Configure; seu default preserva os outros consumidores. Controle/LoS podem cancelar a preparação, mas movimento do alvo não redireciona um tiro anunciado. Viagem, geometria, HP único, dano e cooldown permanecem nas autoridades existentes. Contrato e limites em [inimigos](03_INIMIGOS.md#compromisso-do-sniper--gi18-25102026); evidência antes/depois e regressões na matriz. As correções GI1.6/GI1.7 foram preservadas.
