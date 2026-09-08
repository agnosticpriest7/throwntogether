(function(root,factory) {
  const api=factory();
  if(typeof module==="object" && module.exports) module.exports=api;
  else root.ThrownTogetherWeb=api;
})(typeof window!=="undefined" ? window : globalThis,function() {
  "use strict";
  function fit(width,height,toolbarHeight) {
    const w=Math.max(1,Math.min(width,Math.max(1,height-toolbarHeight)*1.6));
    return {width:w,height:w/1.6};
  }
  function hasFocus(doc,canvas) { return doc.hasFocus() && !doc.hidden && doc.activeElement===canvas; }
  function attach(win,doc) {
    const canvas=doc.getElementById("unity-canvas"), frame=doc.getElementById("game-frame");
    const hint=doc.getElementById("focus-hint"), tools=doc.getElementById("web-tools");
    let ready=false;
    function resize() {
      const size=fit(win.innerWidth,win.innerHeight,tools.offsetHeight);
      frame.style.width=size.width+"px"; frame.style.height=size.height+"px";
    }
    function updateFocus() {
      hint.textContent=hasFocus(doc,canvas) ? "Game focused · F3 / DEV" : "Click game to focus";
    }
    // Only a normal primary click focuses the canvas. Never capture Menu/right-click,
    // poll controller buttons, lock the pointer, or request fullscreen here.
    canvas.addEventListener("pointerdown",event=>{
      if(event.button===0 && ready && doc.hasFocus() && !doc.hidden) canvas.focus({preventScroll:true});
    });
    doc.addEventListener("focusin",updateFocus); doc.addEventListener("focusout",updateFocus);
    win.addEventListener("focus",updateFocus); win.addEventListener("blur",updateFocus);
    doc.addEventListener("visibilitychange",updateFocus);
    win.addEventListener("resize",resize);
    resize(); updateFocus();
    return {ready:()=>{ready=true;updateFocus();resize();}};
  }
  return {fit,hasFocus,attach};
});
