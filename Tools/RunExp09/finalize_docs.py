from pathlib import Path
import json
R=Path(__file__).resolve().parents[2];O=R/'Docs/validation/artifacts/run-exp-09'
def append(path,text):
    p=R/path;s=p.read_text(encoding='utf-8-sig');marker='## Fechamento RUN-EXP-09'
    if marker in s:s=s[:s.index(marker)].rstrip()+'\n'
    p.write_text(s+'\n'+marker+' — 03/11/2026\n\n'+text.strip()+'\n',encoding='utf-8')
core='''DONE: quatro famílias físicas (pátio assimétrico, L, galeria com bolsões e anel) integradas à seleção normal de Combat do FirstSector por override _useSpatialLayouts. Seleção espacial independente do RNG de rewards/inimigos, com retângulo fallback; Boss/Elite/especiais preservados. Pisos ausentes não têm NavMesh, paredes/sockets seguem o contorno e spawns nativos são reposicionados sem alterar roster/rank/pesos. Tratamento painterly08 continua isolado.

DONE técnico: 50 seeds em dados, 50 decks materializados (277 salas espaciais:72 pátios,70 L,70 galerias,65 anéis), 12/12 famílias×armas, 5/5 produção (rotas Nexus/chefe/retorno nas três armas, rota aleatória e foco/C05), Spawner/bordas/HookLine, tripla com caso negativo da massa e Scene View real das quatro formas. Regressão09: 9/9 (cinco ataques físicos/projétil, morte/retorno e três armas na cena08) e2/2 protocolos Titanic/Conductor. Fixtures de posições, imunidade/HP, mana/Asura, boons em memória, Warps e kills estão declaradas; não são aceite humano nem aquisição natural de sinergia.

Custo medido no Editor: bake por deck, frame/CPU/draws e contagens; amostras curtas, seeds/rosters diferentes. GPU/standalone/GC por frame e custo exclusivo de nav/fader NOT TESTED. Integridade por snapshot2393 arquivos/47 prefs, comparação final em artefatos09. Sem commit/push. PENDING DEVELOPER APPROVAL: fluxo, ritmo, câmera, beleza e dificuldade em controle físico. STOP após09.

[Relatório09](RUN_EXP_09_RELATORIO.md) · [Validação09](RUN_EXP_09_6_VALIDATION.md). Pacote: Docs/validation/artifacts/run-exp-09/review/index.html e RUN_EXP_09_REVIEW.zip.'''
append('Docs/decisions/GAMEPLAY_REWORK_PROGRESS.md',core)
append('Docs/validation/01_MATRIZ_DE_VALIDACAO.md',core.replace('(RUN_EXP_09','(../decisions/RUN_EXP_09')+'''

| Gate09 | Status técnico | Evidência / limite |
|---|---|---|
| A — variação real | DONE | Quatro footprints, colliders e NavMesh exportados; 50 seeds |
| B — decisões de jogo | DONE implementação; PENDING DEVELOPER APPROVAL | Curva/bolsões/anel mudam abordagem; dificuldade humana não inferida |
| C — navegação | DONE amostra | 50 decks, caminhos/spawns/portas e negativos nos vazios; não todas as seeds |
| D — progressão | DONE automação | Tickets, gates, trecho físico de socket e rotas reais com fixtures declaradas |
| E — FirstSector | DONE | Override produtivo e portal normal; fallback/especiais mantidos |
| F — combate/leitura | DONE automação e inspeção; PENDING DEVELOPER APPROVAL | 12/12, LOS, tripla, dash/Hook; feel físico pendente |
| G — performance | DONE medição limitada | CSV Editor; GPU, standalone, GC/frame e custo isolado NOT TESTED |
| H — integridade | DONE comparação final | Mudanças autorizadas listadas em integrity-comparison.json; prefs preservadas |
| I — comparação jogável | DONE | Cena09 + menus seed one-shot; cena08 preservada |
''')
append('Docs/gameplay/TECH_GUY_RUN_REWARDS_ROOMS_ENEMIES_MASTERPLAN.md',core.replace('(RUN_EXP_09','(../decisions/RUN_EXP_09'))
append('Docs/systems/05_RUN_E_PROGRESSAO.md','''O ambiente manual de FirstSector fornece SpatialLayoutsEnabled ao randomizador. Em Combat compatível, um plano espacial opcional imutável preservado no snapshot decide footprint, coberturas, spawns e sockets externos. O materializador atual consome o plano, mantém um NavMeshSurface/bake por run e conserva IDs/arestas/estados/recompensas. ContainsPlayer refina o trigger do envelope pelo piso real com inset0,55 m. Não há segundo gerador concorrente nem novo dono de tickets.

Retângulo fallback e cenas sem a opção serializada continuam no caminho anterior, inclusive Review08. No FirstSector normal a opção está habilitada. Forma é seed+nodeId com salt independente, sem consumir streams nativos. Roster/ranks/rewards não mudam, apenas offsets. Boss/Elite/especiais não recebem plano09. [Integração e reprodução09](../decisions/RUN_EXP_09_5_FIRSTSECTOR_INTEGRATION.md), [validação](../decisions/RUN_EXP_09_6_VALIDATION.md). Aceite físico pendente; sem expansão automática.''')
append('Docs/gameplay/GPR_TECHNICAL_DESIGN.md','''DONE: RUN-EXP-09 acrescenta FirstSectorRoomLayout ao contrato do snapshot de nó, sem migrar input, kit, boons ou C05. ProceduralRunGeometryPlan resolve portas no contorno e ManualStageEnvironment materializa piso real/colisão/NavMesh. O retângulo é fallback; o override da cena produtiva controla adoção. O plano é opcional em cenas históricas. [Arquitetura09](../decisions/RUN_EXP_09_2_PLAYABLE_GEOMETRY.md), [relatório](../decisions/RUN_EXP_09_RELATORIO.md). Testes de input/reward e três armas passaram; aceite físico e campanha longa pendentes.''')
for p in ['Docs/art/01_DIRECAO_VISUAL.md','Docs/art/02_PIPELINE_DE_ASSETS.md']:
    append(p,'''RUN-EXP-09 integrado: pátio, L, galeria com bolsões e anel são footprints físicos, usando materiais/meshes05 já existentes. Plantas derivadas de BoxColliders/NavMesh são documentação técnica, não concepts nem screenshots de produção. Capturas de comparação e produção são identificadas separadamente. Massa do anel usa material05, sem nova pintura global.

A cena painterly08, suas texturas, shader e captura histórica permanecem isolados/preservados. Beleza, leitura junto à massa e fluxo precisam de playtest e aceite do desenvolvedor; medição geométrica não equivale a aceite artístico. [Relatório09](../decisions/RUN_EXP_09_RELATORIO.md). STOP após09.''')
append('Docs/decisions/RUN_EXP_09_0_AUDIT.md',core+'''

O checkpoint acima foi escrito antes da primeira edição de geometria; seus NOT TESTED são históricos. Foram criados plano, materializador parcial, opção/override produtivo, cena de comparação, autoria de seed, testes e pacote09. Timers de bake são observação, não tuning. A massa central inicialmente usava o material de blockout e passou a usar o ArchiveWall05; não alterou sua colisão. Scene View inicial via Camera.Render saiu vazia e foi rejeitada; MCP capturou as quatro formas no viewport real com overlay de navegação. Foco, cooldown e alvos mortos foram corrigidos nas fixtures; executores de combate ficaram intactos.''')
integrity=json.loads((O/'evidence/integrity-comparison.json').read_text(encoding='utf-8'))
details='''## Integridade final e encerramento

DONE: comparação com o snapshot anterior09. Nenhum arquivo protegido removido. As únicas diferenças em arquivos existentes são os seis fontes da integração e FirstSector.unity. Prefs TechGuy:47 antes/47 depois, sem valor alterado. HEAD preservado, sem commit/push. Cena final NexusBlockout limpa em Edit Mode, Time.timeScale1, sem seed/assistência pendentes. Arte/scene/scripts08 e prefabs/kits/boons/settings preexistentes preservados.

O pacote inclui evidence/integrity-comparison.json com lista exata. Browser rendering continua NOT TESTED por bloqueio file://; a validação do ZIP é estática. O aceite humano permanece PENDING DEVELOPER APPROVAL.
'''
for name in ['RUN_EXP_09_6_VALIDATION.md','RUN_EXP_09_RELATORIO.md']:
    p=R/'Docs/decisions'/name;s=p.read_text(encoding='utf-8');p.write_text(s+'\n'+details,encoding='utf-8')
print(json.dumps({'changed_existing':integrity['files']['changed'],'prefs':integrity['prefs']},ensure_ascii=False))
