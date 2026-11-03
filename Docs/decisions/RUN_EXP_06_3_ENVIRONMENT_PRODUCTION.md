# RUN-EXP-06.3 — consolidação do ambienteFirstSector

02–03/11/2026, America/Sao_Paulo. Objetivo: consolidar a apresentação05 aprovada no fluxo normal sem alterar o jogo das salas aberta/coberta.

## Publicação já encontrada

`ManualStageEnvironment.BuildGenerated` já cria `FirstSectorRoomPresentation` e depois `FirstSectorEnvironmentArt` para arenasCombat. `FirstSectorContent` contém as cinco referênciasArchive: cobertura, rack, material compartilhado, piso e parede. A checagemMCP06 confirmou persistência, UVs válidas e sourceFBX não readable. O jogo normal não exige menu de revisão para montar essa apresentação.

**DONE:** configuração normal auditada e mantida. Não foi necessária nova camada de cenário, duplicação de referências ou alteração do gerador. A arte permanece a aceita05: cartuchos laminados, sockets e racks periféricos, piso quieto. A sala aberta recebe correspondência limitada, preservando a diferença de circulação/cover. Fontes e apresentação04 continuam para comparação de desenvolvimento.

## Geometria e leitura

NenhumBoxCollider, NavMeshModifier, PhysicsColliders bake, spawn, dimensão de arena, chance45% de cobertura, peso de roster, grafo ou transição foi editado06. As duas massas físicas4,2×2,2×4,2 continuam nos centros aprovados; meshes visuais ocupam o mesmo envelope. Racks não adicionam collider e são combinados por arena. A parede visual recuada fica dentro da parede física e conserva a diferença visual até~0,11m já documentada05; não representa passagem nova.

Baseline06, job `0d8453e3ded9498eac248ed69c7a4313`:13/13Play,0falhas/skipped. O caso de ambiente compara04/05 nas seeds0/63: mesmos bounds/quantidade de colliders, duas rotasNavMesh completas, raio de cobertura, Emissor bloqueado e recuperandoLOS, deslocamento real do jogador. AI e dano continuam ativos nos trechos de comportamento. Capturas não substituem o teste de colisão e não são uma sobrevivência humana.

A sequência final06 também verificou circulaçãoFragmento, bloqueio de projéteis, dash, telegraphs, habilidades e conclusão de salas. Resultados exatos e imagens de fluxo produtivo estão em [06.4](RUN_EXP_06_4_VALIDATION.md), com resultados finais efetivamente executados. O aceite das melhorias05 foi recebido diretamente no pedido06, separadamente da prova técnica.

## Escopo, alternativas e custo

Rejeitadas uma nova geometria de gameplay e a conversão2.5D agora: a tarefa exige consolidar o que já foi aprovado. Preservar o módulo atual evita alterar topologia para acompanhar um detalhe artístico. O trade-off é manter geometria estática adicional —3780triângulos por cobertura e2292por módulo de rack — cujo custo é observado comAI ativa, nunca tratado como gratuito. A publicaçãoC05 não exige trocar materiais/iluminação global ou pós-processamento.

Skills aplicadas: TechGuyGameVision/combat/boon/acceptance e referências, level-design/tech-guy-level-layout, UnityMCP/C#/NavMesh, create-game-assets/art-direction/three-d/provenance eperformance-optimization. Não foi criada uma nova ferramentaBlender, shader ou pipeline; a fonte05 e oURP atual foram preservados.

**NOT DONE:** conversão de mapas2.5D, reforma completa de salas especiais/corredores ou novo gerador. **NOT TESTED:** um método2.5D no jogo e o custo emoutros hardwares. O planejamento06.5/brief07 é separado desta consolidação e não autoriza implementar o próximo experimento.
