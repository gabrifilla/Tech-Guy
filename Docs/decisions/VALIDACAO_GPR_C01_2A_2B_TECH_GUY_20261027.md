# Tech Guy — Validação independente de GPR-C01.2a e GPR-C01.2b

**Data:** 27/10/2026  
**Fonte:** `Docs(2).zip` enviado pelo desenvolvedor, extraído para revisão documental e das evidências.  
**Escopo da revisão:** documentação, testes JSON, métricas, manifestos e capturas incluídos no ZIP. **Não foi feita inspeção do código-fonte do repositório Unity ativo nem novo teste no Unity pelo revisor.**

## 1. Veredito

- **GPR-C01.2a — CONCLUÍDA como protótipo técnico** e subsequentemente aprovada para integração, conforme documentos incluídos.
- **GPR-C01.2b — IMPLEMENTADA e validada tecnicamente no escopo registrado.** Não está aprovada em game feel físico nem representa a conclusão da migração de controles.
- **Próximo gate:** playtest humano real, seguido de **GPR-C01.2c** focada em contexto de input/UI, soltura de teclas, colisão diagonal/bordas e diagnóstico do teste intermitente. **Não liberar GPR-C01.3 (LMB/RMB/Asura) nem GPR-01 até haver aprovação específica.**

## 2. Resultados checados

| Entrega | Testes finais e evidência | Limitação |
|---|---|---|
| C01.2a — arena isolada | 10/10 PlayMode do protótipo + 26/26 regressão + 8/8 dash + 15/15 EditMode = **59/59** | Arena sintética, controles virtuais, não integração de personagem publicado |
| C01.2b — integração | 13/13 integração + 30/30 regressão + 8/8 dash + 15/15 EditMode = **66/66** | Filtros selecionados, não suíte global nem validação física |
| C01.2b — Nexus Game View | 3 PNGs 1160×600; CSV com 461 amostras de input virtual | Capturas estáticas não validam dinâmica, animação nem satisfação do jogador |
| C01.2b — velocidade | Mediana livre de **4,999993 m/s** no agente de produção de velocidade autorada 5 m/s; soltura 0 | A diagonal W+D da Nexus ficou bloqueada na borda; não prova diagonal livre nela |
| Integridade | 1.789 arquivos protegidos iniciais; 1.767 sem mudança; 22 mudanças intencionais; nenhum desaparecimento | Verificação de hashes circunscrita ao inventário; revisor não reconferiu diretamente o repositório ativo |

As primeiras execuções falhas **foram preservadas**:
- Integração inicial: 5/6, falha de foco do Editor em segundo plano; execução virtual passou a declarar foco e soltura explicitamente, com testes posteriores aprovados.
- Regressão inicial: 29/30. Falha em `MovementDuringRecoveryKeepsTheCast_UsesLatestValidOrderOnce`; passou isoladamente e na repetição de 30/30. **Causa não identificada**, risco de intermitência real; deve ser reexecutado sob carga, sem relaxar asserções/tolerâncias.

## 3. Principais acertos

1. Input WASD direto e relativo à câmera, cursor independente, diagonal normalizada e ausência de click-to-move durante modo direto.
2. `PlayerNavigationMotor` como escritor de deslocamento, com varredura de cápsula e limitação de NavMesh; `NavMeshAgent.Move` cru foi corretamente rejeitado após reproduzir travessia de parede sólida não incluída no bake.
3. Uso de deslocamento real para locomotion/Animator, dado que `agent.velocity` reportou zero mesmo com deslocamento próximo de 5 m/s.
4. Adaptação dos deslocamentos existentes (dash, avanço, recuo, Kiting Step, pull) e preservação documentada de eventos de contato, cancels, Asura, recursos e contratos de dano.
5. Migração mínima de skills **Q/E/R** para liberar a tecla W; chaves legadas preservadas e novo namespace de bindings versionado aplicado apenas quando o usuário salva.
6. Evidência de Unity real e capturas, não mockups, com distinção explícita entre dispositivos virtuais e entrada física.
7. Atividades reportadas separadamente, com progresso mestre, critérios de parada e exceção de escopo documentada por autorização direta posterior.

## 4. Pontos a ajustar antes de avançar

### P0 — Aceitação física e bordas/cantos

A Nexus demonstra que o vetor diagonal W+D bloqueado na borda levou a parada total, sem projeção tangencial. Essa sensação pode parecer travamento e comprometer um ARPG de posicionamento rápido. Fazer teste físico em Nexus e FirstSector: movimentos diagonais contra paredes, cantos de corredores, plataformas e ataques enquanto anda. **Se houver rigidez, corrigir localmente a colisão/sliding** sem ultrapassar NavMesh, sem afinar speed/dash ou trocar motor sem evidência. Testar com parede fina não baked, obstáculos próximos e recuperação de cantos.

### P1 — Falha intermitente de recuperação/buffer

Reexecutar `MovementDuringRecoveryKeepsTheCast_UsesLatestValidOrderOnce` isoladamente e em grupos sob carga, variando condições de foco/frames, sem alterar expectativas para fabricar PASS. Caso reproduzida, rastrear causalidade do estado e corrigir no ponto mínimo de integração.

### P1 — Donos de inputs e interação

Hoje os dados indicam Q/E/R para três skills legadas e especial F11 preservado; **R pode também atuar como interação contextual na Nexus** pelo antigo binding Skill3, enquanto RMB ainda usa comportamento antigo de interação. Isso é dívida transitória com risco de UX e conflito: GPR-C01.2c deve garantir que UI/contextos não executem skills, movimento ou interações indevidas e documentar um encaminhamento de ação Interact separada, **sem inventar tecla definitiva**. F permanece finalização contextual e X ainda não possui quarta skill.

### P1 — Cobertura parcial de cenas e rig

Flag de controle foi aplicada a `PrologueTutorial`, `NexusLobby`, `FirstSector` e `NexusBlockout`, mas o smoke visual novo concentra-se na NexusBlockout. **NexusBlockout é uma variante experimental**, não aprovação do design desse cenário. Testar depois FirstSector com inimigos, Arco e Lança, incluindo cast móvel, kiting, dash e interrupções. Screenshots estáticos não provam ausência de jitter, correto blending nem precisão de facing durante golpes.

### P1 — Teclas customizadas, hold e Settings

LMB ainda é legado e **não corresponde** ao modelo final de um ataque por clique; RMB **ainda não ativa Asura**; Q/E/R são três slots legados, X não está preenchido; WASD não é remapeável ainda. Não comunicar migração completa. Evitar rebind duplicado ou perda de customizações F11. A mudança LMB/RMB pertence à C01.3 e deve manter único executor da Asura.

### P2 — QA/performance/integridade

Sem cenário crowd, objetos móveis/OffMeshLinks ou benchmark de build. Checks de GUID focados passaram, mas o checker Python global de metadados falhou por volumes C:/D: e não pode ser proclamado válido. Preservar histórico e novos arquivos `.meta`; não limpar alterações humanas do Git.

## 5. Playtest humano recomendado antes de C01.2c

**Duração sugerida: 8–12 minutos, com entrada física real, sem overlay/test automation.** No jogo aberto:

1. Na NexusBlockout ou NexusLobby, pressionar e soltar W/A/S/D, diagonais e inversão de sentido. Notar resposta/parada.
2. Aproximar-se de paredes, cantos, bordas e obstáculos mantendo W+D; verificar travamento versus deslizamento natural.
3. Andar em uma direção enquanto mira para outra. Confirmar se a rotação e movimento são naturais.
4. Usar dash em movimento e parado; confirmar direção cursor, colisão e tempo/feel.
5. Entrar em FirstSector. Atacar com básico e Q/E/R enquanto anda, recua e muda de direção. Checar enraizamento intencional e animação.
6. Testar Arco/Lança se disponíveis na mesma sessão, com atenção a carga, cast em movimento e aproximação.
7. Abrir/fechar pausa, menus e interações com teclas pressionadas. Verificar ausência de movimento ou cast fantasma após fechar.
8. Anotar exatamente onde faltou resposta, houve travamento, sliding estranho, jitter, input perdido ou câmera errada. Capturas/vídeo com tempo são valiosos, mas não obrigatórios para reportar sensação humana.

O autor decide APROVAR/REFAZER/PENDENTE para movimento e dash separadamente. A sensação subjetiva é evidência legítima de playtest, mas não pode ser falsificada pelo agente.

## 6. Escopo autorizado recomendado para GPR-C01.2c, após playtest

- Primeiro registrar o feedback físico, não assumir que o motor foi aprovado por testes automatizados.
- Fechar gates de contextos (pausa, reward, resultados, foco, disable, perda/retorno de foco, input held e soltura).
- Investigar/corrigir travamento diagonal apenas se o cenário reproduzir feel ruim ou violar contrato esperado; medir antes/depois.
- Revalidar buffer/cast com reexecuções repetidas e informar frequência da falha, sem descarte silencioso.
- Confirmar bindings Q/E/R e proteção contra comandos acionados por UI/Interact, preservando saves customizados.
- Manter integração existente; alterações locais, reversíveis e com regressões GI focadas.
- Entregar relatório `.md` C01.2c + atualização ledger/matriz + evidências e condição de STOP. Não implementar Asura RMB, LMB click-per-hit, X, full Settings nem GPR-01 sem outra autorização.

## 7. Fontes inspecionadas dentro do ZIP

- `Docs/decisions/GPR-C01.2a_RELATORIO.md`
- `Docs/decisions/GPR-C01.2b_RELATORIO.md`
- `Docs/decisions/GAMEPLAY_REWORK_PROGRESS.md`
- `Docs/decisions/ADR-0006-GPR-DIRECAO-E-GATES.md`
- `Docs/gameplay/GPR_TASKS.md`, `GPR_REQUIREMENTS.md`, `GPR_CONTROL_MIGRATION.md`, `GPR_TECHNICAL_DESIGN.md`, `GPR_VALIDATION_PLAN.md`
- `Docs/validation/artifacts/gameplay-rework/c01-2a/` (JSON/CSV/PNG)
- `Docs/validation/artifacts/gameplay-rework/c01-2b/` (JSON/CSV/PNG, integridade, status, logs)

**Conclusão:** evidência suficiente para aceitar a execução técnica documentada de C01.2b; evidência insuficiente para aprovar definitivamente feel, comportamento de paredes/cantos, controle contextual e migração completa de mouse/skills. **Próxima etapa: playtest físico → GPR-C01.2c delimitada → revisão de gate para GPR-C01.3.**
