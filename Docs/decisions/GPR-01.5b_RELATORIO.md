# GPR-01.5b — acesso às recompensas e viabilidade da build

29/10/2026 · **AUDITORIA CONCLUÍDA: A+B+C é acessível na produção atual sem grants de boons.** Não há bloqueio estrutural de uma única oportunidade Protocol na FirstSector atual. O achado anterior era limitado à rota de fixture usada naquele teste.

## Método e fontes

Sem modificar pesos, frequência, tickets, salas, gates, ranks ou catálogo. Inspecionados FirstSector.unity, FirstSectorManual.asset, ProgressionDirector.Manual, ManualRunRandomizer, ManualRoutePolicy, RunRewardPolicy, ManualRewardCatalog e RunBoons.Manual.

FirstSector usa _randomizeRooms=1, ManualSlice e semente por run. O gerador mantém uma Mutation e distribui oportunidades Technique/Protocol; a abertura garante ambas. O asset autorado também possui rewardSteps que a fixture simplificada não reproduzia. A assistência de Editor A+B estava ligada na sessão humana: testes de produção desligaram-na em scope e restauraramTrue ao terminar; asserts conferiram TitanicConductorPlaytest=False no snapshot da rota.

Três categorias de evidência distintas:

1. **Aquisição humana:** não executada nesta auditoria.
2. **Autoridades de produção/tickets reais com salas vencidas automaticamente:** amostra determinística e quatro percursos publicados abaixo. Nenhum boon dado fora de ticket nesses percursos.
3. **Fixtures de combate com grants:** usadas em01.5a/c para isolar dano e visual; não contam como acesso natural.

## Oportunidades e restrições

| Caminho | Nós, do início ao boss | Mutation | Protocols nos64 seeds | Observação |
|---|---|---|---|---|
|0 |0/1/2/3/5/6/9/10 |1 |2–4 |Escolhe reparo e ramo curto |
|1 |0/1/2/3/5/7/8/9/10 |1 |3–6 |Reparo e ramoElite |
|2 |0/1/2/4/5/6/9/10 |1 |2–5 |Combate extra e ramo curto |
|3 |0/1/2/4/5/7/8/9/10 |1 |4–7 |Combate extra e ramoElite |

A Mutation é oferecida no nó5, antes do acesso ao boss10. Na amostra atual o pool contém três opções compatíveis; A esteve em todas as ofertas de Mutation. Isso não é garantia perpétua se o catálogo mudar. Protocols retidos são Ignite/Frost/Conductor/Detonation, sujeitos aos ranks/caps atuais; escolha de rank repetido pode adiar o segundo efeito. Não foi observada raridade oculta ou prerequisite que bloqueieB/C. Pools e pesos exatos estão nosCSV de tickets.

## Amostra determinística:64 seeds ×4 caminhos por política

Mesmos gerador, catálogo elegível, sorteio ponderado e commit de tickets da produção. Políticas de escolha não alteram pesos: B-first prioriza A e depois Protocols ausentes B/C; C-first inverteB/C; uniform-choice escolhe uniformemente entre cards realmente sorteados. Vitórias pressupostas, caminhos estratificados igualmente, sem medida de duração/sobrevivência/preferência humana. **768 simulações de rota, não768 casos UTF nem768 runs humanas.**

| Política de escolha | A+B | A+C | A+B+C | ABC já no nó5 |
|---|---|---|---|---|
|PriorizarB, depoisC |256/256 |253/256 |253/256 (98,83%) |186/256 |
|PriorizarC, depoisB |252/256 |256/256 |252/256 (98,44%) |198/256 |
|Escolha uniforme entre cards |67/256 |62/256 |42/256 (16,41%) |17/256 |

No ramo curto, a políticaB-first teve62/64 e63/64 triplas nos caminhos0/2; caminhosElite1/3 tiveram64/64. Ausências ocorreram mesmo com duas oportunidades porque o card necessário não apareceu ou a sequência consumiu a chance restante. Parte das triplas chega só no nó9, restando o boss; duração real dessa experiência ainda precisa de avaliação física. Estas são frequências da amostra/estratégias, não probabilidades universais de gameplay.

UTF/EditMode: `run_tests(mode=EditMode,test_names=[TechGuy.Tests.Fluidity.EditMode.Gpr015RewardAccessibilityTests],include_details=true,init_timeout=120000)`, job **4d0a0e488c724ca9a466e12a0485de32**, **3/3**,36,8923592s. Novo arquivo Gpr015RewardAccessibilityTests.cs/meta. Asserts verificam commits reais, clique repetido negado, umaMutation, chegada ao gate final e assets originais imutáveis. [Resultado](../validation/artifacts/gameplay-rework/gpr-01-5/b-sample-final.json), [resumo](../validation/artifacts/gameplay-rework/gpr-01-5/b-summary.json), [por caminho](../validation/artifacts/gameplay-rework/gpr-01-5/b-outcomes-by-path.json). CSVs de oportunidades/cards/escolhas em `gpr-01-5/b/<timestamp>/`.

## Quatro percursos na cena publicada

Quatro casos do job **0bb3da4bc2504e4c8d7a5ba93e3436bb** exercitam progressão com FirstSector gerada, ProgressionDirector, mortes/frontier, UI/ticket/Choose e extração reais; warp e dano de conclusão assistidos, imunidade do player e CoinDrop removido apenas na fixture para proteger persistência. Sem grants de boons, reroll ou assistênciaA+B. O job completo passou **8/8**,17,8809324s; os outros quatro casos pertencem a01.5c.

| Caminho | Run_Seed observado | Mutation/Protocol | PrimeiraABC | Resultado |
|---|---|---|---|---|
|0 |5060926114783053727 |1/3 |nó5 |A+B+C; boss/extração→Nexus |
|1 |5103065752092153074 |1/6 |nó5 |A+B+C; boss/extração→Nexus |
|2 |4843244666295260091 |1/4 |nó5 |A+B+C; boss/extração→Nexus |
|3 |5058090748040771259 |1/5 |nó5 |A+B+C; boss/extração→Nexus |

São quatro seeds aleatórios do source publicado, registrados para auditoria, não cherry-picking até obter sucesso. Nenhuma assert exigiuABC em toda seed: os quatro resultados foram observados. Traces completos em `gpr-01-5/b/production/<timestamp>/path-N-tickets.txt`, resumo emJSON e [resultado UTF](../validation/artifacts/gameplay-rework/gpr-01-5/production-first.json). Fonte criada Gpr015ProductionAuditTests.cs/meta; nenhum asset/produção alterado.

## Conclusão, limites e recomendações

Não é necessário alterar a economia para tornarABC alcançável na rota atual. A proposta de rework automático para resolver uma suposta impossibilidade fica sem fundamento neste recorte. Para uma auditoria futura autorizada: comparar decisões informadas vs descoberta espontânea, oportunidades após o nó5, ramo curto vsElite, repetição de ranks e clareza das descrições. Alternativas futuras, sem implementar: explicar sinergias melhor, revisar timing das oportunidades, ou calibrar variedade/pesos somente após dados humanos.

Skills de visão, boons, combate e aceiteUnity, mais Unity C#/MCP, aplicadas. Sem falhas intermediárias nos3 casos de amostra ou8 da produção; Console/importação consultados. Prefs/loadout/assistência e resumo protegidos por scopes; auditoria final em01.5d/consolidado. O sample não valida persistência completa, feel, probabilidade de sobreviver ou duração de run. GPR-01.6, economia nova, salas/inimigos e Singularity continuam exigindo autorização separada.
