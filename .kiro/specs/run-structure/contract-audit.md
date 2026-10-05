# Contract Audit — Fase 4 Run Structure

Atualizado em 04/10/2026 após revisão da auditoria inicial. O histórico dos problemas e da primeira tentativa de validação permanece em task-1-review.md. Esta versão substitui suas conclusões incorretas.

## Contratos e escopo

Fase 1: combat-foundation-rework. Fase 2: posture-enemy-interaction, aceita expressamente em 03/10/2026 (validation.md). Fase 3: gauntlet-2-rework, aceita expressamente pelo usuário (validation.md). Specs procedurais são dependências legadas, não as Fases 1–2 do roadmap; conclusão técnica não implica aceitação humana inferida.

- D1: S → C1 → C2 → {RP/C3} → C4 → {C5/E1→E2} → C6 → B → extração. Quatro percursos, 6/7/7/8 combates. Ramos excluídos não voltam após reconexão/revisita.
- D2: dez Techniques selecionadas da Fase 3, quatro Mutations e quatro Protocols (ignite/frost/conductor/detonation). Sem vitality/focus/transform genérico/Reactor/Resonance/Overflow manual. Até três opções distintas e congeladas por sessão/nó/ordinal/revisão. Alternativa bulwark aplica +40 Armor realmente; primeira Mutation não pode ser substituída pela alternativa.
- D3: transição não cura/reconfigura arma. Reconstrução calcula máximo final e preserva HP absoluto; aquisição legítima de vitality mantém a cura explícita legada. Mana regen continua natural. RefreshLoadout em aquisição mantém reset de timers; não reseta Asura mantendo a arma.
- D4: E1 contém um Elite real existente e quatro comuns; duas aquisições sequenciais de Technique. E2 contém encontro adicional e Protocol. Dois ordinais esgotados entregam duas aplicações de Armor.
- D5: aquisição de Mutation nesta sessão, não oferta/importação preexistente, garante acesso ao Boss. Cancelar UI mantém pendência. Impossibilidade inesperada permite aborto, jamais vitória ou Mutation fabricada.
- D6: autoridade única, fases explícitas, locks locais, rejeição sem mutação. Conclusão candidata aguarda lote/callbacks/confirmações/fechamento do grupo de feedback antes do cleanup futuro e do ticket único.
- D7: Boss exige C6, tickets resolvidos e Mutation efetiva. Morte na mesma resolução vence. ExtractionAvailable não encerra sessão; commit vivo da extração reclama resultado antes da carga. Retry não reaplica resultado.
- D8: ProceduralLegacy=0, ManualSlice=1; RoomType intacto; gerador, fragmentos, descoberta e retorno legados preservados. Novos helpers em Core/Run, sem reorganizar Stages nem terceiros.

## Callers e adaptações

| Sistema | Evidência atual | Adaptação manual |
| --- | --- | --- |
| Stages/ProgressionDirector | Awake valida, gera/materializa e inicia loop de proximidade; OnEnemyDied chama ClearRoom sincronamente | Branch de modo antes de gerar; policy local; candidata pós-lote; manter caminho procedural |
| ProceduralStageEnvironment | Materializa geometria e constrói NavMesh runtime | Não executar em manual; ambiente fixo e navegação autorada exclusivos |
| RoomGraph/RoomType/EncounterGates | Grafo e portas direcionais do legado | Metadados próprios e anchors explícitos; projeção geométrica sem impor grid |
| Core/FirstSectorDirector | Código de fluxo linear antigo; ausente na FirstSector observada | Referência histórica, não novo owner de progressão |
| RunBoons/RewardTrophy | OfferReward(int), pool genérico, RewardChosen sem identidade; troféu se destrói ao reclamar | API contextual com ticket e reabertura; manter APIs legadas |
| AbilityHolder/BreakerGauntletCombat | RefreshLoadout recria timers; Configure retorna imediatamente se _weapon == weapon | Preservar aquisição aceita; cleanup futuro dedicado sem Configure(null) nem reset de energia |
| PlayerActor.RefreshResourceStats | fillToMax cura; padrão preserva proporção | Reconstrução explícita com máximo final e HP absoluto. RefreshLoadout não chama RefreshResourceStats |
| RunBoons.Choose/vitality | +100 maxHP, RefreshResourceStats e RestoreHealthToMax explícitos | Preservar pickup legado; reconstrução não simula pickup |
| RunBoons.Choose/bulwark | AddStat(Armor,40), source RunBoons; mitigação usa Stats | Reutilizar aplicação real para alternativa; remover no teardown owned |
| ScenePortal | Poll de proximidade e LoadSceneAsync sem claim da sessão | Guard opcional do owner para commit/retry; legado sem guard permanece |
| PlayerActor/WeaponLoadout/LobbyArsenal | PlayerActor resolve arma persistida em Awake; WeaponLoadout.LoadSelected usa identificador allow-listed existente | Resolver modo da mesma fonte antes do diretor gerar; informar no Nexus sem trocar arma |

Não existe conflito de reset incondicional de Asura em reward: BreakerGauntletCombat.Configure compara a referência da arma e retorna antes de CancelCast/CancelChildren/EnergyPolicy.Reset quando ela não mudou. Troca real de arma conserva seu lifecycle da Fase 3. Energy/category/epoch não se reiniciam por mapa/reward/porta.

HitStopRunner usa corrotina e Time.realtimeSinceStartup; confirmação do grupo de feedback ocorre ao fechar ImpactFeedbackScope. A fronteira manual deve aguardar resolução mecânica, sem esperar a duração cosmética do hit-stop. OnEnemyDied legado pode registrar ClearRoom imediatamente; mudar somente o fluxo manual.

## Seleção antes de Awake — decisão concreta

Na futura task 5, o campo serializado do diretor mantém default ProceduralLegacy e opt-in ManualSlice. Antes de GenerateAndMaterializeStage em Awake, resolver WeaponLoadout.LoadSelected (fonte já existente que Nexus grava) e validar os quatro slots da Manopla; outras famílias recebem modo legado. Essa consulta não depende da ordem entre Awake do PlayerActor e do diretor. Nexus mostra o modo derivado da mesma seleção antes de carregar; não há parâmetro mágico em SceneManager nem novo estado global. A validação efetiva do player e bindings ocorre antes da materialização. Fixtures usam injeção explícita; FirstSector publicada só muda na task 13.

## Baseline Unity MCP

Instância Tech-Guy@fccafb2b47cd7671; raiz D:/Dev/Personal/My_games/Tech-Guy; Unity 6000.5.10f1. Descobertas 35 capacidades, incluindo Console, execução C#, assets/cenas, refresh e testes. Editor fora de PlayMode, FirstSector limpa. Console não foi limpo.

Inspeção de FirstSector e Nexus por preview scenes (fechadas em finally, sem salvar/carregar a cena do usuário): zero scripts ausentes. FirstSector possui um ProgressionDirector e ProceduralStageEnvironment, player/seed source/objective/enemy prefab/environment conectados e 16 pares arquétipo/prefab preenchidos. Materiais/geometry/director do ambiente preenchidos. Nexus possui player/camera e três pedestais do arsenal conectados.

TechGuy.prefab, PlayerHUD.prefab e PlayerFollowCamera.prefab encontrados sem scripts ausentes. Resources Weapons/Melee/Gauntlet/Gauntlet resolve arma com quatro habilidades. Navigation.asset resolve NavMeshData com bounds centro (-24,1.20,48), extents (36.23,1.85,60.23). Isso captura a autoria existente, não comprova navegação da futura rota manual. O ambiente procedural gera sua navegação runtime.

Testes seletivos via MCP:

| Check | Resultado | Job |
| --- | --- | --- |
| PlayMode BasicAttackR1PlayModeTests.GauntletPhase3RoomRewardPreservesConfirmedEnergyAndCategoryAcrossReequip | 1/1 passou | 01f2a04495d74a618bbc22e8c112a9bd |
| EditMode FirstSectorProceduralTests (autoria, catálogo, portas, fragmentos, Boss/portal e morte) | 9/9 passaram | e936e35c12584eb08eeab69775eeaac2 |

A primeira tentativa EditMode falhou por timeout de inicialização (95adb749528a48f7b8012624cea31254); retry com init_timeout=120000 passou. Avisos de keys/clique/dash/câmera/Animator aparecem na saída da fixture do teste de energia, sem falhas. Avisos anteriores de Fast Script Reload/Input Manager também registrados na revisão. Não se alterou gameplay para silenciar esses avisos. Compilação sinalizada sem erro na inspeção do Editor; não inferir novo build CLI ou playtest humano.

## Próximas etapas e regressões

Task 3: modelos serializáveis, validação bruta, snapshot, quatro percursos e save/reload. Task 4: policy e identidade locais. Integração começa em fixtures; autoria pública somente task 13. Rodar testes específicos de cada helper/consumidor alterado, não a lista indiscriminada da auditoria inicial. Fronteira real de impacto, NavMesh manual, input, morte/extração e duas sessões exigem evidências das tasks 5–14. Gate humano permanece task 15.

## Entrega técnica — 04/10/2026

Decisões D1–D8 implementadas nas tasks 1–14. FirstSector agora publica a rota manual para Manopla, com player ativado pelo único ProgressionDirector após selecionar geometria/NavMesh; Bow/Spear mantêm procedural. Nexus indica o modo pela seleção já persistida. Mapa/UI, ordinais de recompensa, Repair, fronteira de combate e resultado/retorno usam sessão local; não há novo singleton nem migração de RoomType/Resources. Loader local suporta retry do resultado reclamado. Evidências incrementais e falhas corrigidas em validation.md. Este adendo atualiza os trechos de planejamento anteriores sem apagar seu histórico. Gate humano 15 e encerramento 16 continuam pendentes.
