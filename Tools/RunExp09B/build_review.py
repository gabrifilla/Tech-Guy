"""Portuguese09B handoff and offline evidence review. Screenshots are copied byte-for-byte."""
from pathlib import Path
import json,csv,statistics,collections,hashlib,shutil,html,re,runpy
R=Path(__file__).resolve().parents[2];O=R/'Docs/validation/artifacts/run-exp-09b';V=O/'review';D=R/'Docs/decisions';V.mkdir(parents=True,exist_ok=True)
esc=html.escape;manifest=[]
names={'Rectangle':'Retângulo','AsymmetricCourtyard':'Pátio','LChamber':'L','ElongatedGallery':'Galeria','FragmentedRing':'Anel'}
def copy(p,name,category):
    dest=V/name;dest.parent.mkdir(parents=True,exist_ok=True);shutil.copy2(p,dest);manifest.append({'file':name,'sha256':hashlib.sha256(dest.read_bytes()).hexdigest(),'original':str(p.resolve()),'category':category});return name
def latest(folder,required):
    a=sorted(p for p in folder.glob('*') if p.is_dir() and (p/required).exists())
    if not a:raise RuntimeError('Missing passing evidence: '+str(folder))
    return a[-1]
def pic(p,label):return '<figure><a href="'+p+'"><img loading="lazy" src="'+p+'" alt="'+esc(label)+'"></a><figcaption>'+esc(label)+'</figcaption></figure>'
def folder(src,target,category):
    for p in src.iterdir():
        if p.is_file():copy(p,target+'/'+p.name,category)
rows=list(csv.DictReader((O/'statistics/paths.csv').open()));nodes=list(csv.DictReader((O/'statistics/nodes.csv').open()));stats={}
for alg in ['legacy','history']:
    a=[r for r in rows if r['algorithm']==alg];f=collections.Counter();angles=collections.Counter();variants=collections.Counter()
    for r in a:
        fs=r['families'].split('|');qs=r['orientations'].split('|');f.update(fs);angles.update(qs);variants.update(x+':'+q for x,q in zip(fs,qs))
    stats[alg]={'paths':len(a),'selections':sum(f.values()),'families':dict(f),'orientations':dict(angles),'variants':dict(variants),'streak_hist':dict(collections.Counter(int(r['max_streak']) for r in a)),'unique_hist':dict(collections.Counter(int(r['unique']) for r in a)),'adjacent_repeats':sum(int(r['repeats']) for r in a),'orientation_repeats':sum(int(r['orientation_repeats']) for r in a),'combined_repeats':sum(int(r['combined_repeats']) for r in a),'mean_unique':statistics.mean(int(r['unique']) for r in a),'mean_unique_variants':statistics.mean(int(r['unique_variants']) for r in a),'long':sum(int(r['max_streak'])>=3 for r in a),'holdout_long':sum(int(r['max_streak'])>=3 for r in a if int(r['seed'])>=256)}
    stats[alg]['first_room_families']=dict(collections.Counter(r['family'] for r in nodes if r['algorithm']==alg and r['node']=='1'))
    stats[alg]['distinct_family_sequences']=len(set(r['families'] for r in a))
    stats[alg]['distinct_variant_sequences']=len(set(r['families']+'@'+r['orientations'] for r in a))
# Verify the archived old implementation export, captured before edits, against the comparison branch.
archived=list(csv.DictReader((O/'baseline/paths.csv').open()));legacy_lookup={(r['seed'],r['path']):r for r in rows if r['algorithm']=='legacy'}
for r in archived:
    actual=legacy_lookup[r['seed'],r['path']]
    if (r['nodes'],r['families'])!=(actual['nodes'],actual['families']):raise RuntimeError('Legacy comparator differs from pre-edit baseline')
(O/'statistics/summary.json').write_text(json.dumps(stats,indent=2),encoding='utf-8')
perf=list(csv.DictReader((O/'performance/planning.csv').open()));conditions=(O/'performance/conditions.txt').read_text();frequency=int(re.search(r'frequency=(\d+)',conditions)[1]);cost={}
for mode in ['plan','legacy_generate','history_generate','oriented_generate']:
    a=[r for r in perf if r['scope']==mode]
    if not a:raise RuntimeError('Final oriented performance measurement missing')
    times=sorted(int(r['elapsed_ticks'])/frequency*1000 for r in a);heap=[int(r['heap_delta']) for r in a if int(r['gc_collections'])==0]
    cost[mode]={'median_ms':statistics.median(times),'p95_ms':times[int(.95*(len(times)-1))],'allocation_counter_valid':any(int(r['bytes'])>=0 for r in a),'net_heap_median_no_gc_bytes':statistics.median(heap) if heap else None,'net_heap_p95_no_gc_bytes':sorted(heap)[int(.95*(len(heap)-1))] if heap else None,'samples_without_gc':len(heap),'samples':len(a)}
(O/'performance/summary.json').write_text(json.dumps(cost,indent=2),encoding='utf-8')
summary='| Medida nas mesmas1024 seeds /4 rotas |09 original |09B final |\n|---|---:|---:|\n'
for label,key in [('Repetições adjacentes de família','adjacent_repeats'),('Rotas com streak≥3','long'),('Streak≥3 nas768 seeds fora da calibração','holdout_long'),('Média de famílias únicas','mean_unique'),('Média de variantes únicas','mean_unique_variants'),('Repetições de ângulo (inclui formas só0°)','orientation_repeats'),('Repetições de família+ângulo','combined_repeats')]:summary+=f'|{label}|{stats["legacy"][key]:.4f}|{stats["history"][key]:.4f}|\n'
performance='| Escopo | Mediana ms | P95 ms | Heap líquido mediano sem GC (bytes) |\n|---|---:|---:|---:|\n'
for k,v in cost.items():performance+=f'|{k}|{v["median_ms"]:.5f}|{v["p95_ms"]:.5f}|{v["net_heap_median_no_gc_bytes"]}|\n'
seedtable='| Seed / rota2 |09 original |09B família@ângulo |\n|---|---|---|\n'
for seed in ['0','1','2','5','6']:
    a=next(r for r in rows if r['algorithm']=='legacy' and r['seed']==seed and r['path']=='2');b=next(r for r in rows if r['algorithm']=='history' and r['seed']==seed and r['path']=='2')
    seedtable+='|'+seed+'|'+ ' → '.join(names[x] for x in a['families'].split('|'))+'|'+' → '.join(names[f]+'@'+q+'°' for f,q in zip(b['families'].split('|'),b['orientations'].split('|')))+'|\n'

jobs=[]
for p in sorted((O/'evidence').glob('*job.json')):
    x=json.loads(p.read_text(encoding='utf-8-sig'));data=x.get('result',{}).get('data',{});result=data.get('result') or {}
    if result:jobs.append({'file':p.name,'job':data.get('job_id'),'summary':result.get('summary'),'tests':[{k:t.get(k) for k in ['fullName','state','message']} for t in result.get('results',[])]})
(O/'evidence/job-index.json').write_text(json.dumps(jobs,indent=2),encoding='utf-8')
jobtable='| Registro | Job Unity | Resultado |\n|---|---|---|\n'
for j in jobs:
    s=j['summary'];jobtable+=f'|{j["file"]}|{j["job"]}|{s["passed"]}/{s["total"]}; falhas{s["failed"]}|\n'

shared='''
## Fontes, skills e limites

Pedido09B, adendo de orientação e autorização posterior para commit/push/desligamento governam esta entrega. AGENTS/README/visão; docs de run, encontros/combate, câmera/nav, relatórios/revisões09/08, matriz/ledger/masterplan e auditorias reais foram consultados. Skills locais tech-guy-game-vision/vision-and-constraints, tech-guy-combat-unity/combat-matrix e tech-guy-unity-acceptance/editor-validation. Instaladas: router, unity-mcp-orchestrator, unity-csharp-scripting, procedural-gen/dungeon-generation, level-design, tech-guy-level-layout/level-brief, tech-guy-enemy-encounters/encounter-spec, camera-systems, unity-navmesh e performance-optimization. Não se inventou skill especializada em estatística: usamos UTF e dados exportados pelo código real.

Aplicação: RNG isolado e ordem estável; escolhas por rotas possíveis e papéis existentes; validar piso real e apresentação na câmera fixa; medir custo em vez de presumir; diferenciar teste e aceite físico. Nenhuma delegação. A direção das formas09 foi aceita fisicamente pelo desenvolvedor no pedido09B. A nova distribuição e suas orientações permanecem PENDING DEVELOPER APPROVAL.

Projeto confirmado por MCP: D:/Dev/Personal/My_games/Tech-Guy, Unity6000.5.10f1, Tech-Guy@fccafb2b47cd7671. Branch gameplayOverhaul, HEAD inicial766afa66d93a58afc1b9de1f2eafa15ed8a5504b. Antes09B:2413 arquivos existentes e47 preferências posteriores ao playtest humano. Diffs07/08/09 preservados. Hashes antes/depois e estado final em evidence; nenhum save antigo restaurado sobre dados humanos atuais.

NOT TESTED: campanha humana prolongada, todas as2^64 seeds, GPU/standalone e alocação exata por chamada neste Mono. A validação é da seleção executada, não suíte global do projeto. O browser bloqueou file:// anteriormente; não se contornou essa política por servidor/superfície alternativa. QA do pacote é estática (referências/PNG/CRC/hash/ZIP), sem afirmar renderização no browser. Capturas Unity não foram retocadas.
'''
design='''# RUN-EXP-09B.1 — política de variedade e orientação

## Decisão

DONE: pressão suave com três salas Combat ancestrais. Peso normal100; família imediatamente anterior50; duas iguais consecutivas20; três iguais5; família vista há duas salas80 se ela não é a anterior. Pesos são relativos, não percentuais finais: todos os candidatos elegíveis mantêm peso positivo. Não há cota, hard cap, rotação fixa, lista de famílias ainda não usadas ou garantia de cobertura.

O deck já é planejado inteiro. Por isso, usamos os prefixos dos quatro caminhos existentes, em ordem topológica (profundidade, ID). Em merge, fazemos média proporcional por soma inteira dos pesos de históricos distintos de até três famílias. O histórico de um ramo mutuamente exclusivo não é tratado como “sala anterior” de outro. Não é memória da trajetória que o jogador acabará escolhendo: no merge a pressão é uma estimativa das trajetórias possíveis, e um streak ainda pode ocorrer. Nenhum nó é rerrolado na visita/revisita.

Primeiro Combat conserva o hash09, incluindo os utilitários históricos de seed para a família inicial. Depois, hash com domínio espacial e nodeId misturado seleciona pesos cumulativos na ordem enum0..4; não consome streams de enemies/rewards/base-layout. Um HashSet deduplica históricos, mas sua ordem só afeta somas inteiras comutativas. Elegibilidade0 recusa explicitamente; uma família elegível sempre é escolhida.

## Alternativas comparadas antes da adoção

Nas256 seeds de calibração, o original teve980 repetições adjacentes e24 rotas com streak≥4. Trocar apenas o hash, sem penalidade, deu968 e29: não resolve o problema por si. Penalidade leve75/50/25/90 deu816 e4; moderada50/20/5/80 deu578 e2; forte25/5/1/60 deu456 e1. Escolhemos a moderada por reduzir repetições longas mantendo mais repetição legítima que a forte. A expansão a1024 seeds inclui768 fora da calibração. CSVs originais e candidatos preservados, não apenas o perfil vencedor.

Rejeitados: round-robin/bag que força cobertura; least-recently-used que vira checklist; cap rígido que mascara restrições de elegibilidade; histórico global por ID que pune um ramo pelo irmão não visitado; replanejar no encontro que rerrola salas já materializadas. Coeficientes podem ser editados no FirstSectorContent.asset e são normalizados para1..100; autoria de pesos não cria estado de run.

## Adendo: família ≠ orientação

L e pátio:0°,90°,180°,270° elegíveis. São oito configurações genuínas, verificadas contra células/colliders e não quatro materiais. Galeria, anel e retângulo: somente0° nesta etapa. Retângulo0/180 é equivalente no contrato atual; não inflar variedade com isso. Não se habilitou rotação de eixo da galeria nem massa do anel sem passar pela mesma matriz específica. Espelhamentos opcionais NOT DONE para limitar autoria e não multiplicar variantes equivalentes/sem revisão.

Escolha de ângulo usa hash separado de seed+nodeId+família. Não há sequência forçada de ângulos. Pesos de família contam L em qualquer ângulo como L. Pátio fora das dimensões produtivas mínimas22×18 é filtrado para0°; L usa seu envelope canônico fixo28×26. Os sockets cardinais das variantes habilitadas passaram a malha completa de dimensões e os decks reais. Novas restrições exigirão ampliar o filtro antes de habilitar outra forma.
'''
implementation='''# RUN-EXP-09B.2 — implementação limitada

DONE: FirstSectorRoomDistribution.cs é uma passagem de dados chamada pelo ManualRunRandomizer atual. Não existe diretor novo, serviço global, escolha por frame ou nova estrutura de grafo. FirstSectorRoomVarietyWeights é serializável no ContentDefinition/asset já existentes. O mapa de escolhas/históricos é temporário e descartado ao terminar Generate; o snapshot guarda a família/plano escolhido.

FirstSectorRoomLayout mantém a construção canônica09 e aplica uma permutação exata das células em quarter-turns. Pisos e fronteiras são reconstruídos dessas células; rectangles de cobertura giram pelo mesmo mapeamento. Size troca eixos em90/270; CanonicalSize preserva a referência original. ToOriented/ToCanonical são inversos e mantêm Y. Slots e scores de papéis partem do espaço canônico e usam o footprint/approaches finais. Centro/entrada do jogador/reward em0 permanecem invariantes. Nenhuma forma nova foi desenhada.

O materializador existente recebe o plano transformado: floors, colliders, paredes/aberturas, covers, racks e ombreiras são emitidos nas fronteiras/portas reais; um NavMeshSurface faz o bake por run. Arte05 mantém sua convenção de leitura para coberturas cúbicas sem orientação funcional; não há pista de câmera/seta autorada adicional a girar. A câmera e a mira continuam mundiais; não se gira a câmera para fingir uma sala diferente. Arestas/IDs/gates/rewards continuam com os owners anteriores.

_useSpatialOrientations é override somente no FirstSector produtivo. Review09 continua na orientação canônica para comparação histórica; Review08 permanece isolado. O diretor passa a opção ao Generate. Family variety é opcional no método para comparar explicitamente o09 original; orientação é outro parâmetro independente. O padrão dos helpers antigos preserva a comparação canônica.

Arquivos preexistentes alterados09B: ManualRunRandomizer.cs, FirstSectorContentDefinition.cs, FirstSectorContent.asset (quatro pesos), FirstSectorRoomLayout.cs (transformação), ManualStageEnvironment.cs (flag), ProgressionDirector.Manual.cs (opção) e FirstSector.unity (override). ProceduralRunGeometryPlan, materializador físico, kits, boons, prefabs e demais cenas não são reescritos. Novos: seletor, autorador09B, testes EditMode e quatro partials PlayMode, Tools e relatórios.

## Seed, persistência e revisão

O mesmo seed/mesmo grafo/pesos/opções gera o mesmo plano inteiro. Sem dependência de ordem do Dictionary nem relógio. RunSeedSource produtivo mantém randomizeEachRun com GUID; debug válido do Editor é one-shot e consumido. Clear next seed remove inclusive texto inválido. Nenhum PlayerPrefs novo ou reset. Resumo persistido não representa run retomável; não há loader de snapshot em andamento. Replay09B pode diferir de09 após o primeiro Combat; não se migra nem destrói save. Comparação legada fica disponível via Generate(... spatialLayouts:true, spatialVariety:false, spatialOrientations:false).

Menu Tools → Tech Guy → RUN-EXP-09B oferece seed5/6, quatro L e quatro pátios e Clear next seed. Usa o portal normal Nexus→FirstSector e SessionState, não outra cena obrigatória. No fluxo normal nenhum menu é necessário.
'''
statreport='# RUN-EXP-09B.3 — validação estatística\n\nDONE:1024 seeds0–1023 ×4 rotas possíveis por algoritmo;4096 observações por versão. Rotas da mesma seed compartilham nós e não são amostras independentes. Primeiras256 usadas para comparar candidatos;768 adicionais verificam o comportamento fora da calibração.\n\n'+summary+'''
Frequência agregada não é o critério principal. O histórico máximo por rota, famílias únicas e repetição adjacente mostram a experiência de cada caminho possível. A implementação ainda produziu streak4 em sete rotas da amostra; isso não é uma promessa de cap4 em todas as seeds. Há107 rotas com só duas famílias e510 com as cinco: cobertura não é garantida. Nenhuma rotação foi usada para aumentar a contagem de famílias.

Primeiro Combat (nó1), antes e depois: Galeria190, Retângulo199, L211, Anel216, Pátio208 nas1024 seeds. Todas as cinco aparecem sem pin do fluxo normal. Sequências distintas somente de famílias:3148→3054; a pressão de variedade concentra um pouco a distribuição em alternâncias. Essa redução de cerca3% na contagem de sequências é um trade-off explícito; não se alega maximizar entropia ou distância entre todo par de runs. O objetivo medido é reduzir streaks dentro de cada rota, ainda com milhares de sequências distintas. As combinações com ângulo são registradas separadamente no summary.json. Os1024 caminhos originais das256 seeds capturadas antes da edição foram comparados novamente ao ramo legacy e coincidiram.

Angle0 domina porque retângulo/galeria/anel só têm0°; não é defeito de uma roleta uniforme de quatro ângulos em todas as formas. O CSV de nós contém family eligibility31/0, máscara de orientações, ângulo, roster e rewards. O CSV de caminhos separa família, orientação e combinação. Todos os estados nãoCombat preservam restrições/posições/recompensas.

Testes: geração antiga/final/repetida em1024 seeds; RNG de rewards e lista/rank de prefabs iguais; centros/grafo iguais; offsets repetíveis e dentro do piso; Boss/Elite/especiais iguais. Casos de uma/duas famílias, máscara por nó, curto subconjunto compatível, ordem invertida, máscara vazia, seed explícita/normal/limpeza e rotação rígida de células/covers/approaches em todas as dimensões produtivas.

## Seeds reais para reproduzir

Os IDs Combat da rota2 são1|2|4|5|6|9; escolher outro ramo pode reduzir ou mudar a sequência. A tabela não força esse caminho nem essa ordem ao jogador; apresenta uma das quatro rotas já existentes.

'''+seedtable
gameplay='''# RUN-EXP-09B.4 — geometria e gameplay real

DONE:50 decks produtivos com NavMesh,11 variantes possíveis incluindo retângulo; observação direta de centros/spawns/caminhos, pisos permitidos e negativos nos vazios. Gates são abertos explicitamente apenas no ensaio estrutural; ciclo real é coberto pelos testes de combate e produção.

As24 comparações exercitam oito orientações de L/pátio × três armas no FirstSector produtivo. Usam seis papéis reais Fragmento/Emissor/Grunt/Heavy/Hooker/Healer; IA/nav/poses ativos. Câmera normalFOV40, cobertura com fader e collider ativo, HP de básico, Q/E/R/especial, dash, LOS bloqueada/readquirida, projétil do arco bloqueado pela cobertura girada, gates/ticketsC05 e travessia física de trecho do socket. O resto do caminho usa Warp. Há imunidade, mana/Asura, roster controlado e alvo temporário de HP/IA, declarados em passed.txt. Não é uma vitória humana ou distribuição natural de seis papéis.

Os testes de produção entram pelo portal normal do Nexus: três armas, seed5 duas vezes, seed6 e seed aleatória. Roster/boons/rewards são nativos; completion usa Warp/kills controladas. Os logs registram família e ângulo de cada nó visitado, tickets uma vez, chefe/extraction e retorno. Repetir seed5 deve reproduzir a mesma sequência real; seed6 pode repetir famílias legitimamente e difere da5. O pacote verifica essas duas propriedades nos logs.

Tripla TitanicFists+Conductor+Detonation e controles/rewardUI têm regressões focadas adicionais. Não se alterou dano, probabilidade de boon, mana/cooldown, composição, render de telegraph nem comportamento de inimigo.

## Falhas preservadas e correções

Dois jobs EditMode vazios não foram contados como passe. A fixture usou GetInstanceID obsoleto nesta Unity e passou a usar EntityId. Referência da assembly PlayMode a autorador da assembly Editor não era válida; a busca de seed foi movida para dados acessíveis do teste. Nenhum desses erros exigiu alterar gameplay.

Na primeira rodada física, L180/L270 não tinham centro de retângulo de decomposição livre. A fixture foi corrigida para procurar pontos claros do próprio piso, mantendo a exigência de haver destino e deslocamento real. O teste independente de NavMesh já havia passado. Esses resultados iniciais ficam anexados, não substituídos por alegação de passe integral.

PENDING DEVELOPER APPROVAL: conforto da câmera, leitura de ameaças perto das paredes, ritmo dos ramos, sensação de repetição e percepção das rotações. Conectividade técnica não basta para aprovar gosto/feel. Nenhum espelhamento, família nova, universo, boss ou etapa10 foi iniciado.
'''
validation='\n\n## Jobs Unity executados\n\n'+jobtable+'\n\n## Performance do planejamento\n\n'+performance+'''
A sondagem de byte[100000] mostrou que GC.GetAllocatedBytesForCurrentThread retorna0 neste Mono. Os dados finais usam-1/indisponível;0 inicial não é “zero alocação”. ProfilerRecorder GC.Alloc expõe tempo em nanossegundos, não bytes nesta captura. Heap líquido é observado por Profiler.GetMonoUsedSizeLong e contador de coletas; não equivale a alocação exata nem GC/frame. O acréscimo estrutural é um mapa temporário de até sete escolhas, históricos≤número de prefixos, array de cinco pesos e dois campos por plano orientado; sem lista global que cresça entre runs. As rotações criam dados temporários canônicos/transformados uma vez, não a cada visita.

CPU mede plano isolado, geração de dados legada, com política e com política+orientação. Não inclui materialização/renderer/NavMesh nem prova GPU. Amostras curtas de Editor e overhead de coleta estão declarados em conditions.txt.
'''
for file,body in [('1_DISTRIBUTION_DESIGN',design),('2_IMPLEMENTATION',implementation),('3_STATISTICAL_VALIDATION',statreport),('4_GAMEPLAY_VALIDATION',gameplay)]:
    (D/f'RUN_EXP_09B_{file}.md').write_text(body+validation+shared,encoding='utf-8')
consolidated='''# RUN-EXP-09B — variedade controlada e orientações

DONE: política de histórico curto reduz repetição excessiva, mantendo aleatoriedade e replay; L e pátio têm quatro rotações físicas cada no FirstSector normal. As outras três famílias permanecem0°. Geometria canônica09 preservada, sem nova família/grafo/combate/economia. O adendo autoriza a transformação física, sobrepondo a restrição anterior de não mudar orientação.

'''+summary+'\n'+seedtable+'''
Os relatórios0–4 detalham auditoria antes da edição, perfis rejeitados, arquitetura, estatística, testes, falhas e limitações. Matrizes/canônicos registram o aceite humano da direção09 e mantêm a distribuição/rotações09B PENDING DEVELOPER APPROVAL.

Revisão offline: Docs/validation/artifacts/run-exp-09b/review/index.html; pacote RUN_EXP_09B_REVIEW.zip. O ZIP é artefato local reproduzível; HTML, fontes, evidências e construtores são versionados. Arquivos ZIP de revisão ficam ignorados no Git porque o09 já supera o limite individual do GitHub; não foram apagados.

O usuário autorizou commit/push e desligamento somente após a conclusão. As mudanças07/08/09 deste mesmo chat serão incluídas com09B, preservando trabalho anterior; nenhum reset de saves. Verificar upstream e push antes de solicitar desligamento normal do Windows, sem forçar perda de trabalho de apps.

STOP após09B. NOT DONE: espelhamentos e outras rotações não habilitadas, famílias/bosses novos, novos universos ou RUN-EXP-10. NOT TESTED: todas as seeds, campanha humana longa e alocação exata/standalone. Não confundir testes automatizados com aceite físico.
'''
(D/'RUN_EXP_09B_RELATORIO.md').write_text(consolidated+validation+shared,encoding='utf-8')
# Preserve the pre-edit baseline narrative and append the final, superseding checkpoint only once.
p=D/'RUN_EXP_09B_0_BASELINE.md';s=p.read_text(encoding='utf-8');mark='## Fechamento e adendo09B';s=s.split(mark)[0].rstrip();p.write_text(s+'\n\n'+mark+'\n\nO adendo autorizou transformação física consistente, sem reconstruir as formas. Resultados finais:\n\n'+summary+validation+shared,encoding='utf-8')
runpy.run_path(str(R/'Tools/RunExp09B/finalize_docs.py'),run_name='__main__')

folder(O/'baseline','baseline','actual original generator before production changes');folder(O/'candidates','candidates','actual candidate policy calibration');folder(O/'statistics','statistics','1024 seeds, actual old/new generation and replay');folder(O/'performance','performance','Editor data planning, not runtime GPU');folder(O/'evidence','evidence','raw Unity jobs and integrity snapshots')
nav=latest(O/'structural','fifty-decks.csv');folder(nav,'structural','50 real production decks, temporary structural gates opened')
variant_dirs={}
for family in ['LChamber','AsymmetricCourtyard']:
    for q in range(4):
        for weapon in range(3):
            a=sorted(p for p in (O/'variants').glob('*-'+family+'-'+str(q)+'-'+str(weapon)) if (p/'passed.txt').exists())
            if not a:raise RuntimeError('Missing passed variant '+family+str((q,weapon)))
            p=a[-1];key=family+'/'+str(q)+'/'+str(weapon);variant_dirs[family,q,weapon]=p
            keep=['orientation.txt','passed.txt','actual-geometry-navmesh.json','09-gameplay-overview.png','09-technical-top-down.png','09B-occlusion-normal-camera.png','09-actual-player-circulation.png','09-emissor-cover-blocked.png','09-emissor-LOS-reacquired.png','09-native-skill-3.png','09-dash-recovered.png','09-door-open-physical-traversal.png','09B-projectile-blocked-by-oriented-cover.png','09-next-node.png']
            keep+=list(x.name for x in p.glob('09-C05-ticket-*.png'))
            keep+=list(x.name for x in p.glob('natural-Fragmento-windup*.png'))
            for name in keep:
                if (p/name).exists():copy(p/name,'variants/'+key+'/'+name,'actual production room, controlled native gameplay fixture')
prod=sorted(p for p in (O/'production').glob('*') if (p/'route09b.txt').exists())
if len(prod)!=7:raise RuntimeError('Expected seven finished production routes; got '+str(len(prod)))
seed5=[];different=[]
for p in prod:
    text=(p/'route09b.txt').read_text();geometries=[line for line in text.splitlines() if line.startswith('geometry node=')]
    if re.search(r'weapon=0 seed=5 ',text):seed5.append(geometries)
    if re.search(r'weapon=0 seed=6 ',text):different=geometries
    folder(p,'production/'+p.name,'normal Nexus portal/rewards/rosters; automated warps/kills/immunity')
if len(seed5)!=2 or seed5[0]!=seed5[1]:raise RuntimeError('Runtime seed5 replay differs')
if seed5[0]==different:raise RuntimeError('Expected distinct observed runtime sequences for5/6')
(V/'runtime-replay.json').write_text(json.dumps({'seed5A':seed5[0],'seed5B':seed5[1],'seed6':different,'same_seed_same_sequence':True,'different_seed_distinct':True},indent=2),encoding='utf-8')
triple=latest(O/'triple','fixture.txt');folder(triple,'triple','real native triple in L90 with temporary boons/target HP/AI fixture')

def diagram(data,label):
    w,h=data['size']['x'],data['size']['y'];scale=14;pad=30
    def xy(v):return pad+(v['x']+w/2)*scale,pad+(h/2-v['z'])*scale
    out=[f'<svg xmlns="http://www.w3.org/2000/svg" width="{w*scale+2*pad}" height="{h*scale+2*pad}" viewBox="0 0 {w*scale+2*pad} {h*scale+2*pad}"><rect width="100%" height="100%" fill="#14222d"/>']
    for group,color in [('floors','#385365'),('walls','#ad976b'),('covers','#775340')]:
        for b in data[group]:
            x,y=xy(b['center']);sx,sz=b['size']['x']*scale,b['size']['z']*scale;out.append(f'<rect x="{x-sx/2}" y="{y-sz/2}" width="{sx}" height="{sz}" fill="{color}" stroke="#ddd" stroke-width=".7"/>')
    verts=data['navVertices'];inds=data['navIndices'];out.append('<g fill="#62ddc0" fill-opacity=".12" stroke="#62ddc0" stroke-opacity=".22" stroke-width=".4">')
    for i in range(0,len(inds),3):out.append('<polygon points="'+' '.join(','.join(map(str,xy(verts[inds[i+k]]))) for k in range(3))+'"/>')
    out.append('</g>')
    for p in data['sockets']:
        x,y=xy(p);out.append(f'<circle cx="{x}" cy="{y}" r="8" fill="#70bfff" stroke="white"/>')
    for p in data['spawns']:
        x,y=xy(p);out.append(f'<circle cx="{x}" cy="{y}" r="5" fill="#ff8c72"/>')
    out.append('<text x="12" y="20" fill="white" font-size="14">'+esc(label)+'</text></svg>');return ''.join(out)
for (family,q,weapon),p in variant_dirs.items():
    if weapon:continue
    data=json.loads((p/'actual-geometry-navmesh.json').read_text());f=V/'diagrams'/f'{family}-{q}.svg';f.parent.mkdir(exist_ok=True);f.write_text(diagram(data,names[family]+' · '+str(q*90)+'°'),encoding='utf-8')

helper=runpy.run_path(str(R/'Tools/RunExp07/build_review.py'),run_name='helpers_only');md=helper['md']
body='''<h1>RUN-EXP-09B · Variedade sem checklist</h1><p>Política de histórico curto + quatro rotações reais do L e do pátio no FirstSector. Geometria canônica09 preservada; câmera fixa no sistema aprovado.</p><p class="note">DONE técnico, PENDING DEVELOPER APPROVAL para a nova distribuição e orientações. Capturas abaixo são Unity real, com fixtures declaradas; não são mockups nem vitória humana.</p><nav><a href="#stats">Estatística</a><a href="#rotations">Rotações</a><a href="#routes">Runs normais</a><a href="#cost">Custo</a><a href="#reports">Relatórios</a></nav>'''
body+='<section id="stats"><h2>Original × final</h2><p>1024 seeds; quatro rotas possíveis por seed, observações correlacionadas.256 seeds para comparar candidatos +768 fora da calibração.</p>'+md(summary)+'<p>Famílias continuam sendo cinco; as11 variantes habilitadas não são11 famílias. Streak4 ainda ocorreu em sete rotas; não existe cap, rotação fixa ou obrigação de cobertura.</p>'+md(seedtable)+'<p><a href="statistics/paths.csv">Todos os caminhos</a> · <a href="statistics/nodes.csv">Nós/eligibilidade/ângulos/rosters/rewards</a> · <a href="candidates/summary.json">Alternativas comparadas</a></p></section>'
body+='<section id="rotations"><h2>Mesmo L e pátio, outras orientações físicas</h2><p>Plantas derivadas dos BoxColliders, sockets e triangulação exportados no runtime. Azul=portas; verde=NavMesh; pontos coral=spawns. Os recortes são vazios reais. Top-down é câmera temporária técnica; Game View preserva a câmera normalFOV40.</p>'
for family in ['LChamber','AsymmetricCourtyard']:
    body+='<h3>'+names[family]+'</h3>'
    for q in range(4):
        base='variants/'+family+'/'+str(q)+'/0/';body+='<details open><summary>'+str(q*90)+'° — '+(variant_dirs[family,q,0]/'orientation.txt').read_text()+'</summary><div class="grid">'+pic(base+'09-gameplay-overview.png','Game View normal — '+names[family]+' '+str(q*90)+'°')+pic('diagrams/'+family+'-'+str(q)+'.svg','Colliders e NavMesh reais — '+str(q*90)+'°')+pic(base+'09-technical-top-down.png','Vista ortográfica técnica temporária')+pic(base+'09-door-open-physical-traversal.png','Socket físico após resolver C05')+'</div><div class="grid">'
        for weapon,label in enumerate(['Manopla','Arco','Lança']):
            b='variants/'+family+'/'+str(q)+'/'+str(weapon)+'/'
            body+=pic(b+'09-native-skill-3.png',label+' — especial nativo')+'<p><a href="'+b+'passed.txt">Condições '+label+'</a></p>'
        body+='</div><details><summary>Oclusão e projétil</summary><div class="grid">'+pic(base+'09B-occlusion-normal-camera.png','Fader real, collider ainda ativo')+pic('variants/'+family+'/'+str(q)+'/1/09B-projectile-blocked-by-oriented-cover.png','Projétil nativo do arco bloqueado pelo sólido girado')+'</div></details></details>'
body+='</section><section id="routes"><h2>Nexus → FirstSector → chefe → Nexus</h2><p>As famílias/ângulos aparecem pela seleção produtiva. O seed debug é explícito e one-shot; o jogo normal usa nova seed. Aqui completion automatizada usa Warp/kills/imunidade, sem grants de reward.</p><p><a href="runtime-replay.json">Seed5 duas vezes = mesma sequência; seed6 diferente</a></p><div class="grid">'
for p in prod:
    a=sorted(p.glob('06-normal-node-*.png'));base='production/'+p.name+'/'
    body+=pic(base+a[0].name,'Produção normal — '+p.name)+'<p><a href="'+base+'route09b.txt">Famílias/ângulos/tickets e resultado</a></p>'
body+='</div><h3>Tripla nativa dentro do L90°</h3><div class="grid">'+pic('triple/09-triple-warning.png','HeavySlam telegraph com o tornado móvel no L90°')+pic('triple/09-triple-confirmed.png','TitanicFists + Conductor + Detonation confirmados por recibos nativos')+'</div><p><a href="triple/native-damage.json">Recibos</a> · <a href="triple/fixture.txt">Condições da assistência</a></p></section><section id="cost"><h2>Custo e integridade</h2>'+md(performance)+'<p>Byte counter deste Mono não funciona; -1 significa indisponível. Heap líquido não é alocação exata. CPU é Editor, planejamento/materialização excluída; sem alegação de GPU/standalone.</p><p><a href="performance/conditions.txt">Condições</a> · <a href="performance/planning.csv">Amostras</a> · <a href="evidence/integrity-comparison.json">Integridade</a> · <a href="evidence/job-index.json">Jobs, inclusive falhas</a> · <a href="structural/fifty-decks.csv">50 decks NavMesh</a> · <a href="package-qa.json">QA estática</a></p></section>'
links=[]
for p in sorted(D.glob('RUN_EXP_09B_*.md')):
    copy(p,'reports/'+p.name,'Portuguese09B report');dest='reports/'+p.stem+'.html';(V/dest).write_text('<!doctype html><meta charset="utf-8"><link rel="stylesheet" href="../style.css"><main><a href="../index.html">←09B</a>'+md(p.read_text(encoding='utf-8'))+'</main>',encoding='utf-8');links.append('<a href="'+dest+'">'+p.stem+'</a>')
body+='<section id="reports"><h2>Relatórios completos</h2><nav>'+''.join(links)+'</nav><p>Revisão física: escolher seed5/6 ou L/pátio no menu Tools/Tech Guy/RUN-EXP-09B, Play no Nexus e entrar no portal normal. Clear next seed remove o debug. Sem utilitário, cada run continua aleatória. STOP após09B.</p></section>'
(V/'index.html').write_text('<!doctype html><html lang="pt-BR"><meta charset="utf-8"><meta name="viewport" content="width=device-width,initial-scale=1"><title>Tech Guy ·09B</title><link rel="stylesheet" href="style.css"><main>'+body+'</main></html>',encoding='utf-8');copy(R/'Docs/validation/artifacts/run-exp-09/review/style.css','style.css','reused offline review stylesheet')
for p in list((R/'Assets/_Project/Scripts/Core/Run').glob('FirstSectorRoom*.cs'))+[R/'Assets/_Project/Scripts/Core/Run/ManualRunRandomizer.cs',R/'Assets/_Project/Scripts/Editor/RunExp09BAuthoring.cs']+list((R/'Assets/_Project/Scripts/Tests/PlayMode').glob('*Distribution09B*.cs'))+[R/'Assets/_Project/Scripts/Tests/EditMode/Editor/RunExp09BDistributionTests.cs']:
    copy(p,'source/'+p.name,'actual repository source; run in Unity project')
(V/'asset-manifest.json').write_text(json.dumps({'assets':manifest},indent=2,ensure_ascii=False),encoding='utf-8')
print(json.dumps({'stats':stats,'cost':cost,'variants':len(variant_dirs),'routes':len(prod),'manifest_records':len(manifest)},ensure_ascii=False))
