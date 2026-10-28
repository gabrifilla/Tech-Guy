# Docs — mapa de conhecimento do Tech Guy

**Propósito:** instruir agentes e humanos sem transformar o repositório em um depósito de versões redundantes.

## Leia nesta ordem

1. `../AGENTS.md` — regras de execução e validade das evidências.
2. `00_VISAO.md` / `01_PILARES.md` — intenção estável do jogo.
3. `02_ESTADO_E_LACUNAS.md` — implementação inspecionada, lacunas e conflitos; não atesta gameplay aprovada.
4. `03_PRIORIDADES.md` — próximos gates de qualidade.
5. `systems/<tema>.md` e `art/` — comportamento por domínio.
6. `validation/` — evidência e critérios de conclusão.
7. `decisions/` — decisões aprovadas e por aprovar.

## Política: uma fonte principal por tema

- Pilares não descrevem detalhes de classe C#; sistema não duplica a visão inteira.
- **Design aceito** é diferente de **implementação verificada**. Toda afirmação operacional deve citar método/data/commit quando tiver sido validada.
- Para mudanças, atualize o documento canônico existente; evite `V2`, `final`, `(1)` ou séries de *pareceres* no `Docs/` ativo.
- ADR apenas para decisão de impacto durável. As exclusões antigas foram intencionais, confirmadas pelo desenvolvedor em 18/10/2026. Não restaurar o acervo nem criar uma árvore paralela de arquivos históricos como requisito de trabalho. Corrigir dependências apontando para os canônicos atuais.
- `Docs/references/` documenta *quais propriedades* de jogos inspiram o Tech Guy — não atribui propriedade intelectual de terceiros ao projeto.
- `Docs/templates/` contém exemplos reutilizáveis; não tratar exemplos como funcionalidades implementadas.

## Convenção de status

`PROPOSTO` → `APROVADO_DESIGN` → `IMPLEMENTADO_NAO_VALIDADO` → `VALIDADO_TECNICAMENTE` → `VALIDADO_EM_GAMEPLAY` → `APROVADO_PARA_ENTREGA`.

`BLOQUEADO` e `OBSOLETO` descrevem condições adicionais. Status técnicos exigem evidências. Apenas o responsável pelo jogo aceita *feel*, arte e diversão. **Não converter automaticamente status herdado de uma documentação antiga em verdade atual.**

Use os estados por critério, não como selo global: **implementado** exige fonte/asset/cena; **validado por teste** exige método, resultado, data e revisão/configuração; **gameplay aprovada** exige aceite explícito do desenvolvedor para aquele escopo; **proposta** não autoriza implementação. Uma regra de design aceita pode continuar sem implementação ou sem validação de gameplay. Evidência histórica nunca é uma nova execução.

## Fonte principal por tema

| Tema | Fonte oficial |
|---|---|
| Intenção, narrativa e critérios de produto | [00_VISAO.md](00_VISAO.md), [01_PILARES.md](01_PILARES.md) |
| Estado verificado, conflitos e lacunas | [02_ESTADO_E_LACUNAS.md](02_ESTADO_E_LACUNAS.md) |
| Planejamento de próximos gates | [03_PRIORIDADES.md](03_PRIORIDADES.md) |
| Responsabilidades, consumidores e integridade | [04_ARQUITETURA_E_INTEGRIDADE.md](04_ARQUITETURA_E_INTEGRIDADE.md) |
| Política documental e exclusões pendentes | [05_MIGRACAO_E_LIMPEZA.md](05_MIGRACAO_E_LIMPEZA.md) |
| Input, dano, reação, cancelamento e recursos | [systems/01_COMBATE.md](systems/01_COMBATE.md) |
| Kits, identidade e autoria das três armas | [systems/02_ARMAS.md](systems/02_ARMAS.md) |
| IA, arquétipos, resistências e telegraphs | [systems/03_INIMIGOS.md](systems/03_INIMIGOS.md) |
| Boons, catálogos, elegibilidade e propostas | [systems/04_BOONS.md](systems/04_BOONS.md) |
| Rotas, recompensas, persistência e conteúdo analítico do fim da run | [systems/05_RUN_E_PROGRESSAO.md](systems/05_RUN_E_PROGRESSAO.md) |
| Apresentação do HUD, cards, pausa, mapa e resultado | [systems/06_HUD_E_UI.md](systems/06_HUD_E_UI.md) |
| Direção artística e pipeline | [art/01_DIRECAO_VISUAL.md](art/01_DIRECAO_VISUAL.md), [art/02_PIPELINE_DE_ASSETS.md](art/02_PIPELINE_DE_ASSETS.md) |
| Resultados e limites das verificações | [validation/01_MATRIZ_DE_VALIDACAO.md](validation/01_MATRIZ_DE_VALIDACAO.md) |

README e skills resumem ou encaminham a essas fontes; valores mutáveis não devem ganhar outras tabelas paralelas. Capturas/telemetria complementam a matriz, sem converter resultados técnicos em aceite humano.

## Estrutura

- `systems/`: combat, weapons, enemies, boons, runs, HUD.
- `art/`: visão de arte e receita compartilhada de assets.
- `validation/`: cenário de captura, matriz de QA, métricas.
- `decisions/`: ADRs pequenas com histórico imutável de intenção.
- `templates/`: prompt de auditoria, tarefa, handoff e ADR.
- `references/`: papel específico das referências.

## Primeiro dia do Codex

Use [templates/00_AUDITORIA_INICIAL.md](templates/00_AUDITORIA_INICIAL.md) quando uma auditoria for pedida. Em tarefas autorizadas, investigue o escopo antes de editar e atualize o canônico existente. A ausência dos documentos excluídos intencionalmente não bloqueia o trabalho. Artefatos históricos locais são evidência opcional; não são pré-requisito para ler esta documentação.

## Gameplay Rework — GPR-00 (26/10/2026)

A nova visão e o plano WASD estão em [gameplay/README.md](gameplay/README.md). O [ledger](decisions/GAMEPLAY_REWORK_PROGRESS.md) e o [consolidado](decisions/GPR-00_PHASE_SUMMARY.md) registram as cinco atividades de audit/design. Decisões em ADR0006 ainda aguardam aprovação; nenhum controle ou sistema de skills foi implementado nesta fase. Os exemplos históricos QWER dos canônicos não sobrepõem a direção nova WASD/mouse/QERX. GPR-C01 precede GPR-01.

### C01.1 — baseline e desenho revisado

GPR-00 foi aprovada documentalmente com ajustes. [Baseline atual](gameplay/GPR_C01_BASELINE.md), [migração vigente](gameplay/GPR_CONTROL_MIGRATION.md) e [consolidado C01.1](decisions/GPR-C01.1_BASELINE_E_PRONTIDAO.md) registram C01.1a/b executadas e o novo gate. RMB agora é especial/Asura, não terminal do básico; LMB clique pede um ataque. Nenhum controle novo foi implementado; STOP antes de C01.2.
