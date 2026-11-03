# RUN-EXP-02.3 — leitura de escolhas e descoberta

## Escopo e implementação

`RunExp02RewardReview.cs` é uma inspeção local aberta pelo botão Recompensas da fatia. Usa OfferReward(1), definições e RunModifierPresentation existentes em uma run temporária sem aquisições; RunRewardUI produtiva fica desativada nessa inspeção. Não chama Choose, não cria ticket manual, não muda pesos, ranks, slots ou pool. Trocar Manopla/Arco/Lança inicia outra consulta independente e descarta as cópias anteriores. Voltar remove a run temporária, restaura Manopla e retoma o encontro. Nenhuma escolha inspecionada vira recompensa persistente.

O combate fica pausado, incluindo projéteis já em voo; escala anterior é restaurada ao fechar. As barras de inimigos são ocultadas nesse painel, eliminando a sobreposição observada na primeira captura. Famílias, alvo/slot, nome e efeito vêm dos metadados reais. A descrição principal conserva o texto vigente; termos legados e trechos técnicos ainda precisam de uma futura revisão editorial aprovada.

## Estado atual versus possibilidade

O cabeçalho enumera efeitos realmente adquiridos; a consulta independente começa com nenhum. Os cartões distinguem o valor imediato de Conductor das possibilidades de transportar fogo/gelo, e Reactor/Overflow/Resonance das condições que faltam. A verificação usa inventário adquirido, nunca outra carta ainda não escolhida. Cooferta de Frost e Conductor não vira “combinação ativa”. Não se promete drop futuro.

Limite importante: a fatia não concede efeitos para fabricar uma build de demonstração. Logo os screenshots normais mostram contexto vazio, não uma interação já ativa. O texto condicional para inventário adquirido existe, mas a validação física com uma build natural já adquirida continua pendente; não se declara esse cenário demonstrado só pelo ramo de código.

## Comparação real e caminhos orgânicos

Antes/depois usam exatamente as mesmas ofertas em cada arma. O “antes” liga a RunRewardUI existente somente na fixture automatizada, sem input de aquisição; depois retorna ao modo de inspeção. Acquired permaneceu zero.

- Manopla: [rewards-existing-Manopla.png](../validation/artifacts/run-exp-02/integration/20261030-220628-8278188-7de59e16f6f845a1983b5148df31c738/rewards-existing-Manopla.png) / [rewards-proposed-Manopla.png](../validation/artifacts/run-exp-02/integration/20261030-220628-8278188-7de59e16f6f845a1983b5148df31c738/rewards-proposed-Manopla.png).
- Arco: [rewards-existing-Arco.png](../validation/artifacts/run-exp-02/integration/20261030-220628-8278188-7de59e16f6f845a1983b5148df31c738/rewards-existing-Arco.png) / [rewards-proposed-Arco.png](../validation/artifacts/run-exp-02/integration/20261030-220628-8278188-7de59e16f6f845a1983b5148df31c738/rewards-proposed-Arco.png).
- Lança: [rewards-existing-Lança.png](../validation/artifacts/run-exp-02/integration/20261030-220628-8278188-7de59e16f6f845a1983b5148df31c738/rewards-existing-Lança.png) / [rewards-proposed-Lança.png](../validation/artifacts/run-exp-02/integration/20261030-220628-8278188-7de59e16f6f845a1983b5148df31c738/rewards-proposed-Lança.png).

Exemplos que o catálogo suporta: impacto próximo por mutações/básicos da Manopla; distância e posicionamento por flechas/minas do Arco; alcance e retorno da Lança; um Protocol de efeito imediato mesmo sem o parceiro desejado; uma cooferta inesperada que aponta possibilidade sem garantir a próxima carta. Nenhum desses exemplos altera o sorteio ou recomenda ABC como destino obrigatório. Comparar primeiro efeito independente, alcance/risco e custo de abrir mão da outra escolha.

Arquivo criado: `Scripts/Prototypes/RunExp02/RunExp02RewardReview.cs`, conectado por RunExp02Review; ver testes de leitura/sem aquisição no job final. Skills aplicadas: tech-guy-boon-design, tech-guy-game-vision, tech-guy-unity-acceptance, unity-csharp-scripting e unity-mcp-orchestrator. Produção mantém sua interface e semântica atuais. Gate técnico da consulta: aprovado; clareza, estados com aquisições reais e acabamento visual aguardam revisão física.

## Verificação, preservação e limites comuns

Instância MCP `Tech-Guy@fccafb2b47cd7671`, Unity `6000.5.10f1`, projeto real `D:/Dev/Personal/My_games/Tech-Guy`, branch `gameplayOverhaul`, HEAD inicial `314d28ec22ae2d242409fbfd10ea7f8368dd76e9`. O checkout antigo do chat não foi usado para implementar. O cliente `Tools/RunExp02/mcp_call.py` chama o Unity MCP existente, seleciona o projeto e conserva respostas; UTF, compilação, Console, prefabs e Play foram verificados no Editor. Não há servidor ou runner substituto.

Resultados integrais, sem somar execuções sobrepostas:

- Estabilidade: `b007fc8e1c3940cd8cc95cc4fd59dc00` — 13/13 PlayMode, 27.966s, 0 ignorados.
- Fatia final: `3862bb8a2c134ad59fab6fe78808319f` — 10/10 PlayMode, 56.621s, 0 ignorados.
- Geometria: `64ba37df12cc45638fba9ea4d14ece5f` — 1/1 EditMode, 0.512s, 0 ignorados.
- Contratos existentes selecionados: `7eb548c0fd354f1da745694aa4ade0b2` — 11/11 PlayMode, 9.650s, 0 ignorados.

Os nomes exatos, asserções, mensagens e saídas constam nos JSONs correspondentes em `Docs/validation/artifacts/run-exp-02/`. As fixtures usam imunidade do jogador somente nas sequências visuais/medições e testes especificados; o playtest normal não a recebe. Conclusão/death automatizada é prova mecânica, não vitória humana. O teste de obstrução dispara um projétil nativo diretamente, em fixture explícita, e não representa uma aquisição de poder.

306 arquivos protegidos com SHA-256 idêntico ao início, incluindo cenas publicadas, prefabs, definições, Resources, pacotes e configurações. `integrity-final.json` registra a comparação. As três chaves reais de PlayerPrefs — arma, resumo de run e tutorial — mantiveram existência, valor inteiro e hash do texto. Não se chamou Finish/Save das estatísticas na revisão, nem existe ProgressionDirector nela. As moedas adicionadas por Actor.Awake são removidas somente nos candidatos instanciados na revisão. As alterações anteriores em `Docs.zip`/`Docs (2).zip` foram preservadas.

Limite da auditoria de saves: o inventário inicial do registro usou separador incorreto e retornou vazio; ele não demonstra a totalidade das preferências. A comparação válida cobre as três chaves lidas por PlayerPrefs e os arquivos disponíveis, majoritariamente logs/testes/analytics. Não se alega um hash integral do perfil humano. RunSummary foi protegido pela fixture de isolamento existente. Não houve reset de preferências.

Arquivos desta frente: [manifesto](../validation/artifacts/run-exp-02/files-manifest.json), acrescido destes seis relatórios e das atualizações no ledger, matriz, masterplan, requisitos, design, tarefas e estado do projeto. Nenhum prefab publicado foi substituído e nenhum GUID histórico foi recriado. O estudo RUN-EXP-01 continua disponível. Editor final em NexusBlockout, Edit Mode, cena limpa; ver `editor-final.json`. Não houve commit/push nesta nova frente.

Gates: **técnico aprovado no recorte testado**; **gameplay e visual aguardam revisão física**; **arte de produção pendente**. Teste verde não aceita feel, leitura sem rótulos ou arte final. Não publicar candidatos/salas/UI na progressão antes dessa revisão. STOP após 02.4.

Conferência final dos arquivos do persistentDataPath: [save-files-final.json](../validation/artifacts/run-exp-02/save-files-final.json). Quatro dos cinco hashes permanecem iguais; somente TestResults.xml mudou pela execução UTF. Isso não é alteração de save de gameplay. Console final sem erros;306/306 hashes protegidos e três preferências conferidas, com os limites de cobertura já registrados.
