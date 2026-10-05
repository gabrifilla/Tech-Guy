# Performance — execução reproduzível

## Fixture e evidência

`ProtocolBenchmark` existe somente no Editor ou Development Player. Seed 501703; prefab Fragile publicado, AI desativada, HP elevado e dano confirmado de 10 a 10 Hz por execução multialvo. Gerações são renovadas a cada 10 segundos. A composição Ignite/Conductor/Frost/Resonance rank 1 é adquirida pelos tickets publicados; somente o cenário stress injeta os sete Protocolos rank 3. Fixtures não constituem aceitação humana nem prova de custo de todos os produtores.

Sem filtro, a matriz tem 54 cenários: baseline/composição/stress × 8/32/64 alvos × VFX ligado/desligado × três repetições. Cada cenário tem 10 segundos de warmup e 60 de coleta, total aproximado de 63 minutos mais aquisição/setup. `-protocol-smoke` reduz tempos e cenários; não aprova D8.

## Development Windows64

Gerar através de Unity MCP `manage_build`, target `windows64`, development `true`, cenas NexusLobby e FirstSector. Não trocar backend persistentemente. Executável de validação atual: `Builds/ProtocolValidation/TechGuy.exe`.

```powershell
Start-Process -FilePath './Builds/ProtocolValidation/TechGuy.exe' -WindowStyle Hidden -ArgumentList @('-protocol-benchmark','-protocol-output','D:/Dev/Personal/My_games/Tech-Guy/Logs/ProtocolBenchmarks/full')
```

Para repetir somente um cenário afetado, manter os tempos D8 e três repetições:

```powershell
Start-Process -FilePath './Builds/ProtocolValidation/TechGuy.exe' -WindowStyle Hidden -ArgumentList @('-protocol-benchmark','-protocol-case','reachable-rank1:32:off','-protocol-output','D:/Dev/Personal/My_games/Tech-Guy/Logs/ProtocolBenchmarks/retest')
```

Filtros válidos: `baseline`, `reachable-rank1`, `stress-all-rank3`; densidade 8/32/64; `on`/`off`. O Player encerra ao concluir; falha de coroutine gera `failure.txt`, status failed e exit 1. Preferências de arma/moedas, frame rate e vsync são restaurados uma única vez. O diagnóstico solicita janela 1920×1080; resultados registram resolução efetivamente observada.

## Dados e limites da leitura

`environment.txt` registra hardware/Unity/backend/qualidade/seed/configuração. `progress.txt` acompanha execução. Cada cenário produz CSV bruto e JSON com percentis nearest rank, recorders válidos, resolução, memória e drops. CPU usa `TechGuy.Protocol.CPU`, sem contar duas vezes scopes internos. GC usa `GC Allocated In Frame` da Unity: **total do frame como limite superior conservador**, incluindo fixture/outros sistemas, não uma atribuição isolada aos Protocolos. Se exceder 1 KB, investigar atribuição no Profiler antes de culpar somente Protocol. O contador .NET retornou zero no probe de alocação desse Mono e foi removido; os zeros dos smokes antigos são inválidos.

Memória inicial/final não basta para provar ausência de crescimento: examinar séries brutas e repetições após warmup. Comparar frame time com baseline de mesma densidade/VFX/repetição; não aprovar meta absoluta se baseline já a excede. Dados em GPU/RAM superiores não comprovam resultado no hardware de referência. Resultados, falhas e jobs pertencem a `validation.md`; task 13 só conclui com as verificações exigidas, sem substituir coleta longa por smoke.
