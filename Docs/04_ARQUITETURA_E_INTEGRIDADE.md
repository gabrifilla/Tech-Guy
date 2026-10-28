# Arquitetura: investigar antes de expandir

**Mapa inspecionado em 18/10/2026**, sobre o workspace descrito em [02_ESTADO_E_LACUNAS.md](02_ESTADO_E_LACUNAS.md). São responsabilidades encontradas em código/configuração, não prova de execução ou unicidade global de todos os fluxos. Não substituir convenções existentes por novas abstrações.

## Responsabilidades, fontes e consumidores

| Responsabilidade | Fonte real | Consumidores/fronteira e risco |
|---|---|---|
| Input e movimento | [GamePreferences](../Assets/_Project/Scripts/Core/GamePreferences.cs), [CharControlScript](../Assets/_Project/Scripts/Characters/Player/Actions/CharControlScript.cs) | Bindings persistidos, clique/target/ordem de movimento; UI e pausa bloqueiam entrada do mundo |
| Buffer/cancel e habilidades | [AbilityHolder](../Assets/_Project/Scripts/Abilities/AbilityHolder.cs), [CancelResolver](../Assets/_Project/Scripts/Characters/Combat/Core/CancelResolver.cs) | Encaminha executores manopla/arsenal/boons e valida disponibilidade; não criar outro consumidor de QWER/F |
| Básico e cronologia | [BasicAttackDriver](../Assets/_Project/Scripts/Characters/Combat/Core/BasicAttackDriver.cs), [ActionImpactScheduler](../Assets/_Project/Scripts/Characters/Combat/Core/ActionImpactScheduler.cs) | Perfis/janelas/ledger separam intenção e impacto; não aplicar dano em eventos paralelos da animação |
| Dano e notificação | [PlayerActor](../Assets/_Project/Scripts/Characters/Player/PlayerActor.cs), [Actor](../Assets/_Project/Scripts/Characters/Actor.cs) | ResolveAttackDamage confirma HP removido; Actor.HealthChanged, AbilityHolder.AttackHitsResolved e hooks têm papéis distintos; não foi localizado um barramento global chamado DamageEvents |
| Mana, cooldown, Asura | PlayerActor, AbilityHolder, [BreakerGauntletCombat](../Assets/_Project/Scripts/Abilities/Weapon/BreakerGauntletCombat.cs) | Custo validado antes da ativação; estado de energia/cast no executor. Tuning e modificadores nos canônicos de combate/armas |
| Reações/postura/CC | [CombatReactionController](../Assets/_Project/Scripts/Characters/Combat/CombatReactionController.cs), [PostureReactionState](../Assets/_Project/Scripts/Characters/Combat/PostureReactionState.cs) | Resistências, ownership e gates; IA/movimento/feedback consomem o estado |
| IA/telegraphs | [EnemyAI](../Assets/_Project/Scripts/Characters/Enemy/EnemyAI.cs), [TelegraphBeat](../Assets/_Project/Scripts/Characters/Enemy/TelegraphBeat.cs) e comportamentos especializados | Coordenação de vagas, preparação, interrupção e cleanup; consultar o canônico de inimigos |
| Boon autoria/oferta/aplicação | [BoonReworkCatalog](../Assets/_Project/Scripts/Core/Run/BoonReworkCatalog.cs), [BoonOfferCompatibility](../Assets/_Project/Scripts/Core/Run/BoonOfferCompatibility.cs), [RunBoons](../Assets/_Project/Scripts/Core/RunBoons.cs) e suas partes | Asset de autoria não é executor; runtime usa definições executáveis, filtros e inventário. Não criar outro catálogo ativo |
| Impactos derivados | [ProtocolRuntime](../Assets/_Project/Scripts/Characters/Combat/Protocols/ProtocolRuntime.cs), [ProtocolBudget](../Assets/_Project/Scripts/Characters/Combat/Protocols/ProtocolBudget.cs) | Raiz, profundidade, geração e budgets. PlayerActor usa ProtocolRuntime quando presente e fallback onHitEffects no ramo else; não presumir dupla aplicação pelo nome dos sistemas |
| Run/recompensa | [ProgressionDirector.Manual](../Assets/_Project/Scripts/Core/Run/ProgressionDirector.Manual.cs), [RunRewardPolicy](../Assets/_Project/Scripts/Core/Run/RunRewardPolicy.cs) | Sessão/rota, fronteira de encontro e ticket de escolha; callbacks não devem duplicar concessão/retorno |
| HUD/pausa/resultado | [PlayerHUD](../Assets/_Project/Scripts/UI/PlayerHUD.cs), [ManualRunUI](../Assets/_Project/Scripts/UI/ManualRunUI.cs) | UI acompanha estado do player/sessão. Breakdown por habilidade ainda é lacuna, não outro sistema pronto |
| Persistência | [WeaponLoadout](../Assets/_Project/Scripts/Weapons/WeaponLoadout.cs), [CurrencyWallet](../Assets/_Project/Scripts/Core/CurrencyWallet.cs), GamePreferences | PlayerPrefs de arma/desbloqueios, moedas, tutorial/opções; boons de run não viram progressão permanente |

## Assemblies, cenas e carregamento

- [TechGuy.asmdef](../Assets/_Project/Scripts/TechGuy.asmdef) referencia Input System, AI Navigation, TextMeshPro, UI e QuickOutline; Editor, EditMode, PlayMode e suporte têm assemblies separados. Arquivos partial de um mesmo tipo não são sistemas paralelos.
- [EditorBuildSettings](../ProjectSettings/EditorBuildSettings.asset) define cenas de build; a presença de uma cena de estudo na árvore não a torna entrada do jogo. Builders/fixtures podem salvar cenas, tuning e preferências: não executar para uma inspeção só de leitura.
- [WeaponLoadout](../Assets/_Project/Scripts/Weapons/WeaponLoadout.cs) usa chaves Weapons sob `Assets/_Project/Resources`; [CoinPickup](../Assets/_Project/Scripts/Items/CoinPickup.cs) carrega `Items/CoinPickup` sob `Assets/Resources`. Essas raízes coexistem com responsabilidades diferentes.
- [BoonReworkCatalog.asset](../Assets/_Project/ScriptableObjects/Run/BoonReworkCatalog.asset) contém autoria serializada; o grafo de oferta é construído por `ExecutableDefinitions`. Concordância entre fábrica, asset, executor e oferta exige checagem; não resolver drift apagando uma dessas fronteiras automaticamente.
- Observação no Editor: `CombatPolishPreview` partial mantém a sessão/captura/restauração; coletor Editor assina eventos públicos e amostra estado, sem componente ou barramento runtime novo. Cliente MCP e comparação offline em `Tools/GameplayObservation/`; cenário/configuração e limites ficam no canônico de captura.
- Scripts de captura/validação e seus destinos estão em [validation/02_CAPTURA_E_INSPECAO_VISUAL.md](validation/02_CAPTURA_E_INSPECAO_VISUAL.md). Skills são rotas de leitura, não outra fonte de catálogo ou regras.

## Antes de criar uma classe

1. Busca por nome, responsabilidade e callsites (incluindo código legado, scripts editor, asmdefs, assets).
2. Procura por extensibilidade: interface, eventos, Strategy/ScriptableObject/valores de config existentes.
3. Desenha o fluxo do dado e da autoria: quem emite, transforma, consome e persiste?
4. Explica por que não é possível adaptar a implementação existente.
5. Delimita migração/testes das chamadas antigas, se necessário.

## Contratos de integridade Unity

- Metas, GUIDs, `SerializeField`, nomes de campos serializados, `FormerlySerializedAs`, input actions, animações, prefab overrides, cenas, Resources/Addressables, editor assemblies e build scenes precisam de inspeção específica quando afetados.
- Não apagar nem substituir assets/`.meta` de terceiros ou código em `Assets/_ThirdParty`.
- Nunca usar refactor automático de scripts serializados sem verificar referências dos prefabs e cenas afetados no Editor.
- Mudança em save/ID: criar migração explícita, teste de round-trip e compatibilidade com snapshots antigos.
- Pipeline de impacto: preservar causa/raiz e origem nativa ou derivada; verificar procs recursivos, cancelamentos, pausa/hitstop, morte, fim do encontro, troca de arma e desabilitação.
- Evitar mudanças fora da tarefa, especialmente ao normalizar componentes/valores globais do Editor.

## Regras de revisão de código

Sempre medir diffs, nomes e testes alterados. Não acrescentar testes que só reafirmam lógica interna. Exigir testes com comportamento observável, casos-limite e regressão no contrato público. Logs existentes não são provas de testes novos.
