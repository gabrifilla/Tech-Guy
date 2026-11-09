# Prompt — Tech Guy · Fase 0 "Arrumar a casa" + auditoria de progressão

## Contexto e objetivo
O Tech Guy (Unity 6, URP, C#, repositório `D:/Dev/Personal/My_games/Tech-Guy`, branch `gameplayOverhaul`) vai mudar de foco. O combate já é divertido. Os problemas agora são: **runs repetitivas, ausência de meta-progressão e falta de identidade visual**. A próxima frente será a meta-progressão (universos sequenciais, rapport com as armas, moeda persistente no Nexus).

Esta etapa **não implementa gameplay**. Objetivos:
1. Deixar o repositório num estado limpo e seguro.
2. Simplificar o processo de documentação.
3. Auditar o que **já existe** e pode ser reaproveitado para a meta-progressão.

## Leitura obrigatória antes de agir
1. O `CLAUDE.md` (já carregado) e o plano `Docs/plano/TECH_GUY_PLANO_RESET_DE_LOOP.md`.
2. As skills do repositório em `.claude/skills/` (`tech-guy-game-vision`, `tech-guy-combat-unity`, `tech-guy-boon-design`, `tech-guy-unity-acceptance`): ler cada `SKILL.md` e a pasta `references/`, e aplicar o que for pertinente.
3. `AGENTS.md`, `README.md`, `Docs/README.md`, `Docs/00_VISAO.md`, `Docs/01_PILARES.md`.
4. `Docs/systems/05_RUN_E_PROGRESSAO.md` e `Docs/systems/06_HUD_E_UI.md`.
5. `Docs/decisions/RUN_EXP_10_2B_RELATORIO.md` e `Docs/decisions/RUN_EXP_10_2B_7_REGRESSION_TRIAGE.md`.

## Etapa 1 — Estado do Git (somente leitura, depois perguntar)
- Rodar `git status`, `git log --oneline -15`, `git stash list` e `git diff --stat`.
- Listar o que está **não commitado** (esperado: trabalho RUN-EXP-10.x de 05–06/11). Agrupar por tema: código, testes, docs, assets, metas.
- **Não commitar ainda.** Propor uma divisão em commits (ex.: protótipos G1/G2/G3; ferramentas de lab; docs) e **parar para eu aprovar**.

## Etapa 2 — Quarentena dos testes que falham
- Rodar a suíte ampla PlayMode + EditMode pelo Unity MCP (ou pela ferramenta disponível) e listar **pelo nome** as falhas atuais.
- Propor como isolá-las usando o mecanismo **que o Unity Test Framework do projeto já suporta** (ex.: categoria de quarentena). Verificar o que existe antes de propor; não inventar atributo ou API.
- Não corrigir os testes e não alterar gameplay para fazê-los passar. Objetivo: uma suíte padrão verde e uma lista de quarentena explícita, com motivo de uma linha por teste.
- Aplicar a quarentena **só após minha aprovação** da lista.

## Etapa 3 — Auditoria para meta-progressão (somente leitura)
Inspecionar código, cenas, prefabs e ScriptableObjects reais e responder, com caminho de arquivo e classe reais:

| Tema | Pergunta |
|---|---|
| Save/persistência | Onde e como o progresso é salvo (PlayerPrefs, arquivo, SO)? O que já persiste entre runs? Existe migração/versão de save? |
| Moeda/economia | Existe alguma moeda persistente ou de run? Onde é concedida e gasta? |
| Desbloqueios | Existe sistema de unlock (armas, boons, salas)? Como o pool de boons decide o que está disponível? É possível adicionar/remover boons do pool por progresso sem tocar no catálogo? |
| Nexus | Quais interações existem hoje (pedestais de arma, terminais, NPCs)? O que é cena, o que é prefab, o que é dado? |
| Armas | Como uma arma é desbloqueada/selecionada (`WeaponLoadout` ou equivalente real)? Existe algo de personalidade/diálogo das armas? |
| Fim de run | Como a run termina hoje (vitória/boss, morte, abandono)? O boss do FirstSector existe e é vencível no fluxo normal? O que a tela de resultado mostra? O que `RunSummaryStore` (ou equivalente real) registra? |
| Fases/universos | O fluxo suporta encadear mais de um setor/cena numa run? Quanto do grafo de rota está preso ao FirstSector? |
| Diálogo/UI | Existe sistema de diálogo, caixa de texto ou UI de NPC reaproveitável? |

Para cada tema, classificar: **EXISTE e serve / EXISTE e precisa adaptar / NÃO EXISTE**, com uma estimativa de esforço (P/M/G) para chegar ao MVP descrito abaixo.

**MVP de referência (não implementar, só avaliar a viabilidade):** moeda persistente "Fragmentos de Dados" ganha em toda run; terminal no Nexus com 6–8 desbloqueios; rapport por arma em 5 níveis (nível 5 libera um artefato que transforma a arma); vencer o boss do Universo 1 libera o Universo 2 e a Lança; tela de fim de run mostrando o progresso ganho.

## Etapa 4 — Simplificação documental
- Propor (não executar) um único arquivo `Docs/STATUS.md` de no máximo 1 página que substitua a leitura de `02_ESTADO_E_LACUNAS.md` e `03_PRIORIDADES.md` como ponto de entrada. Os arquivos antigos ficam como histórico.
- Daqui em diante: **um relatório de uma página por etapa**, sem subdocumentos por subetapa.

## Restrições
- Não alterar gameplay, combate, boons, salas, C05, controles, saves ou PlayerPrefs.
- Não commitar, não fazer push e não apagar arquivos sem minha aprovação explícita.
- Não inventar classes, caminhos, APIs ou resultados. Se algo não foi verificado, escrever "não verificado".
- Lança, Arco, arte 2.5D e prólogo estão **pausados**: não auditar além do necessário para a tabela.

## Critérios de aceite
- Lista de mudanças não commitadas, agrupada, com proposta de commits.
- Lista nominal das falhas atuais e proposta de quarentena com o mecanismo real do projeto.
- Tabela da Etapa 3 completa, com caminhos reais e classificação P/M/G.
- Rascunho do `Docs/STATUS.md` proposto.

## Relatório final (uma página, PT-BR)
1. **Feito / Não feito / Não testado / Aguardando aprovação.**
2. Proposta de commits.
3. Proposta de quarentena.
4. Tabela de auditoria de progressão + os 3 maiores riscos para o MVP.
5. Recomendação do primeiro passo da Fase 1.

**STOP** ao final: aguardar aprovação antes de qualquer commit, quarentena ou implementação.
