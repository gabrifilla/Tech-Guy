# Contract Audit — Posture & Enemy Interaction

## Decision and scope

Início da implementação: 02/10/2026. A Fase 2 preserva assinaturas, payloads e granularidade dos eventos, mas altera respostas por categoria, hard CC para elegibilidade binária, consumo de um limiar por contribuição e semântica de bloqueios conforme `requirements.md`/`design.md`. Não modifica timing/HP de arma, boons nem a fundação de input/cancelamento/dash fora desses contratos.

Fragile R17.4 foi reconciliado nos documentos de arquétipos: duração integral para launch permitido, sem comparação proporcional de airtime. Testes `RemainingArchetypeUnitTests` e `KnockUpJuggleControlLockPropertyTests` codificam a fórmula antiga; devem ser adaptados quando a política binária for integrada, mantendo cobertura de duração, imunidade, obstrução e recuperação. Até então a implementação atual continua parcial e não se declara esse comportamento entregue.

## Pipelines observed in source

| Entrada / fonte | Ordem observável atual | Contrato e ação |
| --- | --- | --- |
| `PlayerActor.TryApplyAreaDamage` | `TryApplyDamage` → dano/OnHit/OnCrit/OnKill → reação/postura → StanceBroken/ponte OnStanceBreak → `AbilityHolder.NotifyAttackHits` | Preservar ordem externa; publicar estado interno de quebra antes do callback |
| Básico de área em `CharControlScript` | builder lê combo/default Push; usa o pipeline de área acima | Intenção Stagger deve ser autorada por arma; não reclassificar todas as áreas como básico |
| `HitboxDamage.TryDamageActor` | dano via DealResolvedAttackDamage → OnHit/crit/kill → `RaiseBasicAttackHit`/BasicKill → ApplyHitReactionTo | Preservar o canal dedicado do básico; área e hitbox não têm a mesma ordem/granularidade |
| `PlayerActor.ApplyHitReactionTo` | direção relativa → request → WatchStanceBreaks → ApplyReaction | Preservar assinatura exata e derivação de direção; contexto interno aditivo |
| `BreakerGauntletCombat.EmitAuthoredImpact` | Impact/área/NotifyAttackHits → ApplyHitStopForImpact | Substituir aplicação individual pelo fechamento do lote; nenhuma alteração de timing de ImpactEvent |
| Boons (GuardBreaker etc.) | callback do HookBus pode gerar ApplyHitReactionTo reentrante | Estado de imunidade/reserva deve estar consistente; evento de quebra uma vez |

`PlayerActor.WatchStanceBreaks` assina uma vez por controller; sua ponte resolve Actor e chama `Hooks.RaiseStanceBreak`. O apresentador não emite outro hook. Hoje a assinatura persiste até teardown: atribuição causal a impactos não originados pelo jogador deve ser conferida na integração para não emitir procs indevidos.

## APIs and dependencies

Preservar `ApplyReaction`, `ApplyInterruptStagger`, `ConfigureRank`, `ConfigureStance`, propriedades públicas de postura/rank/controle e `StanceBroken(Action<Vector3>)`; preservar o construtor/membros atuais de HitReactionRequest e os enums None=0/Stun=1/KnockUp=2/Knockback=3. Knockdown é aditivo=4.

Consumidores: `EnemyAI.Update/CanAttack`, `SectorBoss`, `ArchetypeBehavior`, `PreferredDistanceLayer`, `HealerBehavior`, `FrontalReflector`, `PlayerActor` (Hooker pull) e `EnemyCombatFeedback`. IsControlLocked passa a agregado de CC e abertura; IsStunned fica físico. False não comprova vida. `AreaHitStep`, `GauntletLoopSteps`, `SpearLoopSteps`, `BowLoopSteps`, `ComboAreaAttackAbility`, `FrontAreaAttackAbility`, `SequencedAreaAttackAbility` e boons continuam nas fachadas existentes.

## Baseline and authoring

Unity MCP: Tech-Guy@fccafb2b47cd7671, D:/Dev/Personal/My_games/Tech-Guy, Unity 6000.5.10f1. MainMenu, EditMode, pronto, sem compilação ativa. Console inicial: CS0108 em ScenePortal.cs:107; não limpo. Dados lidos pelo Editor: Heavy postura 220/KnockUp 0,25; Fragile 30/0; Grunt 100/0. Prefabs de arquétipos/variantes consultados usam Animator Controller Character com GetHit/Death; não foi encontrado clip específico de Stun/queda nesses controladores. Usar pose procedural no filho visual; confirmar sua execução e referências no incremento físico, sem alterar assets de terceiros.

Controlador de reação é configurado/adicionado em runtime nos caminhos existentes; não exigir sua presença serializada em todo prefab. Heavy representa MediumMob por categoria explícita, mantendo rank/stats. Cenas CombatStudy, Playground, NexusLobby, FirstSector e PrologueTutorial possuem defaultAttackReaction=1; inventário/migração precisam incluir overrides e arma equipada, não apenas inicializadores C#.

Defaults de proposta usados no primeiro incremento: queda 0,2 s/chão 0,8 s/recuperação 0,3 s; launch 2 m/1 s; stun 1 s; controle contínuo SmallMob 5 s; decaimento 2 s; watchdog 10 s. Campos opcionais usam flags explícitas de override: ausência ≠ zero intencional. Todas as durações executáveis positivas e ≤10 s; soma de fases Knockdown ≤10 s; resistência [0,1], multiplicador [0,5], imunidade [0,1;10], delay [0;10]. Números são defaults ajustáveis, não aprovação de gameplay.

## Checks previstos no baseline histórico

Validar lógica pura, autoria persistida e avisos; executar contratos EditMode/PlayMode. A sequência acima foi observada na fonte, não capturada como trace runtime. Medir ordem reentrante na integração, verificar causa do hook, prova de hit-stop letal, migração idempotente e clips/poses reais. Registrar resultados por checkpoint no tasks.md; nenhum teste/runtime é dado como aprovado neste baseline.

## Primeiro incremento — 02/10/2026

Decisão: construir dados/modelos puros antes de substituir o controlador de cena, preservando as fachadas e a serialização legada. Adicionados ReactionConfiguration/ReactionAuthoring, ReactionPolicy e PostureReactionState. EnemyProfile ganhou categoria independente e validação de dados brutos. Knockdown foi acrescentado como valor 4. RankInterruptStagger agora conserva todo o excedente, consome um limiar por contribuição positiva e suporta decaimento por relógio local. O teste anterior que consumia todos os limiares foi adaptado ao contrato explícito R7.3; permanecem a cobertura gerada de contabilidade e monotonicidade, acrescida de zero/NaN/infinito/deadline.

Reconciliação adicional: R5.6/design de Heavy também descreviam CC proporcional e foram sincronizados com o contrato binário. Testes antigos de juggle que modelam a fórmula proporcional continuam identificados para adaptação no incremento do controlador; não representam validação do novo movimento.

Evidência Unity MCP:

- Instância Tech-Guy@fccafb2b47cd7671; projeto e MainMenu preservados; Editor fora de PlayMode ao final.
- Refresh/import e compilação solicitados pelo MCP; após reload, Console consultado sem limpeza retornou zero erros. Permanecem CS0108 em ScenePortal e CS0618 em testes legados; não foram corrigidos fora do escopo.
- Job bcddfcd0203f4a2685b4ddbde7f6a6d1: 15/15 EditMode passaram.
- Job 09a293311b6d40e7ad28b2d32ead8627: 29/29 EditMode passaram, zero falhas/skips, 2,1008802 s. Classes: ReactionConfigurationTests, ReactionPolicyTests, PostureReactionStateTests, RankInterruptStaggerAccumulationPropertyTests, MissingProfileRankDefaultsFallbackExampleTests, BreakEffectDeterministicDegradePropertyTests, EliteStanceBreakDifficultyPropertyTests e BossSuppressesConventionalReactionsPropertyTests.
- Teste de serialização criou asset temporário pela AssetDatabase, salvou/reimportou e confirmou categoria MediumMob, resistência Knockdown zero explícita e default Knockdown. Asset e meta temporários foram removidos via AssetDatabase no finally. JSON roundtrip adicional cobre dados legados.
- Unity gerou os metas dos scripts/testes novos. Nenhum asset de jogo foi migrado neste incremento.

Limites históricos desse primeiro incremento: somente 3.1, 4.1 e 5.2 adicionais estavam marcadas concluídas. Modelos puros ainda não governavam a cena; integração, migração, feedback e PlayMode aguardavam os incrementos seguintes.

## Integração e handoff atual

As tarefas 1–16 foram concluídas. Evidências e limitações consolidadas em `validation.md`; 17–18 permanecem dependentes da aceitação humana.

Decisões adicionais verificadas na integração:

- Configuração de reação antes do primeiro hit no spawn, sem reset no Start; rank continua independente de categoria.
- Confirmação interna do dano efetivo antes de DamageReceived preserva o impacto quando o callback cura/reentra/lança exceção, sem substituir a fachada virtual TakeDamage. Confirmação de quebra precede callbacks públicos de apresentação pelo mesmo motivo.
- Escopo local da instância PlayerActor; child scopes compartilham o lote, fechamento root decide um máximo confirmado. HookBus de quebra verifica atacante causal; não transforma ausência de dano ou evento repetido em novo feedback.
- Perfil da Manopla determina hit-stop comum (0,05 s atual); Bow/Spear comuns mantêm timing anterior. Quebra real contribui 0,09 s independente da arma. HitStopRunner usa deadline real e preserva os modificadores de tempo existentes.
- Limites finitos de altura/deslocamento evitam overflow geométrico mesmo quando a entrada não é NaN/infinito. Apresentação local procedural é a alternativa aplicada à ausência de assets específicos.

Migração de quatro assets por MCP salva/reimportada e idempotente (repetição: zero alterações), sem mudar GUIDs. EditMode final: 450/450; PlayMode integrado: 47/47. Regressão ampla: 171 executados, 169 aprovados, 2 falhas de fixture, zero skips; reteste das duas correções e integração: 23/23 aprovados, zero falhas/skips. Resultado amplo obtido em memória via execute_code MCP porque a ferramenta get_test_job omite summary de job failed. Não houve segunda execução integral após ajustes exclusivos de fixture. Testes antigos de pooling, setup homing, ThermalShock e Spawner foram ajustados para os contratos já existentes; lógica de produção desses sistemas não foi modificada para acomodar os testes.

Inspeção MCP de sete cenas e 49 prefabs identificou sete GUIDs ausentes legados e um script ausente. Referências ausentes em Playground, Structure_Prefab, NestedParentArmature_Unpack e PlayerArmature foram comparadas com HEAD; já existiam. Não há diff de cenas/prefabs/metas existentes. Gate humano permanece necessário para avaliar reconhecimento e sensação de combate.
