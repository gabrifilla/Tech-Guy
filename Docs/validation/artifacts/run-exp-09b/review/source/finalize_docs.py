from pathlib import Path
import json
R=Path(__file__).resolve().parents[2];O=R/'Docs/validation/artifacts/run-exp-09b';D=R/'Docs/decisions'
data=json.loads((O/'evidence/integrity-comparison.json').read_text(encoding='utf-8'))
assert not data['files']['removed'];assert not data['prefs']['changed'] and not data['prefs']['added'] and not data['prefs']['removed']
def append(path,body):
    p=R/path;s=p.read_text(encoding='utf-8-sig');marker='## Fechamento RUN-EXP-09B';s=s.split(marker)[0].rstrip();p.write_text(s+'\n\n'+marker+'\n\n'+body.strip()+'\n',encoding='utf-8')
summary='''DONE: controle suave de repetição no planejamento produtivo, com pesos relativos100/50/20/5 e80 para duas salas atrás; históricos distintos de até três Combat e merges conscientes de ramos. Sem quota, round-robin, cap ou lista obrigatória. Seed e streams de reward/roster preservados. Formas canônicas09 mantidas; o adendo09B habilita rotações físicas0/90/180/270 de L/pátio. Galeria/anel/retângulo ficam0°, sem espelho. Família e variante são métricas separadas.

1024 seeds ×4 rotas por versão: repetição adjacente3992→2308; rotas com streak≥3,558→138; fora da calibração407→111. Média de famílias3,4768→3,7483 e de variantes3,4768→4,2310. Há107 rotas finais com só duas famílias e sete com streak4: sem cobertura/cap garantidos. Primeiro Combat conserva hash09; depois há seleção ponderada e orientação em hash independente.

DONE Unity:5/5 EditMode focados (mais reexecução1/1 de performance corrigida),50 decks de NavMesh real,24/24 orientações×armas,7/7 rotas Nexus/chefe/retorno incluindo replay5/5 e outra seed6. Cinco regressões adicionais passaram: tripla no L90°, painterly08 Manopla, foco/Enter/C05 e dois contratos Titanic/Conductor. Primeira rodada teve dois destinos inválidos na fixture L180/L270, corrigidos por procura de ponto no piso real, sem reduzir a exigência de movimento. Jobs vazios/erros de import são evidência de falha, não passes.

FirstSector produtivo tem famílias/orientações habilitadas; cenas09/08 preservam orientação canônica/isolamento. Conteúdo tem pesos serializados e confirmados após salvar/reabrir. Prefs47/47 iguais ao snapshot09B posterior ao playtest humano; nenhum arquivo protegido removido, cenas/prefabs/GUIDs e kits/boons preservados fora das diferenças intencionais. Estado final NexusBlockout limpa/Edit, TimeScale1, seed debug vazia, assistência falsa. Históricos07/08/09 preservados. A direção espacial09 foi aprovada fisicamente no pedido09B; distribuição/rotações novas permanecem PENDING DEVELOPER APPROVAL.

CPU/heap no Editor medidos no CSV; contador de bytes Mono inválido=-1 e heap líquido não é alocação exata. Sem trabalho por frame ou lista global crescente; GPU/standalone e campanha humana longa NOT TESTED. STOP após09B, sem10. O usuário autorizou posteriormente commit/push destas mudanças e do trabalho07–09 deste chat, seguido de desligamento; isso substitui a proibição anterior de commit/push para a entrega atual.

[Relatório09B](RUN_EXP_09B_RELATORIO.md) · [Distribuição](RUN_EXP_09B_1_DISTRIBUTION_DESIGN.md) · [Validação](RUN_EXP_09B_4_GAMEPLAY_VALIDATION.md). Revisão: Docs/validation/artifacts/run-exp-09b/review/index.html e ZIP local reproduzível. Arquivos ZIP de revisão são ignorados no Git por tamanho; fontes/HTML/imagens/evidências e construtores são versionados.'''
append('Docs/decisions/GAMEPLAY_REWORK_PROGRESS.md',summary)
for path in ['Docs/validation/01_MATRIZ_DE_VALIDACAO.md','Docs/gameplay/TECH_GUY_RUN_REWARDS_ROOMS_ENEMIES_MASTERPLAN.md']:
    append(path,summary.replace('(RUN_EXP_09B','(../decisions/RUN_EXP_09B'))
append('Docs/systems/05_RUN_E_PROGRESSAO.md','''ManualRunRandomizer continua planejando o deck completo antes do encontro. FirstSectorRoomDistribution calcula famílias por histórico de até três ancestrais Combat em caminhos válidos; em merge combina históricos distintos, sem tratar irmãos como consecutivos. Primeiro Combat conserva hash09. Não é histórico persistido do caminho efetivamente escolhido: a distribuição no merge é planejada antes de saber a escolha humana. Nenhum reroll em revisita.

FirstSectorContent guarda quatro pesos relativos serializados; planos imutáveis guardam QuarterTurns/CanonicalSize. A transformação física roda uma vez antes de materializar; portas são resolvidas nas fronteiras resultantes pela autoridade atual. _useSpatialOrientations está habilitado somente na cena FirstSector produtiva, preservando reviews históricos. Graph/IDs/rosters/rewards/gates/C05/chefe e persistência não migram.

Não foi localizado save de run em andamento que reconstrua o mesmo snapshot após reinício. Os saves persistentes continuam loadout/moeda/tutorial/resultados, não a agenda espacial atual. Reproduzir seed antiga sob algoritmo novo pode mudar salas após o primeiro Combat; não equivale a invalidar o save. Comparação09 está explícita no método (variedade/orientação desligadas). [Arquitetura e limites09B](../decisions/RUN_EXP_09B_2_IMPLEMENTATION.md), [estatística](../decisions/RUN_EXP_09B_3_STATISTICAL_VALIDATION.md). Aceite físico da nova política/orientação pendente.''')
append('Docs/gameplay/GPR_TECHNICAL_DESIGN.md','''RUN-EXP-09B acrescenta uma passagem de distribuição ao randomizador e quarter-turns aos dados do footprint, sem novo diretor/serviço nem trabalho por frame. A elegibilidade atual continua Combat+conteúdo+opção espacial, com cinco famílias; orientações de L/pátio habilitadas no FirstSector por opção própria, sem confundir variantes com famílias. O snapshot armazena o resultado e mantém replay/revisitas; histórico é local à geração. Pesos no ContentDefinition e strings de seed em SessionState não alteram ownership de boons.

Dados/geometria/gameplay foram verificados com testes e capturas reais; [relatório09B](../decisions/RUN_EXP_09B_RELATORIO.md). Gates: existentes preservados, variedade/replay/progressão e8 orientações testados; GPU/standalone/alocação exata e aceite humano da nova política permanecem pendentes. STOP após09B.''')
append('Docs/art/01_DIRECAO_VISUAL.md','''O pedido09B registra aceite físico da direção das formas09. O adendo habilita quatro orientações reais de L/pátio, usando os mesmos materiais05, piso/colliders/NavMesh/portas e câmera aprovada. As capturas09B são produção/fixtures reais e plantas extraídas de geometria, sem mockup apresentado como gameplay. Painterly08 continua isolado. Distribuição/rotações09B ainda PENDING DEVELOPER APPROVAL; nenhuma nova arte, família ou universo.

[Revisão09B](../validation/artifacts/run-exp-09b/review/index.html) e [relatório](../decisions/RUN_EXP_09B_RELATORIO.md).''')
gates='''
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
'''
for p in D.glob('RUN_EXP_09B_*.md'):
    s=p.read_text(encoding='utf-8');s=s.split('## Gates e integridade de encerramento')[0].rstrip();p.write_text(s+'\n\n'+gates,encoding='utf-8')
print(json.dumps({'existing_changed':data['files']['changed'],'files_after':data['files']['after'],'prefs':data['prefs']},ensure_ascii=False))
