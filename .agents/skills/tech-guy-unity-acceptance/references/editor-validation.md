# Procedimento de Editor / MCP for Unity

A [matriz de validação](../../../../Docs/validation/01_MATRIZ_DE_VALIDACAO.md) define critérios e semântica dos resultados. [Captura e inspeção](../../../../Docs/validation/02_CAPTURA_E_INSPECAO_VISUAL.md) define cenários e artefatos; [telemetria](../../../../Docs/validation/03_TELEMETRIA_E_GAME_FEEL.md) e [playtest](../../../../Docs/validation/04_CHECKLIST_PLAYTEST.md) complementam a evidência.

## Antes de executar

- Confira `git status`, versão em `ProjectSettings/ProjectVersion.txt`, instância/projeto, cena aberta, modo e Console inicial. Preserve logs; não limpe erros para obter resultado aparente.
- Descubra capacidades reais do MCP, servidor e bridge. Se indisponíveis, registre a limitação concreta e avance apenas nas verificações que não dependem do Editor.
- Não dispute o projeto com outra instância: batchmode não deve usar o mesmo checkout aberto no Editor interativo.

## Execução proporcional

- Selecione EditMode/PlayMode e casos reais conforme o contrato alterado; testes antigos ou nomes de fases não garantem cobertura da revisão corrente.
- Para dados serializados, confira GUIDs, referências e campos no Inspector; salvar/reabrir e round-trip são verificações Unity próprias, não leitura de YAML.
- Para combate/input, cubra estados e alvos relevantes, cancelamentos, pausa/hitstop, recursos e teardown; para boons, compatibilidade/oferta e efeito no encontro; para UI, Game View com dados e resolução representativos. Use os documentos canônicos de cada sistema para requisitos.
- Registre revisão e alterações locais relevantes, versão Unity, cenário, filtros/comandos, resultado, Console, artefatos e limites de cobertura. Evidência histórica fica identificada como histórica.

## Fechamento

Saia de Play Mode e restaure cena/seleção/variáveis temporárias quando possível. Inspecione Console e diff, exponha efeitos persistentes da validação e separe pendências técnicas, observação em gameplay e aprovação autoral. `dotnet build` é auxiliar; não valida AssetDatabase, MonoBehaviours, física ou importação Unity.
