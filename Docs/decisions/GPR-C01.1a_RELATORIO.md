# GPR-C01.1a — Baseline e verificação arquitetural

- Data:26/10/2026 (America/Sao_Paulo). FaseGPR-C01, atividade GPR-C01.1a.
- Status: **COMPLETED no recorte de baseline técnica/virtual**; aceite humano e provas do motor futuro pendentes. Evidência: VALIDADO_TECNICAMENTE somente nos critérios executados; não APROVADO_PARA_ENTREGA.
- Autorização: texto colado do desenvolvedor (cópia/hash na evidência), GPR-00 aprovada documentalmente com ajustes; somente C01.1a/b. Validação anexa é revisão documental, não execução Unity.
- Ambiente: D:/Dev/Personal/My_games/Tech-Guy; Unity6000.5.10f1; MCPTech-Guy@fccafb2b47cd7671; gameplayOverhaul/HEAD3a3f0ee + alterações locais preservadas. NexusBlockout limpa antes/depois.

## Objetivo e trabalho realizado

Reconfirmar ambiente e preservar dados; estabelecer baseline das12 áreas da autorização, separando leitura, API, virtual, teste e humano. Lidos AGENTS/canônicos/visão/plano/relatórios e revisão; skills reais redescobertas. Criado [GPR_C01_BASELINE.md](../gameplay/GPR_C01_BASELINE.md) com cobertura, números, capturas, riscos e limitações.21 PlayMode +15 EditMode atuais passaram. Navegação legada medida a~5 m/s planar cardinal/diagonal; energia100 obtida no dummy e Asura F11 aceita após Busy legítimo, custo147 e consumo único do medidor. Painéis/resultados/controles atuais inspecionados sem salvar novas opções.

## Skills aplicadas

Quatro locais `.agents/skills/tech-guy-{game-vision,combat-unity,boon-design,unity-acceptance}/SKILL.md` e referências. Instaladas em `C:/Users/gabri/.codex/skills`: router, unity-csharp-scripting, unity-input-system (+rebinding), input-systems, unity-animation, unity-navmesh, game-feel, game-ui-ux e unity-mcp-skill (nome interno unity-mcp-orchestrator). Aplicação: seleção, ownership, edge/hold, navegação, clocks/pose, input/UI e evidência. Nenhuma selecionada ausente; não há skill Unity UI/QA autônoma específica nesse recorte, suprida por game-ui-ux e acceptance/MCP locais. Guias6000.3 adaptados a6000.5.10f1/Input1.20/AI Nav2.0.14; nenhum upgrade.

## Arquivos criados/modificados

Criados este relatório, `Docs/gameplay/GPR_C01_BASELINE.md`, cópia da revisão `Docs/decisions/VALIDACAO_GPR00_TECH_GUY_20261026.md`, evidências `Docs/validation/artifacts/gameplay-rework/c01-baseline/` (status/hashes/prefs/jobs/metrics/logs/manifests/8 PNG e autorização). Criados diagnósticos temporários invocáveis **fora de Assets** em `Tools/GameplayObservation/c01_baseline_driver.cs.txt`, `c01_training_baseline.cs.txt`, `c01_asura_baseline.cs.txt`, `c01_navigation_measure.cs.txt`; não são componentes nem uma nova arquitetura de input. Alterados ledger/matriz e estado de aprovação documental. A lista completa da evidência fica no manifesto final; PNGs/eventos completos permanecem em Library/GameplayObservation com runIDs do baseline.

Nenhum script runtime, prefab/cena, .meta, pacote, tuning, binding ou schema alterado. As únicas ações foram controles antigos/diagnóstico/observação e documentação. Testes usam isolamento já existente do resumo. Não restaurar snapshots humanos antigos para obter igualdade.

## Testes/evidência

| Método | Resultado | Registro/limite |
|---|---|---|
| PlayMode foco cursor/movimento/Asura |21/21PASS|job 6881b001422847d1974a22f9a82046da; fixtures, não humano|
| EditMode finalização/bindings |15/15PASS|job b582bf9d07d14eac87006387f430ec9f|
| Capturas current |5 válidas para comandos/UI +1 descartada para input|restauração complete/restored;8 PNG copiados/inspecionados; não todos os frames|
| UI resultados |PASS no probe API|BlocksWorldInput=true; básico recusado; Store real somente lido|
| Preferências/summary |PASS de igualdade|Moedas2597, loadout/unlocks/tutorial/bindings e SHA256 do resumo iguais|
| Integridade |1778 arquivos preexistentes sem mudança|snapshot ampliado;546 exclusões totais idênticas,510 fora deDocs|
| Humano/novo WASD/RMB |NOT RUN|Não implementados; nenhum parecer de feel inventado|

Console de erros consultado ao final; warnings das fixtures mantidos. Falhas temporárias de Roslyn/delegate/filtro de input estão documentadas no baseline, sem ocultar tentativas descartadas nem confundi-las com bugs do jogo.

## Decisões, riscos e próxima atividade

Baseline dos controles **atuais** concluída tecnicamente no escopo descrito; não valida satisfatoriedade do Agent.Move futuro ou aceita hold como política final. Fonte atual aprova cliqueLMB→um pedido e RMB→especial/Asura, substituindo terminal alternativo. Asura acoplada ao index3, conteúdo inicial de X, especial Bow/Spear, política de hold/ChargedShot, Interact e direção de dash permanecem questões de design. C01.1b está autorizada para fechar desenho/readiness; C01.2–5/GPR-01 não estão. Nenhuma autorização adicional é inferida do sucesso dos testes.

Fechamento de integridade/arquivos: [manifesto exato](../validation/artifacts/gameplay-rework/c01-baseline/manifesto-arquivos.json) e [QA final](../validation/artifacts/gameplay-rework/c01-baseline/qa-final.json).1778 hashes preexistentes iguais;546 exclusões preservadas;preferências consultadas iguais;Editor limpo/EditMode e0 dispositivosC01 remanescentes. Essa evidência não amplia o aceite para input novo ou humano.
