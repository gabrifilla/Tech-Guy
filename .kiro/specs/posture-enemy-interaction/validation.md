# Validação e handoff — postura e interação com inimigos

## Estado

Implementação e validação técnica da Fase 2 concluídas em 02/10/2026 (tarefas 1–16). Em 03/10/2026 o usuário declarou **“Fase 2 está aceita. Pode seguir com as proximas tasks”**. Essa declaração registra a aceitação humana da tarefa 17; com este handoff consolidado, a tarefa 18 está encerrada e a Fase 3 pode avançar. Não foram fornecidos cena, duração ou observações individuais do playtest; não são inferidos aqui. Evidências técnicas e limitações históricas abaixo permanecem preservadas.

## Decisões e escopo implementado

- Categoria física independente do rank; resistência binária; Boss mantém imunidades físicas obrigatórias. Heavy é MediumMob com Knockdown, mantendo postura 220, HP 2,2 e movimento 0,6.
- ReactionConfiguration/ReactionPolicy/PostureReactionState concentram autoria validada, políticas e leases de controle. CombatReactionController coordena apresentação, movimento limitado, cleanup e relógio mecânico congelado por pausa/hit-stop.
- Knockdown tem queda, chão e recuperação. Launch limita altura a 4 m e Knockback a 8 m, incluindo valores finitos extremos. Push tem limite próprio e não se converte em Knockback.
- Interrupção consome um limiar por contribuição positiva e conserva excedente. Abertura de Elite/Legendary/Boss protege a primeira execução real até sua recuperação, com cancelamento, ausência de ação elegível e watchdog explícitos.
- EnemyVariant aplica o perfil antes do primeiro hit e não reinicializa postura/abertura no Start após dano no frame de spawn.
- Manopla usa Stagger com contribuições 0,35/0,45/0,60. E passa pela entrada existente AbilityHolder. Arco/Lança preservam o canal básico compartilhado.
- ImpactFeedbackScope agrega dano confirmado e quebra por grupo, incluindo callbacks reentrantes. Receipt local de Actor registra o dano efetivo antes de callbacks que podem curar ou lançar exceções; APIs virtuais e ordem pública dos eventos foram preservadas.
- Hit-stop comum acompanha o perfil da Manopla (asset atual: 0,05 s); dano comum de outras armas mantém timing anterior. Quebra confirmada seleciona 0,09 s, inclusive com morte do alvo ou exceção posterior de apresentação.
- Pop/flash e popup usam tempo real; controle usa tempo mecânico. Abertura não é apresentada como stun. Pose/VFX/SFX de quebra são locais e procedurais: inventário MCP não encontrou AudioClips no projeto.

## Migração e autoria

Migração explícita pelo Unity MCP de Gauntlet, Heavy, Grunt e Fragile. Assets salvos e reimportados; dados relidos pelo Editor e validação bruta sem erros. Segunda aplicação: zero alterações. GUIDs existentes preservados; nenhum prefab ou cena de jogo foi alterado. Campos novos têm flags de override para distinguir ausência de zero intencional.

## Evidência executada

Editor: Tech-Guy@fccafb2b47cd7671, Unity 6000.5.10f1. Refresh/compilação e Console consultados pelo MCP, sem limpar mensagens. Warnings iniciais CS0108/CS0618 são anteriores ao incremento.

| Execução | Resultado |
| --- | --- |
| EditMode final — ff0e594693a7444ea2fde030de64d9d4 | 450/450 aprovados, zero falhas/skips; 38,4490683 s |
| PlayMode integração — 69a16b497da74364a40316872a97f8cf | 47/47 aprovados, zero falhas/skips; 19,0745406 s |
| PlayMode regressão ampla — 6f30a40ef89742ba8d636618c292906a | 171 executados: 169 aprovados, 2 falhas de fixture, zero skips; 1667,0197982 s |
| PlayMode reteste — 25b398a4c2ae4b47b0938febc9775c05 | 23/23 aprovados, zero falhas/skips; 40,1872393 s; inclui as duas falhas corrigidas |

A integração cobre Manopla/input real, E comum/Boss, retomada de ação, obstrução física, Rigidbody Push, cleanup, relógios, feedback letal, múltiplas quebras, callbacks reentrantes e contratos de eventos. A regressão final acrescenta perfil no spawn, feedback com subscriber excepcional e timing comum de Manopla versus Arco. As 171 verificações PlayMode têm aprovação observada entre a execução ampla e o reteste; não se afirma uma execução integral 171/171 após as correções. Nenhum defeito demonstrado nesses testes permaneceu aberto. EditMode não foi repetido após os ajustes exclusivamente nas fixtures PlayMode.

## Rastreabilidade das propriedades

| Propriedades | Evidência automatizada |
| --- | --- |
| P1 canais; P2 limites | PostureBoundaryTests, PostureReactionPlayModeTests; dano/postura independentes e movimento real limitado |
| P3 identidade; P4 default/fallback; P12 autorização | ReactionConfigurationTests, ReactionPolicyTests, PostureMigrationTests; categorias, rank, Boss e serialização |
| P5 quebra única; P8 recuperação; P9 reaplicação | PostureReactionStateTests, PostureReactionPlayModeTests; reentrância, leases, pausas, proteção e ação real |
| P6 limiar; P7 cancelamento | RankInterruptStaggerAccumulationPropertyTests, PostureBoundaryTests e testes de windup/Boss |
| P10 feedback | ImpactFeedbackScopeTests, PostureFeedbackPlayModeTests; letal, lote, exceção, relógios e restauração |
| P11 compatibilidade | CompatibilityContractPlayModeTests, HookBusWiringExampleTests, BasicAttackR1PlayModeTests e regressões de boons/armas |
| P13 movimento; P14 bloqueios | PostureReactionStateTests, PostureReactionPlayModeTests e GuardBreakerKnockbackPlayModeTests; prioridade, obstrução, cleanup e abertura sem stun |

P1–P14 têm cobertura técnica. A avaliação perceptiva dos requisitos R2/R3/R5/R7 permanece no gate 17; essa tabela não constitui aprovação de gameplay.

## Adaptações de testes e contratos

- CC proporcional e consumo de todos os limiares foram substituídos pelos contratos binário/um limiar por contribuição registrados no audit. Cobertura de contabilidade e limites mantida.
- Teste de Boss agora distingue Stagger comum que preserva windup de quebra real que cancela; não equipara dano positivo a quebra.
- Fixtures de projéteis isolam o pool entre casos e verificam desativação no retorno ao pool, conforme otimização já existente. Loops deixam de esperar destruição de objetos reutilizáveis. Ordem Pierce é verificada enquanto ainda existe alvo inline pendente, inclusive a transição do último alvo.
- Fixtures homing posicionam os alvos esperados como realmente mais próximos, sincronizam física e alinham colliders com o ponto de mira. A lógica de produção dos projéteis não foi alterada.
- Fixture Detonation desativa ThermalShock não configurado para medir somente o contrato em teste; testes próprios ThermalShock permanecem separados. Spawner recebe dependências antes do Awake.
- Limites de tempo dos casos gerados longos aumentados para 600 s; quantidade de casos e asserções de dano, duplicação e limites permanecem.

Execuções amplas anteriores revelaram esses problemas de fixtures e expectativas; seus jobs sem summary final não são contados como aprovação. Uma inicialização de teste perdida durante reload foi removida com run_tests(clear_stuck), sem limpar Console; regressão reiniciada após compilação limpa.

A regressão ampla terminou com duas falhas restantes: o teste de launch homing dependia de um primeiro frame menor que o intervalo de seek, e a fixture de SplitArrow destruía objetos sem remover referências do pool entre seus casos. O primeiro agora fixa apenas seu tempo simulado em 1/60 s e restaura Time.captureDeltaTime no teardown; o segundo reinicializa as referências do pool após a limpeza. Casos e asserções mantidos. Essas correções são de fixture, sem mudança na produção dos projéteis. O summary da execução ampla foi consultado em memória via execute_code MCP: get_test_job omite result quando o status é failed; foram confirmados 169 aprovados, 2 falhas e zero skips, sem inferência a partir de contagem parcial.

## Referências e limitações

MCP inspecionou sete cenas próprias e 49 prefabs, incluindo resolução de GUIDs pela AssetDatabase. MainMenu e sua condição dirty foram preservados durante preview. Há sete GUIDs ausentes legados em Playground, Structure_Prefab, NestedParentArmature_Unpack e PlayerArmature, além de um componente com script ausente em Playground/Player. Arquivos envolvidos não foram modificados; referências comparadas com HEAD já existiam. Essas pendências estão fora do incremento de postura e não foram recriadas silenciosamente.

Inspeção final: quatro assets reimportados novamente, validação bruta sem erros e migração repetida com zero alterações; Heavy Medium/Knockdown, Grunt e Fragile Small/Launch, Manopla Stagger/0,35/0,45/0,60 confirmados. Editor em MainMenu e fora de PlayMode; Console consultado sem limpeza: zero erros, dois warnings esperados das fixtures (AbilityHolder com bindings incompletos e fallback MissingProfile.Normal). Nenhum diff em cenas/prefabs/metas existentes/ProjectSettings, nenhum script novo sem meta e nenhum meta novo órfão. git diff --check sem problemas. Cenas temporárias do runner removidas pelo próprio runner.

## Aceitação e próximas ações

**Aceitação atual:** confirmada pelo usuário em 03/10/2026, conforme declaração na seção Estado. A tabela abaixo preserva o roteiro de observações que estava pendente antes dessa aceitação; suas células não representam recusa da fase nem evidência nova de cada cenário. Observações específicas não foram fornecidas. Próxima ação autorizada: implementar a Fase 3 e executar seus checks relevantes.

O gate 17 exige jogar Manopla sem boons contra Small, Heavy Medium, Elite, Legendary e Boss, observar reconhecimento sem barra, queda/chão/recuperação, juggle do E e recuperação de ação sob pressão. Os testes técnicos não estabelecem sensação de combate nem aprovação humana. Registrar observações aqui; ajustes futuros exigem repetição dos checks afetados. Tarefa 18 só pode encerrar após essa aceitação.

| Cenário para aceitação humana | Observação a registrar | Estado |
| --- | --- | --- |
| Manopla sem boons, Grunt/Fragile Small | Pequeno reage e Launch da quebra é legível sem consultar barra | Pendente |
| Manopla sem boons, Heavy Medium | Queda, permanência no chão e recuperação comunicam Knockdown | Pendente |
| Elite e Legendary sob sequência de básicos | Abertura reconhecível e primeira ação efetivamente recuperada | Pendente |
| Boss sob básicos e E | Abertura reconhecível; imunidades físicas mantidas; retomada perceptível | Pendente |
| E contra alvo comum elegível | Juggle legível e distinto da reação normal | Pendente |
| Quebra letal e lote multi-alvo | Ênfase de quebra perceptível, sem sensação de pausas somadas | Pendente |

Registrar cena/encontro, configurações, observações de cada linha e aprovação ou defeito. Usar os quatro assets migrados, sem boons para o gate de base; não compensar falhas perceptivas adicionando procs de fases futuras.
