# RUN-EXP-04.1 — composição espacial de encontros existentes

Escopo: melhorar decisões do encontro com papéis atuais, sem aumentar roster/dano/cadência ou garantir dupla/build. Implementado e testado tecnicamente; avaliação física continua pendente.

## Diagnóstico e owners

Inspecionados ManualRunRandomizer, FirstSectorContentDefinition, ManualStageEnvironment, ProgressionDirector.Manual, EnemyVariant/Archetype/Profile, AI/CombatActions, suporte/Spawner e correçãoHook. Catálogo real: Rush/Grunt/Heavy/Charger melee; Shooter/Spread/Sniper/Bomber ranged; HazardCaster/Hooker controle; Healer/ShieldSupport suporte; Swarm/Spawner horda/prioridade; Fragile/Mirror especiais. Fragmento usaRush, EmissorSniper. Perfil e traits são aplicados porEnemyVariant emruntime; valores crus de prefab não representam stats finais. ExportaçãoInspector em`role-audit.json` explicita autoria; regras de ataques e contrajogo vêm dos executores atuais.

| Papéis | Dinâmica / contraposição |
|---|---|
| Rush/Fragmento, Grunt, Swarm | Aproximação e melee com coordenador; mover, dash, interromper conforme perfil |
| Heavy/Charger | Área/investida comprometida; respeitar aviso e sair da trajetória |
| Shooter/Spread/Sniper/Emissor | Distância e projéteis; usar aproximação lateral/cover, esquiva da direção anunciada |
| Bomber/HazardCaster | Controle de espaço por área sinalizada; abandonar a zona sem perder rota de circulação |
| Hooker | Deslocamento por gancho; preservar relógio anti-chain e oportunidade real de recuperação |
| Healer/ShieldSupport | Prioridade altera sustentação do grupo; alcançar retaguarda, interromper conforme contrato |
| Spawner | Reforços nativos contam no encounter; priorizar fonte, não ignorar invocações/cleanup |
| Fragile/Mirror | Fragilidade/negação de projéteis existentes; abordagem e arco/alcance importam |

## Alteração limitada

Novo `FirstSectorEncounterLayout.cs`; integração de uma linha emManualRunRandomizer, só Combat com contentFirstSector. Reatribui os **mesmos slots já validados** ao **mesmo roster/ranks/ordem**, usando stream derivado separado por seed/nó. Três organizações: pressão e retaguarda, separação lateral de pressão/tiro, suporte ao fundo. O nome internoCrossfire designa a separação lateral, não promete tiros dos dois lados. Se há apenas um grupo de papéis, mantém offsets originais. Não altera stream de recompensa, aleatoriedade global, limites, HP, armas, IA, tempo de chegada, grafo ou portas. Massas/circulações e safe-spawns03 continuam na autoridade atual.

Contraste esperado: avançar pela pressão até a retaguarda; flanquear linha de tiro lateral; priorizar suporte protegido. A composição continua sorteada, sem substituir todo encontro por Fragmento+Emissor. Dificuldade não foi declarada equivalente por soma deHP: mudou a posição relativa das ameaças, que exige revisão física. Não há novos objetivos/ondas/spawner/diretor.

## Evidência real e falhas

Unity6000.5.10f1/projetoD:/Dev/Personal/My_games/Tech-Guy viaMCP confirmado em04.0. Job`66a4dc7e8b9849b595972c8dc77e8187`:11/11 EditMode,0,534s.256 seeds comprovam determinismo, multiset de slots e cada prefab/rank preservado; randomização/grafo/recompensas e128seeds de content passam.

PlayMode`884828fb86c349a5bc18ebdb5e9da684`:3/4,69,274s. Três rotas naturais0/1/2 concluíram boss/tickets/Nexus com a organização nova. Novo caso controlado falhou ao consultar HP imediatamente após contador de emissão ranged. Corrigida somente a observação para aguardar **o projétil viajar e HP real cair**, preservando a asserção de dano. Reexecução`1aa3e18588154c4c95303d1213859dbd`:1/1,38,608s. Oito combinações: Fragmento sozinho, Emissor sozinho, dupla e dupla+Grunt+Healer, cada uma em aberto0/cobertura63. Spawn pelo diretor real com roster de fixture explicitamente injetado;AI/dano/nav ativos, jogador comHP10000 e sem imunidade. Caminhos completos, ataques/HP, morte e fronteiraRewardPending; LOS bloqueado/recuperado na cobertura. Não confundir esses rosters controlados com distribuição natural. Capturas temporais/logs nos diretórios únicos20261102 deFirstSector03; pacote04 os organizará sem remover históricos.

O isolamento de moedas do teste foi corrigido emRunExp03FirstSectorTests.cs após contaminação do baseline04 por invocados. Quatro cenários baseline reexecutados`12565ffe79f4445fba2977cfa5bf63df`:4/4,35,952s.32 chaves TechGuy voltaram a coincidir com **entrada atual04** após esses testes; nenhuma preferênciaPlayer ou snapshot03 foi restaurado. Falha intermitente deCC de baseline registrada em04.0 continua classificada, sem modificar postura/duração para esconder o resultado.

## Skills, integridade e gate

Aplicadas quatroTechGuy locais/referências, UnityMCP/C#/NavMesh, enemy-encounters/encounter-spec, level-layout, performance-optimization: partir de papéis/contrajogo existentes, manter ownership e mensurar separado de game feel. Novos arquivos:layout, testeEdit04 e partialPlayEncounters04. Modificados:randomizador e isolamento da fixture03. Prefabs/scenes/GUIDs produtivos ainda inalterados nesta etapa; somente fontesC# descritas. Console após compile0 erros.

Gate04.1 técnicoPASS no recorte observado; sem aceitação subjetiva fabricada. Custo8/32/64 e bateria crítica serão repetidos em04.5; nenhuma otimização global feita aqui. Próximo: refino reversível de duas cópias de arte, mantendo executores e fontes03 disponíveis.

## Fechamento da sequência04

Esta seção não altera os resultados intermediários acima. A integração final foi encerrada em04.5:15/15 Play integrados,1/1 matriz ampliada com12 cenários,33/33 regressões,10/10 efeitos de habilidades e28/28 Edit selecionados.1562/1574 hashes iguais e12 alterações previstas;32 preferências TechGuy atuais iguais, sem restaurar03. Fontes/cenas/metas antigas e UI publicada preservadas, Console0 erros; checker mantém dois diagnósticos históricos. Performance, GUIDs, jobs completos, limites de persistência e aprovação física estão no [relatório de validação](RUN_EXP_04_5_VALIDATION.md). [Entrega e pacote visual](RUN_EXP_04_RELATORIO.md). STOP04.5: arte final, direção de UI e julgamento físico permanecem pendentes.
