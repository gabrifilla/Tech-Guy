# Consolidação documental e revisão de exclusões

Esta página registra o escopo da consolidação documental anterior. O pedido posterior autorizou a infraestrutura de observação, documentada em [captura](validation/02_CAPTURA_E_INSPECAO_VISUAL.md); as restrições sobre gameplay, assets e exclusões continuam aplicáveis.

## Decisão vigente — 18/10/2026

O desenvolvedor confirmou que removeu a documentação antiga intencionalmente para reduzir redundância, obsolescência e conflitos. **AGENTS.md e a estrutura atual de Docs são a base oficial.** Ausência de arquivos antigos não é motivo para restaurá-los, recriar requirements/design/tasks/relatórios ou bloquear manutenção. O roteiro anterior de recuperação integral foi substituído por esta decisão explícita.

O escopo autorizado desta consolidação é atualizar documentos existentes e referências de skills/captura/validação. Não inclui alterar gameplay, tuning, saves, cenas, prefabs, ScriptableObjects ou confirmar exclusões pendentes. Nenhum documento antigo foi restaurado.

## Política permanente

- Uma fonte principal por tema, conforme [README](README.md). Skills encaminham aos canônicos; README do projeto resume e aponta; catálogos e parâmetros mutáveis são conferidos no código/assets.
- Atualizar seções existentes com implementação/fonte, resultado de teste, aceite do desenvolvedor ou proposta claramente separados. Requisito de design aceito não vira comportamento entregue por simples redação.
- Conflitos ficam em [02_ESTADO_E_LACUNAS.md](02_ESTADO_E_LACUNAS.md) e no canônico do sistema; não mudar código nem intenção automaticamente.
- Caminhos de captura/validação seguem [02_CAPTURA_E_INSPECAO_VISUAL.md](validation/02_CAPTURA_E_INSPECAO_VISUAL.md). Corrigir produtores e consumidores juntos, sem alterar asserts ou o comportamento dos fixtures.
- Resultados históricos úteis estão sintetizados na [matriz](validation/01_MATRIZ_DE_VALIDACAO.md). Logs locais são proveniência opcional, não dependência documental obrigatória. Não reexecutar exporters de Library para recriar relatórios antigos.

## Exclusões pendentes: revisão separada de código e assets

Leitura de `git status`, blobs do HEAD, usos C# e referências textuais/GUID em 18/10/2026, base `3a3f0ee`, branch `gameplayOverhaul`. Antes desta consolidação havia 546 exclusões: 36 em Docs, 79 em .kiro e 431 fora dessas árvores. **Nenhuma foi revertida, adicionada ao index ou confirmada por esta tarefa.**

| Grupo excluído antes da tarefa | Constatação e risco |
|---|---|
| 193 arquivos C# de testes EditMode | Os nomes removidos não reaparecem entre os 114 arquivos C# de testes restantes (incluem helpers/rigs, não 114 casos). Há perda aparente de testes diretos de cache/pool/throttle/UI/gates/budget; cobertura indireta pode existir. **Revisar a cobertura antes de confirmar a remoção em uma tarefa própria.** |
| `Assets/_Project/Scripts/Weapons/PlayerWeaponBootstrap.cs` | O script antigo reequipava CurrentWeapon em Start. A inicialização atual em [PlayerActor](../Assets/_Project/Scripts/Characters/Player/PlayerActor.cs) já chama EquipWeapon com seleção de WeaponLoadout. Não foram encontrados callsites ou referências serializadas ao GUID removido. Substituição aparenta coerente; isso não certifica equivalência em todas as cenas. |
| `Assets/_Project/Scripts/Core/Run/ProtocolRewardSelection.cs` | A seleção antiga sequenciava Protocols. O fluxo atual de [RunBoons.Manual](../Assets/_Project/Scripts/Core/Run/RunBoons.Manual.cs) usa [ManualRewardCatalog](../Assets/_Project/Scripts/Core/Run/ManualRewardCatalog.cs) com a política de tickets. Sem uso C#/GUID localizado, mas a compatibilidade do caminho legado não foi revalidada no Editor. |
| Duas cenas `Assets/InitTestScene*.unity` | Nomes de cenas transitórias do Test Runner; ausentes dos Build Settings atuais e sem referências serializadas aos GUIDs. Não restaurar ou atestar descarte só pelo nome; geração de fixtures precisa ser verificada se os testes afetados forem executados. |
| 16 PNG de screenshots e seus metas | Não foram localizadas referências serializadas aos GUIDs removidos. A documentação não exige essas imagens antigas; novas capturas usam destinos oficiais. Isso não comprova que uma evidência visual anterior tenha sido reproduzida. |
| 218 arquivos .meta | Incluem pares das fontes/cenas/imagens acima e metas de diretório. Scan de 1.677 arquivos textuais/serializados vivos em Assets, ProjectSettings e Packages encontrou zero referências aos GUIDs excluídos; não cobre reflexão, strings de Resources, estado do Editor ou código externo. |
| Prefabs, ScriptableObjects, Packages e ProjectSettings | Nenhuma exclusão desses arquivos foi encontrada no status desta revisão. Modificações locais já existentes nessas áreas não foram revertidas nem aprovadas. |
| `.kiro/settings/mcp.json` | Configuração de ferramenta removida junto da árvore antiga. Não restaurada. Sua ausência não elimina o pacote Unity MCP; conexão atual deve ser descoberta pelas ferramentas disponíveis, sem supor que configuração antiga ainda é válida. |

Os grupos se sobrepõem em pares de .meta; não somar cada linha como artefatos independentes. Entre os 195 C# excluídos fora de Docs/.kiro, 193 são testes e dois são os scripts acima.

### Cobertura que precisa de revisão

Os tipos `WeaponLoadout.WeaponCache`, `SimpleObjectPool`, `FrameThrottle`, `UiValueCache`, `RunSelectionReleaseGate`, `ControlLockGate` e `ProtocolBudget` continuam no código; a busca por seus nomes não encontrou menções diretas nas fontes de testes restantes. Isso é sinal de risco, **não prova de ausência de toda cobertura indireta**. Não recriar testes removidos ou relaxar asserts como efeito colateral desta documentação.

## Verificação da consolidação

Conferir links Markdown locais, referências de skills, produtores/consumidores de artefatos e manifest documental; comparar a lista de exclusões e os hashes dos arquivos de gameplay com o estado inicial. Alterações permitidas em C#/Python nesta tarefa limitam-se a caminhos/diretórios de saída de captura e validação; não são validação de compilação Unity nem mudança de funcionalidade do jogo.
