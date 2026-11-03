# RUN-EXP-04.5 — validação integrada, 02/11/2026

Escopo: verificar a consolidação04 na FirstSector real e parar. Integração de encontros/duas cópias de inimigos/apresentação de salas autorizada; UI continua preview. Nenhum boss, arquétipo, boon, economia, kit ou framework novo. Os resultados abaixo são técnicos; não representam aprovação física.

## Projeto, owners e skills

Unity MCP confirmou `D:/Dev/Personal/My_games/Tech-Guy`, Unity6000.5.10f1, instância `Tech-Guy@fccafb2b47cd7671`. Usados execute_code, refresh_unity, recursos de estado, run_tests/get_test_job e read_console pela ponte HTTP existente. Sem segundo Editor/runner, pacote novo ou dependência alterada.

Inspecionados ProgressionDirector/manual session, ManualRunRandomizer, ManualStageEnvironment/NavMesh, FirstSectorContent,16 prefabs nativos, cópias02/03/04, EnemyAI/CombatActions/Actor/reactions, AbilityHolder/controles, executores Gauntlet/Spear/Bow, RunBoons/ProtocolRuntime/budgets, presenter publicado e Model/View/Lab de revisão. Os arquivos produtivos mudados estão enumerados em `integrity-final.json`; novos arquivos/meshes/materials e proveniência em `asset-manifest.json` e relatórios04.1–4.

Skills locais lidas/aplicadas: `tech-guy-game-vision`, `tech-guy-combat-unity`, `tech-guy-boon-design`, `tech-guy-unity-acceptance` e referências. Instaladas: router, unity-mcp-orchestrator, unity-csharp-scripting, unity-animation, unity-navmesh, game-feel, create-game-assets/three-d-pipeline, game-ui-ux, tech-guy-level-layout, tech-guy-enemy-encounters/encounter-spec, performance-optimization; computer-use para mouse nativo. Regras aplicadas: mesmos owners e contatos nativos, testes separados de aprovação humana, baseline atual, captura sem sobrescrita, matriz reproduzível e desempenho com sistemas ativos. Não encontrada skill dedicada de rig/Blender; chamada BlenderMCP não conectou. Isso limita arte a uma revisão intermediária Unity, sem DCC final.

## Jobs finais e cobertura exata

Evidências JSON em [run-exp-04](../validation/artifacts/run-exp-04/). Seleções se sobrepõem a jobs de desenvolvimento; não somar tudo como testes únicos ou declarar suite global verde.

| Job | Seleção | Resultado | Duração |
|---|---|---|---|
| `cd2586b50d234408804d2be5557da77a` | FirstSector e UI integrados | 15/15 PlayMode | 173,517s |
| `e75f95a417d24d38a52bd3894511cdc5` | Matriz ampliada de papéis, incluindo Hookers | 1/1,12 cenários | 79,579s |
| `75f05fe357744cc4adfe3e41a091bd70` | Regressões selecionadas de combate/input/boons | 33/33 PlayMode | 47,268s |
| `4d746fc92cc54f2d8ba0177b48aa16c5` | Content/arte/geração/mapa/slots | 28/28 EditMode | 1,569s |
| `e6b9561b948c4f4aa3ec9576af6f5064` | Efeitos/dano nativo de habilidades nas três armas | 10/10 PlayMode | 11,532s |
| `1c11f19931944dde863eb6dc73da0aba` | Comparação artística aberta e pares de preparação/ataque | 1/1 PlayMode | 11,760s |

Os15 incluem rotas naturais0/1/2 nas três armas até boss/tickets/retorno Nexus; básicos das três armas nos dois candidatos e cobertura; lifecycle de dano/controle/pausa/reciclagem/despawn; comparação artística; salas/nav/LOS;8/32/64 ativos; quatro testes UI. A conclusão das salas e rotas é automatizada, com dano letal de fixture onde documentado, não um playthrough humano sobrevivendo ao combate.

A matriz ampliada usa prefabs04 do Content real: Fragmento; Emissor; dupla; dupla+Grunt+Healer; dupla+Hooker; dupla+2Hookers, cada uma em aberto0/cobertura63. O roster é explicitamente controlado no snapshot; spawn, AI, nav, dano e membership são nativos. JogadorHP10000, sem imunidade. Nos Hookers há reposicionamento inicial declarado na faixa direita para obter conexão real, sem desligarAI. Verificados soltura limitada, movimento pelo binding atual e dash nativo. Logs `04-hook-recovery-*` e imagens `04-mixed-hooks-*`; não equivalem a aprovação de pressão ou sensação.

Os33 cobrem relógio/ownership de um/múltiplos ganchos, pausa/hitstop/morte/unload, movimento direto/parede/dash/foco/UI, especiais reais das três armas via input remapeado, três skills/HUD, aquisição natural Titanic/Conductor, vento/fogo/eletricidade/tripla em produção, contatos Protocol com leases/gerações, Frost e interrupção. Incluem Spear: paredes/pausa/teto32 em alvos densos; Bow: dano interrompe carga, paredes, marcaQ e hold/release do especial. São fixtures específicas e não uma matriz completa de todos os boons em todos os novos inimigos.

Os10 adicionais fecham a distinção entre input e execução: GauntletQ/W/E/R internos comHP/atribuição/receipts reais; SpearHorizon/Siege/Orbit com custo único, contatos e root preservados; BowJudgment curto/completo, tiro carregado com vínculo e minas com cleanup. São fixtures existentes de executores, não dez novas rotasFirstSector. Slots legadosQ/W/E/R não representam uma quarta skill: controles atuais usamQ/E/R para três skills e binding próprio para especial. Não foi executada a combinação cartesiana de cada skill/boon contra cada prefab/variante de sala.

A última inspeção visual encontrou Fragmento oculto pela cobertura no par em câmera normal. A fixture de comparação passou para arena aberta seed0, sem mudança produtiva. A reexecução artística acima passou e acrescentou oito capturas pareadas03/04 de windup/attack dos dois inimigos: executor alcança a fase real, pausa-se o mesmo instante e trocam-se apenas meshes. Poses/timing atuais são comuns aos dois lados; essa comparação mostra a geometria durante a ação, não uma cadência03 artificialmente reconstruída. Imagens abertas/inspecionadas confirmam ambos visíveis na câmera normal. Históricos cobertos anteriores foram preservados.

## Falhas intermediárias e dívida

Baseline antes de04 produtiva: `a4f83a2bd80c49bb8f6a1362f26550a5`3/4, falha CC no lifecycle03; isolado `9d28879fe2734502937743887cc0a3da`1/1 sem mudança. Repetição após isolamento de moeda `12565ffe79f4445fba2977cfa5bf63df`4/4; lifecycle também passou no final15. Classificação: intermitência de baseline, causa não provada. Não houve retiming de gameplay ou enfraquecimento da asserção.

`884828fb86c349a5bc18ebdb5e9da684`3/4: novo observador de ranged liaHP na emissão, antes do tempo de voo. Corrigido apenas para esperar projétil/HP real; reexecução `1aa3e18588154c4c95303d1213859dbd`1/1. Capturas/jobs vermelhos preservados. UI4/4 em dois jobs anteriores; arte1/1 Edit e1/1 Play, salas2/2; detalhes em relatórios individuais. Nenhuma falha produtiva bloqueante nova confirmada no recorte; dívida histórica global continua aberta, sem reexecutar ou aprovar todos353.

Checker da skill:2758 arquivos, um erro histórico `Playground.unity` GUIDausente `9d54880da6396da4c8cb3d4f4d1289a3`, um aviso histórico de `TitanicCompositionFrameSampler` em arquivo de outro nome. Mesmos diagnósticos do checker03. A adaptação emLibrary corrige apenas formatação de path C:/D: do script, sem mudar regras/severidades ou skill instalada. Não corrigir conteúdo histórico alheio para declarar verde. Console final sem erros, sem comando de limpeza.

## Desempenho com AI, dano, navegação e poses ativos

Fonte: `performance-raw-{8,32,64}.json`, resumo `performance-summary.json`; cinco segundos amostrados após aquecimento, Editor Windows. População controlada1:1:1 Fragmento/Emissor/Grunt pelo diretor real. Jogador imune somente no benchmark; inimigos atacam. Não é otimização comparativa ou orçamento standalone.

| Agentes ativos mínimos | Quadros | Quadro p50/p95 ms | BehaviourUpdate p95 ms | NavMeshManager p95 ms | Pose04 p95 ms | GC p95 bytes |
|---|---|---|---|---|---|---|
|8|817|6,03 / 8,24|1,396|0,186|0,096|478004|
|32|516|9,50 / 12,44|1,923|0,348|0,314|728860|
|64|336|14,72 / 18,39|3,075|0,654|0,648|1099162|

| Agentes | MeshRenderers / triângulos | SkinnedRenderers / triângulos | Materiais Mesh | Draw calls Editor p95 | Spawn ms | Cleanup decorrido ms |
|---|---|---|---|---|---|---|
|8|75 /4704|14 /54944|3|500|6,19|436,15|
|32|275 /17248|70 /274720|3|1564|29,45|445,01|
|64|543 /34052|147 /576912|3|3169|45,73|462,35|

MeshSkinning.Update p95:0,107/0,233/0,358ms; MeshSkinning.Skin e Components.NavMeshAgent.State amostraram0, o que não prova custo inexistente. Grunts antigos explicam os SkinnedMeshRenderers; os dois candidatos têm18/7 rígidos. Drawcalls são globais do Editor, não somente personagens; batches=-1 significa indisponível. BehaviourUpdate/MainThread/NavMesh são agregados, não CPU isolada deAI. GC incluiUTF/LINQ/Editor/IMGUI; captura e amostrador têm overhead. Cleanup inclui espera deliberada0,4s e não representa CPU deDestroy. Não remover esses limites ao citar números.

Impactos nativos10/30/54; máximos móveis6/22/49, windups4/12/23, projéteis3/11/14. VFX caracterizado por esses objetos e marcadores agregados, sem timingGPU isolado. Fila/roots/drops0 porque jogador não ataca neste benchmark; não usar esse zero para comprovar orçamento de boons. O teste Spear com alvos densos verifica separadamente o teto32. Após mortes, nenhumActor/ghost/projétil residual.

## Integridade e estado atual do usuário

`integrity-final.json`:1562/1574 hashes04 idênticos,12 alterações esperadas e nenhum arquivo removido no escopo. Os12 são: três arquivos daUI de revisão; três fixtures de teste; FirstSectorContentDefinition; ManualRunRandomizer; ManualStageEnvironment.Procedural; EnemyPose; EnemyDespawn; FirstSectorContent.asset. Arquivos novos são adicionais. Todas as cenas/metas antigas, fontes03/02, UI publicada, controles e definições de armas dentro do baseline são iguais. BranchgameplayOverhaul e HEAD314d28ec preservados; mudanças anteriores e exclusãoDocs.zip continuam. Sem commit/push/desligamento.

InspeçãoUnity reabriu FirstSector:16 famílias antigas,373Fragmentos/389Emissores na amostra128seeds;357Combat cobertos/539abertos. São contagens de conteúdo, não garantia por run. CSV04 próprio, sem sobrescrever evidência03. Dois prefabs04 com missingScripts0, cápsulas0,85/0,8,18/7 renderers. GUIDFragmento `5e96e4399f50d0c42a042dee033e8a6a`, Emissor `576d705b1e1556447b7af975252f403c`; três materiais04 referenciados; FirstSector limpa e missing0. Metas/GUIDs pré-existentes não mudaram.

As32 entradasTechGuy dos escopos Editor/Player iguais ao baseline **atual04**: moedas, loadout, desbloqueios, resumo, controles e tutorial. O vazamento de moeda observado no baseline foi corrigido na fixture e somente seu delta comprovado foi restaurado; scopes por job final não precisaram restaurar moeda. Não houve restauração de snapshot03. Diferenças restantes são quatro IDs/contadores de sessão Unity e `PT_Run` do pacote test-framework.performance, identificado emRuntime/Utils.cs; não são classificadas como progressão do jogo e não foram apagadas.

O baseline cobriu1574 arquivos nos roots listados noJSON e registro do produto; não cobriu universalmente todo arquivo de save/disco. Assets deInput/Settings fora desses roots não receberam edição04; não se inventou um hash de entrada retroativo. Captura legada de movimento foi copiada para04 e somente seu arquivo histórico, comprovadamente limpo antes dos testes, restaurado byte a byte doHEADbase. Valores privados do registro permanecem emLibrary ignorada.

## Revisão e parada

Editor devolvido à cena inicialNexusBlockout, fora dePlay, limpa, Time1, assistênciafalse, seed temporária vazia, nenhum previewUI e nenhum scope de moeda pendente. Verificação em `editor-final.json`, preferências em `prefs-final.json` e Console em `console-final.json`.

Capturas reais das três armas, original/A/B/C, contexto/rank/detalhes, geometria antes/depois com câmera idêntica e salas constam no [pacote portátil](../validation/artifacts/run-exp-04/review/index.html). Montagem final:141 imagens incluídas,17 registros de jobs (incluindo intermediários),0 links faltantes; ZIP21504733 bytes comCRC verificado. Há capturas históricas adicionais no pacote, não141 estados únicos. Estado de montagem em `review-check.json`. Não são mockups ou imagens geradas externamente. Links locais dos relatórios verificados sem faltas; git diff --check sem erros, apenas avisos de conversãoLF/CRLF.

Gate técnico04.5 concluído no recorte descrito. Permanecem revisão física de pressão/CC/telegraphs, julgamento da arte intermediária e escolhaUI. PublicaçãoUI e produção artística final exigem direção explícita. STOP04.5, sem iniciar05.
