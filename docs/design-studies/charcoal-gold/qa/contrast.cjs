const fs=require('node:fs'),path=require('node:path'),assert=require('node:assert/strict');
function luminance(hex){const rgb=[1,3,5].map(i=>parseInt(hex.slice(i,i+2),16)/255).map(c=>c<=.04045?c/12.92:((c+.055)/1.055)**2.4);return rgb[0]*.2126+rgb[1]*.7152+rgb[2]*.0722;}
const pairs=[
 ['Primary text / panel','#F2EDE5','#24201B',4.5],['Primary text / raised panel','#F2EDE5','#302A23',4.5],
 ['Secondary text / panel','#C2B8A9','#24201B',4.5],['Secondary text / raised panel','#C2B8A9','#302A23',4.5],
 ['Gold heading / panel','#C6A76D','#24201B',4.5],['Primary button text / gold','#28231D','#C6A76D',4.5],
 ['Document ink / paper','#28231D','#FFFEFA',4.5],['Document metadata / paper','#6F6252','#FFFEFA',4.5],
 ['Document action / paper','#78561F','#FFFEFA',4.5],['Document warning / paper','#80501C','#FFFEFA',4.5],
 ['Error / error surface','#FFC0B3','#462C27',4.5],['Warning / warning surface','#F2CB83','#43331E',4.5],
 ['Success / panel','#ACD3BD','#302A23',4.5],['Input border / input','#8C7B65','#302A23',3],
];
const measured=pairs.map(([name,foreground,background,minimum])=>{const a=luminance(foreground),b=luminance(background),ratio=(Math.max(a,b)+.05)/(Math.min(a,b)+.05);assert.ok(ratio>=minimum,name+': '+ratio);return{name,foreground,background,minimum,ratio:Number(ratio.toFixed(2))};});
fs.writeFileSync(path.join(__dirname,'contrast.json'),JSON.stringify({method:'sRGB relative luminance; proposed tokens only, not full rendered-state certification',measured},null,2));
console.log(measured.map(p=>`${p.name}: ${p.ratio}:1`).join('\n'));
