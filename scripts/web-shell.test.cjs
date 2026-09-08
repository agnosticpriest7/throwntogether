const test=require('node:test');
const assert=require('node:assert/strict');
const shell=require('../Assets/WebGLTemplates/Development/web-shell.js');
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
test('only focused gameplay scroll keys are prevented; browser shortcuts remain',()=>{
  for(const code of ['ArrowUp','ArrowDown','ArrowLeft','ArrowRight','Space']) {
    assert.equal(shell.blocksScroll({code},true),true);
    assert.equal(shell.blocksScroll({code},false),false);
    assert.equal(shell.blocksScroll({code,ctrlKey:true},true),false);
  }
  for(const code of ['Tab','Escape','F11','KeyR']) assert.equal(shell.blocksScroll({code},true),false);
});
test('focus gesture requires connected standard-mapped south button',()=>{
  const pad={connected:true,mapping:'standard',buttons:[{pressed:true}]};
  assert.equal(shell.southPressed([null,pad]),true);
  assert.equal(shell.southPressed([{...pad,connected:false}]),false);
  assert.equal(shell.southPressed([{...pad,mapping:''}]),false);
  assert.equal(shell.southPressed([{...pad,buttons:[{pressed:false}]}]),false);
});
