# Gameplay Rework — requisitos da migração e da primeira fatia

27/10/2026. **GPR-C01.3a concluída tecnicamente, aguardando revisão.** C01.2c aceita no escopo técnico. LMB direcional por clique implementado; hold do básico somente com ChargedShot por decisão explícita; Asura no RMB da Manopla incluída por autorização adicional.102/102 testes normais; aceite físico pendente. C01.3b/GPR-01 não iniciadas. [Relatório C01.3a](../decisions/GPR-C01.3a_RELATORIO.md).

## Entrega e exceções vigentes — C01.3a

GPR-R03/04/08: Primary edge → direção do cursor no plano do player → pedido individual, inclusive whiff; sem enemy assist/target lock/click-to-move. Buffer compartilhado existente de120ms/single slot, direção capturada e clocks preservados. Manopla/Spear e Bow sem ChargedShot não repetem por hold. Com ChargedShot, hold legado foi preservado por resposta explícita do desenvolvedor; release não cria ataque.

Ampliação autorizada: RMB/canal secundário da Manopla ativa/continua a mesma AsuraBurst histórica, sem básico concorrente ou custo duplicado. F11 permanece compatibilidade; F prioritário; UI/foco/pausa pedem soltura. Fonte/estado index3R e prefs preservados; GetAbilityKey apresenta o secundário. GPR-R05/07 completos continuam pendentes: não há X novo, extração modular concluída ou Settings completo. Arco/Lança mantêm secundário legado até design próprio. Feedback físico de animação e opção de dash permanece aberto.

## Critérios verificados nesta revisão

- V01/02/08: sliding tangencial somente de locomoção, parede/quina/borda/rampa em fixtures reais, sem aumentar diagonal ou mudar speed/dash; mesmo motor e writer.
- V07: gate de entrada cobre pausa, recompensa, resultados, reload, foco, disable e teclas mantidas; requer soltura e frame posterior. Cast já aceito mantém impacto/cooldown.
- R contextual continua reservado; Interact separado sem tecla definida; F11/Asura e Q/E/R preservados.
- Incidente recovery: execução normal passa, expiração sob hitch500 reproduzida; causa histórica e política de TTL sob hitch continuam abertas. Não flexibilizar assert/tolerância para fechar o gate.
- Aceite físico atual pendente. Cliques sem alvo, opção de dash e refino de animação são feedback registrado, não funcionalidades declaradas entregues.

| ID | Requisito e condição observável | Visão/decisão | Fase/tarefa | Validação |
|---|---|---|---|---|
| GPR-R01 | Nenhum runtime/asset/scene/save alterado em GPR-00; hashes e status preservados | Pedido/escopo | 00.1–00.5 | V00 integridade |
| GPR-R02 | WASD gera direção planar direta, diagonais normalizadas; sem destino de clique/perseguição. Referencial câmera/mundo e motor têm decisão/prova pendente | WASD aprovado; D12 detalhes pendentes | C01.2 | V01 paredes/bordas/frame rate e protótipo |
| GPR-R03 | Mira do cursor independente de deslocamento; swing mantém direção capturada; não muda contato/cadência | RF01/10; D11/12 | C01.2/3 | V02 aim/movimento/opostos |
| GPR-R04 | Um cliqueLMB deliberado gera no máximo um pedido básico; cliques avançam a sequência da arma. RMB aciona exclusivamente especial da arma/Asura na Manopla; sem terminal alternativo ou ataque repetido automático por padrão | D11 aprovado; D16hold pendente | C01.3 | V03 tap/hold/release/busy; sem básico de RMB |
| GPR-R05 | Quatro slots modulares Q/E/R/X independentes do especial. Choque, Asura e FinishSequence preservados; separar origem histórica R da teclaX. Conteúdo inicial de X requer decisão | D01/13/15 | C01.3/4 | V04 por arma/slot/especial/Cadenciada |
| GPR-R06 | Interact independente; sem E-interação/skill ou F-interação/finalização acidental | D13 | C01.4 | V05 Nexus, treino, terminais, compact R |
| GPR-R07 | Settings remapeia movimento individual, ataques, skills, dash/interação/finalização; primário+secundário opcional; Apply/Cancel/Reset/conflicto/persistência | D14 | C01.4 | V06 reload, relaunch, corrupt/missing prefs |
| GPR-R08 | Pausa/reward/resultados/rebind/foco perdido bloqueiam mundo; soltura evita replay; menu permanece utilizável | RF14; D14 | C01.2–5 | V07 teclas presas/fechamento/click-through |
| GPR-R09 | Hitstop/cast/rooting/dash/pull/CC/morte mantêm autoridade de movimento e clocks existentes; direção do dash e motor candidato condicionados à prova/decisão, sem inferir do path legado | RF10/D12/18 | C01.2/5 | V08 timings/cancel/owner; protótipo candidato |
| GPR-R10 | A altera geometria do básico terminal; só HP confirmado gera onda; sem novo relógio ou controle universal | RF02/05/10; D06 | 01.1/2 | V09 base/A, whiff/LOS/imune |
| GPR-R11 | A+B compõe onda+Conductor em qualquer ordem de aquisição, com ancestry/dedupe/budget; derivados não recarregam recursos por nova regra | RF03/11/15; D08 | 01.3 | V10 budgets/lineage/total HP |
| GPR-R12 | Swap index0 seguro/atômico no adaptador de variante; preserva outros slots/timers/IDs; incompatível não é ofertado | RF08/09; D04/05 | 01.4 | V11 rollback/replay/stale ticket |
| GPR-R13 | Visual de A/A+B deriva da build, reverte sem resíduos; perigo/silhueta/HUD legíveis | RF07/13/14; D09 | 01.5 | V12 capturas/cleanup/humano |
| GPR-R14 | Tickets/ordinais/primeira Mutation/gates Elite/boss/seed respeitados; rota representativa real | RF16/17; D07 | 01.4/6 | V13 simulação + gameplay real |
| GPR-R15 | Total/subcategorias reconciliam HP efetivo; remapeamento não renomeia slot histórico; RunSummary não é substituído por fixture | RF15/19 | C01.4 +01.6 | V14 receipts/prefs/duas runs |
| GPR-R16 | Regressões compartilhadas incluem Bow/Spear, Common/Heavy/Elite/Boss, cancel/ghost/dedupe/projéteis/novo encontro | RF10/12/19/20 | C01.5 +01.6 | V15 filtros por contrato |
| GPR-R17 | Aprovação humana independente para controles/feel/visual; sem inferência por testes verdes | RF20 | C01.5 /01.6 | V16 roteiro humano |

RF04 (três modificadores), RF06 (camadas cumulativas completas) e awakening: GPR-02; RF18 (duas builds deliberadas): GPR-03; meta lateral: GPR-04; expansão de identidade Bow/Spear: GPR-05. Não desaparecem do produto por estarem fora da primeira entrega.

Não funcionais: desempenho medido no hardware/cena real antes de fixar meta; nenhuma mudança nos GUIDs/shared assets; sem alteração em terceiros ou schema de moeda/unlocks/RunSummary; sem managers/event buses/damage pipelines paralelos. Tratamento de NaN/Infinity/ausência de câmera/dispositivo deve falhar sem mover/atacar em direção inválida. Manter a execução Asura existente: consumo100/mana/cooldown/estágios/continuações/F/cancel/pose/HP/cleanup, nunca clone de executor paraRMB.
