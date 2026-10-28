# Captura e inspeção visual

**Fonte principal para capturas e comparação.** O modo de observação de `CombatPolishPreview` foi executado no Unity 6000.5.10f1 em 18/10/2026, após a consolidação documental. Resultado técnico e limites estão na [matriz](01_MATRIZ_DE_VALIDACAO.md). A execução do novo modo não valida automaticamente as rotinas históricas abaixo nem aprova game feel.

## Ferramentas existentes e destinos

O novo observador grava execuções brutas em `Library/GameplayObservation/<run_id>/`, ignorada pelo Git. As ferramentas históricas abaixo conservam destinos em `Docs/validation/artifacts/`; apenas evidências selecionadas devem ser versionadas após revisão. Não mover logs ou sequências inteiras para Git.

| Ferramenta | Destino relativo à raiz | Contexto e limite |
|---|---|---|
| [CombatStudyBuilder.FinalizeVisualReference](../../Assets/_Project/Scripts/Editor/CombatStudyBuilder.cs) | `Docs/validation/artifacts/combat-study/WorkshopReference.png` | Referência da oficina; também salva detalhe de prefab. |
| [CombatStudyValidation.Run](../../Assets/_Project/Scripts/Editor/CombatStudyValidation.cs) | `Docs/validation/artifacts/combat-study/CombatStudy.png` | Fixture PlayMode de combate/aura; altera estado temporário e encerra Editor ao terminar. |
| [FirstSectorBuilder](../../Assets/_Project/Scripts/Editor/FirstSectorBuilder.cs) | `Docs/validation/artifacts/first-sector/FirstSector-overview.png` | Captura interna do builder de cena; imagem não comprova geometria da run jogada atual. |
| [LobbySceneBuilder](../../Assets/_Project/Scripts/Editor/LobbySceneBuilder.cs), [LobbyCompactLayout](../../Assets/_Project/Scripts/Editor/LobbyCompactLayout.cs) | `Docs/validation/artifacts/lobby/NexusLobby-preview.png`, `NexusLobby-gameplay.png` | Captura e operações de autoria têm entradas distintas; `PolishAndPreview`/`Apply` também modificam assets/cena. |
| [SharedHudValidation.Run](../../Assets/_Project/Scripts/Editor/SharedHudValidation.cs) | `Docs/validation/artifacts/hud/HUD-*.png` | Cenas Playground/CombatStudy/NexusLobby e estados casting/sem mana; fixture não substitui inspeção da FirstSector atual. |
| [TitleTutorialPreview.Run](../../Assets/_Project/Scripts/Editor/TitleTutorialPreview.cs) | `Docs/validation/artifacts/title-tutorial/*Preview.png` | MainMenu, configurações, controles e tutorial; requer lançamento explícito com `-titlePreview`. |
| [CombatPolishPreview.Run](../../Assets/_Project/Scripts/Editor/CombatPolishPreview.cs) | `Docs/validation/artifacts/combat-preview/*Preview.png` | Pausa e poses estáticas; requer `-combatPreview`. Não mede timing/impacto. |
| [RunRewardPreview.Run](../../Assets/_Project/Scripts/Editor/RunRewardPreview.cs) | `Docs/validation/artifacts/rewards/RewardCards.png`, `RewardBuild.png` | Fixture isolado, com cards injetados que incluem conteúdo legado. Não demonstra o catálogo nem a elegibilidade atual. |
| [BoonPublishedEncounterPlayModeTests](../../Assets/_Project/Scripts/Tests/PlayMode/BoonPublishedEncounterPlayModeTests.cs) | `Docs/validation/artifacts/boons/V1/` | Registros textuais dos cenários exercitados; inventário é injetado, não comprova aquisição natural numa run. |
| [BoonRealRouteSimulationTests](../../Assets/_Project/Scripts/Tests/EditMode/Editor/BoonRealRouteSimulationTests.cs) e [agregador Python](../../Tools/BoonValidation/summarize_boon_route.py) | `Docs/validation/artifacts/boons/V2/` | CSV/JSON por arma e resumo agregado; simulação de ofertas não mede sobrevivência, duração real, diversão ou aprovação humana. |

Os builders podem abrir/salvar/reconstruir cenas e prefabs; as fixtures podem entrar em Play Mode e encerrar o Editor. Inspecionar a entrada específica antes de usá-la. Captura não é operação somente leitura por definição. Não executar rebuild para obter apenas uma imagem sem considerar seus efeitos.

Após os três casos Unity V2 passarem com seus CSV/JSON completos, o agregador pode ser chamado com `python Tools/BoonValidation/summarize_boon_route.py`. Ele lê os dados e escreve `V2-aggregate.json` e `V2-summary.md` na mesma pasta. `--directory` permite selecionar outra execução explicitamente; nunca agregar dados parciais como sucesso. A alteração de destino não modifica o conteúdo da simulação ou suas asserções.

Evidências históricas ainda presentes em `Library/BoonReworkValidation/` são locais e opcionais, não dependências documentais oficiais nem validação da revisão corrente. Não movê-las ou reexecutar exportadores para recriar relatórios antigos automaticamente.

## Contrato do modo de observação (etapas A–F)

Implementação autorizada pelo pedido do desenvolvedor de 18/10/2026; cenário de manopla validado tecnicamente, aprovação humana pendente. Contrato organizado conforme [template de tarefa](../templates/02_TAREFA.md), sem outro relatório.

| Dado | Exigência implementada |
|---|---|
| Contexto | Projeto confirmado via recursos do MCP; `seeded_combat` começa em Edit Mode com cenas salvas/limpas; `current` exige Play Mode já preparado |
| Entrada | JSON finito: perfil, fase `baseline/before/after/candidate`, seed uint64 em string, resolução, cadência/duração e comandos ordenados |
| Resultado | PNG real ao fim do frame, `manifest.json` com hashes/condições/revisão e `events.jsonl`; nenhum render substituto ou tela fabricada |
| Exceções | Recusa cenas sujas, perfil não automatizado em modo seeded, frame de comando zero, duração excessiva e comando não suportado; falhas/cancelamento retêm evidência parcial identificada |
| Compatibilidade | Sem salvar cenas/assets, sem alterar HP/mana/imunidade/tuning ou desligar IA; restaura preferências de arma/moedas, cena/seleção, captura/tempo, qualidade, input e Game View temporários |
| Não objetivos | Não certifica determinismo bit a bit, bindings físicos, FPS de produção, diversão ou aprovação artística; sem MP4/Recorder novo |
| Gate técnico | Só comparar `status=complete` com restauração concluída e integridade dos arquivos; condições diferentes são bloqueadas por padrão |

**Inventário A e decisão:** os previews existentes já usam SessionState, Editor update, Play Mode e ScreenCapture; por isso `CombatPolishPreview` foi estendido por arquivos partial [orquestração](../../Assets/_Project/Scripts/Editor/CombatPolishPreview.Observation.cs) e [coletor](../../Assets/_Project/Scripts/Editor/CombatPolishPreview.ObservationTelemetry.cs). Navegação/entrada no nó reutilizam `ManualRunUI`, `ProgressionDirector`, `ManualRunRandomizer` e NavMesh. Fixtures antigas que injetam dano/recursos ou desligam IA não servem como baseline natural. RenderTexture de câmera pode omitir UI overlay/OnGUI; o novo modo captura a Game View inteira. Builders não são executados. Unity Test Framework continua sendo o único runner de testes. O cliente Python apenas chama o MCP instalado; o comparador trabalha offline.

## Perfis disponíveis e cobertura

A configuração executável principal é [scenarios.json](../../Tools/GameplayObservation/scenarios.json); esta tabela descreve capacidades, sem manter outro catálogo de parâmetros.

| `scenario_id` | Preparação e limite |
|---|---|
| `GAUNTLET_SMALL_GROUP` | Automática: nó 1 real de FirstSector, cinco inimigos, manopla base; demonstrado e comparado |
| `BOW_COMBAT` | Mesma preparação, arco base; perfil implementado, ainda sem demonstração visual desta entrega |
| `SPEAR_COMBAT` | Mesma preparação, lança base; perfil implementado, ainda sem demonstração visual desta entrega |
| `HUD_PRESSURE` | Mesmo encontro; HUD real, sem forçar vida/mana baixa. Pressão específica requer gameplay preparado |
| `GAUNTLET_SWARM` | `current`: preparar encontro real com grupo grande; fixture automática de swarm pendente |
| `ENEMY_TELEGRAPHS` | `current`: preparar inimigo comum/elite/boss real; travessia automática até elite/boss pendente |
| `BOON_CHOICE` | `current`: alcançar escolha/recompensa real; não injeta cards ou boons para simular aquisição |
| `RUN_RESULTS` | `current`: concluir run real; não cria estatísticas ausentes no jogo |

`current` observa sem dirigir comandos ou alterar o tempo de simulação; a seed/contagem efetivas são registradas como encontradas. O rótulo do perfil não prova que o usuário preparou o conteúdo correto: conferir os PNGs. Requer PlayerActor ainda presente; uma cena sem player exige extensão futura, não um substituto visual.

**Verificação VS-01, 20–21/10/2026:** corrigido `SaveObservationState` para não chamar `GetSceneManagerSetup` em Play Mode, operação rejeitada pelo Unity. Quando a observação começa jogando, usa setup vazio e preserva o ramo existente que não restaura cenas de Edit Mode. A correção permitiu capturas `current` completas/restauradas; 10/10 testes de observação EditMode passaram. Não mudou o relógio ou a gameplay.

Para automação externa de combate em `current`, atualizar o cursor no player loop e seguir a posição atual do alvo, usando a câmera de produção e a seleção/raycast de `CharControlScript`. Escrita em `EditorApplication.update` pode ser perdida pela atualização do Input System e produzir mira num ponto fixo; `MakeCurrent` sozinho não comprovou correção. Registrar alvo selecionado, projeção e impacto realmente confirmado; aproximação com seleção ainda vazia não é falha de aquisição do alvo. No VS-01, a automação temporária também isolou/restaurou os outros mouses. Isso valida o driver utilizado, não adiciona auto-target ao jogo nem certifica controles físicos humanos.

Capturas `current` têm simulação livre (`simulation_fps=0` no manifesto). Integridade de PNGs/eventos pode ser verificada com os validadores existentes, mas o comparador estrito de cenários seeded não deve aceitá-las como condições equivalentes. Não alterar metadados para contornar essa restrição. Evidências e limites concretos do VS-01 estão na matriz.

## Acionamento pelo MCP existente

Com servidor HTTP e bridge do Unity já conectados, a partir da raiz do projeto:

```powershell
python Tools/GameplayObservation/observe_gameplay.py discover
python Tools/GameplayObservation/observe_gameplay.py run --scenario GAUNTLET_SMALL_GROUP --phase before --seed 1701 --wait
python Tools/GameplayObservation/observe_gameplay.py status
python Tools/GameplayObservation/observe_gameplay.py cancel
# Depois da alteração autorizada, repetir com --phase after; usar os diretórios devolvidos:
python Tools/GameplayObservation/compare_captures.py CAMINHO_BEFORE CAMINHO_AFTER --output Library/GameplayObservation/comparacao
```

Cliente: [observe_gameplay.py](../../Tools/GameplayObservation/observe_gameplay.py). Endpoint padrão `http://127.0.0.1:8080/mcp`; `--endpoint`, `--instance` e `--project` são explícitos quando necessário. Descobre ferramentas/recursos, fixa instância e recusa projeto diferente antes de iniciar captura. Usa o `execute_code` oficial com CodeDom (as chamadas públicas não exigem sintaxe recente); não cria servidor. Espera até restauração final, não apenas até o último PNG. Timeout do cliente não cancela o Editor: consultar `status` e usar `cancel`.

`run --request arquivo.json --wait` permite agenda explícita de `basic`, `skill` (slot 0..3) e `finish`, com `frame` positivo e offsets de mira `x/z`. As APIs públicas de gameplay decidem aceitação/custos/cooldown; recusas ficam nos eventos. O alvo é o inimigo vivo mais próximo. O perfil determina a arma. Agenda omitida no modo seeded usa básicos; em `current` é vazia. Isso não testa o binding do teclado nem comprova fases não exercitadas.

O seed inicial pode avançar até 255 para encontrar cinco spawns de produção no nó 1; manifesto registra seed pedida e efetiva. O fixture configura `RunSeedSource` em memória antes de Awake, `UnityEngine.Random` e o RNG privado existente de cada EnemyAI na thread principal antes do primeiro Update. Essa última adaptação é explícita nos metadados e falha se o campo deixar de existir; não modifica decisões/regras da IA. A câmera original e seu occlusion/follow continuam ativos.

## Comparação e avaliação

[compare_captures.py](../../Tools/GameplayObservation/compare_captures.py) valida schema, conclusão/restauração, caminhos confinados, PNG/CRC/hash/dimensões, sequência de frames e eventos. Requer mesmo cenário/versão Unity, seed, arma, estado inicial dos atores, agenda, câmera, renderização, cadência e resolução. Revisão/fase podem mudar. `--allow-condition CHAVE` declara variável de experimento e permanece no relatório; não comprova autorização humana. Não usar para ocultar divergência acidental.

Alinha por número relativo do frame, preserva lacunas/tempos, gera `comparison.json` e pranchas PNG com antes/depois/diferença absoluta. Não realinha pelo impacto para esconder atraso. Pillow habilita pranchas; sem ele, `--no-images` permite verificar metadados/eventos, com inspeção manual dos PNGs originais. Não sobrescreve entradas ou saída existente. Dano agregado usa apenas `ActorDamageReceived`, sem somar hooks sobrepostos.

O tempo nominal do PNG seeded é `relative_frame/simulation_fps`, **não duração real do hitstop**. Os eventos também registram tempo de gameplay, unscaled, real e timeScale. Leitura PNG/sampling podem afetar temporização real. Comparabilidade das condições não garante imagens idênticas.

O agente deve abrir os PNGs/pranchas, cruzar timeline e regras canônicas, listar problemas por impacto com frame/evento, distinguir observado de hipótese e propor correção mínima. Lost Ark orienta impacto/telegraphs; Hades, cor/contraste; Diablo, hierarquia e vida/recurso visíveis. A análise não autoriza copiar interfaces ou alterar design. Após autorização específica: corrigir → repetir agenda/condições → comparar → relatar melhora/piora/inconclusivo → revisão humana. Se visão automática falhar, apresentar as imagens e evidências à revisão humana.

## Repetibilidade e evidência

- Manter a mesma cena, seed, posição/câmera, FOV/orthographicSize, timeScale, pipeline, resolução, qualidade e pós-processamento. Descrever diferenças inevitáveis.
- Registrar, junto da execução, `scenario_id`, revisão e mudanças locais relevantes, data/hora, Unity, device/GPU, câmera, resolução, seed e parâmetros alterados. Os previews históricos não geram todos esses metadados; o manifesto do novo modo registra as condições verificáveis automaticamente.
- Para vídeo, registrar fps/timecode, início da habilidade, hit confirm, stagger, stance break, cancel/recovery, telegraph, cooldown e morte/disable pertinentes.
- Vincular apenas artefatos que existam de fato. A documentação guarda referências e resultados verificáveis; não criar links para capturas futuras como se fossem evidência.
- Referências externas servem ao estudo: registrar crédito/link/licença e respeitar direitos de uso.

O ciclo é captura de baseline → comparação com a propriedade visual desejada → gaps ordenados → mudança autorizada → captura equivalente → avaliação humana. Alterações de gameplay exigem autorização própria; uma captura não abre novo escopo.

Tela congelada não prova timing, stagger ou animation feel. Resultado de teste, inspeção visual, observação em gameplay e aprovação explícita do desenvolvedor permanecem separados conforme a [matriz de validação](01_MATRIZ_DE_VALIDACAO.md).


A partir da iteração 1.2, PlayerSnapshot também registra `motion_playing`, `motion` e `motion_time` do SkillAnimationPlayer. São estado/tempo do graph manual, distintos do estado de layer 0 do AnimatorController; campo ausente em captura antiga significa cobertura indisponível. Graph ativo por si só não comprova pose renderizada: comparar PNGs e, quando necessário, testar depois da avaliação automática do Animator. Resultados da Manopla ficam na matriz, sem catálogo paralelo de evidências.
