# GPR-00.1 — Leitura, inventário e baseline

- Data: 26/10/2026, America/Sao_Paulo.
- Fase/atividade: GPR-00 / GPR-00.1. Status final: COMPLETED após complemento da visão fornecida durante00.2; evidência: NOT_APPLICABLE para design, testes técnicos discriminados abaixo.
- Autorização: pedido atual, somente auditoria/desenho GPR-00.1–00.5; implementação fora do escopo.
- Projeto: `D:/Dev/Personal/My_games/Tech-Guy`; Unity 6000.5.10f1; MCP `Tech-Guy@fccafb2b47cd7671`.
- Branch `gameplayOverhaul`, HEAD `3a3f0ee8ad82734104511b63877d38df512ea146`, alterações locais numerosas. O HEAD sozinho não representa o conteúdo auditado.

## Objetivo, entradas e aceite

Ler instruções/skills, verificar projeto correto, inventariar owners e separar evidência atual de histórica. Entradas: plano WASD v1.1 de Downloads, AGENTS, Docs canônicos, código/assets e MCP. Aceite: ambiente confirmado, inventário rastreável, riscos explícitos e visão original localizada/lida. Último critério pendente.

## Realizado

O worktree do chat (`C:/Users/gabri/.codex/worktrees/b720/Tech-Guy`, HEAD `67d8065a778c3dd91151718abb4dd3f9ca6e73f9`, Unity 6000.3.3f1) não contém os canônicos/skills locais atuais. O MCP confirmou o checkout D: como projeto aberto; documentação desta auditoria é produzida nele, sem sincronizar ou substituir arquivos entre checkouts. AGENTS inexistente no worktree; encontrado e lido integralmente no projeto D:, assim como referências das quatro skills locais. Busca de AGENTS aninhados não encontrou outros no código próprio.

Inventário: GamePreferences/ControlRemappingPanel (controles/configuração); CharControlScript e BasicImpact (navegação, básico/contato); AbilityHolder (quatro slots, custo/estado/cancel); WeaponScript e executores BreakerGauntletCombat/ArsenalCombat/BowBoonCombat/SpearBoonCombat; RunBoons/WeaponRunModifiers/BoonCapabilityGraph (inventário e compatibilidade); ProtocolRuntime/ProtocolBudget (derivados); Actor/PlayerActor/RunStatistics (HP e causalidade); RunRewardPolicy/RunBoons.Manual (tickets); PlayerHUD/MainMenuUI/PauseMenuUI/LobbyInteraction/LobbyArsenal (apresentação e gates); SkillAnimationPlayer/GauntletImpactVfx (apresentação). Detalhamento será fechado em GPR-00.2.

## Skills encontradas, lidas e aplicadas

Raiz instalada: `C:/Users/gabri/.codex/skills/`, arquivos `<pasta>/SKILL.md` efetivamente lidos.

| Skill/pasta | Aplicação nesta fase |
|---|---|
| router | Seleção Unity e disciplinas; não aplicar exemplos de outro motor |
| unity-csharp-scripting | Lifecycle, contratos serializados e ownership existentes |
| unity-input-system + references/rebinding.md | Comparação de ações, maps e overrides; projeto usa pacote 1.20.0 |
| input-systems | Separação ação/binding, edge/hold e conflitos |
| unity-animation | Auditar animação/root motion; não substituir scheduler por espera do Animator |
| unity-scriptableobjects | Separar autoria compartilhada de estado por run; não criar outro event bus |
| unity-navmesh | Desenho de movimento com um único owner; pacote atual 2.0.14 |
| game-feel | Preservar contato, hitstop e cancel; exemplos de timings não são tuning aprovado |
| game-ui-ux | Foco, bloqueios, prompts e remapeamento |
| unity-mcp-skill (nome interno unity-mcp-orchestrator) | Recursos antes de ações e testes reais |
| .agents/skills/tech-guy-game-vision | Identidade, canônicos e distinção proposta/aceite |
| .agents/skills/tech-guy-combat-unity | Dano/reação/cancel/Asura e preservação GI |
| .agents/skills/tech-guy-boon-design | Capabilities, tickets, orçamento e teardown |
| .agents/skills/tech-guy-unity-acceptance | Evidência técnica separada de gameplay/humano |

As genéricas Unity visam 6000.3; ambiente real 6000.5.10f1, URP17.5.0, Input1.20.0, Test Framework1.7.0, AI Navigation2.0.14. Nenhuma atualização/migração de versão. Não foi localizada skill Unity dedicada de UI ou de testes no conjunto selecionado; UI usa game-ui-ux e validação usa acceptance/MCP locais. Não foram aplicadas skills de outros motores nem as cinco skills globais Tech Guy de mapas/jornada: tarefa não altera esses domínios. A origem exata da instalação da coleção não foi atestada por Git; os arquivos instalados do router/especialistas foram verificados, sem presumir uso de todo o catálogo.

## Evidência

| Checagem | Resultado | Registro/limite |
|---|---|---|
| MCP projeto/Editor | PASS | `../validation/artifacts/gameplay-rework/gpr00/editor-inicial.json`; NexusBlockout limpa, Edit Mode, sem run ativa no Editor |
| Bindings efetivos | PASS de leitura | Primary Mouse0, Move Mouse1, Dash Space, Skill1 Q, Skill2 W, Skill3 E, Skill4 F11, FinishSequence F |
| Console inicial | 0 erros na consulta; 1 warning | Aviso preexistente de depreciação Input Manager; activeInputHandler=2 (Both); Console não limpo |
| EditMode AsuraFinishControlTests | 15/15 PASS | job `d7511749935c444badec8449dc1c0170`; JSON na pasta de evidências; contrato atual, não controles futuros |
| Integridade inicial | PASS de coleta | 1597 hashes SHA256 de arquivos próprios/config/skills/metas em integridade-inicial.json; status completo e baseline.json |
| Disco | C: ~152,3GB; D: ~33,0GB livres | Leitura PSDrive, valores aproximados decimais; sem captura massiva |
| Visão original de mutações/sinergias | PASS após complemento | Inicialmente ausente; desenvolvedor forneceu em Docs/gameplay durante00.2, com nome real terminado em(1).md. Leitura integral concluída e papel de fonte principal confirmado |

Não houve importação, build, screenshots/vídeos novos, PlayMode ou playtest humano nesta atividade. GI1.0–1.8 e falhas Mutation/BurnTick são históricos, não reexecutados aqui. Ausência de run no Editor não comprova ausência de jogo standalone externo.

## Arquivos e integridade

Criados `Docs/decisions/GPR-00.1_RELATORIO.md`, `Docs/decisions/GAMEPLAY_REWORK_PROGRESS.md` e evidências em `Docs/validation/artifacts/gameplay-rework/gpr00/`: `baseline.json`, `git-status-inicial.txt`, `integridade-inicial.json`, `editor-inicial.json`, `editmode-asura.json`; modificada a matriz `Docs/validation/01_MATRIZ_DE_VALIDACAO.md`. Nenhum runtime/asset/scene/meta modificado. Não restaurar exclusões ou snapshots antigos. Os JSON iniciais registram leitura, não um backup a reaplicar. Os testes posteriores usam o isolamento de preferências existente; a comparação final, não ausência de escrita transitória de fixture, demonstra preservação dos valores consultados.

## Limitações, status e próximo passo

O fechamento inicial foi parcial pela referência ausente. Complemento posterior: leitura integral de `Docs/gameplay/TECH_GUY_GAMEPLAY_MUTACOES_SINERGIAS_E_SKILLS_MODULARES(1).md`, fornecida pelo desenvolvedor, eliminou esse bloqueio; GPR-00.1 concluída. Não restaurar/renomear a fonte do usuário. Próxima atividade originalmente indicada: GPR-00.2. Implementação não autorizada; aceite da fase fica no consolidado final.
