# Prompt — Tech Guy · Fase 1b "Fragmentos, primeiro NPC e terminal do Nexus"

## Contexto e objetivo
A Fase 1a deu ao Universo 1 um fim claro e criou o `MetaProgress.v1` (aprovados em playtest). A 1b entrega a **primeira razão concreta para jogar a próxima run**:
1. **Fragmentos de Dados**: moeda persistente ganha em toda run e mantida mesmo na morte.
2. **O primeiro NPC resgatável**, encontrado no Universo 1. Ao ser resgatado, ele passa a morar no Nexus e **abre o terminal de melhorias**.
3. **Terminal do Nexus** com 6 melhorias compráveis com Fragmentos.
4. A **tela de fim de run** mostra o progresso: Fragmentos ganhos, saldo e quanto falta para a próxima melhoria.

Ao final, a run curta deve ter consequência: morrer ainda rende algo e há sempre uma próxima meta visível.

## Leitura obrigatória antes de agir
1. `CLAUDE.md`, `Docs/plano/TECH_GUY_PLANO_RESET_DE_LOOP.md` (seções 4 e 7) e `Docs/STATUS.md`.
2. Skills em `.claude/skills/`: `tech-guy-game-vision`, `tech-guy-boon-design` (por causa das melhorias que mexem em ofertas), `tech-guy-unity-acceptance` e, se tocar no HUD ou feedback de combate, `tech-guy-combat-unity`.
3. `Docs/systems/05_RUN_E_PROGRESSAO.md` e `Docs/systems/06_HUD_E_UI.md`.
4. Código real de: `MetaProgressStore`, `CurrencyWallet`, `CoinPickup`/`CoinDrop`, `Actor` (drop), `WeaponLoadout`/`LobbyArsenal`, `LobbyInteraction` (estações do Nexus), `RunResultsView`/`RunResultsModel`, o fluxo de ofertas de boon (`RunBoonOfferCatalog`, `BoonOfferDraw` ou equivalentes reais), `ProgressionDirector` (Manual, Lifecycle, Statistics) e o modal uGUI/TMP de `RunResultsFocus`, que é a base para o painel de diálogo.

## Etapa 0 — Auditoria curta e plano (parar para aprovação)
Antes de implementar, responda em no máximo meia página:
- Como as moedas são geradas hoje (taxas e valores por inimigo, elite e boss) e quanto uma run típica do Universo 1 renderia.
- **Tabela de economia proposta**: ganho por fonte e custo de cada melhoria. Alvos: a **primeira compra após 1–2 runs** (morrendo ou vencendo) e **todas as 6 em ~10–15 runs**.
- Viabilidade (P/M/G) de cada uma das 6 melhorias abaixo. Se alguma for G, proponha a troca por outra P/M com o mesmo espírito.
- Onde o NPC aparece na run (qual sala ou momento) e como garantir que ele seja encontrado na 1ª ou na 2ª run.

**Pare e aguarde aprovação da tabela de economia e das melhorias antes da Etapa 1.**

## Etapa 1 — Fragmentos de Dados
- Os Fragmentos substituem as moedas atuais no papel de moeda meta. Reaproveite o drop existente (`CoinDrop`/`CoinPickup`), com visual e texto renomeados para "Fragmentos", e acrescente um bônus por vencer o boss, conforme a tabela aprovada.
- Saldo e histórico ficam no `MetaProgress` (migrar para **v2** com leitura automática do v1, e testes de migração).
- **O que foi coletado na run fica com o jogador também na morte e no abandono.**
- O saldo antigo de `Currency.Coins` (provavelmente inflado por testes) **não é migrado**: os Fragmentos começam em 0. A chave antiga fica sem uso e é documentada como legada.
- HUD: contador discreto de Fragmentos da run, sem competir com vida e recurso.

## Etapa 2 — Desbloqueio das armas
- Lança e Arco estão **pausados** no plano e serão liberados pela progressão (vencer universos) na Fase 1d. Nesta fase, os pedestais delas ficam **bloqueados**, com o texto "Liberada ao avançar nos universos". Não há mais compra com moedas.
- A Manopla continua sempre disponível. Não mexer no comportamento das armas.

## Etapa 3 — Primeiro NPC resgatável (placeholder de identidade)
- **Identidade, nome, lore e falas finais serão definidos pelo desenvolvedor.** Use o nome provisório **"Desconhecido"**, com falas curtas marcadas `TODO-TEXTO`. Não invente lore.
- **Na run:** o NPC aparece preso (cápsula, gaiola de dados ou equivalente simples, usando assets existentes) no momento definido na Etapa 0. Interagir com a tecla de interação atual o liberta: diálogo curto (2–3 falas), depois ele some. O resgate é **salvo na hora**, mesmo que o jogador morra depois.
- **No Nexus:** após o resgate, o NPC aparece ao lado do terminal. Interagir abre o diálogo e depois o terminal. Antes do resgate, o terminal não existe ou está apagado.
- **Diálogo:** crie um painel simples e reutilizável (nome, texto, avançar/fechar, bloqueio de input como no `RunResultsFocus`), já pensando em armas e NPCs futuros. Sem retrato, voz ou animação de texto elaborada.

## Etapa 4 — Terminal do Nexus (6 melhorias)
Lista inicial, sujeita aos ajustes de viabilidade aprovados na Etapa 0:
1. **Reroll**: 1 reroll de oferta de boon por run.
2. **Oferta inicial**: começar a run com uma escolha de boon.
3. **Quarta opção**: a primeira oferta de cada run mostra 4 opções em vez de 3.
4. **Segunda chance**: 1 revive por run, com parte da vida.
5. **Sala limpa**: bônus de Fragmentos por sala vencida sem levar dano.
6. **Vida reforçada**: + vida máxima (a única melhoria numérica).

Regras:
- Cada melhoria é comprada **uma vez** (sem ranks nesta fase) e fica ativa em todas as runs.
- Os efeitos entram pelos pontos de extensão reais das ofertas e da run. **Não alterar** IDs de boon, o catálogo nem a lógica do C05 além do necessário para reroll, quarta opção e oferta inicial. Preservar a determinação por seed.
- UI do terminal: lista com nome, descrição de uma linha, custo, estado (comprada / disponível / sem saldo) e saldo atual, no estilo do HUD atual.

## Etapa 5 — Fim de run com progresso
Na tela de resultado, adicionar uma seção com: Fragmentos ganhos nesta run, saldo total, **a próxima melhoria mais barata e quanto falta** e, se aconteceu, "Novo aliado resgatado". O resto da tela continua igual.

## Restrições
- Não alterar combate, boons existentes, seleção e rotação de salas (09/09B) nem controles.
- Lança, Arco e G1/G2/G3 continuam pausados.
- Testes não gravam em `Docs/` nem no perfil real (usar o `SetUpFixture` da 1a).
- Rodar a EditMode completa e as suítes focais. A PlayMode completa não é necessária.
- Sem commit antes do meu playtest. Sem push.

## Critérios de aceite
- [ ] Etapa 0 aprovada antes de qualquer implementação.
- [ ] Fragmentos ganhos e mantidos em vitória, morte e abandono; `MetaProgress` v2 migra o v1 (testado).
- [ ] Pedestais de Lança e Arco bloqueados, com a mensagem; a Manopla funciona.
- [ ] NPC encontrável na 1ª ou 2ª run; resgate salvo na hora; aparece no Nexus e abre o terminal.
- [ ] As 6 melhorias (ou as substitutas aprovadas) compráveis, persistentes e com efeito verificável em jogo.
- [ ] A tela de resultado mostra ganhos, saldo e a próxima meta.
- [ ] EditMode verde; nenhuma falha nova nas suítes focais; `git status` sem artefatos.

## Relatório final (uma página, PT-BR)
1. Feito / Não feito / Não testado.
2. Tabela de economia final.
3. Arquivos e classes alterados (caminhos reais).
4. **O que jogar para validar**: um perfil zerado (como zerar com segurança), o resgate do NPC, a primeira compra, uma morte para conferir que os Fragmentos ficam, e uma run com cada melhoria visível.
5. Proposta de commits.

**STOP** ao final e aguarde meu playtest.
