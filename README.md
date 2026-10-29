# Tech Guy

## Estado vigente — GPR-01.2b, 28/10/2026

A onda instantânea anterior foi aceita tecnicamente pelo desenvolvedor após teste físico, mas rejeitada como gameplay final. Punhos Titânicos agora emite um **tornado móvel de vento** em cada contato terminal executado, inclusive whiff:5,5m/3,5m/s, dano0,60× pacote aceito não crítico, sweep/LOS e mesma raiz/caps. Sem novo grant de Conductor/Detonation (`ProtocolBasicEffects.None`), sem tuning do kit nativo ou mudança de controles. [Relatório da revisão, evidências e limitações](Docs/decisions/GPR-01.2b_RELATORIO.md). **Aceite físico do tornado pendente; STOP antes de GPR-01.3.**

## Histórico da entrega GPR-01.2 anterior à revisão

### Registro histórico — GPR-01.2,28/10/2026

Punhos Titânicos implementados como Mutation Basic da Manopla: onda após HP nativo confirmado do terminal LMB, visual reversível e oferta no catálogo existente.18/18 focados+90/90 EditMode+187/187 regressões nas seleções finais (295 casos distintos, não suíte global). Intermitências anteriores de hitstop/burn continuam registradas; aceite físico/artístico de A pendente. [Relatório e capturas](Docs/decisions/GPR-01.2_RELATORIO.md). **STOP antes de GPR-01.3**, aguardando revisão do desenvolvedor.



## Histórico da entrega01.1

GPR-C01 permanece **COMPLETA e aceita pelo desenvolvedor**; GPR-01.P1 aceita como planejamento. LMB básico, RMB especial permanente e exatamente três modulares Q/E/R, sem X. Moonwalk/cadência continuam refinamentos futuros não bloqueadores, sem tuning nesta etapa.

Direção experimental A→A+B→A+B+C aceita, parâmetros1,5×/0,60× provisórios; autorizada **exclusivamente GPR-01.1**. [Relatório da fundação](Docs/decisions/GPR-01.1_RELATORIO.md) / [fatia e próximo escopo](Docs/gameplay/GPR_01_VERTICAL_SLICE.md). Sem asset/boonTitanic Fists habilitado, onda ou transformação visual de produção. STOP para revisão antes de01.2;01.3–6 também exigem autorização posterior.

Q/Avanço para aproximação é compatibilidade de ações, não sinergia mecânica nova. SwapAvanço→Uppercut adiado por redução aprovada de escopo; aquisição/substituição modular Q/E/R permanece requisito **NÃO IMPLEMENTADO/NÃO VALIDADO**, rastreado em GPR-03.1/RF-TROCA. Gates e decisões históricos abaixo não sobrepõem esta autorização.

## Estado técnico verificado

Inspeção estática em **18/10/2026**, na branch `gameplayOverhaul`, HEAD `3a3f0ee`, com alterações locais. O commit identifica a base Git, não um snapshot imutável do conteúdo inspecionado. A consolidação inicial foi estática; posteriormente o ciclo de observação foi executado no Unity real, com escopo e resultados na matriz. Isso não representa playtest humano aprovado. Resultados históricos e limites estão na [matriz de validação](Docs/validation/01_MATRIZ_DE_VALIDACAO.md).

- [Unity 6000.5.10f1](ProjectSettings/ProjectVersion.txt), C# e [URP 17.5.0](Packages/manifest.json).
- Fluxo atual (28/10/2026): `MainMenu → PrologueTutorial → NexusBlockout → FirstSector → NexusBlockout` (o prólogo depende do save). `NexusBlockout` é o Nexus com salas. `NexusLobby` permanece habilitada apenas para fixtures legadas e como fonte de ferramentas; `Playground` e `CombatStudy` permanecem desabilitadas, mas são fontes de geradores; `ProceduralStageAuthoring` é usada pela validação de rig fora do build. Ver [restauração e auditoria de cenas](Docs/decisions/NEXUS_RESTAURACAO_20261028.md).
- Código próprio em `Assets/_Project/Scripts/`; cenas, prefabs e dados em `Assets/_Project/`; dependências externas em `Assets/_ThirdParty/`.
- O [manifest](Packages/manifest.json) contém Tripo3D por caminho local absoluto em `Downloads`; a disponibilidade dessa pasta precisa ser resolvida na máquina usada para abrir o projeto. O pacote Unity MCP instalado não comprova uma sessão conectada.

Abra o projeto com a versão registrada pelo Unity Hub e use `MainMenu` como entrada do fluxo publicado. Importação, compilação no Editor e execução das cenas continuam verificações distintas da leitura dos arquivos.

## Fluxo e controles atuais

O código de menu/prólogo mantém início de jornada, repetição do tutorial e continuação no Nexus. O arsenal preserva a arma escolhida entre sessões. O portal do Nexus leva à `FirstSector`, configurada para `ManualSlice` com randomização: a rota usa um grafo autorado e varia salas, inimigos e recompensas por seed. Regras de rota, recompensas, extração e persistência têm fonte principal em [run e progressão](Docs/systems/05_RUN_E_PROGRESSAO.md).

O layout vigente de C01.4 contém **três skills modulares e um especial separado**, com bindings configuráveis no Settings. Os padrões abaixo não sobrescrevem personalizações:

| Comando padrão | Ação |
|---|---|
| W/A/S/D | Movimento direto relativo à câmera; cada direção pode ser remapeada. |
| Cursor | Mira independente. |
| Mouse esquerdo | Um pedido de básico por clique, inclusive sem alvo; hold do ChargedShot preservado. |
| Mouse direito | Especial permanente da arma; Asura única na Manopla. |
| Q / E / R | Três skills modulares; referências/identidades internas preservadas. |
| Espaço | Dash: Cursor por padrão ou Movimento; sem direção no modo Movimento, segue o olhar do personagem. |
| G | Interact dedicado no Nexus e em objetos próximos sob o cursor. |
| F | Finalização contextual/retorno já existente. |
| Escape | Pausa/saída segura de menus; a ação de pausa também pode ser remapeada. |

X não recebe ação/placeholder; não existe quarta skill modular. Arrays de quatro entradas e o índice3 permanecem compatibilidade interna, sem renumerar habilidades, boons ou estatísticas. O canal especial reaproveita a quarta ação já existente de Bow/Spear, sem mecânicas novas. F11 não é alias direto da Asura.

Settings oferece conflitos explicados, captura/cancelamento, padrões em draft e persistência somente ao Aplicar. Interact e finisher são ações distintas. Cadência, dano, clocks, recursos e cancelamento do combate não foram retunados. O mapeamento antigo de clique para navegar/QWER continua apenas compatibilidade histórica; não define o modelo direto atual. Resultados e limitações em [GPR-C01.4](Docs/decisions/GPR-C01.4_RELATORIO.md); aceite físico da fase ainda pendente.

## Fontes documentais principais

| Tema | Fonte |
|---|---|
| Regras para trabalhar no projeto | [AGENTS.md](AGENTS.md) |
| Índice e convenções de evidência | [Docs/README.md](Docs/README.md) |
| Estado, conflitos e pendências | [Estado e lacunas](Docs/02_ESTADO_E_LACUNAS.md) |
| Input, dano, recursos e reações | [Combate](Docs/systems/01_COMBATE.md) |
| Kits, custos, Asura e identidade | [Armas](Docs/systems/02_ARMAS.md) |
| Arquétipos, resistências e telegraphs | [Inimigos](Docs/systems/03_INIMIGOS.md) |
| Catálogo, compatibilidade e efeitos | [Boons](Docs/systems/04_BOONS.md) |
| Rotas, recompensas e persistência | [Run e progressão](Docs/systems/05_RUN_E_PROGRESSAO.md) |
| HUD, pausa e resultado | [HUD e UI](Docs/systems/06_HUD_E_UI.md) |

A implementação encontrada não equivale a aprovação de gameplay. Manter vida/recurso visíveis e apresentar dano por habilidade ao fim da run são decisões de produto; a tela de resultado atual ainda precisa atender ao segundo requisito. Divergências de armas e geometria estão registradas no documento de armas, sem alteração automática do design ou gameplay.

**Desenvolvimento:** Gabriel Filla Camargo. Projeto em desenvolvimento.

Para observar gameplay real e comparar execuções, seguir [captura e inspeção visual](Docs/validation/02_CAPTURA_E_INSPECAO_VISUAL.md). O fluxo reutiliza `CombatPolishPreview` e o MCP instalado, registra PNG/JSONL/metadados em `Library/GameplayObservation/` e mantém aprovação de game feel separada da validação técnica.
