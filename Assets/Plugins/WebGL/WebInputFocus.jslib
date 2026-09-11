mergeInto(LibraryManager.library, {
  TT_SampleGamepadHistory: function () {
    // Read-only sampling. Retain A evidence across browser mode/focus changes.
    var history = Module.ttButtonHistory || (Module.ttButtonHistory = {count:0, last:"None yet", states:{}});
    try {
      var pads = navigator.getGamepads ? navigator.getGamepads() : [];
      for(var i=0;i<pads.length;i++) {
        var p=pads[i]; if(!p || !p.connected || p.mapping!=="standard") continue;
        var b=p.buttons[0]; if(!b) continue;
        var down=b.pressed || b.value>0.5;
        if(down && !history.states[i]) {
          history.count++;
          history.last="A pressed="+b.pressed+" value="+b.value+" focus="+(document.hasFocus() && !document.hidden && document.activeElement===Module.canvas);
        }
        history.states[i]=down;
      }
    } catch(e) { history.last="Sampling error: "+e.name; }
  },
  TT_FocusFromGamepad: function () {
    // Called only for a real controller A press. Never capture Edge Menu or contextmenu.
    if(!document.hasFocus() || document.hidden) return false;
    var el=document.activeElement;
    if(el && (el.isContentEditable || /^(INPUT|TEXTAREA|SELECT)$/.test(el.tagName))) return false;
    Module.canvas.focus({preventScroll:true});
    return document.activeElement===Module.canvas;
  },
  TT_HasInputFocus: function () {
    return document.hasFocus() && !document.hidden && document.activeElement === Module.canvas;
  },
  TT_BrowserGamepads__deps: ['$stringToNewUTF8'],
  TT_BrowserGamepads: function () {
    var text;
    try {
      var pads = navigator.getGamepads ? Array.from(navigator.getGamepads()).filter(function(p) { return p && p.connected; }) : null;
      text = pads === null ? "Gamepad API unavailable" : pads.length ? pads.map(function(p) {
        var down = p.buttons.map(function(b, i) { return b.pressed ? i : null; }).filter(function(i) { return i !== null; });
        return p.id.slice(0, 100) + " [" + (p.mapping || "non-standard mapping") + "] down=" + (down.length ? down.join(",") : "none");
      }).join("; ") : "None exposed; press a pad button with this page focused";
    } catch (e) { text = "Gamepad API blocked: " + e.name; }
    var history=Module.ttButtonHistory;
    if(history) text+=" | A samples="+history.count+" last: "+history.last;
    return stringToNewUTF8(text);
  }
});
