# Evidência GPR-C01.2a — 26–27/10/2026

[Relatório completo](../../../../decisions/GPR-C01.2a_RELATORIO.md) / [protocolo canônico](../../../../gameplay/GPR_VALIDATION_PLAN.md).

Unity real6000.5.10f1, projetoD:/Dev/Personal/My_games/Tech-Guy, HEAD3a3f0ee8ad82734104511b63877d38df512ea146 com alterações humanas locais preservadas. Concluído apenas experimento; STOP para revisão. Não há aprovação física nem integração ao player.

- `git-status-inicial.txt`, `integridade-inicial.json`, `editor-inicial.json`: baseline anterior aos arquivos de protótipo.
- `testes-prototipo-primeira-execucao.json`:7/8, falha real do resíduo float. `testes-prototipo-pos-correcao.json`:9/9 intermediários. `testes-prototipo-final.json`:10/10 com parâmetro Animator e metadados finais; regressão26+dash8+Edit15 completam59 casos finais distintos. Não somar reruns como casos adicionais.
- `*.csv`: um arquivo por teste. Coordenadas locais à origem(1000,0,1000). `dt` é passo solicitado, não FPS observado; `cpuMicroseconds` mede só a chamada Step. Cenários com bloqueios/dash/teclado subsequente não têm média representativa de velocidade livre.
- `medicoes-resumo.json`: agregação por cenário; distância integra amostras com dt arredondado no CSV, use posição bruta/assertions para precisão final. `virtual-input-real-time.json`: trecho com dispositivos virtuais e Time.deltaTime real.
- `character-controller-assessment.json`: CC avaliado enquanto agente/motor estavam desativados; parede física bloqueada, limite NavMesh não respeitado.
- `arena-*-gameview.png`: quatro capturas reais Game View1160×600 inspecionadas. Boundary mostra parada; thin-wall é quadro posterior ao dash com movimento paralelo; slope mostra topo; virtual-input mostra estado após soltura. Não comprovam rig/feel/jitter temporal universal.
- `metadados.json`: checker recomendado incompatível com pacote em outro volume; NÃO PASS. `metadados-alternativa.json`: checagem focada de novos GUIDs contra metas de Assets. `editor-final.json`: tipos importados, cena/prefs/summary/bindings e ausência de resíduos.
- `integridade-final.json`:1778 fontes/metas/configurações preexistentes preservadas. `git-status-delta.json`: snapshots com as mesmas opções; novos paths do protótipo e desaparecimento de Docs.zip não versionado na raiz, que não foi manipulado por esta atividade e não tinha hash no inventário. Origem dessa diferença não determinada. `limpeza-final.json`: consulta após o último job. `manifesto.json`: lista/hash de artefatos, sem incluir a si mesmo.

Reprodução: conferir Edit Mode/cena limpa, importar novos arquivos com force refresh, executar PlayMode assembly `TechGuy.Tests.GprMovement`. Para preview local usar menu `Tech-Guy/Experiments/C01.2a/Open isolated WASD arena`; é uma cena não salva, sem combate/save. WASD, mira mouse, Space dash cursor e Tab câmera/mundo são apenas entradas da fixture. Sair de Play Mode restaura a cena anterior. Depois executar o analisador `python Tools/GameplayObservation/c01_2a_analyze.py` na raiz. Os nomes exatos dos filtros legados estão nos JSONs de resultados.

Rollback do experimento: encerrar Play Mode, remover somente a pasta nova GprMovementPrototype com sua `.meta` pelo Editor e o analisador novo se desejado. Nenhum prefab/cena/asset/binding/save de produção precisa rollback. Preservar documentação e evidência para auditoria. Não remover nem resetar as alterações humanas registradas na baseline.
