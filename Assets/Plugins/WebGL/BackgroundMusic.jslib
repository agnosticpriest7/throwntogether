mergeInto(LibraryManager.library, {
  TT_MusicStart: function(first, second) {
    if (window.ttMusic) return;
    var tracks=[UTF8ToString(first),UTF8ToString(second)], index=0;
    var audio=new Audio();
    audio.id='background-music'; audio.hidden=true;
    document.body.appendChild(audio);
    audio.preload='metadata';
    audio.volume=window.ttMusicVolume === undefined ? 0.6 : window.ttMusicVolume;
    window.ttMusic=audio;
    var pending=false, failed=false;
    function play() {
      if(document.hidden || failed || pending || !audio.paused) return;
      pending=true;
      var promise=audio.play();
      if(promise) promise.then(function(){pending=false;},function(error){
        pending=false;
        // Autoplay denial is expected until a trusted browser gesture; never alert.
        if(error.name !== 'NotAllowedError' && error.name !== 'AbortError') {
          failed=true; console.warn('Background music unavailable',error.name);
        }
      }); else pending=false;
    }
    audio.addEventListener('ended',function(){index=(index+1)%tracks.length;audio.src=tracks[index];play();});
    audio.addEventListener('error',function(){failed=true;console.warn('Background music could not load: '+tracks[index]);});
    // Passive listeners leave Edge's own Menu UI and gameplay events untouched.
    document.addEventListener('pointerdown',play,{passive:true});
    document.addEventListener('keydown',play,{passive:true});
    document.addEventListener('visibilitychange',function(){if(document.hidden) audio.pause(); else play();});
    audio.src=tracks[index]; play();
  },
  TT_MusicVolume: function(value) {
    window.ttMusicVolume=Math.max(0,Math.min(1,value));
    if(window.ttMusic) window.ttMusic.volume=window.ttMusicVolume;
  }
});
