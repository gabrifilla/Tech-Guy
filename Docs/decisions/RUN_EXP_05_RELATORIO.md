# RUN-EXP-05 — entrega da fatia visual

02/11/2026. A frente foi concluída tecnicamente até05.4 e parou. A revisão física continua aberta: **C05 não foi publicada permanentemente**, modelos/ambiente não foram declarados arte final e nenhum gate humano foi concedido por testes.

## O que está disponível

- **05.0 DONE:** auditoria atual e direção de trabalho Arquivo estratificado, guia visual de materiais/valores e conceito identificado como referência.
- **05.1 DONE técnico:** C05 opcional com ilustrações originais, contexto real/rank, foco/detalhes/confirmação e movimento reduzido. Comparação original/C04/C05 nas três armas/resoluções, mouse/teclado/gamepad e mostruário somente leitura. Aprovação/publicação pendentes.
- **05.2 DONE no recorteDCC:** Fragmento remodelado, Emissor com passe secundário coerente, fontes.blend/FBX/UVatlas/normais e cópias05 naFirstSector. Mesmos pivôs/cápsulas/AI/contatos/IK. NOT DONE: rig/clips/pintura final de produção; não prometer o nível de detalhe do conceito.
- **05.3 DONE técnico:** cartuchos de cobertura e infraestrutura de arquivo, piso quieto e coerência limitada na sala aberta. Física/nav/duas rotas/LOS/spawns permanecem; comparação04/05 disponível.
- **05.4 DONE com limites:** UTF, build release executado e8/32/64 ativos emEditor/standalone, referências/arquivos/preferências verificados nos escopos documentados. Outros hardwares e aprovação de orçamento não foram testados.

| Seleção | Job | Resultado | Duração |
|---|---|---|---|
|FirstSector e recompensas, incluindo matriz12cenários|`d1652afc48ef4b01a31f361beb9de68c`|21/21, falhas0, skipped0|263.759s|
|Regressões selecionadas de combate/input/boons|`20501c947d8d45e1b6f073462f80ccff`|33/33, falhas0, skipped0|44.593s|
|HP/atribuição e efeitos nativos de habilidades|`35ae7fabc39844c3b8edf2cb77f6b0a4`|10/10, falhas0, skipped0|11.346s|
|Conteúdo, geometria, encontros e arte|`391fbbeca474439bb40e58a44a36a99c`|29/29, falhas0, skipped0|1.739s|

São seleções específicas; reexecuções e jobs sobrepostos não somam testes únicos. Falhas intermediárias, correções de fixture, shader inicial ciano e build inicialmente recusado emLibrary estão registrados, sem enfraquecer asserções ou retunar o jogo.

## Revisão pronta

[Abrir comparação visual portátil](../validation/artifacts/run-exp-05/review/index.html) · [BaixarZIP com todas as imagens](../validation/artifacts/run-exp-05/RUN_EXP_05_REVIEW.zip).

O pacote contém guia de estilo, original/C04/C05, três armas, contexto/rank, Fragmento/Emissor antes/depois e fases, cobertura/aberta, movimento/LOS/skills e medições. Capturas são do jogo real; conceito gerado é rotulado separadamente. Instruções reproduzemFirstSector seed63/0 e a préviaC05 reversível. C05 e arte precisam de **PENDING DEVELOPER APPROVAL** após essa revisão.

| Ativos | Quadros04/05 | Quadro p95 04→05 ms | GPU p95 04→05 ms | Memória alocada05 p95 MiB |
|---|---|---|---|---|
|8|2844/2876|2.07→1.85|0.81→0.91|265.89|
|32|1718/1698|3.04→3.00|0.89→0.92|271.33|
|64|837/963|6.49→5.35|2.44→2.12|276.23|

Standalone1920×1080, Ryzen55600X/RTX3060Ti, releaseMono. Esses pares mantêm a população, masAI/posições/fases evoluem; não provam otimização. Geometria rígida dos rosters aumentou cerca de5×; materiais compartilhados3→2 e renderers dos candidatos18/7 preservados. GPU/CPU/memória e métricas indisponíveis estão discriminados no05.4, sem orçamento universal.

Integridade: 2050/2058 hashes iguais,8 mudanças previstas,0 removidos;32prefs existentes idênticas. 15 adições no registroPlayer original anteriores ao build foram preservadas, sem origem atribuída; não há garantia de registro inteiro ou todo disco inalterado. Branch/HEAD e alterações02–04 mantidos. Console final0erros; checker retém um erro/umaviso históricos fora do escopo.


## Documentação e próximo pedido

[05.0 Direção](RUN_EXP_05_0_ART_DIRECTION.md) · [05.1 Recompensas](RUN_EXP_05_1_REWARD_UI.md) · [05.2 Inimigos](RUN_EXP_05_2_ENEMY_ART.md) · [05.3 Ambiente](RUN_EXP_05_3_ENVIRONMENT.md) · [05.4 Jobs, custo e integridade](RUN_EXP_05_4_VALIDATION.md).

Masterplan, design técnico, direção/pipeline de arte, UI, matriz, ledger, estado, requisitos/tarefas/validação foram atualizados preservando históricos. O pedido adicional do desenvolvedor de02/11 ficou previsto no [plano de mapas híbridos2.5D](MAP_ART_2_5D_FUTURE_PLAN.md): aprovar imagens primeiro, aplicar emplanos/volumes simples, manter física independente e medir um pequeno protótipo antes de adotar. **Só planejamento**, sem renderer ou conversão agora.

Editor finalNexusBlockout limpa, fora dePlay, Time1 e sem assistência/seed/preview/scope moeda. FontesDCC salvas separadamente preservam aScene originalBlender. Semcommit, push, desligamento ou início06 nesta entrega. Nenhum novo boon/boss/arquétipo/economia/Singularity/QERmodular.
