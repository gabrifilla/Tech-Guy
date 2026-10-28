# GPR-C01 — resumo consolidado da fase

28/10/2026 · **COMPLETA — implementação técnica e aceite humano dos controles recebidos.**

## Entrega

WASD direto relativo à câmera, mira independente, LMB por clique/combos (exceção ChargedShot preservada), especial permanente RMB/Asura única, exatamente três skills modulares Q/E/R. Dash Cursor ou Movimento; sem direção pressionada no modo Movimento, segue o olhar. InteractG e finisherF separados e remapeáveis; configuração/persistência, HUD e barreiras de soltura/UI/foco integrados. Sem X modular ou alias automático F11.

Uma autoridade de movimento, holder/executores/scheduler/cancel/dano existentes. Arrays e identidades históricas Q/W/E/R continuam compatibilidade interna: índices0/1/2 são Q/E/R físicas; index3 é o especialRMB, não uma quarta modular.

| Atividade | Fechamento | Fonte |
|---|---|---|
| C01.1a/b | Baseline e desenho de migração; decisões históricas depois revisadas | Relatórios individuais C01.1 |
| C01.2a/b/c | Experimento, integração de motor, sliding/contextos/release | Relatórios individuais C01.2 |
| C01.3a/b | Básico direcional, RMB no owner Asura, correções de fixture e primeiro comando após soltura | Relatórios individuais C01.3 |
| C01.4a/b/c/d | Rebinding, dash, interação e layout final implementados | [Consolidado4](GPR-C01.4_RELATORIO.md) |
| C01.5a/b | Evidência revisada e aceite físico explícito registrado; sem reteste desnecessário | [Fechamento5](GPR-C01.5_RELATORIO.md) |

## Evidência e limites

116 casos distintos aprovados nos filtros finais de4; contagens históricas anteriores não são somadas. O Nexus com salas foi restaurado posteriormente, com29 casos próprios e smoke real via MCP, sem reapresentar os testes como suíte global.

**Humano aprovado:** movimento, mira, modos de dash, básicos/combos, RMB/Asura, Q/E/R, fluidez, configuração e interação experimentadas. **Automação:** contratos e contextos cobertos pelos requests/outputs existentes. **Não aferido integralmente:** standalone/restart, dispositivos/gamepad, crowd/obstáculos móveis, catálogo/arte/encounters completos. Nenhuma aprovação desses tópicos inferida.

Moonwalk direcional e básicos percebidos rápidos são refinamentos futuros não bloqueadores. Sem ajuste de animação, cadência, recovery, velocidade ou impacto. Incidente histórico TTL/hitch permanece delimitado; Tripo3D/keys/Playground têm pendências próprias. Não houve nova regressão significativa demonstrada que impeça encerrar a migração.

## Arquivos, skills e transição

Este resumo, relatório5 e documentos canônicos/ledger/matriz foram atualizados; nenhum runtime/asset/config/save alterado neste fechamento. Skills: visão, combate, boons e aceitação Unity locais. Evidências e inventário no [relatório5](GPR-C01.5_RELATORIO.md) e [P1](GPR-01.P1_RELATORIO.md).

GPR-01.P1 planejamento concluído; [proposta](../gameplay/GPR_01_VERTICAL_SLICE.md) aguarda revisão. Próxima implementação exige aprovação explícita dessa fatia. Não repetir o aceite completo de C01 sem regressão nova ou condição crítica não coberta.

