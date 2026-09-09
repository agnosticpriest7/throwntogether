mergeInto(LibraryManager.library, {
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
    return stringToNewUTF8(text);
  }
});
