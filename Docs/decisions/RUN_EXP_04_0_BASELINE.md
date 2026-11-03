# RUN-EXP-04.0 — baseline e lacunas,02/11/2026

Escopo autorizado: consolidar encontros, somente dois candidatos, salas e UX de recompensa; executar sequencialmente04.0–5 e parar. UI não aprovada não será publicada. Sem commit/push, alterações de arma/economia ou novo framework. O progresso atual é a autoridade.

## Estado encontrado e preservado

Projeto confirmado por Unity MCP: D:/Dev/Personal/My_games/Tech-Guy,6000.5.10f1, instânciaTech-Guy@fccafb2b47cd7671, plataformaWindows64. Editor estava fechado; foi aberto com a instalação existente. Primeiras conexões recusadas durante startup, depois ponte existente disponível. Cena inicialNexusBlockout, EditMode, limpa, assistênciafalse. Versões manifest: URP17.5, Navigation2.0.14, Input1.20, UTF1.7; sem migração/dependência nova. BranchgameplayOverhaul/HEAD314d28ec; alterações02/03 e exclusão anteriorDocs.zip preservadas.

1574 hashes e63 hashes de preferências atuais em`Docs/validation/artifacts/run-exp-04/baseline-files-prefs.json`; cópia restaurável de fontes/metas no ZIP adjacente. Snapshot privado do registro somente emLibrary, não destinado à publicação. Não restaurar qualquer baseline03 sobre o usuário. ContextoUnity registrado em`editor-prefs-baseline.json`; Console inicial0 erros.

| Área | Implementação inspecionada | Alvo aprovado / lacuna | Risco |
|---|---|---|---|
| Fragmento | PrefabFirstSector/RunExp03, Rush nativo;18MeshRenderer; pose com IK, mandíbulas/placas | Melee baixo reconhecível; conexões articulares, peso e derrota ainda intermediários | Ocultar tell ou alterar footprint |
| Emissor | Mesmo diretório, Sniper nativo;7MeshRenderer, carcaça/canal articulados | Canal alinhado e LOS aprovados; superfícies, ritmo e reação ainda intermediários | Desalinhar origem/tell/impacto |
| Salas | ManualStageEnvironment.BuildGenerated; duas massas deslocadas, chance45% Combat | Contrajogo aprovado; caixas/materiais planos pouco diferenciados | Decorativo bloquear nav ou parecer telegraph |
| Recompensa | RunRewardUI publicado; Model/View/Lab03 IMGUI isolados, dois layouts | A/B não escolhidos; símbolos compartilhados, contraste/feedback e comparação ainda podem melhorar | Publicação acidental, promessa de sinergia/posse falsa |
| Segurança | Hook anti-chain e ownership nativo; coordenador melee, suportes até2 | Regressão de CC deve ser distinguida de intermitência da fixture | Loop de controle ou validação falsa |

## Owners e modificações planejadas, antes de produção

Encontros: ManualRunRandomizer.PlanEnemies mantém roster4–10/primeiros combatentes/suportes<=2; posições são embaralhadas sem função tática. FirstSectorContentDefinition é a extensão opcional. Ajuste limitado proposto: distribuição espacial de rosters existentes por papel, preservando multiset/rank/contagem e os pontos seguros já válidos; comparar dinâmicas de avanço, tiro e suporte sem obrigar a dupla em toda sala. Nenhum diretor/spawner novo. Desenho efetivo deverá ser confirmado em04.1 antes de integrar.

Arte: cópias próprias dos candidatos03 e novos assets04, manter fontes03/02. Autoria pelo Editor se DCC indisponível. FirstSectorEnemyPose permanece apresentação sobre Actor/AI/CombatActions/reaction; dano/timing são protegidos. Salas: extensão da autoridadeBuildGenerated, detalhes sem collider e sem coleta de nav; mesmas massas/portas/circulações. UI: candidato refinado opt-in no Lab, presenter publicado e escolhas/RNG intactos; detalhes essenciais e inventário real. Arquivos reais dessas extensões e fontes03 foram lidos; nenhum rollout UI está autorizado.

## Baseline observado e bloqueios antes da integração

Job`a4f83a2bd80c49bb8f6a1362f26550a5`:3/4 PlayMode,33,696s. Rota0 atéboss/Nexus, UIoriginal/A/B nas três armas/dois aspectos e Hooker com janela de movimento/dash passaram. Lifecycle03 falhou no controle após quebra, linha43. Sem mudança produtiva04 anterior à falha. Isolado`9d28879fe2734502937743887cc0a3da`:1/1,11,935s, sem correção. Classificação: intermitente de baseline, causa ainda pendente; não transformar3/4 em verde nem atribuir a04. Capturas atuais foram produzidas em destinos únicos03 pelas fixtures legadas; serão referenciadas/copidas para o pacote04.

A comparação após esses testes detectou alteração da moeda do Editor, com arma/resumo/controles preservados. A fixture removiaCoinDrop inicial, mas não isolava CurrencyWallet de invocações novas do Spawner. **Bloqueio de isolamento de testes antes de mais rotas.** Corrigir somente fixture para capturar/restaurar moeda da entrada de cada teste, remover pickups residuais e registrar comparação. A moeda pré-baseline04 foi capturada nesta sessão; somente o efeito comprovado destes testes será desfeito, sem reverter valores anteriores03 nem tocar Playerprefs do Player.

## Skills e documentação

Descobertas e lidas: quatro skills locaisTechGuy e referências; router, unity-mcp/C#/animation/NavMesh, create-game-assets/three-d-pipeline, game-ui-ux, performance-optimization, tech-guy-level-layout/encounters e encounter-spec. Aplicadas no mapeamento de owners, baseline atual, separação autoral/técnica, autoria reversível e regras de contrapressão/LOS. Fontes consultadas:AGENTS/README/visão, direção visual, contratos inimigos/HUD/ownership, requisitos/design/tasks/matriz/ledger/masterplan, relatórios01–03 e evidências associadas. Catálogo não revelou skill dedicada Blender/rig. Blender get_addon_status tentou e não conectou; nenhuma execução/modelagem Blender alegada.

Aceite físico da base02/03 permanece; nenhuma aprovação artística04 ou UIA/B. Performance e validação integrada04 ainda não executadas. Gate04.0: owners mapeados e snapshots capturados; resolver isolamento e caracterizar a intermitência antes de integração afetada. Próximas etapas não estão concluídas por este relatório.

## Fechamento da sequência04

Esta seção não altera os resultados intermediários acima. A integração final foi encerrada em04.5:15/15 Play integrados,1/1 matriz ampliada com12 cenários,33/33 regressões,10/10 efeitos de habilidades e28/28 Edit selecionados.1562/1574 hashes iguais e12 alterações previstas;32 preferências TechGuy atuais iguais, sem restaurar03. Fontes/cenas/metas antigas e UI publicada preservadas, Console0 erros; checker mantém dois diagnósticos históricos. Performance, GUIDs, jobs completos, limites de persistência e aprovação física estão no [relatório de validação](RUN_EXP_04_5_VALIDATION.md). [Entrega e pacote visual](RUN_EXP_04_RELATORIO.md). STOP04.5: arte final, direção de UI e julgamento físico permanecem pendentes.
