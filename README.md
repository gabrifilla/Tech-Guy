# Tech Guy

Tech Guy é um ARPG roguelite top-down em Unity/C#. Um desenvolvedor preso em um universo digital explora incursões a partir do hub Nexus. O combate combina comando ativo, posicionamento e três identidades de arma: manopla para pressão/Asura, lança para geometria/espaçamento e arco para alvo/distância.

A narrativa de mundos interconectados e armas como companheiras com personalidade é direção de produto; este README não atesta a entrega desses conteúdos. Visão, referências e limites de design estão em [Docs/00_VISAO.md](Docs/00_VISAO.md) e [Docs/01_PILARES.md](Docs/01_PILARES.md).

## Estado técnico verificado

Inspeção estática em **18/10/2026**, na branch `gameplayOverhaul`, HEAD `3a3f0ee`, com alterações locais. O commit identifica a base Git, não um snapshot imutável do conteúdo inspecionado. A consolidação inicial foi estática; posteriormente o ciclo de observação foi executado no Unity real, com escopo e resultados na matriz. Isso não representa playtest humano aprovado. Resultados históricos e limites estão na [matriz de validação](Docs/validation/01_MATRIZ_DE_VALIDACAO.md).

- [Unity 6000.5.10f1](ProjectSettings/ProjectVersion.txt), C# e [URP 17.5.0](Packages/manifest.json).
- [Build Settings](ProjectSettings/EditorBuildSettings.asset): `MainMenu`, `PrologueTutorial`, `NexusLobby` e `FirstSector` habilitadas; `Playground` e `CombatStudy` desabilitadas.
- Código próprio em `Assets/_Project/Scripts/`; cenas, prefabs e dados em `Assets/_Project/`; dependências externas em `Assets/_ThirdParty/`.
- O [manifest](Packages/manifest.json) contém Tripo3D por caminho local absoluto em `Downloads`; a disponibilidade dessa pasta precisa ser resolvida na máquina usada para abrir o projeto. O pacote Unity MCP instalado não comprova uma sessão conectada.

Abra o projeto com a versão registrada pelo Unity Hub e use `MainMenu` como entrada do fluxo publicado. Importação, compilação no Editor e execução das cenas continuam verificações distintas da leitura dos arquivos.

## Fluxo e controles atuais

O código de menu/prólogo mantém início de jornada, repetição do tutorial e continuação no Nexus. O arsenal preserva a arma escolhida entre sessões. O portal do Nexus leva à `FirstSector`, configurada para `ManualSlice` com randomização: a rota usa um grafo autorado e varia salas, inimigos e recompensas por seed. Regras de rota, recompensas, extração e persistência têm fonte principal em [run e progressão](Docs/systems/05_RUN_E_PROGRESSAO.md).

Os bindings padrão de [GamePreferences](Assets/_Project/Scripts/Core/GamePreferences.cs) são:

| Comando | Ação |
|---|---|
| Mouse esquerdo | Selecionar inimigo/aproximar/atacar; no chão, mover. Segurar repete ataques elegíveis. |
| Mouse direito | Movimento/interação no mundo. |
| Shift + comando primário ou de movimento | Básico parado na direção do cursor, sem selecionar alvo. |
| Q / W / E / R | Quatro slots da arma equipada. |
| Espaço | Esquiva; o asset base não cobra mana. |
| F | Finalizar sequência quando disponível: finalizador antecipado da Asura Cadenciada ou retorno da órbita da lança. |
| E / Esc | Interação no Nexus / pausa e fechamento de painéis, conforme contexto. |

Os comandos de combate, inclusive F, usam bindings configuráveis; não confundir E de interação contextual com a skill E em combate. A execução respeita janelas de cancelamento e buffer. O arco permite movimento reduzido em casts nativos; a Asura Cadenciada permite movimento durante a espera. Portanto não existe bloqueio universal de movimento/dash/outras skills em toda execução. Contratos e limitações estão em [combate](Docs/systems/01_COMBATE.md).

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
