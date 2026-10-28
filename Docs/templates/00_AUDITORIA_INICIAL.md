# Prompt para Codex — auditoria de projeto (executar antes de alterar código)

Atue como engenheiro de gameplay/arquitetura em meu projeto Unity Tech Guy.

**MISSÃO: SOMENTE AUDITORIA. NÃO MODIFIQUE/EXCLUA ARQUIVOS E NÃO IMPLEMENTE FEATURES.**

1. Leia `AGENTS.md`, `Docs/README.md`, `Docs/00_VISAO.md`, `Docs/01_PILARES.md`, `Docs/02_ESTADO_E_LACUNAS.md` e o README real do repositório.
2. Registre branch, commit, `git status`, arquivos sujos/não rastreados e versão Unity/pipeline.
3. Descubra se Unity MCP está **conectado à instância correta**. Liste capacidades realmente disponíveis; registre impossibilidades. Inspecione cenas, prefabs, ScriptableObjects, Console e test runner somente se houver acesso.
4. Mapeie com paths reais: input/movement, abilities/slots/QWER/F, damage/events, cooldown/mana/Asura, stance/stagger/launch, inimigos/telegraphs, armas, boons/catalogues, run/reward, HUD/results, persistência, testes.
5. Procure sistemas duplicados e caminhos paralelos; **não conclua que duplicata existe apenas pelo nome**. Trace callsites/consumidores e uma evidência concreta por suspeita.
6. Compare documentação atual com o código/cenas reais, identificando `documentado` vs `implementado` vs `validado no Unity` vs `aprovado`.
7. Para boons, confira catálogo/runtime, gate de Mythics e a matriz de validação vigente. Separe resultados históricos de uma nova execução e não confunda compilação CLI com Unity.
8. Verifique a documentação canônica e suas referências. As exclusões documentais antigas foram intencionais: não restaurar nem recriar requirements/design/tasks/relatórios. Audite separadamente exclusões pendentes de código, testes, cenas, assets e configuração, indicando riscos sem revertê-las ou confirmá-las.
9. Entregue: (a) mapa de arquitetura; (b) divergências por severidade; (c) riscos de regressão; (d) top 5 mudanças com menor custo/maior benefício; (e) evidências/testes realmente executados; (f) dúvidas bloqueantes.

**Critério de conclusão:** eu consigo escolher a próxima tarefa com base em fatos do projeto, sem que você tenha alterado nenhum artefato.
