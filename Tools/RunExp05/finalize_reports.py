"""Write05.4 and consolidated handoff from the actual captured results."""
from pathlib import Path
import json

ROOT=Path(__file__).resolve().parents[2];OUT=ROOT/'Docs/validation/artifacts/run-exp-05';DOC=ROOT/'Docs/decisions'
def read(name):return json.loads((OUT/name).read_text(encoding='utf-8'))
perf=read('performance-summary.json');integrity=read('integrity-final.json');review=read('review-check.json')
names=[('final-play.json','FirstSector e recompensas, incluindo matriz12cenários'),('regression.json','Regressões selecionadas de combate/input/boons'),('skill-effects.json','HP/atribuição e efeitos nativos de habilidades'),('final-edit.json','Conteúdo, geometria, encontros e arte')]
table='| Seleção | Job | Resultado | Duração |\n|---|---|---|---|\n'
for name,label in names:
    data=read(name)['data'];summary=data['result']['summary']
    table+=f'|{label}|`{data["job_id"]}`|{summary["passed"]}/{summary["total"]}, falhas{summary["failed"]}, skipped{summary["skipped"]}|{summary["durationSeconds"]:.3f}s|\n'
editor='| Ativos | Quadro p95 baseline04→05 ms | BehaviourUpdate05 p95 ms | Nav05 p95 ms | Pose05 p95 ms | GC05 p95 bytes |\n|---|---|---|---|---|---|\n'
geometry='| Ativos | Renderers rígidos | Triângulos rígidos04→05 | Skinned / triângulos, inalterados | Materiais rígidos04→05 | DrawcallsEditor05 p95 | Spawn05 / cleanup decorrido ms |\n|---|---|---|---|---|---|---|\n'
standalone='| Ativos | Quadros04/05 | Quadro p95 04→05 ms | GPU p95 04→05 ms | Memória alocada05 p95 MiB |\n|---|---|---|---|---|\n'
for count in (8,32,64):
    a=next(x for x in perf['editor'] if x['count']==count and x['version']=='baseline04');b=next(x for x in perf['editor'] if x['count']==count and x['version']=='current05')
    marker=lambda key:b['markers'][key]['p95']/1e6
    editor+=f'|{count}|{a["frameMs"]["p95"]:.2f}→{b["frameMs"]["p95"]:.2f}|{marker("BehaviourUpdate"):.3f}|{marker("NavMeshManager"):.3f}|{marker("RUNEXP03.EnemyPose"):.3f}|{b["markers"]["GC Allocated In Frame"]["p95"]}|\n'
    geometry+=f'|{count}|{b["renderers"]}|{a["triangles"]}→{b["triangles"]}|{b["skinned"]}/{b["skinnedTriangles"]}|{a["materials"]}→{b["materials"]}|{b["drawCallsEditor"]["p95"]}|{b["spawnMs"]:.2f}/{b["cleanupMs"]:.2f}|\n'
    a=next(x for x in perf['standalone'] if x['population']==count and x['version']=='04');b=next(x for x in perf['standalone'] if x['population']==count and x['version']=='05')
    standalone+=f'|{count}|{a["frames"]}/{b["frames"]}|{a["frameMs"]["p95"]:.2f}→{b["frameMs"]["p95"]:.2f}|{a["gpuMs"]["p95"]:.2f}→{b["gpuMs"]["p95"]:.2f}|{b["allocatedMemoryBytes"]["p95"]/1048576:.2f}|\n'

body='''# RUN-EXP-05.4 — validação integrada e entrega para revisão

02/11/2026, America/Sao_Paulo. Objetivo: encerrar a fatia visual autorizada na FirstSector real, com combate preservado, comparações locais, custo observado e integridade do estado atual. O alvo é disponibilidade técnica e evidência; aparência e game-feel continuam dependentes de revisão humana. STOP05.4.

## Auditoria, implementação e ferramentas

Baseline atual05 antes de produção:2058 arquivos emAssets/_Project/ProjectSettings/Packages,63 entradas de registro e32 preferênciasTechGuy existentes. BranchgameplayOverhaul eHEAD314d28ec22ae2d242409fbfd10ea7f8368dd76e9, alterações anteriores02–04 e exclusão anteriorDocs.zip preservadas. Não houve commit, push, branch ou desligamento nesta etapa. Não se restaurou snapshot03/04.

UnityMCP confirmou D:/Dev/Personal/My_games/Tech-Guy, Unity6000.5.10f1, instânciaTech-Guy@fccafb2b47cd7671. Executados refresh/import, inspeçãoC# e referências, recursos de estado, UTF e Console. BlenderMCP9876, Blender5.2.2LTS: fontesDCC reais com UVs/atlas/normais, FBX calibrado e guias separados; Scene originalCube/Camera/Light preservada. Fonte.blend não foi confundida com rig/clips finais.

Implementação05.0–3: direção de trabalho Arquivo estratificado; C05 opcional com8 ilustrações originais, contexto/rank e feedback breve; Fragmento18renderers/5772tris e Emissor7/2172 emcópias05; coberturas/cartuchos e racks de serviço sobre os mesmos volumes. Componentes nativos continuam donos deAI, ataques, hitboxes, reações, IK, geração, tickets e progressão. Nenhum stat, novo boon/boss/arquétipo, economia, binding, Singularity, Q/E/R modular ou pipeline global foi alterado.

Arquivos produtivos modificados no baseline são enumerados emintegrity-final.json; novos arquivos/fontes/GUIDs/hashes/proveniência emasset-manifest.json e relatórios05.0–3. FirstSectorEnvironmentArt só muda apresentação, com restore04 e liberação das malhas que cria. A proposta futura2.5D pedida depois foi documentada separadamente, sem renderer/conversão nesta frente.

Skills aplicadas: quatroTechGuy locais e referências; router, unity-mcp-orchestrator, unity-csharp-scripting, unity-animation, unity-navmesh, game-feel, game-ui-ux, create-game-assets/art-direction/raster/three-d/provenance, imagegen, level-design/tech-guy-level-layout e performance-optimization. Computer-use foi usado para mouse nativo; unity-build-pipeline para build isolado. Mantidos owners nativos, câmera real, saves atuais e distinção entre teste e aprovação. Nenhuma skill dedicadaBlender/rig foi encontrada ou inventada.

## Seleções finais exatas

'''+table+'''
Seleções e resultados integrais em `validation-selections.json` e nosJSONs acima. Não somar jobs de desenvolvimento/reexecuções como testes únicos. Não declarar a suite histórica inteira verde.

Os21 incluem14FirstSector e7UI: rotas naturais0/1/2 nas três armas até boss/tickets/Nexus; básicos contra ambos candidatos e cobertura; lifecycle de dano/pausa/controle/reuso/despawn; arte04/05; ambiente aberto0/coberto63, duas rotas eLOS;8/32/64 ativos; matriz com12cenários de papéis isolados/dupla/mistos e um/doisHookers; C05 emtrês resoluções/armas, contexto/rank, input e mostruário. Conclusão de salas/rotas é automatizada e usa dano letal/imunidade quando declarado, não sobrevivência humana. A matrizHooker mantémAI eHP real, sem imunidade, e verifica janelas nativas de movimento/dash.

O teste novo de sala executa slots internos0/1/2 (bindings atuaisQ/E/R), especial3 e dash para cada arma, AI ativa, mana/Asura e imunidade somente na fixture. Aceitação de comando, custo real e recuperação são verificados. Não afirma que todo skill danifica cada prefab. Os33 cobrem ganchos/stunlock/relógios, fogo/vento/eletricidade, controles remapeados, especiais, paredes, pausa/hitstop, protocolo/gerações/leases, Frost, morte e teto32 deSpear; os10 adicionais cobrem contatosHP/atribuição de executores, Bow curto/completo/minas/charge eSpear órbita/retorno/Horizon/Siege. São seleções concretas, não produto cartesiano de todos os boons.

## Falhas intermediárias e correções

- UI `67ffd450984d42c79768b456035f5d97`:5/6,20,819s. Fixture lia choices depois deApplyOffer já fechá-las. Corrigida a fixture para abrir novo request e repopular o mostruário explícito; reexecução `4e2ee2d2a7a64b11ada186ae0c3e1dd6`:6/6,21,189s; finalvisual `7fcc50d08c6b497b9dc626ddf2b19da7`:6/6,23,018s. Correção deFrame05 retirou linhas espúrias deGUI.matrix emwide, sem alterarC04.
- Arte `5675aa08bf01495caf1f0793d538495a`:1/1 mecânico, mas primeira captura ciano inadequada. Reexecução `d0b42145bf444eb3beea82e8ba76446a`:1/1 com materiais corretos, sem mudá-los. Causa compatível com aquecimento inicial de shader, não retrospectivamente provada. Fixture passou a compilar/esperar os passes; `5f39b0a28e514783a92b8642b46cce16`:1/1,10,101s, e final21 também passou. Imagens ruins preservadas, fora dos comparadores finais.
- Ambiente inicial `c7822075a0bc4dee9e8d8ae0f49a352d`:1/1,15,414s. Mesmo collider count/bounds e duas rotas nos estados04/05; LOS e movimento reais.
- Integração nova `4eb68ba0b8914c77981987c659beed94` não iniciou porCS0103: testePlay referenciava classeEditor diretamente. Corrigido paraExecuteMenuItem real. `d25be072df654c39a773209f6a51467a`:1/2,1,763s: teste de skills tentava agir antes do frame de liberação de input. Corrigida apenas a fixture para aguardarGameplayInputReady, sem remover o gate. `7e0c1c0f5c0742c386714df8db4e0eb5`:2/2,18,133s; depois ambos passaram no21.
- Build inicial recusado pelaUnity por destino emLibrary; mudado paraBuilds/RunExp05Profile, já ignorado. Agendamento atravessou reload sem relatório conclusivo; chamada direta produziuSucceeded apesar da respostaMCP ter sido perdida durante build. Relatório salvo e aplicativo executado são a confirmação. Inspeção auxiliar de cena usou uma referênciaScene depois deCloseSingle e perdeu seu path; corrigida para guardar a string antes da troca e repetida, sem mudar cenas.

Nenhuma asserção correta foi enfraquecida e nenhum tuning foi mudado para obter verde. O registro individualEdit05 antigo conserva statusSucceeded no cacheMCP, mas seus detalhes expiraram; a seleção final29 contém esse teste e preserva resultado completo.

## Custo observado noEditor

'''+editor+'\n'+geometry+'''
Cinco segundos após aquecimento, população1:1:1 Fragmento/Emissor/Grunt pelo diretor real. AI/nav/dano/poses continuam ativos. Jogador imune e sem atacar; fila/roots0 não provam orçamento de procs. O teto32 é validado separadamente. GC incluiUTF/LINQ/Editor/IMGUI; BehaviourUpdate é agregado e não custo isolado deAI. Drawcalls são globais doEditor; batches=-1 e marcadores ausentes são indisponíveis. Cleanup inclui espera deliberada0,4s, não CPU deDestroy. Custos individuais deVFX/GPU e memória deEditor não foram medidos aqui.

Baseline04 foi capturado nesta entrada05, não os números antigos04 usados como orçamento. Blender/ferramentas e estado doEditor diferem entre sessões; não atribuir diferenças exclusivamente à arte. Geometry acima é só atores rígidos; skinned deGrunt e ambiente são separados. Racks acrescentam2292tris por módulo, combinados emuma malha/material por arena, e coberturas3780tris cada; não são geometria grátis. Compartilhamento de atlas reduz materiais rígidos3→2, preservando18/7renderers dos candidatos.

## Standalone release realmente executado

BuildWindows64 Mono2x,202090787bytes,45,066s,0erros e9avisos. Mensagens completas emstandalone-build-messages.json: umCS0108 históricoScenePortal, seis de APIs de busca deprecadas no harness novo e dois de reconexãoMCP. Permanecem avisos, não ocultados como build sem warnings. O harness só existe emcenaReview e exige--runexp05-profile; não está emcenas produtivas.

App.info confirma produtoTechGuyReviewLocal/RUNEXP05Profile, separado dos saves do jogo. PlayerSettings decompany/product/frameTiming foram restaurados e os hashes deProjectSettings coincidem com o baseline. Lista de build própria, sem mudar cenas globais. Cópia diagnóstica daFirstSector altera somente a seed para0; mesmos componentes/ativos nativos. Não é um novo modo de jogo.

Hardware: Ryzen5 5600X eGeForceRTX3060Ti. Resolução efetiva1920×1080, aplicada pelas preferências nativas apesar do argumentoCLI1160×600. Cada par usa a mesma população8/32/64, troca meshes/materiais/posevisual e todas as camadas de ambiente,1,5s de aquecimento e4s amostrados. Ordem04→05, invertida em32. Os inimigos continuam se movendo/atacando: posições, windups e projéteis não são os mesmos instantes entre amostras.

'''+standalone+'''
GPU veio deFrameTimingManager; considerar apenas amostras positivas, contagens emperformance-summary. MainThread disponível; marcadores deAI/nav/pose/GC/drawcalls/batches não disponíveis no release são NOT MEASURED, não zero. Memória é alocação totalUnity, nãoVRAM nemworking set. Ambos conjuntos de assets estão carregados no processo, portanto não isola o custo residente de05. Triângulos rígidos aumentaram cerca de5× nos rosters; isso está reconhecido, sem redução especulativa que sacrifique arte antes de medição controlada.

As diferenças de p95 não demonstram otimização: por exemplo,32/05 tinha mais agentes emmovimento que32/04, enquanto64/05 tinha menos que64/04. Mudanças de fase/câmera/visibilidade e aquecimento confundem uma inferência causal. Não foi observada regressão bloqueante nesse recorte, mas não há orçamento de hardware aceito nem perfil completo de combate com todas as habilidades/procs. Standalone saiu sozinho após seis relatórios ecomplete.txt; capturas confirmam renderização/materials/ameaças ativos.

## Integridade e limites do estado atual

'''+f'{integrity["unchangedFiles"]}/{integrity["baselineFiles"]} hashes idênticos, {len(integrity["changedFiles"])} alterações esperadas e{len(integrity["missingFiles"])} arquivos removidos no escopo. Cenas/metas antigas, inputs, definições de armas/boons, reward tickets/weights e gerações anteriores permanecem byte a byte. Os8 mudados sãoView/Model/Lab de revisão, fixture de básicos, ContentDefinition, criação opcional de apresentação noEnvironment, EnemyPose eContent.asset. Novos assets/fontes são adicionais.\n\n'+'''
ReaberturaUnity deFirstSector: GUIDf62bb8b29264e014aa2e95e916388ed9, limpa, missingScripts0; dependências eContent persistidos. Prefabs05 GUIDsaf5d41ab4bac433419d3d4399f164eca/b47ff7d4f586a5b4abd96e57dbfaf617, cápsulas0,85/0,8, missing0. FontesFBX não readable, meshes assados com UVs/tangentes/normais; referências de ambiente válidas. Verreferences-final.json.

'''+f'As{integrity["gameplayPreferencesCompared"]} preferênciasTechGuy existentes na entrada05 permanecem com os mesmos hashes; removidas/alteradas existentes:{integrity["baselineGameplayPreferencesChanged"]}. Há{integrity["addedGameplayPreferences"]} adições no escopoPlayer original (controles/resumo), preservadas. O lastWrite desse registro é02/11 23:09UTC, anterior ao build isolado03/11 01:51UTC. A origem dessas adições não foi estabelecida; não atribuir automaticamente ao perfil nem apagá-las para simular integridade perfeita. Isso limita o gateG: não é alegação de registro inteiro inalterado.\n\n'+'''
Valores privados ficam somente emLibrary. Diferenças de contadores de sessãoUnity/PT_Run foram registradas sem apagar estado. Os scopes de moeda dos jobs33/10 não detectaram delta e não restauraram moeda. Não houve restauração global de save ou snapshot antigo. A comparação inicial de paths acusou seis materiais ausentes por leitura doJSON emcodepageWindows; o leitor foi corrigido paraUTF8 e hashes confirmaram os mesmos arquivos, sem renomear assets.

A captura legada de movimento regravada pelo teste foi arquivada emmovement-regression-current05.json; somente esse arquivo histórico comprovadamente limpo no baseline foi restaurado byte a byte doHEAD, semreset/checkout. Checker2870arquivos: ummissing-script históricoPlayground GUID9d54880da6396da4c8cb3d4f4d1289a3 e umaviso históricoTitanicCompositionFrameSampler/filename. Não corrigidos fora de escopo, não declarar global verde. Console final semerros; nunca foi limpo manualmente.

## Revisão e aceitação

'''+f'Pacote portátil: [HTML](../validation/artifacts/run-exp-05/review/index.html) e [ZIP](../validation/artifacts/run-exp-05/RUN_EXP_05_REVIEW.zip), {review["images"]} imagens locais referenciadas, links faltantes{len(review["missing"])}, CRCdoZIP semerro. Imagens deUnity, comparadores original/C04/C05 nas três armas, contexto/rank, guia de cores/materiais, inimigos04/05/fases, ambientes04/05/circulação/LOS e seis amostras doaplicativo incluídos. Conceitoimage_gen explicitamente separado de arte implementada.\n\n'+'''
GatesA/B/C/D: implementação e testes disponíveis; aprovação visual/comportamental humana pendente. GateE: medições limitadas e lacunas explicitadas, sem orçamento aceito. GateF: nenhum bloqueador novo confirmado nas seleções executadas. GateG: arquivos e32prefs existentes preservados, adiçõesPlayer sem atribuição permanecem como limitação. GateH: PENDING DEVELOPER APPROVAL.

NOT DONE: rig deformável/clipsDCC finais, pintura final porparte/LOD dedicado, catálogo inteiro ilustrado, redesign de todas as salas ou sistema2.5D. NOT TESTED: outros hardwares, custo isolado de cadaVFX e combinações cartesianas de todo boon/arma/ator. O conceito é mais detalhado que o modelo; não afirmar correspondência final. Prompt original doatlas salvo; prompts exatos da correção/conceito não foram retidos no repositório, logo a proveniência não finge recuperação verbatim completa.

Reproduzir pelo menu04 seed63 e portalNexus, assistência desligada; primeira sala contém cobertura e dupla emroster legítimo. Seed0 permite comparação aberta. Menu05 abre laboratório, C05 temporária na run e restorepublicado; mostruário é assistido/somenteleitura. Ambiente04/05 tem toggles próprios no menu05. Confirmar emrunreal consome ticket real; comparadores não concedem automaticamente.

Editor devolvido aNexusBlockout limpa/Edit, Time1, assistênciafalse, próxima seed vazia, nenhum previewUI ou scope moeda pendente. Nada foi forçado a fechar com dados não salvos. Alterações anteriores preservadas. STOP05.4; próxima frente depende da revisão. O novo pedido2.5D foi registrado emMAP_ART_2_5D_FUTURE_PLAN.md e fontes canônicas, sem iniciar06.
'''
(DOC/'RUN_EXP_05_4_VALIDATION.md').write_text(body,encoding='utf-8')
summary='''# RUN-EXP-05 — entrega da fatia visual

02/11/2026. A frente foi concluída tecnicamente até05.4 e parou. A revisão física continua aberta: **C05 não foi publicada permanentemente**, modelos/ambiente não foram declarados arte final e nenhum gate humano foi concedido por testes.

## O que está disponível

- **05.0 DONE:** auditoria atual e direção de trabalho Arquivo estratificado, guia visual de materiais/valores e conceito identificado como referência.
- **05.1 DONE técnico:** C05 opcional com ilustrações originais, contexto real/rank, foco/detalhes/confirmação e movimento reduzido. Comparação original/C04/C05 nas três armas/resoluções, mouse/teclado/gamepad e mostruário somente leitura. Aprovação/publicação pendentes.
- **05.2 DONE no recorteDCC:** Fragmento remodelado, Emissor com passe secundário coerente, fontes.blend/FBX/UVatlas/normais e cópias05 naFirstSector. Mesmos pivôs/cápsulas/AI/contatos/IK. NOT DONE: rig/clips/pintura final de produção; não prometer o nível de detalhe do conceito.
- **05.3 DONE técnico:** cartuchos de cobertura e infraestrutura de arquivo, piso quieto e coerência limitada na sala aberta. Física/nav/duas rotas/LOS/spawns permanecem; comparação04/05 disponível.
- **05.4 DONE com limites:** UTF, build release executado e8/32/64 ativos emEditor/standalone, referências/arquivos/preferências verificados nos escopos documentados. Outros hardwares e aprovação de orçamento não foram testados.

'''+table+'''
São seleções específicas; reexecuções e jobs sobrepostos não somam testes únicos. Falhas intermediárias, correções de fixture, shader inicial ciano e build inicialmente recusado emLibrary estão registrados, sem enfraquecer asserções ou retunar o jogo.

## Revisão pronta

[Abrir comparação visual portátil](../validation/artifacts/run-exp-05/review/index.html) · [BaixarZIP com todas as imagens](../validation/artifacts/run-exp-05/RUN_EXP_05_REVIEW.zip).

O pacote contém guia de estilo, original/C04/C05, três armas, contexto/rank, Fragmento/Emissor antes/depois e fases, cobertura/aberta, movimento/LOS/skills e medições. Capturas são do jogo real; conceito gerado é rotulado separadamente. Instruções reproduzemFirstSector seed63/0 e a préviaC05 reversível. C05 e arte precisam de **PENDING DEVELOPER APPROVAL** após essa revisão.

'''+standalone+'''
Standalone1920×1080, Ryzen55600X/RTX3060Ti, releaseMono. Esses pares mantêm a população, masAI/posições/fases evoluem; não provam otimização. Geometria rígida dos rosters aumentou cerca de5×; materiais compartilhados3→2 e renderers dos candidatos18/7 preservados. GPU/CPU/memória e métricas indisponíveis estão discriminados no05.4, sem orçamento universal.

'''+f'Integridade: {integrity["unchangedFiles"]}/{integrity["baselineFiles"]} hashes iguais,8 mudanças previstas,0 removidos;32prefs existentes idênticas. {integrity["addedGameplayPreferences"]} adições no registroPlayer original anteriores ao build foram preservadas, sem origem atribuída; não há garantia de registro inteiro ou todo disco inalterado. Branch/HEAD e alterações02–04 mantidos. Console final0erros; checker retém um erro/umaviso históricos fora do escopo.\n\n'+'''
## Documentação e próximo pedido

[05.0 Direção](RUN_EXP_05_0_ART_DIRECTION.md) · [05.1 Recompensas](RUN_EXP_05_1_REWARD_UI.md) · [05.2 Inimigos](RUN_EXP_05_2_ENEMY_ART.md) · [05.3 Ambiente](RUN_EXP_05_3_ENVIRONMENT.md) · [05.4 Jobs, custo e integridade](RUN_EXP_05_4_VALIDATION.md).

Masterplan, design técnico, direção/pipeline de arte, UI, matriz, ledger, estado, requisitos/tarefas/validação foram atualizados preservando históricos. O pedido adicional do desenvolvedor de02/11 ficou previsto no [plano de mapas híbridos2.5D](MAP_ART_2_5D_FUTURE_PLAN.md): aprovar imagens primeiro, aplicar emplanos/volumes simples, manter física independente e medir um pequeno protótipo antes de adotar. **Só planejamento**, sem renderer ou conversão agora.

Editor finalNexusBlockout limpa, fora dePlay, Time1 e sem assistência/seed/preview/scope moeda. FontesDCC salvas separadamente preservam aScene originalBlender. Semcommit, push, desligamento ou início06 nesta entrega. Nenhum novo boon/boss/arquétipo/economia/Singularity/QERmodular.
'''
(DOC/'RUN_EXP_05_RELATORIO.md').write_text(summary,encoding='utf-8')
print('Wrote05.4 and consolidated reports from real job/performance/integrity data.')
checkpoint='''# RUN-EXP-05 — concluída tecnicamente até05.4; STOP

Este checkpoint substitui o estado parcial anterior. Não repetir05 nem iniciar06 automaticamente. O pedido adicional de mapas2.5D é planejamento para uma futura frente, não autorização de conversão agora.

Projeto real D:/Dev/Personal/My_games/Tech-Guy, não oworktreea6f0 dochat. Unity6000.5.10f1 viaMCP127.0.0.1:8080, instânciaTech-Guy@fccafb2b47cd7671. Cliente Tools/RunExp02/mcp_call.py; CodeDomC#6. BlenderMCP9876/5.2.2LTS funcionou após aviso do usuário. TrabalhoDCC emcena separada/fontes.blend, Scene originalCube/Camera/Light preservada.

05.0 direção Arquivo estratificado;05.1 C05 opt-in com8 ilustrações originais, foco/detalhes/contexto/rank;05.2 Fragmento remodeladoDCC/Emissor secundário emcópias05;05.3 cartuchos/racks visuais sobre colliders nativos;05.4 validação selecionada e perfilStandalone concluídos. C05 não publicada. Rig/clips/pinturaDCC finais, aprovação visual e aceitação humana de pressão permanecem pendentes.

'''+table+'''
BuildWindows64 releaseMono executado:0erros/9avisos (umhistóricoScenePortal, seisdeprecações doharness, doisreconexãoMCP); produto isoladoTechGuyReviewLocal/RUNEXP05Profile confirmadoapp.info. Seis relatórios8/32/64×04/05 emBuilds/RunExp05Profile/ProfileResults e cópias públicas05. Resoluçãoefetiva1920×1080. Samecohort, AI/nav/dano/posesativos, jogadorimuneidle; posições/fases evoluem entre pares, não é demonstração deotimização ou orçamento aceito.

Baseline atual05 emDocs/validation/artifacts/run-exp-05:2058hashes,2050iguais/8esperadosmudados/0removidos;32prefsTechGuy existentesiguais.15adiçõesPlayeroriginal preservadas, lastWrite23:09UTC02/11 antesdoperfil; origem nãoatribuída. Não apagar nem restaurarsnapshot03/04. ValoresprivadossóLibrary. Scopesmoeda dosjobs33/10não detectaramdelta. Capturamovimento arquivada05 e somentehistóricolimpo restauradobytesHEAD. Checklistglobalretém erroPlaygroundGUIDausente e avisoFrameSampler históricos. BranchgameplayOverhaul/HEAD314d28ec preservados comalteraçõesanteriores/exclusãoDocs.zip. Semcommit/push/shutdown.

Editor finalNexusBlockout limpa/Edit,Time1,assistfalse,nextseedempty,preview0,moedascopependingfalse,missing0. FirstSector eContent reabertos/MCPverificados:duplaguidsaf5d41ab4bac433419d3d4399f164eca/b47ff7d4f586a5b4abd96e57dbfaf617, cápsulas0,85/0,8,18/7renderers,5772/2172tris. Console0erros semclear. Sources eassets salvo, usuárioBlender originalpreservado.

Pacote review/index.html +RUN_EXP_05_REVIEW.zip emDocs/validation/artifacts/run-exp-05:81imagensreferenciadas,14registrosjobs (sobrepostos),0linksmissing,CRCsemerror. Fontes/originalatlas/conceito emTools/RunExp05/Source; scriptsDCC ebuild/review/performance/validation emTools/RunExp05. Não repetir capture_baseline.py: baselineéimutável. Tools scriptsescrevemevidênciaprópria05; helperslegados03usamdestinosUTCúnicos. DatasUTC03/11correspondema02/11localnosrelatórios.

Entrega completa: RUN_EXP_05_RELATORIO.md e05.0–05.4. Fontescanônicas masterplan/technical/art/UI/matrix/ledger/state/tasks/requirements atualizadas preservandohistóricos. OsrelatóriosregistramfalhasintermediáriasUI, espera deinput, shaderciano e buildLibrary;nenhumaasserçãoenfraquecida. Promptoriginalatlas salvo;promptsexatoscorreção/conceitonãoretidos, lacunadeproveniênciadeclarada.

Pedido maisrecente dousuário: prever imagens2D que pareçam3D para mapas futuros, comaprovaçãodasimagensantesdeaplicar. Feitoem MAP_ART_2_5D_FUTURE_PLAN.md +Docs/03_PRIORIDADES,artdireção/pipeline,GPR_TASKS eMasterplan. Avaliar umplanofachada euma cobertura, física/LOS/profundidade/oclusão/sombras/câmera tratadosseparadamente ecomparaçãoCPU/GPU/memória antesdeadotar. Semrenderer2.5Dnovouconversãoimplementados. Próximo passo depende deautorizaçãofuturademapas/revisãohumana.
'''
(DOC/'RUN_EXP_05_RESUME_CHECKPOINT.md').write_text(checkpoint,encoding='utf-8')
