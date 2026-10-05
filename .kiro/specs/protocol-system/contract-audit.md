# Contract Audit — Sistema de Protocolos

## Task 1 — baseline e decisões de integração

Inspeção estática do código atual e das specs `gauntlet-2-rework`, `run-structure` e `modifier-synergies-theme17`. Fases 2–3 aceitas; iniciar Fase 5 foi autorizado. Preservar alterações extensas já presentes no git status e exclusão preexistente do documento histórico de modificadores.

| Produtor / arquivo | Contrato atual e integração necessária |
| --- | --- |
| Básico / `CharControlScript`, `HitboxDamage`, `PlayerActor` | Receipt HP e lote de feedback; básico nativo possui contexto próprio. Transportar root inteiro para Protocol, sem energia duplicada. |
| Q/W/E/R / `BreakerGauntletCombat` | `GauntletImpactContext` possui Epoch, Execution, RootExecution, Origin e EnergyEligible. Preservar política nativa, etapas e custo; Protocol não usa passagem pela fachada como autorização de energia. |
| Guard Impact / `BeginGuardImpact` | Filho Technique do contexto pai; classificação própria e root herdado. Não tratar impacto adicional como segundo básico nativo. |
| Techniques/Mutations / `GauntletExecutionSnapshot`, `ScheduleChildren` | Modificação/substituição de etapa conserva origem nativa; impacto adicional recebe contexto Technique. Auditar cada entrada na task 6 e conservar produtores já selecionados. |
| Ecos / `BeginChild(...Echo...)` | Contexto descendente existente; criar reserva Protocol antes de agendar, manter origem e cancelar em finally. |
| Zonas / `BeginChild(...Persistent...)` | Pulsos futuros compartilham raiz da execução; não gerar root por pulso para contornar cap. |
| Bow/Spear / `ArsenalCombat`, `ArsenalProjectile` | Projétil chama `TryApplyDamage`, sem transportar raiz Protocol. Adapter por execução/salva será necessário; não alterar identidade de gameplay da arma. |
| Elementos / `PlayerOnHitEffects` | ApplyTo aplica elementos, resolve sinergias e Overflow sem payload completo de origem. Evoluir fachada por adapter/contexto; conservar hooks Burn/Freeze. |
| Choque/Overflow | Choque aplica TakeDamage e posteriormente ReportImpact; Overflow pode desviar do cap sem Resonance. Design exige um receipt por burst e orçamento comum em todos os caminhos. |
| Conductor/Detonation / `RunSynergyEffects` | Resolve reinicia fila/visited/32 por chamada; morto é consultado após dano para classificar elemento. Compartilhar orçamento por execução e capturar morte antes de cleanup. |
| Burn / `BurnStatus`, `StatusEffect` | DPS máximo na renovação; timer começa em zero, tick sem origem em TakeDamage; Update pode Tick com delta zero. Migrar primeiro tick, autoria/ordinal, relógio e limites. |
| Frost / `ChillStatus` | Controle direto de speed/isStopped; restaurar pode liberar outro proprietário. Aplicar política de rank e ownership em task 7. |
| Dano ambiental / `Actor.TakeDamage` sem autor | Não inferir Protocol ou energia; capturar somente fontes explicitamente autorizadas. |

Actor já possui `CombatGeneration`, incrementado em OnEnable. Contextos da Manopla incluem alvo/geração. `CaptureNextDamage` separa confirmação e notificações; `Death` invoca Died e destrói objeto. Snapshot Protocol precisa anteceder cleanup/sinais que removem status, inclusive morte por tick e reuso na mesma sessão.

`RunBoons` é proprietário de inventário/HookBus, aplicação e cleanup; `RunBoons.Manual`, `RunRewardPolicy` e diretor são consumidores dos tickets. `ManualNodeDefinition` hoje tem uma categoria e quantidade por nó; RewardSteps exige migrar snapshot, validator, chave, policy, UI e authoring conjuntamente, sem usar categoria antiga para todos os tickets.

### Migrações explícitas

- Catálogo legado atual contém sete IDs, mas não filtra pré-requisitos de Reactor/Resonance/Overflow na montagem lida de `RunBoons`. O design adiciona esses filtros e cap 3; não declarar que já existiam.
- Manual atualmente oferece somente quatro IDs; D1 expande pool/ordinais e oportunidades sem trocar Mutation ou topologia.
- Burn inicial imediato/default mínimo → primeiro tick 0,5s/DPS autorado; Frost → resistências e ownership; choque → amplificação única; limites por chamada → execução compartilhada.
- Preservar asserts corretos de energia, categorias, feedback, HP, interrupção e tickets. Expectativas substituídas por esses contratos precisam de rationale e cobertura nova, nunca relaxamento sem migração.

### Unity baseline efetivamente observado

MCP conectado a `Tech-Guy@fccafb2b47cd7671`, Unity `6000.5.10f1`, `Application.dataPath = D:/Dev/Personal/My_games/Tech-Guy/Assets`. Cena NexusLobby, limpa, fora de Play; sem falha de compilação. Definição publicada carregada pelo Editor, GUID `4d17687f81ce0dc42b2035a3dec505e3`. Console inicial: quatro avisos de bindings insuficientes em AbilityHolder; nenhum erro retornado. Console não foi limpo.

Baseline PlayMode dirigido: `ThermalShockBurstMagnitudeTests` e `DetonationClassificationTests`, job `9181badced76444aae81298cfbb9b35d`, **4/4 passed, 0 failed/skipped**, 28,775s. Primeira chamada perdeu sessão sem produzir job/resultado; repetição após reconexão executou os testes. Esses resultados verificam o legado antes de alteração, não aprovam os contratos novos.

Próxima ação: implementar catálogo validado (task 2.1), com Inspector/save/reload e testes de dados; integrar inventário na 2.2 antes de mudar tickets.

## Migração de eventos e regressões da Fase 3

OnHit/OnCrit/OnKill permanecem canais de impacto nativo com ProtocolRuntime. Echo/Persistent/Technique e impactos Protocol são observáveis em OnProtocolImpact; não produzem eventos nativos adicionais para trackers ou SplitArrow. Os testes EchoesRequireExecutedProducerAndSurviveOrdinaryCancel e PressureFieldHasFourDistinctPulsesWithoutEchoReproduction migraram apenas a observação dos filhos para esse canal, mantendo contagem, dano, identidade, energia, postura, mana, cancelamento e liberação das raízes. Job `329f013d92154ef3a22b0a9d23ef8d2b` revelou duas expectativas antigas de canal (4 passed/2 failed); job dirigido após migração `1d8f26a7e9434c4f99bdeb1663d9047b` passou 3/3, incluindo preservação dos 33 receipts nativos com cap de 32 impactos Protocol. Não reduziram contagens esperadas nem valores de energia/dano.
