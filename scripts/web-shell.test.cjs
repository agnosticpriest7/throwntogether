const test=require('node:test');
const assert=require('node:assert/strict');
const shell=require('../Assets/WebGLTemplates/Development/web-shell.js');
test('browser button diagnostics report standard indices without changing input',()=>{
  const vm=require('node:vm'),fs=require('node:fs');
  const library={},canvas={},moduleState={canvas};
  const pad={id:'Test controller',mapping:'standard',connected:true,buttons:Array.from({length:17},()=>({pressed:false}))};
  pad.buttons[0].pressed=true; pad.buttons[9].pressed=true;
  vm.runInNewContext(fs.readFileSync(require('node:path').join(__dirname,'../Assets/Plugins/WebGL/WebInputFocus.jslib'),'utf8'),{
    LibraryManager:{library},Module:moduleState,document:{hasFocus:()=>true,hidden:false,activeElement:canvas},mergeInto:Object.assign,stringToNewUTF8:text=>text,navigator:{getGamepads:()=>[null,pad]}
  });
  assert.equal(library.TT_BrowserGamepads(),'Test controller [standard] down=0,9');
  pad.buttons.forEach(b=>b.pressed=false);
  assert.match(library.TT_BrowserGamepads(),/down=none$/);
  pad.buttons[0]={pressed:true,value:0};
  library.TT_SampleGamepadHistory(); library.TT_SampleGamepadHistory();
  pad.buttons[0]={pressed:false,value:0}; library.TT_SampleGamepadHistory();
  assert.match(library.TT_BrowserGamepads(),/A samples=1 last: A pressed=true value=0 focus=true/);
  pad.buttons[0]={pressed:false,value:1}; library.TT_SampleGamepadHistory();
  assert.match(library.TT_BrowserGamepads(),/A samples=2 last: A pressed=false value=1/);
});
test('resize/fullscreen geometry preserves original aspect and fits viewport',()=>{
  for(const [w,h] of [[1280,720],[1920,1080],[3840,2160],[720,1280],[800,600]]) {
    const size=shell.fit(w,h,32);
    assert.equal(size.width/size.height,1.6);
    assert.ok(size.width<=w && size.height<=h-32);
  }
});
test('focus requires active visible document and canvas focus',()=>{
  const canvas={},doc={hasFocus:()=>true,hidden:false,activeElement:canvas};
  assert.equal(shell.hasFocus(doc,canvas),true);
  doc.activeElement={}; assert.equal(shell.hasFocus(doc,canvas),false);
  doc.activeElement=canvas;doc.hidden=true;assert.equal(shell.hasFocus(doc,canvas),false);
  doc.hidden=false;doc.hasFocus=()=>false;assert.equal(shell.hasFocus(doc,canvas),false);
});
function page() {
  class Target {
    constructor() { this.listeners={}; this.style={}; this.dataset={}; }
    addEventListener(type,handler,options) { (this.listeners[type]??=[]).push({handler,options}); }
    dispatch(type,extra={}) {
      const event={button:0,defaultPrevented:false,preventDefault(){this.defaultPrevented=true;},...extra};
      for(const {handler} of this.listeners[type]??[]) handler(event);
      return event;
    }
  }
  const elements=Object.fromEntries(['unity-canvas','game-frame','focus-hint','web-tools','loading'].map(id=>[id,new Target()]));
  elements['web-tools'].offsetHeight=32;
  const doc=Object.assign(new Target(),{hidden:false,hasFocus:()=>true,activeElement:null,getElementById:id=>elements[id]});
  const win=Object.assign(new Target(),{innerWidth:1280,innerHeight:720});
  elements['unity-canvas'].focus=()=>{doc.activeElement=elements['unity-canvas'];};
  return {elements,doc,win};
}
test('native Menu, context menu and right-click are neither cancelled nor refocused',()=>{
  const {elements,doc,win}=page(),canvas=elements['unity-canvas'];
  shell.attach(win,doc).ready();
  for(const type of ['contextmenu','keydown','keyup']) {
    for(const target of [canvas,doc,win]) {
      assert.equal(target.dispatch(type,{code:'ContextMenu'}).defaultPrevented,false);
      assert.equal((target.listeners[type]??[]).length,0);
    }
  }
  canvas.dispatch('pointerdown',{button:2}); assert.equal(doc.activeElement,null);
  canvas.dispatch('pointerdown',{button:0}); assert.equal(doc.activeElement,canvas);
  doc.activeElement=null; win.dispatch('focus'); assert.equal(doc.activeElement,null);
  // The harness intentionally exposes no fullscreen, pointer-lock, gamepad polling or rAF APIs.
});
test('focus requires a loaded visible page and resize accounts for wrapped hint text',()=>{
  const {elements,doc,win}=page(),canvas=elements['unity-canvas'];
  const app=shell.attach(win,doc);
  canvas.dispatch('pointerdown'); assert.equal(doc.activeElement,null);
  app.ready(); doc.hidden=true; canvas.dispatch('pointerdown'); assert.equal(doc.activeElement,null);
  doc.hidden=false; elements['web-tools'].offsetHeight=64; win.dispatch('resize');
  assert.ok(parseFloat(elements['game-frame'].style.height)<=720-64);
});
test('actual development template overrides Unity context-menu suppression before loader startup',async()=>{
  const fs=require('node:fs'),vm=require('node:vm');
  const html=fs.readFileSync(require('node:path').join(__dirname,'../Assets/WebGLTemplates/Development/index.html'),'utf8');
  assert.match(html,/Xbox Edge: hold Menu \(≡\) → Use game controls/);
  assert.doesNotMatch(html,/id="(?:fullscreen|focus-game)"/);
  assert.ok(html.includes('<script src="web-shell.js?build={{{ DATA_FILENAME }}}"></script>'),'Helper cache key must track the built data hash');
  const source=html.match(/<script>\s*([\s\S]*?)<\/script>/)[1]
    .replace(/\{\{\{ JSON.stringify\([^)]+\) \}\}\}/g,'"test"').replace(/\{\{\{[^}]+\}\}\}/g,'test');
  const {doc,win}=page(); let received;
  doc.createElement=()=>({}); doc.body={appendChild:loader=>loader.onload()};
  vm.runInNewContext(source,{document:doc,window:win,ThrownTogetherWeb:shell,performance:{now:()=>1},console,
    createUnityInstance:(_canvas,config)=>{received=config; return Promise.resolve();}});
  await Promise.resolve();
  assert.deepEqual(Array.from(received.disabledCanvasEvents),['dragstart']);
  assert.equal(received.autoSyncPersistentDataPath,true);
});
test('explicit gamepad focus respects browser focus and editable elements',()=>{
 const vm=require('node:vm'),fs=require('node:fs'),path=require('node:path');
 const library={},doc={hasFocus:()=>true,hidden:false,activeElement:{tagName:'BODY'}};
 const canvas={focus:()=>{doc.activeElement=canvas;}};
 vm.runInNewContext(fs.readFileSync(path.join(__dirname,'../Assets/Plugins/WebGL/WebInputFocus.jslib'),'utf8'),{LibraryManager:{library},Module:{canvas},document:doc,mergeInto:Object.assign});
 assert.equal(library.TT_FocusFromGamepad(),true);
 doc.activeElement={tagName:'INPUT'};assert.equal(library.TT_FocusFromGamepad(),false);
 doc.activeElement={tagName:'BODY'};doc.hidden=true;assert.equal(library.TT_FocusFromGamepad(),false);
 doc.hidden=false;doc.hasFocus=()=>false;assert.equal(library.TT_FocusFromGamepad(),false);
});
