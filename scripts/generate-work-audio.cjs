// Original procedural PCM sound effects; no external recordings.
const fs=require('fs');const folder='Assets/Audio/WorkSounds';fs.mkdirSync(folder,{recursive:true});
let seed=1287;const noise=()=>{seed=(Math.imul(seed,1664525)+1013904223)>>>0;return seed/2147483648-1;};
function wav(name,duration,sample){const rate=22050,n=Math.round(rate*duration),b=Buffer.alloc(44+n*2);b.write('RIFF');b.writeUInt32LE(b.length-8,4);b.write('WAVEfmt ',8);b.writeUInt32LE(16,16);b.writeUInt16LE(1,20);b.writeUInt16LE(1,22);b.writeUInt32LE(rate,24);b.writeUInt32LE(rate*2,28);b.writeUInt16LE(2,32);b.writeUInt16LE(16,34);b.write('data',36);b.writeUInt32LE(n*2,40);let low=0;for(let i=0;i<n;i++){let t=i/rate;low=low*.83+noise()*.17;const v=sample(t,low)*Math.min(1,t*300,(duration-t)*100);b.writeInt16LE(Math.round(Math.max(-1,Math.min(1,v))*32767),44+i*2);}fs.writeFileSync(`${folder}/${name}.wav`,b);}
for(let i=0;i<2;i++){wav('footstep'+i,.16,(t,n)=>Math.exp(-t*35)*(n*.55+Math.sin(t*(120+i*12)*Math.PI*2)*.14));wav('plate'+i,.32,(t,n)=>Math.exp(-t*22)*(Math.sin(t*(1300+i*70)*Math.PI*2)*.22+Math.sin(t*2141*Math.PI*2)*.10+n*.1));}
wav('washing',1.5,(t,n)=>n*(.22+.08*Math.sin(t*Math.PI*4)));
