// Original deterministic synthesis; no recordings or third-party audio. Node 20+.
const fs=require('node:fs'), path=require('node:path');
const root=path.resolve(__dirname,'../Assets/Audio/Placeholders');
fs.mkdirSync(root,{recursive:true});
const cues={pickup:[.12,660,.05],place:[.12,180,.2],chop:[.26,120,.7],fryerStart:[.3,90,.85],sizzle:[1,0,1],complete:[.3,880,.01],plate:[.15,1100,.03],success:[.45,660,.01],uiClick:[.06,500,.02]};
for(const [name,[duration,frequency,noise]] of Object.entries(cues)) for(let variant=0;variant<(name==='sizzle'?1:2);variant++) {
  const rate=22050,n=Math.round(duration*rate),wav=Buffer.alloc(44+n*2); let seed=12345+variant*97;
  wav.write('RIFF');wav.writeUInt32LE(36+n*2,4);wav.write('WAVEfmt ',8);wav.writeUInt32LE(16,16);wav.writeUInt16LE(1,20);wav.writeUInt16LE(1,22);wav.writeUInt32LE(rate,24);wav.writeUInt32LE(rate*2,28);wav.writeUInt16LE(2,32);wav.writeUInt16LE(16,34);wav.write('data',36);wav.writeUInt32LE(n*2,40);
  for(let i=0;i<n;i++) {
    seed=(Math.imul(seed,1664525)+1013904223)>>>0;
    const t=i/rate,edge=Math.min(1,i/220,(n-1-i)/220);
    const envelope=name==='sizzle' ? edge : edge*Math.exp(-t*6/duration);
    const tone=Math.sin(2*Math.PI*frequency*(1+variant*.015)*t);
    const sample=.16*envelope*((1-noise)*tone+noise*(seed/2147483648-1));
    wav.writeInt16LE(Math.round(sample*32767),44+i*2);
  }
  fs.writeFileSync(path.join(root,`${name}${variant+1}.wav`),wav);
}
console.log('Generated 17 original placeholder WAVs (mono PCM16, 22050 Hz).');
