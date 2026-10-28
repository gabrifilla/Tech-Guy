// Review artifact only. Does not write Unity assets, scenes or preferences.
const fs = require('fs');
const path = require('path');
const sharp = require('sharp');
const root = path.resolve(__dirname, '../../..');
const output = path.join(root, 'Library/GameplayObservation/vs033-direction');
fs.mkdirSync(output, { recursive: true });
const reference = fs.readFileSync(path.join(root, 'Library/GameplayObservation/vs032/after-delivery-720/arsenal.png')).toString('base64');
const text = (x,y,s,size=19,fill='#d7e0ea',weight=400) => `<text x="${x}" y="${y}" font-size="${size}" fill="${fill}" font-weight="${weight}">${s}</text>`;
const room = (x,y,w,h,title,subtitle,fill) => `<rect x="${x}" y="${y}" width="${w}" height="${h}" rx="5" fill="${fill}" stroke="#a8b8c8" stroke-width="7"/>${text(x+18,y+31,title,21,'#fff',700)}${text(x+18,y+57,subtitle,15)}`;
const player = (x,y) => `<circle cx="${x}" cy="${y}" r="8" fill="#dce3ea"/><path d="M${x},${y+9}v35m0,-22l-13,17m13,-17l13,17m-13,5l-10,24m10,-24l10,24" stroke="#dce3ea" stroke-width="7" stroke-linecap="round"/>`;
const svg = `<svg xmlns="http://www.w3.org/2000/svg" width="1600" height="1120" viewBox="0 0 1600 1120">
<rect width="1600" height="1120" fill="#111820"/>
<g font-family="Arial, sans-serif">
${text(44,39,'ESTUDO DE EXPERIÊNCIA · PROPOSTA PARA APROVAÇÃO · NÃO IMPLEMENTADO NO UNITY',17,'#e8b971',700)}
${text(44,94,'Nexus: preparar-se e continuar a busca por casa',38,'#f4f4ef',700)}
${text(44,130,'Salas com função, leitura por intenção e armas na escala do personagem. Arte final e medidas ainda em estudo.',20)}
<path d="M44 155H1556" stroke="#53616f"/>
${text(44,196,'ATUAL — CAPTURA REAL DO UNITY',20,'#e8b971',700)}
${text(44,222,'Game View 720p · referência da VS-03.2, capturada em22/10',16,'#a9bacb')}
<image x="44" y="243" width="690" height="388" href="data:image/png;base64,${reference}"/>
${text(44,675,'Problemas apontados pelo desenvolvedor',24,'#f4f4ef',700)}
${text(44,711,'• Atividades concentradas e sem espaços distintos.')}
${text(44,744,'• Cards automáticos grandes e informação competindo.')}
${text(44,777,'• Armas expostas com proporção de ícone gigante.')}
${text(44,810,'• Objetos e portais sem papel claro na busca pela Terra.')}
${text(44,860,'PROPOSTA DE INTERAÇÃO NO ARSENAL',18,'#e8b971',700)}
<rect x="44" y="882" width="330" height="93" rx="7" fill="#25323d" stroke="#8093a3"/>
${text(62,913,'ARCO',23,'#fff',700)}${text(62,941,'Disponível · precisão e distância',16,'#b9d6b6')}
${text(62,963,'[E] equipar · detalhes por escolha explícita',14)}
${text(400,912,'Proximidade: nome e ação.',18)}
${text(400,942,'Kit completo: somente ao pedir.',18)}
${text(400,970,'Sem painel extenso ao passar.',18)}
${text(44,1022,'Não copiar salas ou assets de Hades:',20,'#f4f4ef',700)}
${text(44,1053,'usar como referência de escala, composição e objetos com sentido.',18)}

${text(800,196,'PROPOSTA — PLANTA FUNCIONAL',20,'#8dcfd8',700)}
${text(800,222,'Esquema de adjacências; não é render nem layout validado.',16,'#a9bacb')}
<!-- Shared circulation first; rooms overlap it at wide thresholds. -->
<path d="M1225 728V555H966V383M1125 555V365H1340" fill="none" stroke="#344552" stroke-width="72" stroke-linejoin="round"/>
${room(812,251,274,157,'TREINO','Como domino esta arma?','#253c3a')}
${room(812,457,274,191,'OFICINA / ARSENAL','Com o que vou atravessar?','#3e3430')}
${room(1178,251,367,252,'TRAVESSIA','Continuar a busca pela Terra','#243b49')}
${room(1122,650,365,157,'RECONEXÃO','Onde estou? Como volto?','#343243')}
<!-- Wide, readable openings; no gates or mandatory sequence. -->
<path d="M932 408h62M932 457h62M1086 529v55M1122 705v55M1280 650h70M1178 340v70" stroke="#344552" stroke-width="12"/>
<path d="M1305 704V555H1125V375H1250" fill="none" stroke="#e5d5ab" stroke-width="4" stroke-dasharray="10 8"/>
<path d="M1215 728V572H966V385" fill="none" stroke="#7ebbb5" stroke-width="4"/>
<path d="M958 399l8,-14l8,14M1235 367l15,8l-15,8" fill="none" stroke="#d9e5de" stroke-width="3"/>
<circle cx="1359" cy="374" r="46" fill="#152832" stroke="#75dae0" stroke-width="7"/>
<circle cx="1359" cy="374" r="32" fill="none" stroke="#386974" stroke-width="2"/>
${text(1281,465,'Única saída jogável em destaque',15,'#a8d9df')}
<rect x="944" y="335" width="26" height="39" rx="13" fill="#a6c9bd"/>
<path d="M937 378h41" stroke="#8dc5b6" stroke-width="3"/>
<circle cx="875" cy="583" r="21" fill="#c19a58"/><circle cx="947" cy="583" r="21" fill="#589fa9"/><circle cx="1019" cy="583" r="21" fill="#a46c83"/>
${text(863,590,'M',19,'#14212b',700)}${text(940,590,'A',19,'#14212b',700)}${text(1013,590,'L',19,'#14212b',700)}
${text(836,625,'Objetos ao alcance; escala de uso.',15)}
<rect x="1144" y="752" width="114" height="33" fill="#4b4559" stroke="#a9a1c0"/>
${text(1153,774,'Última incursão',13,'#f3f1ed')}
${text(1273,775,'Origem: Terra',16,'#d6cbe4')}
${text(812,847,'— Preparação opcional',17,'#94c9c0')}${text(1135,847,'- - Acesso rápido à próxima run',17,'#e5d5ab')}
${text(812,883,'Sem ordem obrigatória. Sem NPCs ou progressão nova.',17)}
${text(812,932,'ESTUDO DE PROPORÇÃO, NÃO NOVOS MODELOS',17,'#e8b971',700)}
${player(871,958)}${player(1094,958)}${player(1320,958)}
<path d="M881 980l12,3l4,19l-14,4z" fill="#d7ad69"/>
<path d="M1114 964Q1154 998 1114 1033M1114 964v69" fill="none" stroke="#7ed3dc" stroke-width="4"/>
<path d="M1343 1042v-90m0,-11l-8,13h16z" fill="#b499bc" stroke="#b499bc" stroke-width="3"/>
${text(824,1074,'Manopla / antebraço',15)}${text(1057,1074,'Arco / silhueta legível',15)}${text(1270,1074,'Lança / arma de haste',15)}
${text(812,1102,'Câmera40° e sistemas atuais preservados até aprovação do blockout.',15,'#a9bacb')}
</g></svg>`;
fs.writeFileSync(path.join(output, 'nexus-direction.svg'), svg);
sharp(Buffer.from(svg)).png().toFile(path.join(output, 'nexus-direction.png')).then(()=>process.stdout.write(output+'\n')).catch(e=>{process.stderr.write(String(e));process.exitCode=1;});
