# Prompt — Tech Guy · Fase 1a "Fim claro do Universo 1"

## Contexto e objetivo
Fase 1 do plano "Reset de Loop": dar ao jogador **um motivo para jogar a próxima run**. A 1a fecha o Universo 1 (FirstSector) com um fim claro e confiável. O fluxo esperado é Nexus → Travessia → salas → boss → **momento de vitória** → portal → Nexus → resultado mostrando **Vitória**. Junto, cria a base invisível do save de meta-progressão, que a Fase 1b vai usar.

Segundo a Fase 0, o fluxo boss → portal → Nexus → resultado já existe. Faltam: vitória verificada por uma pessoa, um momento de vitória e a triagem das 12 falhas de rota e salas que estão em quarentena.

## Leitura obrigatória antes de agir
1. `CLAUDE.md` (já carregado), `Docs/plano/TECH_GUY_PLANO_RESET_DE_LOOP.md` e `Docs/STATUS.md`.
2. Skills em `.claude/skills/`: `tech-guy-game-vision`, `tech-guy-combat-unity` e `tech-guy-unity-acceptance` (o `SKILL.md` e a pasta `references/` de cada uma). Aplicar o que for pertinente.
3. `Docs/systems/05_RUN_E_PROGRESSAO.md`.
4. O código real de: `ProgressionDirector` (Lifecycle e Manual), `SectorBoss`, `ScenePortal`, `RunResultsView`/`RunResultsPresenter`, `RunSummaryStore`, `CurrencyWallet`, e as ferramentas e testes que salvam e restauram listas de chaves de PlayerPrefs (`CombatPolishPreview.Observation`, `GausBlockoutPlaySession`, `ProtocolBenchmark` ou equivalentes reais).

## Etapa 1 — Triagem das 12 falhas de rota e salas
- Rodar isoladamente as 12 (marcadas `QUARENTENA-F0` com o motivo de rota e salas). Para cada uma, classificar:
  - **BUG REAL:** o jogo está errado.
  - **TESTE DESATUALIZADO:** a premissa mudou com a RUN-EXP-09/09B ou o C01. Exemplo: verificação de cobertura com posição fixa em `RunExp03FirstSectorTests.cs:78`.
  - **AMBIENTE/ORDEM**
- Bug real no caminho até o boss: corrigir dentro do escopo, sem mexer na seleção de salas aprovada (09/09B), no C05 nem no combate.
- Teste desatualizado: reescrever a asserção para a regra atual e justificar em uma linha. Não enfraquecer o teste só para passar.
- Remover o `Explicit` só dos testes que passarem a refletir o comportamento correto. Os outros continuam em quarentena, com motivo atualizado.

## Etapa 2 — Boss alcançável e vencível
- Confirmar pelo código e pelo Editor que uma run normal com a Manopla chega ao boss e que matar o boss leva à vitória, não a outro estado.
- Criar um **atalho de desenvolvimento** para eu testar o boss sem refazer a run inteira. Ele só pode existir no Editor ou em Development Build (ex.: tecla ou opção de debug que leva à sala do boss com um kit padrão da Manopla) e não pode aparecer nem compilar em build de release. Descrever no relatório como usá-lo.
- Não balancear o boss nesta etapa. Se houver um bloqueio óbvio (impossível de vencer, invulnerável, sem telegrafia), registrar e propor a correção sem aplicar.

## Etapa 3 — Momento de vitória
Ao morrer o boss:
1. Feedback de impacto final curto, reaproveitando o que existe (hitstop/slow-motion breve, VFX/SFX de morte ou explosão já presentes). Sem assets novos pesados.
2. Faixa de texto na tela: **"UNIVERSO 1 CONCLUÍDO"** (placeholder de texto, estilo coerente com o HUD atual), por cerca de 2–3 s, sem bloquear input além do necessário.
3. O portal para o Nexus abre depois do banner. **O jogador entra quando quiser**, sem teleporte automático.
4. O resultado da run registra **Vitória**, e a tela de resultados no Nexus mostra isso com destaque.

O fluxo de morte continua como está.

## Etapa 4 — Base do save de meta-progressão (`MetaProgress.v1`, invisível)
- Perfil persistente e versionado em JSON, gravado em **arquivo próprio em `Application.persistentDataPath`**, não em PlayerPrefs, para escapar das listas fixas de chaves que ferramentas e testes salvam e restauram. Se encontrar um motivo forte para outra solução, propor antes de implementar.
- Campos mínimos: `schemaVersion`, `runsStarted`, `runsFinished`, `victories`, `deaths`, `universesCleared` (lista de IDs; hoje só o Universo 1), `lastUpdatedUtc`.
- Atualizar no início e no fim da run (vitória, morte e abandono, se o abandono já tiver um caminho claro).
- O caminho do arquivo precisa ser **injetável**: testes usam pasta temporária e nunca tocam o perfil real nem `Docs/`.
- Leitura robusta: arquivo ausente ou corrompido gera um perfil novo e um aviso no log, sem quebrar o jogo.
- **Nenhuma UI** nesta etapa. A `CurrencyWallet` atual não muda; ela vira Fragmentos na 1b.

## Restrições
- Não alterar combate, boons, C05, seleção e rotação de salas (09/09B), controles, Nexus (fora do resultado) nem `WeaponLoadout`.
- Lança, Arco e G1/G2/G3 continuam pausados.
- Testes novos ou alterados não gravam em `Docs/` nem em pastas versionadas.
- Não rodar a suíte PlayMode completa. Rodar: os 12 triados, os testes do fluxo de boss e resultado, os testes novos do `MetaProgress` e a EditMode completa.
- Não commitar sem minha aprovação. Não fazer push.

## Critérios de aceite
- [ ] Cada uma das 12 falhas tem classificação e destino (corrigida, reescrita ou mantida em quarentena com motivo).
- [ ] Atalho de debug para o boss, ausente em build de release (verificado pelos símbolos de compilação, não suposto).
- [ ] Morte do boss → feedback → banner → portal manual → Nexus → resultado "Vitória".
- [ ] `MetaProgress.v1` grava e lê; testes cobrem vitória, morte, arquivo ausente e arquivo corrompido usando pasta temporária.
- [ ] EditMode 100% verde; nenhuma falha nova nas suítes focais.
- [ ] `git status` mostra só mudanças de código, testes e docs do escopo, sem artefatos.

## Relatório final (uma página, PT-BR)
1. Feito / Não feito / Não testado.
2. Tabela das 12 falhas: teste, classificação e destino.
3. Arquivos e classes alterados (caminhos reais).
4. Onde fica o arquivo do `MetaProgress` e um exemplo do JSON.
5. **O que eu devo jogar para validar**, em passos curtos: incluir uma run completa com a Manopla, o atalho do boss e uma morte proposital, e onde conferir o JSON depois.
6. Proposta de commits.

**STOP** ao final e aguarde meu playtest.
