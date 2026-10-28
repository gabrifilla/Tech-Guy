// Compare actual Unity geometry with the spatial portion of the original reference.
const path = require('path');
const sharp = require('sharp');
const root = path.resolve(__dirname, '../../..');
const folder = path.join(root, 'Library/GameplayObservation/vs034-revision');
const text = (value,w,h,size=23) => Buffer.from(`<svg width="${w}" height="${h}" xmlns="http://www.w3.org/2000/svg"><rect width="100%" height="100%" fill="#101720"/><text x="24" y="${h/2+size/3}" fill="#e5eef2" font-family="Arial" font-size="${size}">${value}</text></svg>`);
async function sheet(file,title,items) {
  const layers=[{input:text(title,1920,64),left:0,top:0}];
  for(let i=0;i<items.length;i++) {
    const x=i%2*960,y=64+Math.floor(i/2)*582;
    layers.push({input:text(items[i][0],960,42,20),left:x,top:y});
    layers.push({input:await sharp(path.join(folder,'final-1080',items[i][1]+'.png')).resize(960,540).png().toBuffer(),left:x,top:y+42});
  }
  await sharp({create:{width:1920,height:1228,channels:4,background:'#101720'}}).composite(layers).png().toFile(path.join(folder,file));
}
async function main() {
  const reference=await sharp(path.join(root,'Library/GameplayObservation/vs033-direction/nexus-direction.png'))
    .extract({left:795,top:170,width:780,height:720}).resize(1040,960).png().toBuffer();
  const overhead=await sharp(path.join(folder,'topdown-no-labels.png')).resize(1120,1120).png().toBuffer();
  await sharp({create:{width:2200,height:1280,channels:4,background:'#101720'}}).composite([
    {input:text('VS-03.4 revisão · Referência espacial original × planta realmente construída no Unity',2200,64,28),left:0,top:0},
    {input:text('PLANTA ORIGINAL · recorte da proposta aprovada',1060,54,23),left:0,top:64},
    {input:text('UNITY · vista vertical · UI e rótulos ocultos',1140,54,23),left:1060,top:64},
    {input:reference,left:10,top:160},{input:overhead,left:1070,top:118},
    {input:text('Reconexão SE · Oficina oeste · Treino NW · Travessia NE · Vazios reais e conexões curtas · Gameplay40°',2200,42,21),left:0,top:1238}
  ]).png().toFile(path.join(folder,'reference-comparison.png'));
  await sheet('four-areas.png','Capturas reais · câmera de gameplay40° · input virtual · 1920×1080',[
    ['Reconexão · chegada e retorno','reconexao'],['Oficina · bancada e foco único','oficina'],
    ['Treino · área aberta e dano no alvo','treino'],['Travessia · terminal de partida','travessia']]);
  await sheet('connections.png','Percurso real entre os volumes · movimento e picking de produção · câmera40°',[
    ['Reconexão → patamar de preparação','arrival-connection'],['Acesso direto → patamar de Travessia','north-connection'],
    ['Oficina → Treino','practice-connection'],['Treino → Travessia','practice-departure-connection']]);
  console.log('Created reference-comparison.png, four-areas.png, connections.png.');
}
main().catch(error=>{console.error(error);process.exitCode=1;});
