# GPR-01.4c — composição tripla causal

29/10/2026. **Gate técnico satisfeito: 7/7 PlayMode, zero ignorados.** Próxima01.4d já autorizada; visual obrigatório ainda não entregue nesta fronteira.

## Resultado mecânico e arquitetura

A autorização explícita do ramo em01.4b reutiliza a política de linhagem da fundação: vento Technique pode conduzir uma vez; morte por vento ou Conductor desse ramo pode detonar uma vez. Detonation não gera Conductor/Detonation, Conductor não gera Conductor, e nenhum filho cria tornado ou raiz nova. A política nativa B+C fora da linhagem continua existente. Não foi necessário segundo executor ou refatoração maior.

Prova nova além de A+B/A+C: vento não letal12HP → Conductor mata vizinho5HP → Detonation causa3,75HP em outro alvo. Os três receipts compartilham Basic/Titanic/parent;20,75HP efetivos sem duplicação. Variante explosiva letal:12+5+1=18HP, duas mortes creditadas, sem dano num quarto alvo que seria alcançado por recursão. Com30 slots ocupados, vento+Conductor esgotam32 e não explodem; com29, o32º paga exatamente Detonation. Nenhuma raiz extra.

## Validação e evidência

`run_tests(mode=PlayMode,category_names=[GPR014c],include_details=true,init_timeout=120000)` via MCP real. Job **ec5acae1ed1a41388a4413d7c979aa80**,7/7,15,3720882s; sem falhas intermediárias. Detalhes em `../validation/artifacts/gameplay-rework/gpr-01-4/c-first.json`; receipts JSON em `gpr-01-4/receipts/triple-*.json`, com HP efetivo, mortes, produtores e parents.

Ordens A→B→C, A→C→B, B→A→C, C→A→B passam pelo adapter legítimo de inventário com concessão **assistida explícita**, sem modificação de tickets/pesos/custos. Cada ordem produz o mesmo dano causal. Não é prova de quatro rotas naturais ou playtest físico.

## Arquivos, skills, integridade e limitações

Acrescentados sete testes/helper de receipts em `TitanicCompositionPlayModeTests.cs`; nenhuma mudança de produção adicional nesta etapa, pois os owners já suportam a linhagem restrita. Skills locais de visão/combate/boons/aceite e Unity C#/MCP aplicadas. Mesmo branch/HEAD, Editor e baseline de01.4a. Compile/Console consultados sem erro; cenas/assets/saves/preferências e alterações anteriores preservados. Dedupe de colliders, quota e cancelamento complementar continuam nas seleções01.4a/b/e.

Limites: fixture Unity com controle automatizado, não aceite humano; snapshots visuais, Game View comparável, aquisição natural e regressões amplas ainda nas etapas01.4d/e. Avançar apenas para01.4d; GPR-01.5 não executada.
