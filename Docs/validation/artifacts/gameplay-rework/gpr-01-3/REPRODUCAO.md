# Reprodução técnica — GPR-01.2c /01.3

Usar Unity6000.5.10f1 com MCP conectado a `D:/Dev/Personal/My_games/Tech-Guy`. Conferir projeto/instância, estado do Editor, Console e cena humana antes de iniciar; não executar batchmode no mesmo checkout aberto.

1. No Unity MCP, guardar a cena atual e abrir uma execução nova de evidências, por exemplo `gpr-01-3/review-<identificador-único>`. Não reutilizar `final-capture` ou `regression-1` históricos.
2. Por `execute_code` (compiler `codedom`), definir `UnityEditor.SessionState.SetString("TechGuy.Tests.GprPhase", "gpr-01-3/review-<id>/regression")` e criar essa pasta em `Docs/validation/artifacts/gameplay-rework/`. Definir também `TechGuy.Tests.TitanicCompositionEvidence` para `gpr-01-3/review-<id>/captures`.
3. Executar via `run_tests` os argumentos de `focused-final-pass.request.json`. O teste de GameView e o de tickets recusam sobrescrever arquivos existentes. Sem a chave específica de captura, cada fixture cria uma pasta com timestamp em `gpr-01-3/replays/`.
4. Usar `get_test_job` com o job realmente retornado, aguardar conclusão e salvar a resposta antes de nova recompilação: o plugin pode perder detalhes na domain reload. Não contar status “started”,0 testes ou filtros sem correspondência como aprovação.
5. Executar `regression.request.json` e `edit.request.json`. A primeira seleção inclui o novo cenário de captura de clear por selecionar a classe inteira: seus PNG têm diretórios únicos em `gpr-01-2c/captures/`. Resultados atuais não incluem esse caso na antiga bateria196; ele foi executado separadamente e contado uma única vez.
6. Sair dePlayMode, reabrir a cena humana sem salvar cenas temporárias, remover as duas chaves deSessionState e conferir Console, scripts ausentes, pools e diff.

Comparadores observáveis:

- Base: sem tornado; B isolado: nenhum tornado, condução nativa somente de hit válido.
- A: terminal em whiff emite vento; impacto tardio causa12HP no alvo da fixture, vizinho lateral permanece intacto.
- A+B rank1: o mesmo vento causa12HP e o arco5,4HP no vizinho. Duas transações somam17,4HP, mesma causaBasic/Titanic/parent; três básicos criam três raízes nativas, nenhum arco cria outra.
- Sem impacto/imunidade/LOS/cancel: sem condução falsa. Com31 impactos reservados, o vento usa o32º e o arco é omitido; não aumentar orçamento.
- Rank3: até três vizinhos com LOS e dedupe, sem recursão. Aquisição deB após aceitar o terminal não altera snapshot.
- FirstSector: concluir encontro, resolver os tickets e comprometer próximo destino, permanecendo na sala concluída. O terminal tem somente vento cosmético, zero raízes/eletricidade/HP/reward extra; entrar no encontro seguinte limpa esse vento e restabelece emissão combatente.

Oferta legal demonstrada: seed0, topologia de fixture, rota1→2→4→5→6. A é escolhida no ticketMutation do nó5 e B noProtocol do nó6. Nenhum grantA/B nesse teste; mecânica/visuais usam grants identificados. Não confundir com economia ou run física.

Fixtures comandam APIs reais, com alvos/posicionamento assistidos; não substituem teste físico. Não modificar velocidade, raio, alcance, dano ou cadência para facilitar a reprodução. Encerrar após01.3.
