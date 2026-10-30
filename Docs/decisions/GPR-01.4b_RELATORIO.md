# GPR-01.4b — vento + Detonation

29/10/2026. **Gate técnico satisfeito: 6/6 PlayMode, zero ignorados.** 01.4c já autorizada, iniciada somente após este resultado.

## Entrega e decisão

Após o gate01.4a, a reserva específica Titanic passou a aceitar Detonation/Both com ranks válidos da raiz. O owner Scope concede somente efeitos possuídos no inventário e capturados na aceitação. Reutilizados ProcessBasicBranch, fila, executor e definição Detonation existentes. Uma morte confirmada pelo vento pode emitir uma explosão; a linhagem já existente limita uma emissão e não permite Detonation→Detonation/Conductor. Nenhum novo root, recurso, definição, rank ou balanceamento.

Alterados `ProtocolBudget.cs`, `ProtocolRuntime.Scope.cs`, `ProtocolRuntime.Damage.cs` e acrescentados seis testes em `TitanicCompositionPlayModeTests.cs`. A emissão derivada consulta capacidade antes de mostrar feedback: o último slot pode pagar o vento letal, mas não inventa explosão sem orçamento. Caminho nativo permanece intocado.

## Testes e evidência

`run_tests(mode=PlayMode,category_names=[GPR014b],include_details=true,init_timeout=120000)` via Unity MCP. Job **32667d6225c84d2fbf7aa2d6dc4bc07d**,6/6,12,2018539s. `../validation/artifacts/gameplay-rework/gpr-01-4/b-first.json` conserva detalhes. Sem falha comportamental intermediária nesta etapa; compilação e Console consultadas, zero erros.

Vento que mata alvo1HP autoriza0,75HP no vizinho, calculados pelo HP efetivo1 × fator nativo0,75. Contato não letal, imunidade, cancelamento no receipt e quota32 esgotada após vento não explodem. Duas mortes de vento na mesma viagem produzem só uma explosão: segundo alvo ativado pelo callback do primeiro dano explosivo, depois da consulta, para permanecer elegível a impacto posterior. Nenhuma mana/Asura adicional, nenhuma raiz extra; atribuição secundária Basic/parent e caps conferidos.

## Integridade e limites

Mesmo projeto/Editor/HEAD/snapshot inicial descritos em01.4a. Skills aplicadas: quatro locais de visão/combate/boons/aceite, unity-csharp-scripting e unity-mcp-orchestrator. Saves/preferências/cenas/assets e mudanças preexistentes preservados; nenhum novo balanceamento ou reward. Fixture assistida real no Unity, sem alegação de playtest humano, oferta natural ou aprovação da suíte global. Capturas reais e regressão ampliada pertencem a01.4d/e. Próxima: provar morte causada especificamente por Conductor derivado e causalidade tripla em01.4c; nenhuma nova aprovação necessária dentro de01.4.
