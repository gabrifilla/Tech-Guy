# Validação e handoff — Fase 3, Manopla 2.0

## Estado

Tasks 1–16 concluídas tecnicamente com checks dirigidos via Unity MCP. Em 04/10/2026, após receber o resultado do checkpoint, o usuário declarou **“aprovado. POde marcar como finalizado então”**. Essa declaração registra a aceitação humana da task 17. Com este handoff e os documentos atualizados, a task 18 está concluída e a Fase 3 está encerrada. A Fase 2 já havia sido aceita expressamente em 03/10/2026.

Não foram fornecidos duração/cena do playtest, medições até primeiro/segundo R ou observações individuais dos builds. Não são inferidos nem apresentados como medições verificadas. Aprovação humana, testes automatizados e observação técnica runtime são evidências distintas.

## Decisões e escopo

- Política única de Asura com capacidade 100: básico confirmado 3, Q/W/E confirmado 10, alternância +15 e quebra +20, limitada a duas recompensas por raiz. HP/postura independentes, ganhos consolidados antes das notificações públicas, sem energia de descendentes ou durante R.
- R exige recurso completo e disponibilidade real; consome energia/mana e aplica reserva uma vez. Rejeição preserva a ação e os recursos; cancelamento não reembolsa. Asura Cadenciada usa três pressionamentos, divisão 25%/25%/50%, espera inclusiva e transição coordenada para outra ação disponível.
- Dez Techniques e quatro Mutations, com composição determinística e snapshots imutáveis. Avanço Demolidor limita travessia e uppercut; Campo de Pressão cria quatro pulsos; Epicentro altera a geometria; Cadenciada mantém uma execução manual. Echoes exigem produtor direto realmente executado.
- Sem medidor nativo adicional de momentum, conforme D2. Mana, cooldowns, fundação de ações e autoridade da Fase 2 preservados. Kit base sem RunBoons e conteúdo sem Protocols universais exercitados tecnicamente.
- Aquisição reutiliza ofertas/UI existentes, ranks máximos e exclusividade por slot. HUD compartilhado mostra disponibilidade, ganho efetivo/origem, reserva e nomes de Mutations, sem nova emissão mecânica.
- Overrides de autoria têm semântica explícita em Data Models do design; flags sem efeito são rejeitadas. Valores/perfis são copiados na aceitação, sem editar assets fonte. Relógios mecânicos respeitam pausa/hit-stop; feedback cosmético não concede recurso.

## Compatibilidade e migração

ShockRing conserva ID/rank/raio e seleciona Epicentro sem oferta duplicada. ComboNova/Shockwave e consumidores de loadouts legados permanecem fora das novas ofertas. Q Arsenal permanece até seleção explícita da Mutation. Adaptadores de importação são idempotentes pelo maior rank válido e preservam registros originais/desconhecidos com diagnóstico; não foi criado um sistema global de save.

Foco tinha desconto aplicado novamente sobre ManaCost efetivo da Manopla. O defeito demonstrado foi corrigido escalando custo autorado do clone; regressão real de Foco nas três famílias passou. Nenhum rework de Arco/Lança, raridade global, Protocol System, mapas ou meta progressão foi incluído. GUIDs, enums e chaves Resources existentes preservados neste incremento.

## Evidências técnicas

Editor confirmado: Tech-Guy@fccafb2b47cd7671, Unity 6000.5.10f1. O histórico completo, falhas corrigidas, retries e cobertura P1–P16/requisitos estão em [contract-audit.md](contract-audit.md). Não foi executada a suíte completa no checkpoint final.

| Execução Unity MCP | Resultado e escopo |
| --- | --- |
| 0e6195c719e34d828509e4be403739cc | 3/3 PlayMode: ofertas/Mutations/Q legado, reuso de run e Foco nas três famílias |
| 013927cdee384abdb6f7853f8f0afd3b | 1/1 PlayMode: sala/recompensa mantém energia/categoria, reequipamento reseta recurso e conserva aquisição |
| 91d86abcdfd54f82819c6c9d398a8670 | 1/1 PlayMode: importação durante R conserva reserva capturada |
| 51d293c5dbad445e8561ed3394287f02 | 3/3 PlayMode: HUD real e fronteiras/buffer Cadenciado |
| 078a3ac642ba4aadaf470e5ed2fadd43 | 3/3 EditMode: autoria, snapshot e save/reload de referência de perfil |
| abb5e70e25444d21a2cfd6e13c0aeb3d | 4/4 PlayMode: Cadenciada customizada, dois HUDs e Hungry/Fist |
| 54e7dc82200341a387de44d75eb7cfee | 31/31 EditMode: snapshots, autoria/contextos e formatos públicos legados; zero falhas/skips |

Demais evidências incrementais cobrem HP/postura, multi-alvo/letal, atomicidade/reentrância, ranks/permutação, cancelamento/buffer, limites físicos/resistência, produtor/filhos/eventos, clocks e cleanup. Não se afirma uma única execução integral de todos esses testes após a última alteração. As regressões foram escolhidas pelas dependências afetadas.

MCP inspecionou prefab HUD com cinco slots e referências obrigatórias válidas; arma fonte e quatro abilities válidas, sem escrita runtime nos assets. NexusLobby: um jogador, um HUD vinculado, zero HUD legado; validação de cena sem missing scripts/broken prefabs/issues. Cena preservada não dirty e fora de PlayMode. Persistência de autoria/perfil verificada por salvar/recarregar assets reais no teste, com teardown dos temporários.

Compilação/Console inspecionados sem limpar. Incidentes de runner/domain reload e warnings FSR/transporte estão no audit; retries relevantes aprovados. Diff e diff --check revisados. Alterações anteriores do usuário e cenas temporárias preexistentes preservadas. O encerramento atual é documental: nenhum novo teste ou playtest foi executado pelo agente neste turno.

## Próximas ações

Nenhuma task permanece aberta nesta spec. Qualquer ajuste futuro de tuning deve partir de observação concreta e repetir apenas checks afetados. Trabalho de fases futuras deve seguir sua própria documentação e autorização, sem expansão automática do escopo entregue.
