# RUN-EXP-03.1 — inimigos e variedade na FirstSector

Escopo autorizado: integrar os dois candidatos aprovados e salas abertas/com cobertura antes do primeiro boss, preservando autoridades, grafo, kits e recompensas. Implementado na FirstSector publicada; não exige abrir o laboratório de02.

## Implementação e arquivos

`FirstSectorContentDefinition.cs` e `ScriptableObjects/Run/FirstSectorContent.asset` acrescentam Fragmento/Emissor ao pool existente, uma entrada de cada. `ManualRunRandomizer.cs` mantém contagens, ranks, primeiros combatentes, limite de suportes e streams de layout/recompensa; sorteio de cobertura usa um stream próprio por seed/nó. Não há garantia de build ou aparição em toda rota. O pool nativo de16 famílias permanece no Inspector. Fragmento usa Rush; Emissor usa Sniper. Não foram criados novos IDs de arquétipo ou multiplicadores de HP/dano.

`ManualStageEnvironment.Procedural.cs` cria duas massas opacas de4,2×2,2×4,2 em posições deslocadas, marcadas Not Walkable antes do bake nativo. Só nós Combat têm chance45%; Start/Repair/Elite/Boss mantêm a geometria anterior. Offsets encobertos/próximos são realocados para pontos periféricos. Mantêm-se duas circulações laterais, portas e validação de caminhos. `ProgressionDirector.Manual.cs` injeta o asset; spawn, membership, coordinator, estatísticas, mortes, cleanup, tickets e extração continuam na autoridade existente.

`FirstSector.unity`: nova referência `_firstSectorContent`. Unity também serializou defaults já vigentes `_directInteractionRange:3.5` e `_enemyPointerRadiusPixels:18`; não houve remapeamento. `RunSeedSource.cs`: override Editor de uso único em SessionState, sem alterar seed persistida ou builds. `RunExp03Authoring.cs` fornece menus. Prefabs novos: `Prefabs/FirstSector/RunExp03/{Fragmento,Emissor}.prefab`; originais02 preservados. O refino posterior está documentado em03.3.

## Verificação real

Unity MCP: projetoD:/Dev/Personal/My_games/Tech-Guy, Unity6000.5.10f1, instânciaTech-Guy@fccafb2b47cd7671. Authoring persistido e recarregado. EditMode de distribuição job`7e0a8c2973e44491a9bbf583fb3a5eb6`:1/1. A amostra usa pool ordenado para comparar algoritmos:128 seeds,357 salas cobertas/539 abertas,373 spawns Fragmento/389 Emissor;17 nomes antigos incluem boss. Não representa probabilidade da ordem produtiva nem garante aparecimento. CSV em[distribution-128.csv](../validation/artifacts/run-exp-03/distribution-128.csv).

Rotas reais iniciais job`02d5b164cdf242e4999df014a6bb0d8a`:3/3; rótulos Bow/Spear estavam invertidos no teste, apesar das três armas terem executado. Corrigido com a ordem real0Manopla/1Arco/2Lança. Reexecução refinada job`267fbd214fcb4651ac279dd80b116b6b`:3/3,64,665s. Dados de cada roster/caminho em[artefatos FirstSector](../validation/artifacts/run-exp-03/firstsector/), consolidação final em03.4.

| Arma/seed | Caminho legítimo escolhido | Evidência antes do boss |
|---|---|---|
| Manopla0 | 0/1/2/4/5/7/8/9/10 | Fragmento4 coberto; Emissor5 aberto |
| Arco2 | 0/1/2/4/5/6/9/10 | cobertura1; Fragmento2; Emissor4 |
| Lança1 | 0/1/2/4/5/7/8/9/10 | Fragmento1; cobertura2; Emissor4 |
| qualquer arma63 | primeiro nó1 | cobertura e dupla: Swarm/Emissor/Fragile/Spawner/ShieldSupport/Fragmento |

As rotas automatizam deslocamento por Warp e abates, com jogador imune e moedas removidas somente na fixture. Elas provam grafo, rosters naturais, caminhos completos, duas circulações, boss, tickets com commit único e Nexus; não provam vitória humana ou dificuldade equilibrada. Seed63 usa o mesmo sorteio produtivo, sem forçar roster. Foi encontrada na busca da ordem real do Inspector em[reproducible-seeds.json](../validation/artifacts/run-exp-03/reproducible-seeds.json).

Básicos das três armas contra ambos os candidatos: job`3086d727e4e1457c889e69e87179b59b`,1/1,16,284s. Dano em espaço aberto, nenhum dano através da massa. Fixture explicitamente congela AI/isola colliders para testar contato; rotas e benchmark mantêm AI ativa. Nenhuma correção global de oclusão de áreas foi feita. Dash/colisão e área/Protocols têm regressões selecionadas em03.4.

## Skills, limites e aceite

Aplicadas: quatro skills locais TechGuy; unity-mcp-orchestrator/C#/ScriptableObjects/NavMesh; tech-guy-level-layout e enemy-encounters, referências de métricas/encounter. A extensão opcional preserva APIs antigas com config nula e evita diretor paralelo. Catálogo e limitações de ferramentas estão em03.0.

GateA técnico: integração demonstrada. GateB tem evidência de contato, navegação e LOS, detalhada em03.4; comparação subjetiva de pressão/legibilidade continua para o desenvolvedor. Avaliar a seed63 via portal normal do Nexus, assistência desligada. Não extrapolar para toda seed ou para áreas que já atravessavam paredes por contrato anterior.
