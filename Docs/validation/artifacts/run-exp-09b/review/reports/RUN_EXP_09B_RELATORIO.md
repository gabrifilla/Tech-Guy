# RUN-EXP-09B — variedade controlada e orientações

DONE: política de histórico curto reduz repetição excessiva, mantendo aleatoriedade e replay; L e pátio têm quatro rotações físicas cada no FirstSector normal. As outras três famílias permanecem0°. Geometria canônica09 preservada, sem nova família/grafo/combate/economia. O adendo autoriza a transformação física, sobrepondo a restrição anterior de não mudar orientação.

| Medida nas mesmas1024 seeds /4 rotas |09 original |09B final |
|---|---:|---:|
|Repetições adjacentes de família|3992.0000|2308.0000|
|Rotas com streak≥3|558.0000|138.0000|
|Streak≥3 nas768 seeds fora da calibração|407.0000|111.0000|
|Média de famílias únicas|3.4768|3.7483|
|Média de variantes únicas|3.4768|4.2310|
|Repetições de ângulo (inclui formas só0°)|18432.0000|9276.0000|
|Repetições de família+ângulo|3992.0000|1596.0000|

| Seed / rota2 |09 original |09B família@ângulo |
|---|---|---|
|0|Galeria → Galeria → L → Retângulo → L → Anel|Galeria@0° → Anel@0° → Retângulo@0° → L@270° → Anel@0° → Anel@0°|
|1|Galeria → Galeria → Retângulo → L → Galeria → Retângulo|Galeria@0° → Retângulo@0° → L@270° → Pátio@270° → Pátio@270° → Anel@0°|
|2|Galeria → Galeria → L → Galeria → L → Pátio|Galeria@0° → Pátio@270° → Galeria@0° → L@0° → Retângulo@0° → Anel@0°|
|5|L → Galeria → Galeria → Galeria → Galeria → Retângulo|L@180° → Pátio@90° → L@180° → Anel@0° → Pátio@0° → Pátio@90°|
|6|Galeria → L → Galeria → Galeria → Galeria → Anel|Galeria@0° → Galeria@0° → Pátio@0° → Anel@0° → Anel@0° → Galeria@0°|

Os relatórios0–4 detalham auditoria antes da edição, perfis rejeitados, arquitetura, estatística, testes, falhas e limitações. Matrizes/canônicos registram o aceite humano da direção09 e mantêm a distribuição/rotações09B PENDING DEVELOPER APPROVAL.

Revisão offline: Docs/validation/artifacts/run-exp-09b/review/index.html; pacote RUN_EXP_09B_REVIEW.zip. O ZIP é artefato local reproduzível; HTML, fontes, evidências e construtores são versionados. Arquivos ZIP de revisão ficam ignorados no Git porque o09 já supera o limite individual do GitHub; não foram apagados.

O usuário autorizou commit/push e desligamento somente após a conclusão. As mudanças07/08/09 deste mesmo chat serão incluídas com09B, preservando trabalho anterior; nenhum reset de saves. Verificar upstream e push antes de solicitar desligamento normal do Windows, sem forçar perda de trabalho de apps.

STOP após09B. NOT DONE: espelhamentos e outras rotações não habilitadas, famílias/bosses novos, novos universos ou RUN-EXP-10. NOT TESTED: todas as seeds, campanha humana longa e alocação exata/standalone. Não confundir testes automatizados com aceite físico.


## Jobs Unity executados

| Registro | Job Unity | Resultado |
|---|---|---|
|compile-empty-job.json|36fafdb91ece4803a9cf6b8ab4e96e42|0/0; falhas0|
|edit-final-job.json|e6f63c976b1f4df9920ea0f624bd7eb1|5/5; falhas0|
|first-play-job.json|3f7f86e94fcd4c2485b7431d2ee7d49d|7/9; falhas2|
|initial-empty-job.json|b50e6275e94c4b9a95ab1982ed59bc39|0/0; falhas0|
|performance-final-job.json|d2e1c7d486b14df5a03d200383e8eeee|1/1; falhas0|
|regression-job.json|54a03aee47044a7ca8e43a22350806df|5/5; falhas0|
|second-play-job.json|9596028854864fd0af44b7bbaa774c07|25/25; falhas0|


## Performance do planejamento

| Escopo | Mediana ms | P95 ms | Heap líquido mediano sem GC (bytes) |
|---|---:|---:|---:|
|plan|0.01435|0.02240|4096.0|
|legacy_generate|2.63895|4.13350|892928.0|
|history_generate|2.61430|3.93480|913408.0|
|oriented_generate|3.07230|4.38820|913408.0|

A sondagem de byte[100000] mostrou que GC.GetAllocatedBytesForCurrentThread retorna0 neste Mono. Os dados finais usam-1/indisponível;0 inicial não é “zero alocação”. ProfilerRecorder GC.Alloc expõe tempo em nanossegundos, não bytes nesta captura. Heap líquido é observado por Profiler.GetMonoUsedSizeLong e contador de coletas; não equivale a alocação exata nem GC/frame. O acréscimo estrutural é um mapa temporário de até sete escolhas, históricos≤número de prefixos, array de cinco pesos e dois campos por plano orientado; sem lista global que cresça entre runs. As rotações criam dados temporários canônicos/transformados uma vez, não a cada visita.

CPU mede plano isolado, geração de dados legada, com política e com política+orientação. Não inclui materialização/renderer/NavMesh nem prova GPU. Amostras curtas de Editor e overhead de coleta estão declarados em conditions.txt.

## Fontes, skills e limites

Pedido09B, adendo de orientação e autorização posterior para commit/push/desligamento governam esta entrega. AGENTS/README/visão; docs de run, encontros/combate, câmera/nav, relatórios/revisões09/08, matriz/ledger/masterplan e auditorias reais foram consultados. Skills locais tech-guy-game-vision/vision-and-constraints, tech-guy-combat-unity/combat-matrix e tech-guy-unity-acceptance/editor-validation. Instaladas: router, unity-mcp-orchestrator, unity-csharp-scripting, procedural-gen/dungeon-generation, level-design, tech-guy-level-layout/level-brief, tech-guy-enemy-encounters/encounter-spec, camera-systems, unity-navmesh e performance-optimization. Não se inventou skill especializada em estatística: usamos UTF e dados exportados pelo código real.

Aplicação: RNG isolado e ordem estável; escolhas por rotas possíveis e papéis existentes; validar piso real e apresentação na câmera fixa; medir custo em vez de presumir; diferenciar teste e aceite físico. Nenhuma delegação. A direção das formas09 foi aceita fisicamente pelo desenvolvedor no pedido09B. A nova distribuição e suas orientações permanecem PENDING DEVELOPER APPROVAL.

Projeto confirmado por MCP: D:/Dev/Personal/My_games/Tech-Guy, Unity6000.5.10f1, Tech-Guy@fccafb2b47cd7671. Branch gameplayOverhaul, HEAD inicial766afa66d93a58afc1b9de1f2eafa15ed8a5504b. Antes09B:2413 arquivos existentes e47 preferências posteriores ao playtest humano. Diffs07/08/09 preservados. Hashes antes/depois e estado final em evidence; nenhum save antigo restaurado sobre dados humanos atuais.

NOT TESTED: campanha humana prolongada, todas as2^64 seeds, GPU/standalone e alocação exata por chamada neste Mono. A validação é da seleção executada, não suíte global do projeto. O browser bloqueou file:// anteriormente; não se contornou essa política por servidor/superfície alternativa. QA do pacote é estática (referências/PNG/CRC/hash/ZIP), sem afirmar renderização no browser. Capturas Unity não foram retocadas.


## Gates e integridade de encerramento

| Gate09B | Situação observada |
|---|---|
| A — formas existentes | DONE: construção canônica preservada e transformada rigidamente no adendo; outras famílias/reviews preservadas |
| B — variedade | DONE amostra: repetição reduzida; pares e streak4 ainda possíveis; sem cobertura garantida |
| C — replay | DONE:1024 seeds repetidas + seed5 em duas runs reais com famílias/ângulos/tickets iguais |
| D — fluxo natural | DONE automação:7 runs pelo portal Nexus, seleção produtiva, chefe e retorno |
| E — conteúdo | DONE escopo: rosters/rewards/ranks/centros/grafo iguais; Boss/Elite/especiais fora da transformação |
| F — gameplay | DONE técnico:24 variantes×armas,50 NavMesh, LOS/projétil/dash/oclusão/C05/tripla; aceite físico pendente |
| G — custo | DONE CPU/heap líquido limitado; NOT TESTED alocação exata/GPU/standalone; nenhuma avaliação por frame |
| H — integridade | DONE snapshot2413→2427, sete existentes alterados intencionalmente, nenhum removido e47 prefs iguais |

Lista exata das diferenças em evidence/integrity-comparison.json. Os sete preexistentes são FirstSector.unity, FirstSectorContent.asset, FirstSectorContentDefinition.cs, FirstSectorRoomLayout.cs, ManualRunRandomizer.cs, ManualStageEnvironment.cs e ProgressionDirector.Manual.cs. .gitignore/Docs/Tools têm mudanças intencionais fora do escopo de hashes de gameplay. Novos scripts/metadata09B foram importados pelo Unity; nenhum GUID preexistente foi substituído.

O perfil de revisão física futura deve verificar visibilidade/pressão em L0/90/180/270, recortes do pátio e sensação de repetição na seed5/6 e em runs sem debug. Isso não autoriza uma etapa10. Publicação Git e pedido de desligamento foram autorizados pelo usuário após o escopo inicial; somente serão executados após a revisão do diff/pacote.
