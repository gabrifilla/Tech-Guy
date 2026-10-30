# GPR-01.5c — leitura visual na câmera de gameplay

29/10/2026 · **CAPTURAS E VALIDAÇÃO TÉCNICA CONCLUÍDAS; ACEITE FÍSICO PENDENTE.** Não houve redesign de VFX ou alteração de câmera, inimigos ou cenários publicados.

## Objetivo e execução

Comparar A/AB/AC/ABC na FirstSector atual, com câmera normal, HUD, vários inimigos, básicos reais, arco/explosão legítimos e telegraph real de ataque pesado. Gpr015ProductionAuditTests.cs carrega FirstSector, entra no nó1 pelas autoridades publicadas e usa os atores gerados. Somente a fixture posiciona atores, desliga decisão autônoma para controlar a comparação e chama EnemyCombatActions.Perform(HeavySlam) com windup/dano normais. Não desenha uma imitação de telegraph. Player imune e CoinDrop removido protegem a execução/persistência. Aquisição dos boons nestes quatro casos é **assistida explicitamente**, distinta dos quatro percursos sem grants de01.5b.

UTF/MCP: `run_tests(mode=PlayMode,test_names=[TechGuy.Tests.Gpr015ProductionAuditTests],include_details=true,init_timeout=120000)`, job **0bb3da4bc2504e4c8d7a5ba93e3436bb**, **8/8**,17,8809324s: quatro casos de visual e quatro de rota/rewards, sem contar o mesmo job duas vezes. [Resultado](../validation/artifacts/gameplay-rework/gpr-01-5/production-first.json). Importação/Console verificadas; nenhuma falha nesta seleção.

## Comparadores reais1160×600

As quatro imagens abaixo foram abertas e inspecionadas. A câmera/escala são as publicadas, sem close-up ou câmera de fixture substituta. Layout varia por seed; posicionamento relativo dos alvos e direção do tornado são controlados. Em cada pasta: native-basic, travel-warning-1/2, confirmed-event, cleanup, context.txt e hp-lineage.json. Sem edição de pixels.

**A — vento puro e ameaça vermelha**

![A em FirstSector](../validation/artifacts/gameplay-rework/gpr-01-5/c/20261030-003743-0763622/build-0-travel-warning-2.png)

**A+B — eletricidade persistente**

![AB em FirstSector](../validation/artifacts/gameplay-rework/gpr-01-5/c/20261030-003734-8241149/build-1-travel-warning-2.png)

**A+C — fitas de fogo**

![AC em FirstSector](../validation/artifacts/gameplay-rework/gpr-01-5/c/20261030-003737-6086719/build-2-travel-warning-2.png)

**A+B+C — vento, laranja e azul simultâneos**

![ABC em FirstSector](../validation/artifacts/gameplay-rework/gpr-01-5/c/20261030-003740-3275685/build-3-travel-warning-2.png)

## Observações e HP causal

- A espiral branca permanece a base. Azul em zigue-zague e laranja são identificáveis durante viagem; o azul é fino/subtil nessa escala, especialmente onde cruza o branco. Não certificamos leitura inequívoca em toda densidade/monitor.
- O anel/preenchimento vermelho da pancada pesada continua visível nos quadros observados, separado do anel amarelo de impacto/explosão. Isso demonstra aquele enquadramento, não ausência universal de oclusão em swarms.
- Básicos/poses/feedback nativos foram capturados antes do terminal. O texto +3ASURA-BÁSICO é de acertos diretos anteriores, não geração pelo vento. As imagens de viagem conservam esse feedback transitório.
- [ArcoAB real](../validation/artifacts/gameplay-rework/gpr-01-5/c/20261030-003734-8241149/build-1-confirmed-event.png), [explosãoAC real](../validation/artifacts/gameplay-rework/gpr-01-5/c/20261030-003737-6086719/build-2-confirmed-event.png) e [ABC com morte elétrica e explosão](../validation/artifacts/gameplay-rework/gpr-01-5/c/20261030-003740-3275685/build-3-confirmed-event.png) foram abertos separadamente. O arco externo não é o zigue-zague persistente da identidade.
- O receiptABC confirma12HPTechnique +1HPConductor letal +0,75HPDetonation =13,75HP, três transações secundárias Basic/Titanic com o mesmo parent gauntlet:3:3, um kill creditado, zero duplicatas/erro do observador. O alvo configurado abaixo de1HP é normalizado pelo Actor para1; reportamos o receipt, não o número solicitado à fixture. Imagens não são a prova de HP.

O teste encerra o contexto, exige zero raízes e WaveActive=False e captura cleanup. A seleção01.5a cobre salaCompleted, próximo encontro, pausa/hitstop, morte/disable/swap/run/cena e ausência de dano/feedback no tornado cosmético. Cosmetic pode conservar identidade aceita, mas nunca produz arcos/explosões sem evento. Snapshot por aceitação/aquisição durante voo e pooling foram revalidados também na seleção final abaixo.

## Snapshot, reutilização e limites

Job **40bd953aa78d4a91a955d62b76c0ca40**, **10/10**,22,25779s, inclui seis casos visuais/snapshot/pool existentes, três cargas e um diagnóstico de pausa. Não somar os dez como dez novos casos visuais. [Seletores](../validation/artifacts/gameplay-rework/gpr-01-5/final-checks.request.json), [resultado](../validation/artifacts/gameplay-rework/gpr-01-5/final-checks.json), capturas em final-checks/gameview. A aquisição durante antecipação/voo não altera o projétil antigo; próximo terminal tem a nova identidade; lease velha não limpa a instância reutilizada e não sobram layers.

Arquivos de teste criados/alterados: Gpr015ProductionAuditTests.cs/meta, TitanicElementalVisualPlayModeTests.cs e TitanicCompositionPlayModeTests.cs para destinos de evidência. Nenhuma fonte de VFX/combatente ou asset de produção alterado em01.5. Skills locais visão/combate/boons/aceite e Unity C#/MCP aplicadas. Evidência histórica preservada; scopes restauram preferências/assistência e resumo.

Limites visuais: amarelo/vermelho/azul ainda demandam avaliação humana com inimigos se movendo autonomamente; não houve simulação de todos os perigos juntos nem playtest físico. Nenhuma aceitação artística foi deduzida de assertions. Velocidade/tamanho/mira/cadência/moonwalk continuam diferidos. Próximo aceite é revisão do desenvolvedor, sem autorização implícita de redesign ou GPR-01.6.
