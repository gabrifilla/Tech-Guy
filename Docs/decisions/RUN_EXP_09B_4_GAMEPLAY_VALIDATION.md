# RUN-EXP-09B.4 — geometria e gameplay real

DONE:50 decks produtivos com NavMesh,11 variantes possíveis incluindo retângulo; observação direta de centros/spawns/caminhos, pisos permitidos e negativos nos vazios. Gates são abertos explicitamente apenas no ensaio estrutural; ciclo real é coberto pelos testes de combate e produção.

As24 comparações exercitam oito orientações de L/pátio × três armas no FirstSector produtivo. Usam seis papéis reais Fragmento/Emissor/Grunt/Heavy/Hooker/Healer; IA/nav/poses ativos. Câmera normalFOV40, cobertura com fader e collider ativo, HP de básico, Q/E/R/especial, dash, LOS bloqueada/readquirida, projétil do arco bloqueado pela cobertura girada, gates/ticketsC05 e travessia física de trecho do socket. O resto do caminho usa Warp. Há imunidade, mana/Asura, roster controlado e alvo temporário de HP/IA, declarados em passed.txt. Não é uma vitória humana ou distribuição natural de seis papéis.

Os testes de produção entram pelo portal normal do Nexus: três armas, seed5 duas vezes, seed6 e seed aleatória. Roster/boons/rewards são nativos; completion usa Warp/kills controladas. Os logs registram família e ângulo de cada nó visitado, tickets uma vez, chefe/extraction e retorno. Repetir seed5 deve reproduzir a mesma sequência real; seed6 pode repetir famílias legitimamente e difere da5. O pacote verifica essas duas propriedades nos logs.

Tripla TitanicFists+Conductor+Detonation e controles/rewardUI têm regressões focadas adicionais. Não se alterou dano, probabilidade de boon, mana/cooldown, composição, render de telegraph nem comportamento de inimigo.

## Falhas preservadas e correções

Dois jobs EditMode vazios não foram contados como passe. A fixture usou GetInstanceID obsoleto nesta Unity e passou a usar EntityId. Referência da assembly PlayMode a autorador da assembly Editor não era válida; a busca de seed foi movida para dados acessíveis do teste. Nenhum desses erros exigiu alterar gameplay.

Na primeira rodada física, L180/L270 não tinham centro de retângulo de decomposição livre. A fixture foi corrigida para procurar pontos claros do próprio piso, mantendo a exigência de haver destino e deslocamento real. O teste independente de NavMesh já havia passado. Esses resultados iniciais ficam anexados, não substituídos por alegação de passe integral.

PENDING DEVELOPER APPROVAL: conforto da câmera, leitura de ameaças perto das paredes, ritmo dos ramos, sensação de repetição e percepção das rotações. Conectividade técnica não basta para aprovar gosto/feel. Nenhum espelhamento, família nova, universo, boss ou etapa10 foi iniciado.


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
