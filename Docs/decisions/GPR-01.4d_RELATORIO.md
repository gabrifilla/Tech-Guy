# GPR-01.4d — composição visual modular

29/10/2026. **Gate técnico/visual de protótipo satisfeito nas capturas inspecionadas; aceite físico desta revisão pendente.** 12/12 PlayMode finais, zero ignorados. Próxima01.4e já autorizada.

## Entrega

Uma instância/pool TitanicFistsVisual compõe vento estrutural4 ribbons, fogo3 ribbons alaranjados afilados e eletricidade2 linhas azuis em zigue-zague. A permanece igual; A+B adiciona tempestade persistente; A+C adiciona fogo; A+B+C conserva simultaneamente as duas identidades. Nenhum collider, DoT, burn, dano, explosão ou arco externo é emitido pela camada visual. Feedback real continua no ProtocolFeedbackPool após impacto elegível; explosão requer morte e orçamento.

GauntletExecutionSnapshot captura flags de ownership no aceite, inclusive para terminais cosméticos sem raiz. Combate intersecta flags, rank capturado da raiz e inventário válido; visual recebe exatamente a política da reserva. Aquisição posterior não altera aquele ataque nem sua aparência. Futuro terminal usa os boons novos. Vento cosmético de sala Completed conserva a identidade do snapshot aceito, com zero raiz/dano/arcos/explosões. Clear/release limpam todas as layers; geração antiga não desativa lease reutilizada.

Mudanças: GauntletExecutionSnapshot.cs, BreakerGauntletCombat.cs, ProtocolImpactIdentity.cs, ProtocolRuntime.Scope.cs, ProtocolRuntime.TitanicFists.cs, TitanicFistsVisual.cs. Novos testes TitanicElementalVisualPlayModeTests.cs/meta; extensão de TitanicRoomClearPlayModeTests.cs. Nenhum asset/prefab/cena/rig ou pipeline de combate duplicado. Atualização de ribbons usa arrays reutilizados, sem criar objetos por frame; pool ainda aloca objetos no primeiro uso, como anteriormente.

## Evidência e tentativas

`run_tests(mode=PlayMode,category_names=[GPR014c,GPR014d],include_details=true,init_timeout=120000)`: job2a552cb258e643739079b65cac4c1a09,13/13. **Não fechou o visual:** inspeção real revelou camadas brancas porque URP Unlit ignora vertex color. Capturas originais preservadas em gameview/20261029-235340-2554456.

Correção usa MaterialPropertyBlock/_BaseColor, conforme padrão já existente de ProtocolFeedbackPool. Primeira rodadaf4855cf66c8847e09a65e7a40c77ac71 falhou0/12 por criar MaterialPropertyBlock em inicializador de MonoBehaviour. Corrigido no owner para Awake, sem LogAssert.Expect ou relaxar assertions. Falha completa em d-tint-initialization-failure.json. Rodada final **b6bc6de15b4a4b1baef7f2660adffe3e**,12/12,34,5934424s, categoriasGPR014d; detalhes d-tint-and-lifecycle.json. Compile/Console consultados via MCP, zero novos erros após correção.

Game View real1160×600 em `../validation/artifacts/gameplay-rework/gpr-01-4/gameview/20261029-235620-0049383`: A/AB/AC/ABC-travel-1/2, confirmed-event, after-event e cleanup; ABC-reused-as-pure-wind; reuse-before/after-pure. ComparadoresAB/AC/ABC de viagem e evento triplo abertos e inspecionados: azul e laranja reconhecíveis separadamente do vento branco, arco externo e anel de explosão separados. Recibos de HP/kill/parents gravados separadamente em receipts, não inferidos dos PNG.

FirstSector real: seis testes adicionais cobrem cosmetic elemental com zero feedback de dano, pausa, próxima sala, última morte/clear, disable, troca, fim de run e cena; capturas próprias em regression/room-captures. Fixture de comparação usa câmera ortográfica5,5m, rig publicado e três alvos, sem close-up exclusivo. O teste de reutilização puramente visual é identificado como fixture; a captura ABC-reused-as-pure-wind mantém câmera comparável.

## Limites, integridade e próximo gate

Skills locais visão/combate/boons/aceite e Unity C#/MCP aplicadas. Mesmo Editor/projeto/baseline de01.4a; ajustes reversíveis, sem saves/preferências/controle de progressão novo. Sem playtest humano, benchmark GPU/standalone ou certificação artística final. Sobreposição com telegraphs reais e multidões merece revisão física; comparação isolada não prova legibilidade de toda batalha. Velocidade, raio, distância, TTL, dano e balanço nativo preservados. Avançar somente para regressão/integração01.4e, sem GPR-01.5.
