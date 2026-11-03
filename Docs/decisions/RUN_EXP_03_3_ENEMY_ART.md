# RUN-EXP-03.3 — refino intermediário dos candidatos

Escopo: melhorar anatomia, articulação e leitura dos dois conceitos aprovados, sem convertê-los em humanoides ou alterar executores/dano/timing. Entrega **intermediária**, não arte de produção final. Somente cópias novas integradas receberam o refino; todo RUNEXP02 permanece disponível para comparação/rollback.

## Conteúdo efetivo

`RunExp03EnemyArtAuthoring.cs` autoria malhas facetadas com volumes fechados, normais externas, UV planar e materiais URP/Lit próprios em `Art/RunExp03/Enemies`. `Prefabs/FirstSector/RunExp03` mantém Actor/EnemyVariant/AI/CombatActions/reactions/agente/cápsula originais. Nenhum Animator/controller antigo ou inimigo não relacionado foi substituído.

Fragmento: quadrúpede baixo, testa/mandíbulas articuladas, três placas dorsais combinadas, coxa/canela/apoios separados. Solver de duas articulações mantém apoio no mundo durante a fase de sustentação e eleva a pata no avanço; não move a raiz. Emissor: carcaças biseladas em dobradiças, núcleo cerâmico/energia e tubo realmente oco com borda interna. Canal alinhado à origem nativa; recuo só depois do disparo. Materiais distinguem estrutura, placas e núcleo sem depender só de brilho.

`FirstSectorEnemyPose.cs` lê velocidade, windup, impactos e reação nativos; apresenta idle/move/windup/attack/recovery/hit, sem decidir ataques. `FirstSectorEnemyDespawn.cs` colapsa apenas cópia visual durante0,28s escalados; ghost sem Actor/collider/AI permanece filho do encontro e é removido no cleanup. Reativação limpa estado visual e usa a nova CombatGeneration nativa. Não se acrescentou pool global ou root motion.

## Ferramentas e limites de arte

Blender MCP `get_addon_status` tentou conectar e falhou. Nenhuma modelagem Blender, rig externo, retopologia profissional, textura pintada, atlas, LOD ou animação final foi produzida. A pipeline disponível foi autoria real de Mesh/Material/hierarquia no Editor C#, seguida de importação e runtime Unity. Não é uma pilha de cubos, mas também não é um asset final. UV simples não é unwrap final sem sobreposição. Próximas etapas artísticas: revisar proporções/attachments, unwrap/atlas, textura/roughness e rig/clipes finais; manter timing do executor e silhouette aprovada.

Skills lidas/aplicadas: quatro TechGuy locais; create-game-assets e three-d-pipeline; unity-animation/C#/MCP; game-feel; performance-optimization. Catálogo inspecionado sem skill dedicada Blender/rig disponível. Regras aplicadas: ativos novos com GUID próprio, pivôs coerentes, navegação/controle nativos e prova em câmera real, não somente close-up. Proveniência em[manifesto](../validation/artifacts/run-exp-03/asset-manifest.json).

## Evidência e correções

Unity MCP confirmou importação/compilação e prefabs persistentes no projeto real. Job`b7c1fd8f2fbf408f8898489fb5c7b1f8`:2/2 EditMode,1,221s, geometria finita/volume positivo/UV/materiais, arquétipo/HP/dano/intervalo/raio preservados. Casos de contato das três armas passaram sem mudar hurtboxes.

As primeiras rotas refinadas passaram, mas a instrumentação identificou desvio de apoio até0,532m em curvas rápidas: a pata excedia o alcance do solver. Corrigido levantando-a antecipadamente quando o apoio fica longe, em vez de arrastá-la com o clamp. Bateria seguinte mediu máximo≈4,1e-8m nos apoios em sustentação; tolerância4cm permaneceu no teste. Isso não é avaliação artística completa do andar.

Job integrado inicial`70751110b2fb4982976caf3d6e0a4b15`:11/12. Falha nova de1,713mm na hierarquia pausada revelou um avanço no mesmo quadro da pausa, pois deltaTime ainda era positivo. Guards explícitas `timeScale<=0` foram adicionadas apenas às duas apresentações novas. Reexecução`d663395b664a4850b645e6b1fffd2c91`:1/1,11,922s, com asserção original de1mm. Dano real, impactos dos dois executores, pausa da hierarquia/HP, CC finito, reativação/geração e morte/cleanup passaram. Fixture reposiciona os candidatos na sala63 e aumenta HP do jogador; não usa imunidade nesse teste. A descrição histórica do helper LOS diz imune por ter sido criada para rotas; nesse caso específico o jogador não é imune e o log lifecycle é a referência.

Capturas reais em[REVIEW.html](../validation/artifacts/run-exp-03/REVIEW.html): câmera isométrica normal com inimigos antigos, fases separadas no tempo, LOS bloqueado/recuperado, close-ups temporários e hit/despawn. Foram abertas e inspecionadas. Silhuetas continuam distintas sem nomes ou círculos próprios; barras de HP/telegraphs nativos e círculos de inimigos antigos continuam presentes. Câmera de close-up foi restaurada após a captura.

## Performance e aceite

03.4 registra custo8/32/64, renderers/triângulos e limitação Editor; mais triângulos não foram usados como prova de qualidade. Pernas segmentadas aumentam renderers, alvo para batching/skin/LOD após direção artística aprovada, sem otimização especulativa agora.

GateD **aguarda avaliação física/artística**. Papéis e base02 foram aprovados pelo desenvolvedor; novo refino03 ainda não. Não houve aprovação fabricada de animação final, polimento ou custo standalone.
