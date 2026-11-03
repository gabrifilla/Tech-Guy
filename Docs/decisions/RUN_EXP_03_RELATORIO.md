# RUN-EXP-03 — entrega até03.4 e revisão do desenvolvedor

Fragmento, Emissor e salas com cobertura agora fazem parte da FirstSector normal, antes do primeiro boss, junto das famílias anteriores. Os modelos integrados receberam geometria/articulação intermediária. Duas propostas substancialmente distintas de recompensa estão disponíveis para comparação na Unity; a interface publicada permanece preservada.

## Como testar

1. No projetoD:/Dev/Personal/My_games/Tech-Guy, fora de Play, use **Tools → Tech Guy → RUN-EXP-03 → Próxima incursão com seed63 (cobertura e dupla)**. Mantenha a assistência de recompensas desligada. Entre pela FirstSector/portal normal do Nexus com a arma desejada.
2. O primeiro nó de combate1 sorteia cobertura e os dois candidatos, misturados a Swarm/Fragile/Spawner/ShieldSupport. A seed é consumida uma única vez; o conteúdo usa o mesmo algoritmo produtivo e não vira garantia em runs futuras. Seed0 demonstra primeira sala aberta e outros encontros com os novos candidatos mais adiante. Caminhos completos reproduzíveis estão em[03.1](RUN_EXP_03_1_FIRSTSECTOR_INTEGRATION.md).
3. Esconda-se nas duas massas deslocadas, circule pelos lados e reapareça para o Emissor. Observe o Fragmento contornando obstáculos, sua preparação/contato/recuperação e o canal do Emissor antes da emissão. Experimente as três armas e dash. Não é necessário abrir as cenas de RUNEXP02 para encontrar o gameplay.
4. Para UI, **Tools → Tech Guy → RUN-EXP-03 → Recompensas → Abrir laboratório visual**, então Play. Alterne Manopla/Lança/Arco, Original/A/B e detalhes. Selecionar não concede; confirmar concede só ao inventário temporário desse laboratório. Saia de Play para descartar. A cena não entra no build publicado.
5. Opcionalmente, numa run real em Play, os menus **Conceito A/B na run atual** instalam uma apresentação temporária sobre ofertas/tickets reais; **Restaurar interface atual** a remove. A confirmação nessa situação é uma escolha real da run. Não altera a cena salva. RecomendoA como direção para revisão, sem publicá-la antes da sua escolha.

Comparação visual pronta:[REVIEW.html](../validation/artifacts/run-exp-03/REVIEW.html). Imagens são capturas reais Unity; cenas/fixtures assistidas estão identificadas. Conteúdo inicial autorizado e arquitetura:[03.0](RUN_EXP_03_0_BASELINE.md); integração:[03.1](RUN_EXP_03_1_FIRSTSECTOR_INTEGRATION.md); UI:[03.2](RUN_EXP_03_2_REWARD_UI_DESIGN.md); arte:[03.3](RUN_EXP_03_3_ENEMY_ART.md); jobs/performance/integridade:[03.4](RUN_EXP_03_4_INTEGRATION.md).

## Validação e limites

Final:12/12 PlayMode integrados (`37282363f9d443efa8ff6dc5c4c7d6a4`),24/24 regressões selecionadas (`e59091e659fb4cd1bb1a03cd6b676a9f`) e26/26 EditMode (`4366e480686840c7a4d9f16a77955d22`). Console0 erros. Rotas das três armas concluem boss, tickets únicos e Nexus; contato das três armas respeita cobertura; Emissor recuperaLOS; pausa/CC/reativação/cleanup foram observados. Mouse, teclado e gamepad operam os conceitos sem aquisição acidental. Bateria global não foi declarada verde.

As duas correções novas foram visuais e limitadas: pata excedendo alcance em curva e avanço da pose no quadro da pausa. O vermelho11/12 intermediário está preservado, seguido de verde com a mesma asserção. Erro transitório de API de estatística e limitação do checker cross-drive estão registrados. Checker ainda encontra missing script legado em Playground e aviso de sampler legado; não foram confundidos com defeitos03.

8/32/64 ativos: quadro Editor p95≈5,28/9,65/13,31ms; poses novas p95≈0,061/0,239/0,485ms. Muitos renderers/draw calls/GC exigem avaliação posterior standalone, com arte aprovada. Nenhuma AI/animação/dano foi desligada para produzir esses números; só jogador imune e instrumentação declarados. Filas de procs não são estressadas nesse benchmark sem jogador atacando. Dados e limites em03.4.

Segurança: cinco arquivos existentes de produção alterados nos pontos previstos;1495/1500 hashes do baseline idênticos.32 preferências TechGuy iguais na última verificaçãoUnity30/10; caches de testes/sessões separados. Assets02 e alterações anteriores do usuário foram mantidos. Último estadoUnity verificado: ArenaB original, fora de Play, limpa, assistênciafalse e sem override pendente.

Encerramento documental02/11: Editor fechado/MCP indisponível na conferência posterior. Quatro valores de gameplay do registro diferem do checkpoint salvo (moedasPlayer/Editor, armaPlayer e resumoEditor); a origem posterior não foi atribuída e os valores atuais foram preservados. Não houve reset/restauração sobre progresso atual nem nova execução de testes alegada. Detalhes em03.4 e`integrity-final.json`.

## Gates e decisão pendente

| Gate | Estado |
|---|---|
| A — integração | PASS técnico: conteúdo em progressão legítima antes do boss |
| B — gameplay | PASS técnico nas fixtures/rotas; contrajogo demonstrado, pressão/game feel dependem de playtest humano |
| C — UI | **REVISÃO HUMANA PENDENTE**: escolherA/B ou pedir ajuste; nada publicado |
| D — arte | **REVISÃO HUMANA PENDENTE**: refino intermediário; sem qualidade final presumida |
| E — regressão | PASS no recorte selecionado; dívida histórica preservada |
| F — segurança | PASS no checkpointUnity30/10; quatro divergências posteriores preservadas, sem atribuição ou restauração |

Os conceitos-base e a cobertura de02 já tinham aprovação física. Isso não aprova automaticamente a UI03, o andar novo ou a arte final. Faltam decisão visual humana, produção artística final/atlas/LOD/rig/clipes conforme direção escolhida e perfil standalone. Não há autorização nesta etapa para boons/bosses novos, Singularity, Q/E/R modular, substituição de todo o ecossistema ou continuação além03.4.

Skills descobertas/lidas/aplicadas e indisponibilidade Blender estão rastreadas nos relatórios individuais. Origem dos novos assets:[manifesto](../validation/artifacts/run-exp-03/asset-manifest.json). Nenhuma aceitação humana foi inventada. Trabalho interrompido no gate de revisão solicitado.
