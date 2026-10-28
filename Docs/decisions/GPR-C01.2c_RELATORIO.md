# GPR-C01.2c — confiabilidade de movimento e segurança de contextos

27/10/2026 · America/Sao_Paulo · fase GPR-C01 · atividade **GPR-C01.2c**.

**Status: CONCLUÍDA no recorte técnico autorizado, aguardando revisão do desenvolvedor.** Aceitação física da revisão atual e migração completa de controles permanecem pendentes. GPR-C01.3–5 e GPR-01 não foram iniciadas.

## Autorização, preparação e baseline

A solicitação direta autorizou somente C01.2c e aceitou a evidência técnica anterior dentro do escopo testado. A [validação documental recebida](VALIDACAO_GPR_C01_2A_2B_TECH_GUY_20261027.md) foi lida integralmente: não constitui inspeção independente do código nem playtest físico. Sua sugestão de ordem não substitui a autorização direta atual.

Foram consultados AGENTS.md, skills locais/referências, visão de gameplay, plano WASD, requisitos, ownership, ADR-0006, desenho, tarefas e matriz canônica. Projeto real: `D:/Dev/Personal/My_games/Tech-Guy`, branch `gameplayOverhaul`, HEAD inicial `3a3f0ee8ad82734104511b63877d38df512ea146`, com numerosas alterações e arquivos não versionados preexistentes. O cwd antigo do chat em C: não é o checkout aberto no Unity. Nenhum reset, commit ou push foi feito.

Unity **6000.5.10f1**, instância `Tech-Guy@fccafb2b47cd7671`; Input System **1.20.0**, AI Navigation **2.0.14**, Test Framework **1.7.0**. Editor inicialmente em Edit Mode, NexusBlockout, ocioso e desfocado; nenhuma sessão humana ativa era observável. As skills genéricas de Unity visam 6.3 LTS: aplicadas às APIs compiladas na versão real, sem atualizar Editor/pacotes ou introduzir PlayerInput/segundo backend.

O MCP real foi usado pelo cliente HTTP local já existente, após o serviço iniciado pelo usuário. Capacidades descobertas em [mcp-tools.json](../validation/artifacts/gameplay-rework/c01-2c/mcp-tools.json). O conector nativo desktop não estava exposto; HTTP MCP não foi substituído por simulação de motor. Baseline em `git-status-inicial.txt`, `integridade-inicial.json`, `snapshot-before.json` e `console-inicial.json` no [diretório de evidências](../validation/artifacts/gameplay-rework/c01-2c). Console inicial: zero erros e quatro avisos existentes sobre keys incompletas.

## Feedback físico recebido

O desenvolvedor relatou estranheza na animação de caminhar acompanhando a mira; básico dependente de clicar no inimigo; dash direcionado ao mouse, com desejo de escolher mouse ou direção WASD nas configurações. Isso é feedback físico real da versão anterior, não aprovação física de C01.2c. Não foi fornecido relato físico específico de paredes/quinas ou replay de UI.

Registro de encaminhamento: refino de animação/facing permanece pendente; clique LMB sem alvo pertence a C01.3; opção de direção do dash pertence ao desenho/configurações posterior. O comportamento atual do dash para o cursor foi preservado. Nenhum desses pedidos foi usado para ampliar silenciosamente C01.2c.

## O que foi implementado

### Movimento e colisão

O mesmo `PlayerNavigationMotor` continua sendo o escritor de posição do jogador direto. Para **locomoção**, após percorrer a parcela segura até o contato, projeta somente o deslocamento restante na tangente da normal. Reconsulta capsule sweep e limite NavMesh em cada segmento, com até três segmentos e skin existente de 0,02 m. Não normaliza o resíduo, não concede velocidade diagonal extra e não amostra destinos do outro lado da parede. Normal inválida ou saturação do buffer de hits mantém parada segura.

Dash, pulls, reactions, boons e deslocamentos de ações continuam com truncamento direcional, sem adquirir sliding por acidente. São segmentos no mesmo writer, não controladores concorrentes. Root, casts, hitstop e prioridades existentes permanecem no controlador/holder. Velocidade autorada de produção **5 m/s** e dash **20 m/s × 0,25 s** não foram ajustados. A fixture usa **8 m/s**, identificados como valor de teste. Parâmetros de animação continuam derivados do deslocamento efetivo, sem alterar clips, rigs ou Animator Controllers.

### Contextos e intenções

O controlador direto ganhou uma barreira local de soltura que consulta WASD e as oito ações efetivas do perfil atual, incluindo mouse, dash, skills e finalização. Pausa/contexto modal/foco perdido/disable/inicialização invalidam entrada; é necessário soltar os controles de gameplay e atravessar um frame liberado antes de um novo comando.

`AbilityHolder` consulta a mesma barreira antes de aceitar ou armazenar skill/dash/finalização. A invalidação descarta apenas intenções não comprometidas: ground buffer, dash/continuação/deferred input e tap/hold básico. `BasicAttackDriver.DiscardPendingInput` preserva o relógio de cadência. Indicador/acúmulo de charge básico é reiniciado no contexto; nenhum básico novo é disparado por isso. Casts aceitos, impacto agendado e cooldowns conservam seus owners. A pausa não cancela o golpe aceito nem zera recursos/timers.

Abertura de pausa, recompensas legadas/manuais e interação do lobby sinalizam explicitamente a invalidação; cobre também abrir/fechar no mesmo frame. Os caminhos existentes de fechamento de reward/results/pausa já pediam soltura de ataque e agora reutilizam a proteção compartilhada. Não foi criado um menu stack ou singleton adicional. Pull continua podendo desabilitar o controle sem bloquear seu próprio motor forçado.

### Ownership legado e Interact

Q/E/R, index3 especial/F11, finisher configurado e política atual de Asura foram preservados. Não houve ligação de RMB a Asura ou novo básico, nem alteração do gesto direcional legado Shift+mouse.

Perto de estação/arsenal, o lobby continua reservando a ação histórica compartilhada com Skill3 antes do holder; o fechamento informa a barreira para impedir fall-through. O teste verifica abrir e fechar a estação com R sem publicar uma skill. Não foi escolhido G, F ou outra tecla de interação.

**Design separado de Interact:** intenção contextual própria, com identidade da estação/alvo, sequência e geração do contexto; decisão de ownership antes da entrega ao combate; cada press chega a no máximo um consumidor. UI modal precede interação válida, que precede skill; sem alvo contextual, apenas a skill recebe a intenção. O adapter legado continua lendo a binding histórica temporariamente. Separar a ação e sua configuração em etapa autorizada futura, sem mudar índices serializados nem reutilizar FinishSequence. Binding física de Interact permanece aberta.

## Comparação medida

| Cenário | Parada completa anterior / variante sem slide | Sliding atual | Evidência e limite |
|---|---|---|---|
| Parede não baked, pedido (3,0,3) | posição X/Z ≈1,635858/1,635858 | X≈1,635858; Z=3,000000 | `wall-comparison.json`; mesma capsule/skin, sem atravessar e sem exceder magnitude pedida |
| Quina ortogonal | bloqueio esperado | X/Z≈1,635858/1,635858, estável com comandos repetidos | fixture reseta origem antes de construir a segunda parede; sem penetração inicial |
| Borda NavMesh diagonal | truncava o vetor inteiro | conserva componente tangencial e permanece na superfície | teste real Agent/NavMesh, não cálculo geométrico isolado |
| Rampa baked de 15° | baseline histórica de protótipo, não comparação nova pareada | motor integrado sobe >0,3 m em Y e permanece a <0,06 m da superfície | fixture de rampa temporária; não valida toda escadaria/mapa |
| Nexus, W livre | C01.2b mediana ≈4,999993 m/s | mediana **4,999984 m/s** | valor autorado 5; amostra nova real |
| Nexus, W+D na borda | C01.2b parava completamente | mediana **3,535536 m/s**, X 6,5→8,951401 | componente lateral de 5/√2; sem renormalizar para 5 |
| Pausa e retomada segurando W+D | cobertura anterior parcial | posição fixa (8,968699; 0,056667; −4,000720), zero AbilityUsed | CSV novo: estágios5/6; soltura posterior também para |

A comparação de feel com click-to-move legado usa a baseline documentada: destinos, aceleração/rotação/pathfinding diferem de intenção direta. A prova pareada nova compara a restrição de colisão anterior ao sliding, sem trocar velocidade/dash. Não é um novo aceite físico do legado ou da versão atual.

## Testes e resultados exatos

**83/83 casos distintos na bateria normal final**, sem casos ignorados, não é a suíte global:

| Bateria | Resultado | Job MCP | Arquivo |
|---|---|---|---|
| Integração direta, motor, contextos e expiração |23/23 PASS, 8,612326 s |`d772c22bd06f4cf89e8b99b1e00238a3` |`integration-final.json` |
| UI/results, reload real, reward/pause |7/7 PASS, 5,217401 s |`d0f7ba4fcc934474ac1855dff1afbf72` |`contexts-final.json` |
| GI/legado/Asura/Bow/Spear/recovery |30/30 PASS, 24,626751 s |`ba0c658bd8fb41d19ad06a9f4d6d0a03` |`regression-final.json` |
| Dash/cancel/imunidade/cleanup |8/8 PASS, 2,017362 s |`3b37d6f6bc4948bfb2ba1ba79a29e4a8` |`dash-final.json` |
| EditMode identidade/finalização/bindings |15/15 PASS, 0,036380 s |`cab379521ae54d54b644e795f93d3ad0` |`edit-final.json` |

Arquivos `.request.json` preservam os filtros exatos; `.start.json` preservam o despacho. Runner reproduzível: `python Tools/GameplayObservation/c01_2c_validation.py <request.json> <resultado.json>`, usando o MCP do Editor correto. Não executa retries para esconder falhas.

Cobertura nova inclui paredes/quina, borda, slope, pausa/foco/disable com W+Q, abrir/fechar pausa no mesmo frame, fechamento de reward com W+Q, resultados com teclas mantidas, reload real de NexusLobby com W+Q, R contextual, descarte sem reset de cadência e preservação do impacto básico durante pausa. São **12 novos casos**, somados aos filtros existentes.

Primeira iteração nova: 3/4 PASS; teste de quina falhou porque criava a segunda parede atrás do personagem já deslocado. Corrigida a montagem com Warp para a origem **antes** de criar essa parede; limites/asserts preservados. Depois 17/17, 20/20 e a bateria final23/23 passaram. As falhas e resultados intermediários estão arquivados; não são tratados como evidência final verde.

### Investigação do teste intermitente de recovery

`MovementDuringRecoveryKeepsTheCast_UsesLatestValidOrderOnce` passou isolado **1/1 antes das alterações**, no grupo30 inicial e no grupo30 final. Acrescentado somente diagnóstico test-only com CSV e hitch opcional via Editor SessionState; asserções, lifetime0,35 s e tolerâncias originais foram preservados.

- Hitch150 ms após armazenar intenção: **1/1 PASS**, job`7a92a05d27354dac9fbebbff2a637410`. Medido unscaledDelta0,1546081; clock0,5638868, expiração0,7592787: intenção ainda válida.
- Hitch500 ms: **0/1, falha reproduzida deliberadamente**, job`16bd3b824369452080ad013384d1b6f3`. Medido unscaledDelta**0,5160198**, delta escalado limitado a**0,333333343**, clock**0,9262705**, expiração**0,7602507**. Ao liberar o cast, o buffer já expirou; destino não muda. Asserção original esperava distância<0,1 e observou**8,47599506**. `recovery-hitch500.json/.csv` conservam a falha.
- Novo caso negativo de expiração com hitch500 passou na bateria23: comprova que comando vencido não governa o agente. A injeção opcional foi resetada para0 antes das baterias finais e do gameplay; as chaves temporárias de SessionState foram removidas no fechamento (`cleanup-final.json`).

**Conclusão limitada:** demonstrada sensibilidade da premissa “ordem ainda válida” a um frame hitch que excede o TTL, com clocks de intenção e ação avançando de forma diferente. Isso **não prova retrospectivamente** que a falha histórica de C01.2b teve essa causa. Nenhum conserto cosmético, retry como prova de correção ou extensão de tolerância foi feito. A revisão de semântica do TTL sob hitch continua decisão aberta; o comportamento atual rejeita intenção vencida. Não declarar o incidente histórico definitivamente corrigido.

## Game View e observação de performance

Capturas reais **1160×600**, abertas e inspecionadas; teclado/mouse e foco enviados por automação, não playtest humano:

- [W em movimento](../validation/artifacts/gameplay-rework/c01-2c/nexus-wasd-walk.png).
- [Diagonal na borda](../validation/artifacts/gameplay-rework/c01-2c/nexus-wasd-diagonal.png).
- [Parada](../validation/artifacts/gameplay-rework/c01-2c/nexus-wasd-stop.png).
- [Retomada com teclas mantidas](../validation/artifacts/gameplay-rework/c01-2c/nexus-resume-held.png).
- [Pausa com overlay na Game View focada](../validation/artifacts/gameplay-rework/c01-2c/nexus-pause-game-view.png).
- [Resultado armazenado aberto](../validation/artifacts/gameplay-rework/c01-2c/nexus-results-game-view.png).

O driver inicial em background gerou também `nexus-pause.png`, mas sem compor visualmente o overlay IMGUI; não prova a apresentação da pausa. Foi feita captura complementar via MCP após focar/repaint da Game View, com estado IsPaused=true/TimeScale0 registrado em `pause-capture-state.json`. A captura de resultados lê o resumo preexistente, não representa uma run nova da atividade.

Driver reproduzível: `Tools/GameplayObservation/c01_2c_nexus_driver.cs.txt`, 4,5 s em NexusBlockout já em Play Mode; não altera saves/assets. **757 amostras** em `nexus-runtime.csv`, zero AbilityUsed, 5 m/s autorados; dispositivos virtuais removidos ao término. `c01_2c_measurements.py` gera `nexus-measurements.json`. Frames do trecho W: mediana5,338 ms/p95 7,232 ms; diagonal: mediana4,730 ms/p95 6,027 ms. Há frames de0,333333 s em inicialização/retomada; não foram escondidos. Dados são observação de Editor, não CPU Profiler, GC benchmark, build ou orçamento de crowd. O motor reutiliza o array de hits; em contato pode realizar até três sweeps e três raycasts. Não inferir custo isolado desses números de frame.

## Console, integridade e arquivos

Importação/compilação reais concluídas; testes executaram no Unity. Console final consultada: **zero erros**, um warning legado de keys incompletas (`console-errors-final.json`, `console-warnings-final.json`). Compilação também expôs avisos existentes de APIs obsoletas/ScenePortal em arquivos não alterados; preservados em `console-compile.json`. Nenhum comando Clear foi executado. Resultados de testes negativos permanecem nos arquivos próprios, independentemente do estado corrente da Console.

Checker global da skill: **FALHOU**, código de saída1, `ValueError: path is on mount C:, start on mount D:` ao ler pacote externo. Não conta como PASS. Alternativa focada: importação/compilação Unity, testes, rig real sem Rigidbody concorrente, **1795 hashes iniciais**, **1783 iguais**, **12 alterações intencionais**, zero ausências; três GUIDs existentes de movimento preservados e sem duplicação no inventário de metas consultado. Nenhuma cena, prefab, asset, meta, GUID ou balanceamento protegido mudou nesta atividade. Ver `integridade-final.json` e `guids-movimento.json`.

`snapshot-before.json` e `snapshot-after.json` são iguais: bindings legados `[323,324,32,113,119,101,292,102]`, moedas2597, resumo42387 caracteres, SHA256`9751067523EABB7557FFEC504779DB853925D6665AE876DAF8C0C489D52189B3`. São os campos persistentes consultados, não um dump de todas as preferências. Não foi acionado Apply de Settings. Editor final: **Edit Mode, NexusBlockout limpa**, sem teste/compilação ativos ou dispositivos GPR restantes no runtime inspecionado.

Alterações de runtime em relação ao início de C01.2c (não confundir com o diff acumulado contra HEAD):

1. `Assets/_Project/Scripts/Characters/Player/PlayerNavigationMotor.cs`: sliding de locomoção protegido.
2. `Assets/_Project/Scripts/Characters/Player/Actions/CharControlScript.DirectMovement.cs`: barreira e invalidação compartilhadas.
3. `Assets/_Project/Scripts/Characters/Player/Actions/CharControlScript.cs`: ciclo de vida/soltura usa a barreira.
4. `Assets/_Project/Scripts/Abilities/AbilityHolder.cs`: gating e descarte de intenções sem cancelar cast aceito.
5. `Assets/_Project/Scripts/Characters/Combat/Core/BasicAttackDriver.cs`: descarte de input preservando cadência.
6. `Assets/_Project/Scripts/UI/PauseMenuUI.cs`: invalidação na abertura.
7. `Assets/_Project/Scripts/UI/LobbyInteraction.cs`: invalidação na interação contextual.
8. `Assets/_Project/Scripts/Core/RunBoons.cs` e `Core/Run/RunBoons.Manual.cs`: invalidação na abertura de recompensa.

Testes modificados: `DirectMovementIntegrationPlayModeTests.cs`, `BasicAttackR1MovementFluidityTests.cs` e `RunResultsPlayModeTests.cs`, em `Assets/_Project/Scripts/Tests/PlayMode/`. Nenhum script novo em Assets.

Criados em Tools/GameplayObservation: `c01_2c_validation.py`, `c01_2c_audit.py`, `c01_2c_measurements.py`, `c01_2c_nexus_driver.cs.txt`. Criados este relatório, cópia da validação recebida e artefatos em `Docs/validation/artifacts/gameplay-rework/c01-2c/`. Atualizados ledger, ADR-0006, requisitos, desenho, tarefas, plano de validação, migração de controles e matriz canônica. Alterações preexistentes do usuário foram preservadas; status inicial/final arquivados. `git diff --check` no recorte dos12 scripts passou (exit0); a verificação global acusa whitespace em FirstSector/NexusLobby (exit2), cenas já modificadas antes da atividade e com hashes preservados. Não foram normalizadas alterações alheias. Links locais dos documentos alterados verificados sem alvos ausentes. Ver `diff-check.json` e `documentation-links.json`.

## Skills realmente encontradas e aplicadas

Locais em `.agents/skills/`, SKILL.md e referências lidos: **tech-guy-game-vision**, **tech-guy-combat-unity**, **tech-guy-boon-design**, **tech-guy-unity-acceptance**. Aplicação: visão/escopo; clocks, impacto e writer único; não alterar catálogo/IDs/recompensas; evidência real e separação técnica/humana.

Instaladas em `C:/Users/gabri/.codex/skills/`: **router**, **unity-csharp-scripting**, **unity-input-system**, **input-systems**, **unity-navmesh**, **unity-animation**, **game-feel**, **unity-physics**, **unity-mcp-orchestrator** (pasta `unity-mcp-skill`) e **game-ui-ux**. Aplicação: seleção por engine real; lifecycle/C#; dispositivos virtuais e fachada existente; soltura/contexto; navegação/collision; animação por deslocamento e rotação comprometida; resposta sem tuning; casts físicos; importação/testes MCP; ownership modal. Não foram adicionados componentes/exemplos genéricos que substituíssem os contratos do jogo.

Não foi encontrada skill dedicada chamada Unity testing na seleção instalada; cobertura guiada pela skill customizada de aceitação e orquestração MCP. Skills Tech Guy de mapas/jornada/encounters instaladas globalmente não foram aplicadas porque esse conteúdo está fora do escopo. Incompatibilidades concretas: alvo Unity6.3 vs real6.5 adaptado; checker C:/D: falhou como descrito. Não declarar aplicação de skills ausentes nem aprovação de validação não executada.

## Riscos, limitações e recomendação

**Recomendação técnica: manter NavMeshAgent + writer existente, com sliding restrito à locomoção.** Há melhora medida em paredes/bordas sem perda dos contratos testados; não surgiu falha que justificasse migrar toda a arquitetura para CharacterController. Não implica aprovação humana definitiva de feel.

Pendentes: quinas agudas/irregulares, overlap inicial/depenetração, obstáculos móveis/carving, links/escadas/seams de todos os mapas, crowd e custo de build; política de TTL sob hitch e causa histórica exata; refino de animação ao mirar; escolha de binding Interact; dash mouse/WASD configurável. A contenção cobre os cenários executados, não uma promessa de inexistência universal de jitter/teleporte.

Próxima recomendação: desenvolvedor revisar este relatório e testar fisicamente sliding, parada/frente de parede, quina, borda e fechar contextos com teclas mantidas. Depois de aprovação explícita, recortar **C01.3** para clique LMB por intenção, incluindo ataque sem alvo e contratos de buffering/hold/ChargedShot; RMB Asura continua dependente do recorte/decisões aprovados, sem resolver X ou especiais Bow/Spear por suposição. Configuração da direção do dash deve ser incluída no desenho de Settings, mantendo o default atual até decisão.

**STOP em GPR-C01.2c. Revisão solicitada antes de GPR-C01.3.**
