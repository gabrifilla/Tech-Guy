# Tech Guy — instruções para Claude Code

Este arquivo é lido automaticamente pelo Claude Code. Ele importa as regras históricas do repositório e, logo abaixo, registra o que **mudou** e prevalece sobre elas.

@AGENTS.md

---

## O que prevalece sobre o AGENTS.md (decisões do desenvolvedor, 09/11/2026)

### Controles vigentes
- WASD move; mouse mira; LMB básico/combos; RMB especial permanente da arma (Asura na Manopla); **Q/E/R** = três skills; dash e bindings remapeáveis.
- **Não** reintroduzir click-to-move, quarta skill (X/W), F11 ou LMB/RMB por braço na Manopla. Trechos do `AGENTS.md`/`README.md` que citam click-to-target, Q/W/E/R ou E para interagir no Nexus são **históricos**.
- Identidades internas legadas (Q/W/E/R, índice 3, arrays de 4) podem existir por compatibilidade; não significam skill extra para o jogador.

### Narrativa
- A história vigente está em `Docs/00_VISAO.md` → Narrativa (10/11/2026): um dev de jogos desmaia no crunch e acorda dentro do próprio jogo fragmentado; a Manopla se funde a ele e transmite o boxe; os universos são o caminho de volta. A sinopse do `AGENTS.md` ("software developer trapped inside his own computer") é histórica.

### Skills do projeto
- As skills ficam em `.claude/skills/` (cópias das antigas `.agents/skills/`, usadas pelo Codex). Antes de mexer em design, combate, boons, UI/HUD, arte ou validação Unity, carregue a skill pertinente.
- Também estão instaladas 27 skills genéricas de [awesome-gamedev-agent-skills](https://github.com/gamedev-skills/awesome-gamedev-agent-skills) (Apache 2.0, commit `dcf3bbb`): Unity, disciplinas, roguelike/RPG, prototipagem e Steam, mais o roteador (`gamedev-router`). Elas são referência técnica; **as skills `tech-guy-*`, este arquivo e o plano prevalecem** quando houver conflito.

### Direção atual: "Reset de Loop"
- Plano em `@Docs/plano/TECH_GUY_PLANO_RESET_DE_LOOP.md`. Ele define as fases 0–3 (arrumar a casa → meta-progressão → variedade de run → identidade visual), as decisões tomadas e o que está em aberto.
- Prioridade: **motivo para jogar a próxima run** (meta-progressão) e **variedade entre runs**. O combate base já é considerado divertido pelo desenvolvedor.
- **Pausados** até decisão nova: Lança, Arco, arte 2.5D/pictórica, prólogo e os protótipos G1/G2/G3 (que continuam fora do pool normal).
- **Preservar:** combo Titanic Fists → Conductor → Detonation; salas RUN-EXP-09/09B; tela de recompensas C05; controles acima.

### Processo novo (substitui o ritual de relatórios das fases RUN-EXP)
- Cada etapa termina com algo **jogável**; o desenvolvedor joga antes da próxima.
- Relatório de **uma página**, em PT-BR: Feito / Não feito / Não testado / O que jogar para validar. Sem subdocumentos por subetapa.
- Não criar novos arquivos em `Docs/decisions/` por etapa. Atualizar `Docs/STATUS.md` (quando existir) e o plano.
- Protótipos de design podem ser descartáveis: não preservar por padrão o que não foi aprovado.
- Boons: se a mudança não aparece numa gravação de 10 s, não serve.
- Uma frente por vez.

### Segurança (continua valendo)
- Inspecionar o código e o Editor reais antes de mudar; não inventar classes, caminhos, APIs ou resultados de teste.
- Usar o Unity MCP para compilar, rodar testes e verificar cenas quando a mudança tocar Unity. Se o MCP não estiver conectado, dizer isso em vez de supor.
- Preservar GUIDs/.meta, saves, PlayerPrefs e IDs de boons. Não commitar, fazer push ou apagar arquivos sem pedido explícito do desenvolvedor.
