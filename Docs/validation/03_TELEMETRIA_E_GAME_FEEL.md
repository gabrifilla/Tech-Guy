# Game feel e telemetria

## Eventos candidatos (mapear aos eventos reais)

`AttackStarted`, `AttackCommitted`, `HitConfirmed`, `DamageApplied`, `StaggerApplied`, `StanceDamaged`, `StanceBroken`, `LaunchStarted`, `EnemyRecovered`, `TelegraphStarted`, `TelegraphResolved`, `DodgeStarted`, `BoonProcTriggered`, `RunEnded`.

Nomes **ilustrativos**. Não criar um segundo barramento; instrumentar o pipeline existente.

## Pontos de observação realmente encontrados

Inspeção estática de 18/10/2026, workspace `gameplayOverhaul` com base `3a3f0ee` e alterações locais. Esta seção identifica fontes; não declara captura/perfilamento executados.

| Fonte | Dados disponíveis e cuidado ao consumir |
|---|---|
| [`PlayerActor.ResolveAttackDamage`](../../Assets/_Project/Scripts/Characters/Player/PlayerActor.cs) | Calcula ataque e confirma o HP efetivamente aplicado por `Actor.TakeDamage` antes dos callbacks. Não usar dano nominal como dano efetivo, especialmente com resistências, morte ou efeitos encadeados. |
| [`HookBus`](../../Assets/_Project/Scripts/Core/HookBus.cs), pertencente a `RunBoons` | Eventos reais `OnHit`, `OnCrit`, `OnKill`, `OnBasicHit`, `OnBasicKill`, `OnFreeze`, `OnBurn`, `OnStanceBreak`, `OnDash`, `OnExplosion` e `OnProtocolImpact`. Básico também emite hit genérico; somar ambos duplica dano. `Clear` encerra inscrições/dedupes por run. |
| [`ProtocolCombatEvent`](../../Assets/_Project/Scripts/Characters/Combat/Protocols/ProtocolCombatEvent.cs) | HP confirmado com `SessionId`, `RootId`, `EncounterId`, alvo/geração, `Producer`, `Depth`, dano, posição e morte. Não contém, por si só, um ID de habilidade para relatório completo. Correlacionar raiz com execução exige contrato explícito antes de exibir breakdown. |
| [`ProtocolBudget`](../../Assets/_Project/Scripts/Characters/Combat/Protocols/ProtocolBudget.cs) e `ProtocolRuntime` | Contextos/limites por raiz causal e trabalho pendente; usar esses donos para investigar cascatas, não adicionar budget ou raiz paralela. `ProtocolRuntime.Profiling` possui marcador `TechGuy.Protocol.CPU`; presença do marcador não prova custo aceitável. |
| [`AbilityHolder`](../../Assets/_Project/Scripts/Abilities/AbilityHolder.cs) e [`PlayerHUD`](../../Assets/_Project/Scripts/UI/PlayerHUD.cs) | Uso/rejeição, cooldown/estado e feedback de recurso; podem contextualizar execução/input. Eventos de apresentação não devem virar nova fonte de dano. |
| [`ManualRoutePolicy`](../../Assets/_Project/Scripts/Core/Run/ManualRoutePolicy.cs) / [`RunRewardPolicy`](../../Assets/_Project/Scripts/Core/Run/RunRewardPolicy.cs) | Sessão, fase, nó, ordinal/revisão e ticket resolvido identificam oportunidades e concessões. Diferenciar elegível, ofertado, escolhido e efeito aplicado. |

A auditoria acima descreve a base anterior à VS-02A. O resumo analítico implementado em 21/10/2026 está definido abaixo e no [lifecycle de run](../systems/05_RUN_E_PROGRESSAO.md); a tela de resultados permanece futura. Não há aqui baseline aprovado de FPS, GC, duração ou satisfação humana. Os resultados históricos já medidos ficam na [matriz](01_MATRIZ_DE_VALIDACAO.md), sem outra série de relatórios.

## Dados valiosos

- por skill/execution ID: start/commit/active/recovery, duração de hitstop, custo e geração de recursos, hit count, dano causado, procs nativos/derivados, cancelamentos;
- por inimigo: categoria e perfil CC, tempo em stagger/airborne, ataques interrompidos, tempo de telegraph e recuperação;
- por run: duração, reward/boon offer rates, combinações montadas, classes de dano recebido, kills, performance;
- por runtime: FPS, frame-time percentis, GC alloc, pool usage, número de projéteis/entidades, budget consumido e depth de efeitos.

## Causalidade

Não contar dano derivado como se fosse acerto básico adicional; especificar se dano por habilidade usa *skill causadora* ou *origem efetiva* e como efeitos globais são atribuídos. Distinguir evento replicado, echo e dano ao longo do tempo. Nunca exibir precisão falsa.

## Experimentos humanos curtos

- Jogar manopla 5–10 min sem boons e identificar dois momentos de input impreciso/sem peso.
- Repetir arena e arma com um boon transformador; registrar qual decisão diferente foi tomada.
- Capturar falha do jogador por ataque inimigo: havia tempo/sinal de resposta? Era distinguível de VFX do aliado?
- Pedir classificação descritiva (peso/clareza/risco/sinergia) e exemplos, **não** apenas nota numérica.

## Limiar

Não inventar ms, DPS máximo ou budgets universais neste documento. Medir baseline do jogo e fixar thresholds sob aprovação; métricas isoladas não substituem julgamento humano de diversão.

## Coletor de observação implementado

`CombatPolishPreview.ObservationTelemetry` assina callbacks existentes de Actor, HookBus, AbilityHolder e energia da manopla; amostra Actor/Animator, reação, EnemyAI/EnemyCombatActions, habilidade, hitstop e run. Grava JSONL com sequência, frame relativo/absoluto, tempos, timeScale, alias/geração, origem e `evidence_kind` (`callback`, `snapshot`, `sample_transition`). É Editor-only, com fechamento idempotente e cleanup em saída de Play Mode/reload/quit. Erros de escrita são retidos e tornam a captura falha; callback de observação não propaga exceção para o combate.

`ActorDamageReceived` com `has_value=true` é perda efetiva de HP. `HookHit`, `HookBasicHit`, crit e ProtocolImpact podem descrever o mesmo impacto e não são parcelas adicionais. Desde GI1.4, `BasicAttackPerformed` confirma aceitação/setup; o básico autorado da Manopla ainda aguarda contato. Capturas anteriores registravam esse callback após dano síncrono. `HpContactTiming` acrescenta somente metadados do recibo publicado, execução, progresso mecânico e pose no callback de HP; não contém uma segunda parcela de dano. A fase da habilidade só é declarada quando a API existente a expõe; amostra Animator não é marcador autoral de impacto. Snapshot/transição é resolução de amostragem, não instante exato de stagger/cancelamento. Nenhuma causa de cancelamento é inventada.

Há lacunas explícitas: eventos anteriores à descoberta de novo ator/componente; intervalo entre HookBus.Clear e reinscrição; efeitos de ambiente fora de ProtocolImpact; o JSONL histórico não recebe automaticamente os novos metadados causais do resumo; nenhum cancel/boon/launch considerado validado sem ocorrer na agenda. IDs de entidade só valem dentro da execução. PNG e sampling custam tempo e podem afetar hitstop em relógio real. Não usar esse fixture como benchmark de desempenho.

As fontes permanecem as APIs descritas acima. Não existe um segundo event bus, pipeline de dano ou catálogo de boons. O analítico VS-02A observa o mesmo callback final de HP, conforme contrato abaixo. A [matriz](01_MATRIZ_DE_VALIDACAO.md) distingue testes do coletor, gameplay observada e aprovação humana; [captura](02_CAPTURA_E_INSPECAO_VISUAL.md) define os comandos e condições.

## Diagnóstico de resposta — GI1.5

O mesmo coletor acrescenta `InputEdgeObserved`, leitura passiva dos getters rebindáveis usados pelo jogo, e `NavigationSnapshot`, estado real do NavMeshAgent. Não chama InputSystem.Update, não consome/reenvia comandos, não cancela ações e não cria eventos de combate. Dispositivos/teclas/pointer/Shift/foco da aplicação ficam nos metadados; nomes de dispositivos não provam hardware físico ou participação humana. Os testes do sensor usam input virtual explicitamente.

As amostras são ao fim do frame; callbacks de aceitação/rejeição podem aparecer antes da leitura de input no JSONL. Correlacionar `absolute_frame`, relógios e vídeo, sem afirmar ordem subframe. Estado held no início pode preceder a gravação. O foco da aplicação não certifica foco da Game View. NavigationSnapshot informa caminho/destino/velocidade, não recibo do clique; `distance_known=false` significa distância indisponível, não zero real. Basic pending/progress e recibo HpContactTiming continuam sendo fontes diferentes: presença de ação não prova hit, e callbacks sobrepostos não são dano adicional.

| Evidência observada | Classificação permitida |
|---|---|
| Input e AbilityUsed/BasicAttackPerformed no mesmo frame | Aceitação imediata; verificar motion, impacto e HP separadamente |
| AbilityRejected Busy + fase/progresso fora da janela correspondente | Rejeição temporal; skill comum não fica numa fila geral |
| AbilityUsed + hitstop ativo + apresentação que retoma ao descongelar | Aceito sob freeze existente; não classificar como buffer de skill só por haver espera visual |
| Input sem aceitação nem rejeição | Inconclusivo: conferir foco/UI, gates, alvo e vídeo; não inferir automaticamente comando perdido |
| Mudança posterior de path após clique na recuperação | Compatível com espera de movimento; não comprova buffer geral de ofensiva |

SkillSnapshot registra fase/local e elapsed/duration da Manopla; a razão desses tempos dá progresso global. Fronteiras Recovery/idle e quedas de basic_pending são transições amostradas, não recibos universais de cancelamento. Dash e continuação Cadenciada têm mecanismos próprios; aceitação de intenção não equivale sempre à ativação imediata. O básico não possui callback geral de rejeição. Essas lacunas permanecem explícitas.

Roteiro físico e preset `current` em [checklist](04_CHECKLIST_PLAYTEST.md#gi15--resposta-da-manopla-sessão-física-de-812-minutos). Coleta esparsa de PNG reduz custo, mas não substitui vídeo para motion ou teste sem coletor. A sessão natural pode alterar progressão; não restaurar um backup antigo sobre progresso humano novo.

`observe_seconds` opcional (0..30, apenas `current`) limita o trecho pelo relógio real sem impor FPS, captureDeltaTime ou timeScale. Zero mantém o limite histórico por duration_frames. Foi necessário para preparar clipes humanos previsíveis: as sessões físicas produziram mais de1800 frames em30 s. O preset humano usa30 s e um PNG a cada60 frames; a quantidade de PNGs varia com FPS. Pedido inválido/uso no modo seeded é rejeitado antes de iniciar. Esse relógio encerra somente observação, não a run.


## Run Analytics — contrato VS-02A (21/10/2026)

Fonte principal deste contrato: este documento. Lifecycle e persistência em [run](../systems/05_RUN_E_PROGRESSAO.md); resultados, jobs e limitações na [matriz](01_MATRIZ_DE_VALIDACAO.md).

- **Contagem única:** `Actor.TakeDamage` cria `HpDamageTransaction` após mitigação/absorção. `Before - After` é o dano efetivo; o pedido final de HP é `HpAttempt`, não dano bruto. Overkill conhecido é `max(0, HpAttempt - Before)`. O recibo acompanha a publicação existente de `DamageReceived`, inclusive se adiada/reentrante; a propriedade `PublishedDamageTransaction` só é válida durante o callback. Não há novo evento de dano.
- **Deduplicação:** ator + geração + sequência da transação. Intervalos contíguos comprimem os IDs já observados e aceitam publicação fora de ordem. Zero HP removido, imunidade, rejeição e cancelamento sem contato não geram dano agregado. HookHit/BasicHit/ProtocolImpact não são somados novamente.
- **Escopo:** `RunStatistics`, pertencente ao `ProgressionDirector` de `ManualSlice`, observa jogador e inimigos autorados/produzidos registrados. `EffectiveDamage` contém somente proveniência da sessão do jogador. `ObservedEnemyHpLoss = EffectiveDamage + UnattributedEnemyHpLoss`. Dano sem origem ou com sessão antiga fica fora do total do jogador; habilidade desconhecida de origem comprovadamente do jogador entra como `Unattributed` dentro desse total.
- **Causalidade:** contexto imutável da Manopla fornece família, slot, época, execução e raiz. A ativação aceita em `AbilityHolder` e o básico comprometido abrem um contexto transitório; raízes e reservas existentes de Protocols conservam a origem através de corrotinas/projéteis. Efeitos filhos preservam habilidade/execução parental e identificam produtor, separando nativo de secundário. Uma habilidade nativa persistente não vira proc apenas pelo nome do produtor.
- **Transformações e modificadores:** Mutation é tag da habilidade transformada, capturada na execução, não uma parcela exclusiva. Resonance é marcada quando a amplificação realmente participa; não se calcula dano contrafactual por boon. Não atribuir percentuais individuais a Technique/Augment/Protocol por mera presença no inventário. Dano secundário confirmado de Overflow/ThermalShock/Conductor/Detonation/BurnTick pode ser agrupado por produtor, mantendo a raiz.
- **Queimaduras mescladas:** o runtime conserva maior DPS e renova duração; não há parcelas individuais demonstráveis por aplicação. Origens de habilidades/Mutations diferentes ficam `mixed_burn_origins`, habilidade `Unattributed`, com até oito pais e flag de truncamento. Execuções diferentes da mesma habilidade podem ser agrupadas como `multiple_executions`. Mistura entre sessões ou com origem ausente não ganha crédito de uma sessão conhecida. Nada altera DPS, ranks, timers ou budgets.
- **Reconciliação:** `Damage` é a partição exclusiva. `Abilities.EffectiveIncludingChildren` é outro agrupamento das mesmas transações, incluindo filhos; nunca somar os dois níveis. Acertos são transações positivas de HP por alvo, separados em nativos/filhos; não são quantidade de ImpactEvents ou animações. Usos são `AbilityUsed` e `BasicAttackPerformed`; inputs rejeitados/bufferizados ainda não executados e continuações de R sem novo evento não fabricam usos.
- **Demais fatos:** kills totais observadas vêm de `Died`; crédito do jogador exige recibo letal correspondente. Dano recebido usa HP real do jogador, sem promessa de identificar cada agressor. Build contém IDs/famílias/quantidade adquirida; recompensas usam ticket resolvido por sessão+nó+ordinal. Destaques são maior dano confirmado por habilidade/fonte, sem nota de sinergia ou causalidade marginal estimada.
- **Relógios e volume:** duração de início da sessão manual ao outcome reclamado, com relógio real incluindo pausa e relógio escalado de `Time.timeAsDouble`. Sem DPS/tempo ativo de combate inferido. Agregados, histórico de fase/recompensa e somente 64 amostras recentes de HP; nenhum snapshot por frame no save, nenhum objeto Unity serializado. Histórico JSONL/PNG continua ferramenta de observação separada.

Limites: cobertura de produção inicialmente `ManualSlice`; não prometer retomada de run, histórico de várias runs, telemetria causal completa de fontes legadas/ambientais, dano evitado, cura, fonte detalhada de dano recebido ou contribuição marginal de cada boon. Ausência de atribuição é explícita. Validação técnica não é aceite humano de game feel.
