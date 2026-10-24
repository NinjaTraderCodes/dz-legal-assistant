const fs=require('node:fs'),path=require('node:path'),assert=require('node:assert/strict');
function luminance(hex){const rgb=[1,3,5].map(i=>parseInt(hex.slice(i,i+2),16)/255).map(c=>c<=.04045?c/12.92:((c+.055)/1.055)**2.4);return rgb[0]*.2126+rgb[1]*.7152+rgb[2]*.0722;}
function blend(foreground,background,alpha){return '#'+[1,3,5].map(i=>Math.round(parseInt(foreground.slice(i,i+2),16)*alpha+parseInt(background.slice(i,i+2),16)*(1-alpha)).toString(16).padStart(2,'0')).join('');}
const glassUpper=blend('#3C352B','#5C4932',212/255);
const pairs=[
 ['Document ink / paper','#27241F','#FFFEFC',4.5],['Page number / paper','#77736D','#FFFEFC',4.5],
 ['Field controls / blue-grey strip','#344D5A','#DFE7E9',4.5],['Field action / button','#344D5A','#F0F4F5',4.5],
 ['Outline label / solid panel','#D8D0C4','#2C261E',4.5],['Outline number / solid panel','#AA9C87','#2C261E',4.5],
 ['Current section / selected row','#F1D7A4','#423724',4.5],['Primary text / brightest glass','#F2EDE5',glassUpper,4.5],
 ['Muted text / brightest glass','#C2B8A9',glassUpper,4.5],['Gold heading / brightest glass','#C6A76D',glassUpper,4.5],
 ['Review strip / solid surface','#E6D5B2','#28231C',4.5],
];
const measured=pairs.map(([name,foreground,background,minimum])=>{const a=luminance(foreground),b=luminance(background),ratio=(Math.max(a,b)+.05)/(Math.min(a,b)+.05);assert.ok(ratio>=minimum,name+': '+ratio);return{name,foreground,background,minimum,ratio:Number(ratio.toFixed(2))};});
fs.writeFileSync(path.join(__dirname,'contrast.json'),JSON.stringify({method:'sRGB relative luminance; selected static text pairs. Glass upper color is the brightest tinted-panel stop composited over the brightest ambient stop. Not complete rendered-state certification.',measured},null,2));
console.log(`${measured.length} selected contrast pairs passed; brightest glass composite ${glassUpper}.`);
