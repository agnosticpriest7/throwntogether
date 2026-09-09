const {test}=require('node:test');
const assert=require('node:assert/strict');
const vm=require('node:vm');
const fs=require('node:fs');
function setup(reject=false) {
  const listeners={},warnings=[], library={}; let count=0;
  class Audio {
    constructor(){count++;this.paused=true;this.listeners={};}
    addEventListener(name,fn){this.listeners[name]=fn;}
    play(){if(reject)return Promise.reject({name:'NotAllowedError'});this.paused=false;return Promise.resolve();}
    pause(){this.paused=true;}
  }
  const context={window:{},document:{body:{appendChild:()=>{}},hidden:false,addEventListener:(name,fn,options)=>{listeners[name]={fn,options};}},Audio,
    LibraryManager:{library},mergeInto:Object.assign,UTF8ToString:x=>x,console:{warn:(...args)=>warnings.push(args)}};
  vm.runInNewContext(fs.readFileSync('Assets/Plugins/WebGL/BackgroundMusic.jslib','utf8'),context);
  return {context,library,listeners,warnings,count:()=>count};
}
test('music starts at sixty percent, uses supplied relative-path URLs and remains singleton',()=>{
  const s=setup();s.library.TT_MusicStart('StreamingAssets/Music/a.ogg','StreamingAssets/Music/b.ogg');
  assert.equal(s.context.window.ttMusic.volume,.6);assert.equal(s.context.window.ttMusic.src,'StreamingAssets/Music/a.ogg');
  s.library.TT_MusicStart('a','b');assert.equal(s.count(),1);
});
test('playlist alternates and wraps; settings control the existing voice',async()=>{
  const s=setup();s.library.TT_MusicVolume(.24);s.library.TT_MusicStart('a','b');await Promise.resolve();
  const a=s.context.window.ttMusic;assert.equal(a.volume,.24);a.paused=true;a.listeners.ended();await Promise.resolve();assert.equal(a.src,'b');
  a.paused=true;a.listeners.ended();assert.equal(a.src,'a');s.library.TT_MusicVolume(0);assert.equal(a.volume,0);
});
test('autoplay denial is handled quietly and focus helpers never capture controls',async()=>{
  const s=setup(true);s.library.TT_MusicStart('a','b');await Promise.resolve();
  assert.equal(s.warnings.length,0);assert.equal(s.listeners.pointerdown.options.passive,true);
  assert.equal(s.listeners.keydown.options.passive,true);assert.equal(s.listeners.contextmenu,undefined);
  s.listeners.pointerdown.fn();await Promise.resolve();assert.equal(s.warnings.length,0);
});
test('hidden tabs pause; returning resumes',async()=>{
  const s=setup();s.library.TT_MusicStart('a','b');await Promise.resolve();
  s.context.document.hidden=true;s.listeners.visibilitychange.fn();assert.equal(s.context.window.ttMusic.paused,true);
  s.context.document.hidden=false;s.listeners.visibilitychange.fn();assert.equal(s.context.window.ttMusic.paused,false);
});
