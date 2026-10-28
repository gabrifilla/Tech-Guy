# Evidência GPR-C01.2b —27/10/2026

[Relatório](../../../../decisions/GPR-C01.2b_RELATORIO.md), [protocolo](../../../../gameplay/GPR_VALIDATION_PLAN.md). Implementação de motor e migração mínima de teclas autorizadas; sem aceite físico, RMB/AsuraX, modularidade nova ou GPR-01.

- Git/1789 hashes iniciais e integridade final:22 mudanças intencionais,1767 iguais,0 ausências. `novos-guids.json` é focado, não checker global.
- `editor-inicial.json` registra tentativa de snapshot que falhou pela API obsoleta de seleção; usar `persistencia-antes-ativacao.json` e `persistencia-final.json` para a comparação válida. Ambos foram realmente consultados e são iguais. Snapshot inicial de Console não obtido.
- `ativacao-cenas.json` comprova o procedimento Unity; `diff-cenas.json` mostra defaults que SaveScene materializou. Esses defaults foram removidos seletivamente. `diff-cenas-final.json` comprova só a flag por cena; backups originais em Library/GprC01Integration/scene-backup-20261027.
- `testes-integracao-primeira.json`:5/6, foco virtual não preparado para o caso de velocidade. Iterações posteriores e `testes-integracao-final.json`:13/13. Não contar iterações como casos adicionais.
- `testes-regressao-30.json`:29/30, falha de buffer; `recovery-isolado.json`:1/1; `testes-regressao-final-30.json`:30/30. Nenhum código/tolerância mudou entre esses reruns; causa não confirmada. Demais finais:dash8 eEdit15, total66 distintos.
- `nexus-runtime.csv`, `nexus-medicoes.json`, `nexus-proveniencia.json`:2,5s,461 amostras, entrada/foco virtuais em cena/player reais; sem injeção de recursos. Cardinal livre≈5m/s, soltura0, diagonal contra borda bloqueada,0 AbilityUsed. Fixture livre separada mediu8m/s cardinal/diagonal, velocidade histórica dessa fixture.
- `nexus-wasd-*.png`:três Game View1160×600 abertos e inspecionados. Walk=movimento; diagonal=comando bloqueado pela borda; stop=soltura. Não afirmar movimento diagonal livre ou aprovação física dessas imagens.
- `console-runtime.json` consulta erros, sem Clear. Warnings de fixtures estão nos JSONs de testes.
- `checker-metadados.stderr.txt`: falha real entre volumes C:/D: do checker da skill. JSON de status não é PASS; `novos-guids.json` e compilação/importação complementam.
- `manifesto.json`: fontes/documentos/evidências da atividade e hashes, sem incluir a si mesmo.

Reprodução automatizada usa o cliente MCP HTTP descoberto em Tools/GameplayObservation/c01_2b_mcp.py; requer servidor e bridge ativos no projeto correto. Os13 métodos GPRDirect da assembly PlayMode e nomes legados estão nos resultados. Para smoke, iniciar NexusBlockout em Play Mode e executar o código do driver pelo execute_code MCP. A entrada é virtual; aguardar a proveniência final e então sair de Play Mode. Não executar sobre gameplay humano ativo.

Próximo: humano físico e proposta C01.2c; permanecer explícito sobre borda sem deslizamento tangencial e falha transitória do buffer. Não restaurar backups de cenas sobre alterações humanas posteriores.
