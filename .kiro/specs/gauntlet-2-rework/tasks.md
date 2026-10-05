# Implementation Plan: Manopla 2.0 (Fase 3)

## Overview

Este plano converte [requirements.md](requirements.md) e [design.md](design.md), sincronizados com os pareceres de 03/10/2026, em incrementos da **Fase 3 — Manopla 2.0**. Ordem: auditoria/reconciliação → contextos/recurso/base → R → snapshots/Techniques/Mutations → aquisição/compatibilidade/HUD → validação técnica → aceitação humana.

**Status: finalizado em 04/10/2026.** Tasks 1–16 concluídas tecnicamente; Fase 3 aprovada expressamente pelo usuário e tasks 17–18 concluídas. Fase 2 já aceita. Evidências e cobertura P1–P16 em `contract-audit.md`; encerramento em `validation.md`.

Preservar mana, Arco/Lança, fundação da Fase 1 e autoridade da Fase 2. Não acrescentar momentum nativo, perfect dodge, Protocol System, Mutation rooms ou novas rotas de run. Capacidade/taxas e conteúdo seguem o design como parâmetros iniciais; ajustar tuning com evidência não equivale a expandir escopo.

P1–P16 referem-se a **Correctness Properties** do design. Referências `R2.3`, por exemplo, apontam ao critério 3 do Requirement 2. Testes de propriedades usam as convenções existentes (`PropertyCheck.ForAll`, casos determinísticos, seed/contraexemplo e tags de feature/propriedade/requisito) quando apropriado. Cenários de pipeline, input, custo, colisão, scheduler e lifecycle exigem integração real; helper isolado não os substitui. Nenhum teste/critério listado é opcional.

**Validação incremental:** antes de trabalho dependente do Unity, confirmar Unity MCP, projeto/Editor correto, capacidades e Console inicial sem limpar. Após C#/assets relevantes, importar quando necessário, aguardar compilação e inspecionar Console. Executar somente testes relevantes ao incremento e dependências diretas; ampliação precisa de justificativa concreta. Usar MCP em todos os checks Unity necessários que ele suporta, incluindo testes, referências, save/reload e runtime; sair de PlayMode preservando a cena do usuário. Registrar tentativa/limitação e manter validação pendente se MCP falhar. Evidência histórica não substitui execução atual.

Antes de editar, conferir `git status` e preservar alterações existentes. Código/assets novos seguem AGENTS.md; testes em `Assets/_Project/Scripts/Tests/EditMode/Editor` e `Tests/PlayMode`. Preservar GUIDs, IDs e limites de Resources. Marcar subtarefa apenas após implementação e seus checks exigidos; checkpoints/gates exigem evidência própria, não somente soma de caixas.

## Tasks

- [x] 1. Auditar contratos, consumidores e gates antes de implementar
  - [x] 1.1 Conferir dependências e escopo da fase
    - Ler requirements/design/tasks atuais das Fases 1–2 e specs de boons/sinergias afetadas. Conferir tasks 17–18 da Fase 2 e evidência em seu `validation.md`; registrar pendência real ou orientação explícita de sobreposição.
    - Preservar decisão sem momentum nativo, mana atual e exclusão de fases futuras. Documentar situação em `contract-audit.md` desta spec, sem marcar aceitação de outra fase.
    - _Requisitos: R1.4, R4.1, R4.5, R8.2, R9.7_
  - [x] 1.2 Inventariar pipeline e reconciliar adaptações específicas
    - Mapear emissores/assinantes de HP, básico, quebra, `AttackHitsResolved`, HookBus e adição explícita de energia; registrar payload, contagem, ordem, proprietário e funcionamento sem RunBoons.
    - Auditar Momentum, AsuraFist/AsuraSurge, GuardBreaker, HungryCombo, SeismicFist, ShockRing e MomentumStrike; registrar contrato antigo/novo, arquivos/specs/testes afetados e expectativas substituídas antes de editar consumidores.
    - Registrar aliases de Q, ComboNova/Shockwave e IDs/ranks existentes. Delimitar exclusão de Asura/reprodução local; inventariar outros consumidores diretamente afetados, sem desligar sinergias globalmente.
    - Sincronizar apenas contratos afetados nas specs anteriores durante a implementação; preservar asserts corretos de eventos, imunidade e cancelamento.
    - _Requisitos: R2.4, R5.2, R5.7, R8.1, R8.4–R8.6_
  - [x] 1.3 Capturar baseline Unity quando iniciar trabalho dependente do Editor
    - Confirmar MCP/instância/projeto e capacidades disponíveis, Console e estado da cena. Inspecionar arma, habilidades, perfis, HUD e fluxo `RunBoons.OfferReward`/`RunRewardUI` pertinentes.
    - Identificar testes direcionados e erros preexistentes. Registrar no audit sem limpar Console nem executar suíte ampla por padrão.
    - _Requisitos: R7.8, R9.1, R9.2_

- [x] 2. Checkpoint — contratos reconciliados e dependências liberadas
  - Dependência: tarefa 1. Confirmar adaptações e testes esperados registrados, inclusive granularidade privada versus eventos públicos e política de descendentes.
  - Não iniciar código dependente se gates da Fase 2 estiverem abertos sem orientação explícita de sobreposição. Registrar essa condição no audit e preservar os gates humanos originais.
  - _Requisitos: R8.1, R8.4, R9.7_
  - Evidência: declaração de aceitação da Fase 2 pelo usuário em 03/10/2026, handoff consolidado e contratos/testes esperados registrados no audit. Não se trata de autorização presumida de sobreposição.

- [x] 3. Implementar autoria validada e contextos causais
  - [x] 3.1 Definir dados serializáveis e validação bruta
    - Implementar taxas/limites de D1, capacidade 100 e metadados/ranks/perfis de conteúdo; diferenciar ausência de override de zero. Validar NaN/infinito, negativos, ranks, slots, tempos, referências e incompatibilidades antes de normalizar.
    - Verificar Inspector/save/reload via MCP; configuração inválida impede entrada afetada sem execução parcial. Não usar dicionário runtime como fonte serializada.
    - _Requisitos: R2.7, R5.3, R6.2, R7.5, R7.8_
  - [x] 3.2 Implementar contexto/recibos e registry local
    - Capturar atacante, família, epoch, execução/raiz, passo, alvo/geração, categoria e origem. Usar IDs não reciclados no epoch; registrar filhos autorizados e fechamento da raiz.
    - Emitir recibos independentes de HP efetivo/quebra; HP letal continua válido. GuardBreaker herda raiz do básico; echo/zona mantém origem descendente.
    - Contexto encerrado ou epoch invalidado rejeita replay mesmo após liberar chaves; callback não recria contexto. Liberar registry após filhos sem cache global infinito.
    - _Requisitos: R2.5, R2.6, R7.7, R8.1, R8.3_
  - [x] 3.3 Testar dados e lifecycle de contexto
    - EditMode: P1/P2/P6/P12, recibo sem dono, pooling, duplicação/reentrância, filho legítimo após produtor e replay após fechamento/liberação.
    - Serialização: números/referências persistem após reload; payload legado continua inalterado. Executar checks pertinentes via MCP.
    - _Requisitos: R2.5–R2.7, R7.8, R9.2–R9.4_
    - Evidência: GauntletAuthoringAndContextTests, 10 casos distintos aprovados por MCP entre jobs 86d7db485d564fc1afffd9971bd17a0f (7/7), 383c495471cc4b1ea2089fa3be83fd9f (2/2) e 21714873da044424b82ce107a53135f4 (4/4, retestes pertinentes). Save/reload real, perfil referenciado, NaN/infinito, registry/replay e casos gerados de lifecycle; zero falhas/skips. Integração no pipeline pertence à tarefa 5.

- [x] 4. Implementar política de Asura e transação de ganhos
  - [x] 4.1 Evoluir proprietário único do recurso
    - Evoluir AsuraMomentum/fachadas sem segunda barra: básico 3; Q/W/E 10, alternância +15; quebra +20 até duas por raiz. Categoria muda somente na confirmação elegível de Q/W/E.
    - Bloquear geração de combate durante R, inclusive callbacks anteriores; R/descendentes não geram energia. Exceção interna de reserva; adição externa validada não contorna bloqueio.
    - Quebra além do limite continua mecanicamente válida, com reação/abertura/feedback/eventos. Cancelamento anterior não ganha, posterior não desfaz ganhos.
    - _Requisitos: R2.1–R2.7, R3.2, R8.5_
  - [x] 4.2 Consolidar parcelas antes de publicação
    - Preparar nativo, alternância, quebra e bônus da mesma confirmação; consumir chaves/aplicar tudo antes de notificar. Registrar parcelas/ganho efetivo/motivos e teto, inclusive ganho zero na capacidade.
    - Reentrada de observador não concede novamente nem permite ativar R entre parcelas. Revalidar solicitações ao terminar a resolução do impacto; preservar ordem/contagem pública auditada.
    - _Requisitos: R2.5, R2.8, R7.3, R8.1_
  - [x] 4.3 Testar política e atomicidade
    - EditMode P1–P8: vazio/rejeição, repetição/alternância, multi-alvo, terceira quebra sem energia, HP imune com quebra, físico imune com HP, teto e inválidos.
    - Testar observador tentando consumir R entre ganhos de HP/quebra/Technique, bloqueio durante R e não reaplicação posterior; tratar confirmação letal sem consultar vida posteriormente.
    - _Requisitos: R2.1–R2.8, R3.2, R9.3_
    - Evidência: job MCP c2025966ffd3481c9f6ce1403981f391, 10/10 EditMode, zero falhas/skips; 8 casos da política e regressões AsuraCategoryAlternationEnergy/AsuraDenyWhenNotFull. Casos gerados incluem saturação com int.MaxValue, confirmação consumida no teto, reset e replay. Integração com emissão/custos reais segue nas tarefas 5/7.

- [x] 5. Integrar recibos e geração ao kit base
  - [x] 5.1 Conectar básico/Q/W/E e postura ao pipeline existente
    - Remover geração no início de Q/W/E e consumir confirmação contextualizada. Garantir energia base sem boons/RunBoons por sinais disponíveis ou observador injetado.
    - Preservar eventos legados, feedback agrupado, resistência e autoridade única. Não inferir arma atual em callback atrasado nem gerar impacto por proximidade.
    - _Requisitos: R1.1–R1.6, R2.1–R2.6, R8.1–R8.5_
  - [x] 5.2 Validar integração real
    - PlayMode P1–P5/P11/P15/P16: básicos multi-alvo/letais, Q/W/E vazio/confirmado, quebra sem HP, físico imune, duas/três quebras, cancelamento antes/depois e callbacks reentrantes.
    - Registrar sequência/contagem real de eventos e ausência de RunBoons. Executar regressões dirigidas de Arco/Lança somente nos caminhos compartilhados alterados.
    - _Requisitos: R1.2, R1.6, R2.3, R8.1–R8.3, R9.1–R9.4_

- [x] 6. Checkpoint — kit base e recurso confirmados
  - Dependências: 3–5. Confirmar compilação/Console, dados persistidos e testes relevantes via MCP; separar inspeção estática de runtime observado.
  - Evidência: 7 novos cenários PlayMode, 2 regressões de input/postura e 7 PostureFeedback; jobs e retestes registrados no audit. EditMode 0d01afc607754a57b2763171f2c690a4: 18/18. Referências da arma e quatro perfis inspecionadas por MCP; cena NexusLobby preservada, sem erro de compilação. Não equivale a aceitação humana.
  - Não seguir com consumidores de conteúdo se contexto, atomicidade ou eventos tiverem falhas. Registrar evidência no audit, sem chamar este checkpoint de aceitação de gameplay.
  - _Requisitos: R2.1–R2.8, R8.1, R8.5, R9.2–R9.4_

- [x] 7. Consolidar ciclo de R base e reserva
  - [x] 7.1 Implementar aceitação/consumo/cleanup atômicos
    - Gate cheio 100, mana/cooldown/estado atuais; rejeição preserva recursos/ação. Consumir uma vez, reserva do snapshot +20/rank até 60, sem refund após cancelamento.
    - Preservar compromisso e perfil base; bloquear geração enquanto ativo e liberar somente locks/modificadores próprios, sem auto-R ou prolongar proteção com echoes.
    - _Requisitos: R3.1–R3.7, R7.7_
  - [x] 7.2 Testar R no pipeline
    - PlayMode P7/P12: cheio/incompleto, mana/cooldown indisponíveis, reentrada, reserva ranks 0–3, cancelamento/morte/troca/disable e retorno por input/buffer.
    - Confirmar nenhum ganho por R ou impacto anterior durante R, nenhum débito duplicado e nenhum refund implícito.
    - Evidência: 5 novos cenários PlayMode de R (ranks 0–3, atomicidade/reentrada, rejeição sem cancelar, cleanup, recibo anterior e buffer); jobs cbdb1bbe52af48e1a7324c92752c14f9 4/4, 8aebb30f7ffb4231976018359f9db00b 1/1 e 0777adbca8b94ba1bd840129d13ab950 1/1, mais cenário de rejeição e 2 regressões QWE/reentrada aprovados no job fdd1fd7e3f1d4879a21ea255f0f7621b. Histórico e correções de fixtures no audit. Perfil/commitment base mantidos; conteúdo descendente permanece para as tasks 8–12.
    - _Requisitos: R3.1–R3.7, R9.3_

- [x] 8. Implementar snapshots e composição de conteúdo
  - [x] 8.1 Separar parâmetros imutáveis de contadores runtime
    - Builder: base → Mutation → Techniques → limites, por prioridade/ID fixos. Copiar passos/perfis necessários; aquisição durante ação só muda próxima execução.
    - MomentumStrike captura pilhas antes do lote, aplica multiplicador uma vez a todos os alvos e atualiza pilhas após suas confirmações; nenhuma dependência da ordem dos callbacks.
    - _Requisitos: R5.6, R5.8, R6.4, R6.5_
  - [x] 8.2 Validar snapshots e produtores locais
    - EditMode P8–P10: permutar aquisição/ranks, exclusividade por slot, aliases equivalentes, imutabilidade e aquisição durante ação.
    - Selecionar último passo direto como produtor de echo/zona; descendente não reproduz esses produtores. Manter elegibilidade dos demais consumidores conforme audit.
    - _Requisitos: R2.8, R5.5–R5.8, R6.4, R6.5, R8.1_
    - Evidência: snapshots/perfis, composição por slot/alias, produtores locais e limites; MCP 499a08b41cc348a59cb0cae7ad3d9f72 22/22 e 3f97be0814ff493a9724f29ab8701717 9/9. Pipeline/lote e regressões dirigidas no audit. Colisão/zona/continuação das Mutations continuam nas tasks 11–12.

- [x] 9. Implementar Techniques de energia e combo
  - [x] 9.1 Adaptar Momentum, AsuraFist e AsuraReserve
    - Momentum +10/rank por Q/W/E confirmado; AsuraFist +2/rank por básico confirmado, além do nativo; Reserve conforme tarefa 7.
    - Migrar AsuraSurge para granularidade por execução; reconfiguração substitui consumidor sem handlers duplicados. Não bloquear funcionamento nativo sem RunBoons.
    - _Requisitos: R2.1, R2.8, R3.2, R5.1–R5.5, R8.4, R8.5_
  - [x] 9.2 Adaptar HungryCombo e MomentumStrike
    - HungryCombo reduz cooldown ativo 0,3 s/rank uma vez por básico, limitado a zero, sem afetar R em execução. MomentumStrike até 10 pilhas conforme snapshot por lote; echoes/zonas não empilham.
    - HP recebido efetivo limpa pilhas uma vez por causa; absorção/imunidade total não limpa. Desativação limpa contadores, sem apagar inventário de ranks da run.
    - _Requisitos: R5.3–R5.6, R5.8, R7.7_
  - [x] 9.3 Validar ganhos/ranks/contadores
    - Testes relevantes P4/P7/P9/P10/P15/P16: rank 0–3, multi-alvo sem bônus duplicado, dano letal, reconfiguração repetida e mesma composição em ordens distintas.
    - Pipeline de dano prova aplicação única das pilhas por lote, incremento posterior e reset por HP real; manter emissão pública original.
    - _Requisitos: R2.8, R5.5, R5.6, R8.1, R9.4_
    - Evidência: MCP b902e05cf4e043aeae3771bf4cddeeec 5/5 PlayMode e 3b893147505348a5a347516f6febf5de 1/1 (Momentum ranks 0–3); regressões EditMode e retestes no audit, com última execução aprovada.

- [x] 10. Implementar Techniques de postura, entrada e echoes
  - [x] 10.1 Adaptar GuardBreaker, SeismicFist e RocketAdvance
    - GuardBreaker conta 3 básicos confirmados por execução, aplica postura no primeiro alvo confirmado da terceira e reseta por Q/W/E aceito; herdar raiz e energia de quebra.
    - SeismicFist escala somente Knockback autorizado, uma vez e dentro dos limites; não substitui Launch. RocketAdvance +70%/rank, teto 8 m, terreno válido e nenhum dano no trajeto.
    - _Requisitos: R1.5, R1.6, R2.3, R5.1–R5.5, R8.3, R8.4_
  - [x] 10.2 Adaptar FlurryEcho e AsuraEcho
    - Usar taxas/quantidades de D3, IDs filhos e último produtor direto realmente executado. Cancelamento antes dele não fabrica echo; depois, echoes agendados podem terminar, sem proteção/locks extras.
    - Descendentes não geram Asura nem novos produtores locais. Eventos/consumidores legados permanecem conforme audit; não suprimir HookBus.
    - _Requisitos: R2.5, R5.3, R5.8, R8.1, R8.3_
  - [x] 10.3 Validar Techniques no pipeline
    - PlayMode P2/P8/P11/P15: básico multi-alvo, GuardBreaker + SeismicFist sem deslocamento duplo, Boss imune, cancelamento antes/depois do produtor, dano/eventos de echoes e consumidor legado afetado.
    - Verificar ranks 0–3, limites físicos, parede e cleanup de filhos. Atualizar apenas contratos deliberadamente substituídos.
    - _Requisitos: R5.1–R5.8, R8.1–R8.4, R9.4_

- [x] 11. Implementar Mutations de Q/W/E
  - [x] 11.1 Avanço Demolidor
    - Transformar Q em travessia limitada e uppercut final; preservar fases/custo/cooldown, solicitar Launch somente na quebra e respeitar resistência.
    - Ignorar apenas bloqueio locomotor de corpos durante avanço; manter hurtboxes, HP recebido, terreno e ausência de invulnerabilidade. Restaurar colisões próprias em todo cleanup.
    - _Requisitos: R1.5, R6.1–R6.3, R6.5–R6.7_
  - [x] 11.2 Campo de Pressão
    - Criar uma zona após último impacto direto de W: 2 s mecânicos, quatro pulsos/0,5 s, 25% HP/postura do passo final. Cancelamento anterior não cria zona; após criação, persistir conforme lifecycle.
    - FlurryEcho não produz novas zonas; pulsos têm recibos próprios sem Asura/reprodução local, mantendo demais contratos/eventos.
    - _Requisitos: R6.1–R6.3, R6.5–R6.7, R7.6, R8.1_
  - [x] 11.3 Epicentro
    - Substituir geometria de E por radial centrada no jogador; preservar fases/custo/cooldown e HP/postura por alvo, fórmula de raio e autoridade da Fase 2.
    - Uma confirmação de skill por execução; até duas quebras remuneradas por raiz, sem apagar eventos de quebras excedentes.
    - _Requisitos: R2.3, R6.1–R6.3, R6.5, R6.6, R8.3_
  - [x] 11.4 Validar Mutations e combinações
    - PlayMode: Q atravessa corpo sem desligar hurtbox/HP recebido e para em sólido; uppercut Boss/Heavy, RocketAdvance/SeismicFist; W + FlurryEcho sem replicar zona; E multi-alvo/terceira quebra.
    - Testar cancelamento, filhos, eventos reais, pausa/hit-stop, morte/troca/disable e nenhuma escrita em asset. Inspecionar autoria/referências e save/reload via MCP.
    - _Requisitos: R1.5, R2.3, R6.4–R6.7, R7.6–R7.8, R9.4_

- [x] 12. Implementar Asura Cadenciada e disponibilidade da espera
  - [x] 12.1 Implementar sequência manual no controlador existente
    - Três golpes, um novo pressionamento R por golpe, distribuição 25%/25%/50%, tempos de D4 e débito/consumo/reserva únicos. Continuação roteada somente para execução vigente, sem segundo gate de nova ativação.
    - Buffer e janelas da Fase 1; segurar R não avança. AsuraEcho somente se terceiro produtor executado; proteger somente durante golpes, nunca durante espera/echoes.
    - _Requisitos: R1.2, R1.4, R3.2–R3.7, R6.1–R6.3, R6.5_
  - [x] 12.2 Aplicar transição coordenada na espera
    - Movimento preserva espera; básico/Q/W/E disponível e aceito encerra R antes da nova ação; destino rejeitado preserva espera sem custo. Validar viabilidade antes de cancelar, sem coexistência ofensiva.
    - Dash autorizado encerra; timeout/morte/desequipamento descartam continuação. R bloqueia energia enquanto aguarda; nova ação após encerramento gera normalmente.
    - Limite inclusivo `elapsed <= 0,8 s`: input válido ganha no tick exato; depois, timeout precede input e buffer não revive R. Continuação obsoleta não interrompe outra ação Committed.
    - _Requisitos: R1.4, R3.3–R3.7, R6.2, R7.6, R7.7_
  - [x] 12.3 Validar input, custos e concorrência reais
    - PlayMode P7/P13/P14: R → espera → cada básico/Q/W/E aceito/rejeitado; dash; três taps versus hold; fronteira antes/no/depois do timeout; input atrasado e continuação após outra ação Committed.
    - Verificar débito total único, perfil em startup/active/recovery, pausa/hit-stop, geração bloqueada na espera/liberada na nova ação e cancelamento antes do terceiro sem AsuraEcho fictício.
    - _Requisitos: R1.2, R1.4, R3.1–R3.7, R6.2, R9.3, R9.4_

- [x] 13. Checkpoint — conteúdo executável e contratos preservados
  - Dependências: 7–12. Conferir 10 Techniques/4 Mutations distintas, combinações previstas e resultados relevantes; nenhuma alias conta duas vezes.
  - Confirmar ausência de recursão local e geração indevida, preservação real de eventos/outros consumidores e cleanup; registrar evidência técnica via MCP no audit.
  - _Requisitos: R5.1–R5.8, R6.1–R6.7, R8.1, R9.2–R9.4_

- [x] 14. Integrar aquisição, compatibilidade e lifecycle da run
  - [x] 14.1 Adaptar catálogo/ofertas existentes
    - Oferecer 10 Techniques com ranks 1–3 e 4 Mutations de rank único por família/slot; tipo, antes/depois e incompatibilidade visíveis. Não ofertar rank extra de Mutation já selecionada.
    - Reutilizar OfferReward/RunRewardUI; sem rooms, raridade global ou Protocols. Aliases equivalentes não coexistem; dados ignorados não são apagados silenciosamente.
    - _Requisitos: R5.1, R5.5, R6.1, R6.4, R6.8, R7.4_
  - [x] 14.2 Migrar dados antigos com compatibilidade explícita
    - ShockRing → Epicentro preservando ID/rank/raio legado, sem segunda Technique; manter consumidores de loadouts ComboNova/Shockwave fora das novas ofertas.
    - Preservar IDs retirados, aliases locais de Q e referências; migração idempotente, origem preservada e diagnóstico de desconhecidos. Não alterar outras famílias/GUIDs/Resources.
    - _Requisitos: R5.7, R6.4, R8.2, R8.6_
  - [x] 14.3 Implementar reset/persistência e validar aquisição
    - Sala mantém energia/categoria; troca/morte/disable/reuso zeram e invalidam epoch. Troca desativa consumidores/filhos mas mantém aquisições; fim da run apaga ranks/seleções/contadores.
    - Testar ofertas/seleção real, ranks máximos, mesma aquisição em ordens diferentes, aquisição durante execução, reconfiguração sem handlers extras, reequipamento e replay depois de teardown.
    - Verificar serialização/migração/save/reload, referências e ausência de alterações runtime em assets via MCP.
    - _Requisitos: R5.5–R5.8, R6.5, R6.7, R7.7, R7.8, R8.6, R9.4_

- [x] 15. Integrar HUD, feedback e autoria final
  - [x] 15.1 Apresentar energia/disponibilidade/conteúdo
    - HUD carregando, cheio mas bloqueado (mana/cooldown/estado) e pronto somente com todos os gates; ocultar Asura na outra arma.
    - Feedback mostra ganho efetivo e origem sem novos eventos mecânicos/hit-stop; Mutations reconhecíveis e ofertas com mudança de comportamento/incompatibilidade.
    - _Requisitos: R7.1–R7.4_
  - [x] 15.2 Verificar apresentação e relógios
    - Com MCP, inspecionar componentes/referências, persistência de campos, pausa/hit-stop e runtime de alternância/quebra/reserva/Mutation, sem ganho por tempo cosmético.
    - Confirmar cancelamento/desequipamento não deixam HUD/VFX/modificadores e feedback mantém agrupamento da Fase 2.
    - _Requisitos: R7.1–R7.8, R8.3, R9.2_

- [x] 16. Checkpoint — validação técnica e revisão final
  - Dependências: 13–15. Mapear P1–P16 e critérios de requisitos à evidência registrada, sem repetir testes já suficientes salvo mudanças/falhas/risco concreto.
  - Executar regressões dirigidas de APIs/boons/Arco/Lança efetivamente afetados. Confirmar compilação/Console, dados, referências e runtime necessários com Unity MCP; registrar pendências, skips, falhas e limitações.
  - Revisar diff: alterações involuntárias, GUIDs, enums, Resources, .meta e serialização; sair de PlayMode preservando estado da cena.
  - Corrigir defeitos demonstrados dentro do escopo; não marcar validado enquanto checks Unity obrigatórios estiverem pendentes. Este checkpoint não substitui aceitação humana.
  - _Requisitos: R8.1–R8.6, R9.1–R9.4, R9.7_

- [x] 17. Gate manual — aceitação de gameplay da Fase 3
  - [x] 17.1 Avaliar kit base sem boons
    - Playtest humano de aproximadamente 10 min, sem boons/Protocols, contra grupo comum, Heavy/Elite/Boss. Avaliar contato, input, básico, papéis Q/W/E/R, limites físicos e ação real dos inimigos.
    - Registrar tempo mecânico/ações até primeiro e segundo R, fontes de energia e cenário resistente versus grupo; 15–30 s é hipótese de tuning, não aprovação automática.
    - Registrar diversão/clareza e necessidade demonstrada de ajustes mantendo decisão sem medidor novo; testes técnicos não satisfazem este gate.
    - _Requisitos: R1.1–R1.6, R4.1, R4.5, R9.5_
  - [x] 17.2 Avaliar builds distintos e ajustar apenas com evidência
    - Sem Protocols universais: pressão (HungryCombo/FlurryEcho/Campo de Pressão) e quebra/ciclo (GuardBreaker/SeismicFist/AsuraReserve/Epicentro); incluir observação de Q e Cadenciada em cenário representativo.
    - Registrar decisões distintas, reconhecimento de transformações, mana/cooldown, ritmo e cleanup, incluindo comum/Heavy/Elite/Boss. Testar independência de Protocols não implica desativar sinergias quando adquiridas.
    - Se tuning/código mudar, repetir apenas checks afetados antes de registrar aceitação. Pendência humana permanece desmarcada.
    - _Requisitos: R5.4, R6.3, R7.4, R9.1, R9.6, R9.7_

- [x] 18. Consolidar handoff e encerramento
  - Dependências: 16–17 e situação dos gates da Fase 2 registrada. Produzir `validation.md` desta spec com decisões/rationale, escopo, contratos adaptados, migrações, evidências de compilação/testes/runtime/aceitação humana separadas e próximas ações.
  - Atualizar status de requirements/design/tasks e audit sem apagar histórico. Não declarar fase plenamente validada se checks Unity ou aceitação humana estiverem pendentes; autorização de sobreposição não equivale a encerramento da Fase 2.
  - Manter propostas futuras fora da implementação entregue. Marcar etapas somente conforme efetivamente concluídas.
  - _Requisitos: R9.7; AGENTS.md — Completion and Handoff_

## Dependencies

| Tarefa | Depende de |
| --- | --- |
| 1 | Documentos atuais; baseline Unity na subtarefa 1.3 |
| 2 | 1 e gates da Fase 2 resolvidos ou sobreposição explicitamente autorizada |
| 3 | 2 |
| 4 | 3 |
| 5 | 3, 4 |
| 6 | 3–5 |
| 7 | 6 |
| 8 | 6 |
| 9 | 7, 8 |
| 10 | 8, 9 |
| 11 | 8, 10 |
| 12 | 7, 8, 10 |
| 13 | 7–12 |
| 14 | 13 |
| 15 | 13, 14 |
| 16 | 13–15 |
| 17 | 16 |
| 18 | 16, 17; dependências da Fase 2 explicitadas |

Preparar dados/documentos de uma tarefa futura não autoriza integrar seu comportamento antes das dependências. Alterações no mesmo pipeline/scheduler seguem incrementos coordenados para evitar autoridades concorrentes temporárias.

## Requirements Coverage Map

| Requisito | Tarefas principais |
| --- | --- |
| R1 — Kit base e pressão | 1, 5, 7, 11, 12, 17 |
| R2 — Geração/alternância/quebra | 1, 3–6, 9–11 |
| R3 — Ciclo R | 4, 7, 9, 10, 12, 17 |
| R4 — Momentum condicional | 1, 17; decisão negativa D2 satisfaz R4.1/R4.5, sem implementar R4.2–R4.4 |
| R5 — 10 Techniques | 1, 8–10, 13, 14, 17 |
| R6 — 4 Mutations | 3, 8, 11–14, 17 |
| R7 — Feedback/autoria/lifecycle | 3, 7, 11, 12, 14, 15 |
| R8 — Compatibilidade/integração | 1–6, 8–16 |
| R9 — Validação/aceitação | Checks incrementais 3–15, checkpoints 6/13/16, gates 17 e handoff 18 |

## Review Coverage Map

| Ponto do parecer de design | Contrato sincronizado / tarefas |
| --- | --- |
| A — Espera manual e desempate temporal | Design D4; 12.1–12.3 |
| B — Bloqueio local, eventos e consumidores | Design D1/P8/D7; 1.2, 5.2, 8.2, 10.2–10.3, 11.2/11.4 |
| C — Quebra válida versus recompensa | Requirements R2.3/glossário; 4.1/4.3, 5.2, 11.3/11.4 |
| Contexto encerrado/replay | Design D1/P12; 3.2–3.3, 14.3 |
| Ganhos atômicos antes de publicação | Design fluxo causal; 4.2–4.3, 5.2 |
| Echo com produtor real | Design D3/D4; 10.2–10.3, 12.1/12.3 |
| Travessia sem invulnerabilidade | Design D4; 11.1/11.4 |
| Auditoria de boons e snapshot/contadores | Design D3/D7/Data Models; 1.2, 8.1, 9.3 |

**Registro histórico — elaboração do plano:** preparação documental, sem implementação, Unity MCP, compilação, testes ou playtest naquele momento.

**Incremento de auditoria — 03/10/2026:** tarefa 1 concluída por inspeção estática e baseline Unity MCP atual, registrados em `contract-audit.md`. Instância/projeto confirmados, NexusLobby preservada fora de PlayMode, referências da arma/habilidades/HUD inspecionadas; Console com dois warnings preexistentes e nenhum erro retornado. Não houve mudança de scripts/assets, compilação do projeto, testes ou playtest. Checkpoint 2 iniciado e pendente de gates da Fase 2 resolvidos ou autorização explícita de sobreposição solicitada ao usuário. Tarefas de código e gates humanos permanecem desmarcados.

### Aceitação humana e encerramento — 04/10/2026

O usuário declarou **“aprovado. POde marcar como finalizado então”** após o checkpoint técnico 16. Sua declaração registra a aceitação da Fase 3, incluindo kit base e builds do gate 17, e autoriza o encerramento. Não foram fornecidas duração, cenas, tempos até R ou observações individuais do playtest; não são inferidos. Handoff em `validation.md`; tasks 17–18 concluídas e todos os itens finalizados. Este incremento é documental, sem tuning, código/assets ou novos testes. Evidências anteriores permanecem preservadas.