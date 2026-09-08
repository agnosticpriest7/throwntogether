(function(root,factory) {
  const api=factory();
  if(typeof module==="object" && module.exports) module.exports=api;
  else root.ThrownTogetherWeb=api;
})(typeof window!=="undefined" ? window : globalThis,function() {
  "use strict";
  function fit(width,height,toolbarHeight) {
    const w=Math.max(1,Math.min(width,Math.max(1,height-toolbarHeight)*1.6));
    return {width:w,height:w/1.6}; // Preserve the original 960x600 camera aspect.
  }
  function hasFocus(doc,canvas) { return doc.hasFocus() && !doc.hidden && doc.activeElement===canvas; }
  function blocksScroll(event,focused) {
    return focused && !event.ctrlKey && !event.metaKey && !event.altKey && ["ArrowUp","ArrowDown","ArrowLeft","ArrowRight","Space"].includes(event.code);
  }
  function southPressed(pads) { return pads.some(p=>p && p.connected && p.mapping==="standard" && p.buttons[0] && p.buttons[0].pressed); }
  function attach(win,doc) {
    const canvas=doc.getElementById("unity-canvas"), frame=doc.getElementById("game-frame");
    const host=doc.getElementById("web-host"), hint=doc.getElementById("focus-hint"), full=doc.getElementById("fullscreen");
    let ready=false, previousSouth=false;
    function focus() { if(ready && doc.hasFocus() && !doc.hidden) canvas.focus({preventScroll:true}); }
    function resize() {
      const size=fit(win.innerWidth,win.innerHeight,32);
      frame.style.width=size.width+"px"; frame.style.height=size.height+"px";
    }
    function updateFocus() {
      hint.textContent=hasFocus(doc,canvas) ? "Game focused · F3 / DEV diagnostics" : "Click game / press A to focus (activate this tab first)";
    }
    canvas.addEventListener("pointerdown",focus);
    doc.getElementById("focus-game").addEventListener("click",focus);
    doc.addEventListener("keydown",event=>{ if(blocksScroll(event,hasFocus(doc,canvas))) event.preventDefault(); },true);
    doc.addEventListener("focusin",updateFocus); doc.addEventListener("focusout",updateFocus);
    win.addEventListener("focus",updateFocus); win.addEventListener("blur",updateFocus);
    doc.addEventListener("visibilitychange",updateFocus);
    win.addEventListener("resize",resize);
    doc.addEventListener("fullscreenchange",()=>{resize();full.textContent=doc.fullscreenElement ? "Exit fullscreen" : "Fullscreen";focus();});
    full.disabled=!doc.fullscreenEnabled;
    full.title=full.disabled ? "Fullscreen is unavailable in this browser; use the browser's own fullscreen command" : "Development fullscreen (Escape exits)";
    full.addEventListener("click",async()=>{
      try {
        if(doc.fullscreenElement) await doc.exitFullscreen();
        else await host.requestFullscreen();
      } catch(error) { hint.textContent="Fullscreen unavailable: "+error.name+". Use browser fullscreen."; }
    });
    // Poll only to focus an already-active page. Unity remains the sole gameplay input consumer.
    function poll() {
      let pads=[];
      try { if(win.navigator.getGamepads) pads=Array.from(win.navigator.getGamepads()); } catch (_) { /* Unity diagnostics reports API restrictions. */ }
      const down=southPressed(pads);
      if(down && !previousSouth && doc.hasFocus() && !doc.hidden) focus();
      previousSouth=down;
      win.requestAnimationFrame(poll);
    }
    resize(); updateFocus(); win.requestAnimationFrame(poll);
    return {ready:()=>{ready=true;updateFocus();}};
  }
  return {fit,hasFocus,blocksScroll,southPressed,attach};
});
