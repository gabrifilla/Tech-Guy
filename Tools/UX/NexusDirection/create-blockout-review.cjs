// Presentation of actual Unity captures; source PNGs are never overwritten.
const fs = require('fs');
const path = require('path');
const sharp = require('sharp');
const root = path.resolve(__dirname, '../../..');
const folder = path.join(root, 'Library/GameplayObservation/vs034');
const label = (text, width, height, size = 22) => Buffer.from(`<svg width="${width}" height="${height}" xmlns="http://www.w3.org/2000/svg"><rect width="100%" height="100%" fill="#0c1220"/><text x="24" y="${height / 2 + size / 3}" fill="#e2ebf3" font-family="Arial" font-size="${size}">${text}</text></svg>`);
async function tile(file, width = 960, height = 540) { return sharp(path.join(folder, file)).resize(width, height).png().toBuffer(); }
async function main() {
  const panels = [
    ['Reconexão · chegada, origem Terra e resultados', 'reconexao'],
    ['Oficina · três armas e detalhes por intenção', 'oficina'],
    ['Treino · alvo existente e HUD contextual', 'treino'],
    ['Travessia · única saída jogável: FirstSector', 'travessia'],
  ];
  const layers = [{ input: label('VS-03.4 · Blockout isolado · Capturas reais de gameplay em 1920 × 1080 · Câmera 40°', 1920, 64), left: 0, top: 0 }];
  for (let i = 0; i < panels.length; i++) {
    const x = i % 2 * 960, y = 64 + Math.floor(i / 2) * 582;
    layers.push({ input: label(panels[i][0], 960, 42, 20), left: x, top: y });
    layers.push({ input: await tile(`final-1080/${panels[i][1]}.png`), left: x, top: y + 42 });
  }
  await sharp({ create: { width: 1920, height: 1228, channels: 4, background: '#0c1220' } }).composite(layers).png().toFile(path.join(folder, 'four-areas.png'));
  await sharp({ create: { width: 1920, height: 686, channels: 4, background: '#0c1220' } }).composite([
    { input: label('Comparação real no Unity · Mesma câmera temporária de visão geral · Gameplay permanece em 40°', 1920, 64), left: 0, top: 0 },
    { input: label('NexusLobby atual · preservada', 960, 42, 21), left: 0, top: 64 },
    { input: label('NexusBlockout · para avaliação jogando', 960, 42, 21), left: 960, top: 64 },
    { input: await tile('overview-NexusLobby.png'), left: 0, top: 106 },
    { input: await tile('overview-NexusBlockout.png'), left: 960, top: 106 },
    { input: label('Render de Editor sem UI · posição (-5,36,-22), alvo (-5,0,2), ortográfica 14 · 24/10/2026', 1920, 40, 18), left: 0, top: 646 },
  ]).png().toFile(path.join(folder, 'comparison.png'));
  console.log('Created four-areas.png and comparison.png from unmodified Unity captures.');
}
main().catch(error => { console.error(error); process.exitCode = 1; });
