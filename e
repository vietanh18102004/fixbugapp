private string BuildVideoHtml(string videoUrl)
{
    return $@"<!DOCTYPE html>
<html>
<head>
<meta name='viewport' content='width=device-width, initial-scale=1.0'>
<style>
  * {{ margin:0; padding:0; box-sizing:border-box; }}
  body {{ background:#000; width:100vw; height:100vh; overflow:hidden; user-select:none; }}
  video {{ width:100%; height:100%; object-fit:contain; display:block; }}

  #controls {{
    position:fixed; inset:0;
    display:flex; flex-direction:column;
    justify-content:space-between;
    background:linear-gradient(to bottom, rgba(0,0,0,0.35) 0%, transparent 30%, transparent 70%, rgba(0,0,0,0.55) 100%);
    transition:opacity 0.3s;
    opacity:0;
    pointer-events:none;
  }}
  #controls.visible {{
    opacity:1;
    pointer-events:all;
  }}

  #top {{
    display:flex; justify-content:flex-end;
    padding:10px 12px;
  }}

  .icon-btn {{
    width:36px; height:36px; border-radius:50%;
    background:rgba(0,0,0,0.45);
    display:flex; align-items:center; justify-content:center;
    border:none; cursor:pointer; flex-shrink:0;
    -webkit-tap-highlight-color:transparent;
  }}
  .icon-btn svg {{ display:block; pointer-events:none; }}

  #middle {{
    display:flex; justify-content:center; align-items:center; gap:40px;
  }}
  #playBtn {{ width:56px; height:56px; }}

  #bottom {{
    padding:0 12px 14px;
    display:flex;
    flex-direction:column;
    gap:6px;
  }}
  #timeRow {{
    display:flex; justify-content:space-between;
    font-size:11px; color:#fff;
    font-family:sans-serif;
  }}
  #seekRow {{
    display:flex;
    align-items:center;
    gap:10px;
  }}
  #progress {{
    -webkit-appearance:none; appearance:none;
    flex:1;
    height:3px; border-radius:2px;
    background:#ffffff44; outline:none; cursor:pointer;
  }}
  #progress::-webkit-slider-thumb {{
    -webkit-appearance:none;
    width:13px; height:13px; border-radius:50%;
    background:#fff; cursor:pointer;
  }}
  .small-btn {{
    width:32px; height:32px; border-radius:50%;
    background:rgba(0,0,0,0.45);
    display:flex; align-items:center; justify-content:center;
    border:none; cursor:pointer; flex-shrink:0;
    -webkit-tap-highlight-color:transparent;
  }}
  .small-btn svg {{ display:block; pointer-events:none; }}
</style>
</head>
<body>
<video id='v' src='{videoUrl}' autoplay loop playsinline webkit-playsinline></video>

<div id='controls'>
  <!-- Top: placeholder -->
  <div id='top'></div>

  <!-- Middle: seek back, play/pause, seek forward -->
  <div id='middle'>
    <button class='icon-btn' ontouchend='seekBy(-10)' onclick='seekBy(-10)'>
      <svg width='20' height='20' viewBox='0 0 24 24' fill='#fff'>
        <polygon points='12,4 2,12 12,20'/><polygon points='22,4 12,12 22,20'/>
      </svg>
    </button>

    <button class='icon-btn' id='playBtn' ontouchend='handlePlay()' onclick='handlePlay()'>
      <svg id='icPause' width='26' height='26' viewBox='0 0 24 24' fill='#fff'>
        <rect x='6' y='4' width='4' height='16'/><rect x='14' y='4' width='4' height='16'/>
      </svg>
      <svg id='icPlay' width='26' height='26' viewBox='0 0 24 24' fill='#fff' style='display:none'>
        <polygon points='5 3 19 12 5 21 5 3'/>
      </svg>
    </button>

    <button class='icon-btn' ontouchend='seekBy(10)' onclick='seekBy(10)'>
      <svg width='20' height='20' viewBox='0 0 24 24' fill='#fff'>
        <polygon points='2,4 12,12 2,20'/><polygon points='12,4 22,12 12,20'/>
      </svg>
    </button>
  </div>

  <!-- Bottom: time + [mute] [progress] [fullscreen] -->
  <div id='bottom'>
    <div id='timeRow'>
      <span id='curTime'>0:00</span>
      <span id='durTime'>0:00</span>
    </div>
    <div id='seekRow'>
      <!-- Mute button -->
      <button class='small-btn' id='muteBtn' ontouchend='handleMute()' onclick='handleMute()'>
        <svg id='icMuteOff' width='18' height='18' viewBox='0 0 24 24' fill='none' stroke='#fff' stroke-width='2' stroke-linecap='round' stroke-linejoin='round'>
          <polygon points='11 5 6 9 2 9 2 15 6 15 11 19 11 5'/>
          <line x1='23' y1='9' x2='17' y2='15'/><line x1='17' y1='9' x2='23' y2='15'/>
        </svg>
        <svg id='icMuteOn' width='18' height='18' viewBox='0 0 24 24' fill='none' stroke='#fff' stroke-width='2' stroke-linecap='round' stroke-linejoin='round' style='display:none'>
          <polygon points='11 5 6 9 2 9 2 15 6 15 11 19 11 5'/>
          <path d='M19.07 4.93a10 10 0 0 1 0 14.14'/>
          <path d='M15.54 8.46a5 5 0 0 1 0 7.07'/>
        </svg>
      </button>

      <!-- Progress bar -->
      <input type='range' id='progress' min='0' max='100' value='0'
             oninput='onSeekInput()' onchange='onSeekChange()' />

      <!-- Fullscreen / exit button -->
      <button class='small-btn' id='fsBtn' ontouchend='handleFs()' onclick='handleFs()'>
        <svg id='icFsIn' width='18' height='18' viewBox='0 0 24 24' fill='none' stroke='#fff' stroke-width='2' stroke-linecap='round' stroke-linejoin='round'>
          <polyline points='15 3 21 3 21 9'/><polyline points='9 21 3 21 3 15'/>
          <line x1='21' y1='3' x2='14' y2='10'/><line x1='3' y1='21' x2='10' y2='14'/>
        </svg>
        <svg id='icFsOut' width='18' height='18' viewBox='0 0 24 24' fill='none' stroke='#fff' stroke-width='2' stroke-linecap='round' stroke-linejoin='round' style='display:none'>
          <polyline points='4 14 10 14 10 20'/><polyline points='20 10 14 10 14 4'/>
          <line x1='10' y1='14' x2='3' y2='21'/><line x1='21' y1='3' x2='14' y2='10'/>
        </svg>
      </button>
    </div>
  </div>
</div>

<script>
  var v = document.getElementById('v');
  var controls = document.getElementById('controls');
  var icPause = document.getElementById('icPause');
  var icPlay = document.getElementById('icPlay');
  var icMuteOff = document.getElementById('icMuteOff');
  var icMuteOn = document.getElementById('icMuteOn');
  var icFsIn = document.getElementById('icFsIn');
  var icFsOut = document.getElementById('icFsOut');
  var progress = document.getElementById('progress');
  var curTime = document.getElementById('curTime');
  var durTime = document.getElementById('durTime');

  var isMuted = true;
  var isSeeking = false;
  var isFullscreen = false;
  var hideTimer = null;

  // iOS fix: dùng attribute thay vì property để set muted lần đầu
  v.setAttribute('muted', '');
  v.muted = true;

  function fmt(s) {{
    s = s || 0;
    var m = Math.floor(s / 60);
    var sec = Math.floor(s % 60);
    return m + ':' + (sec < 10 ? '0' : '') + sec;
  }}

  v.addEventListener('timeupdate', function() {{
    if (!isSeeking && v.duration) {{
      var pct = (v.currentTime / v.duration) * 100;
      progress.value = pct;
      progress.style.background =
        'linear-gradient(to right, #fff ' + pct + '%, #ffffff44 ' + pct + '%)';
      curTime.textContent = fmt(v.currentTime);
      durTime.textContent = fmt(v.duration);
    }}
  }});

  v.addEventListener('ended', function() {{
    v.currentTime = 0; v.play();
  }});

  v.addEventListener('play', function() {{
    icPause.style.display = 'block';
    icPlay.style.display = 'none';
  }});
  v.addEventListener('pause', function() {{
    icPause.style.display = 'none';
    icPlay.style.display = 'block';
  }});

  // Click video -> fullscreen + bật tiếng
v.addEventListener('click', function(e) {

  if (
    e.target.closest('.icon-btn') ||
    e.target.closest('.small-btn') ||
    e.target === progress
  ) {
    return;
  }

  isMuted = false;
  v.muted = false;

  icMuteOff.style.display = 'none';
  icMuteOn.style.display  = 'block';

  if (!isFullscreen) {
    toggleFullscreen();
  }

  resetHideTimer();
});

  // iOS: lắng nghe volumechange để sync icon mute
  v.addEventListener('volumechange', function() {{
    isMuted = v.muted;
    icMuteOff.style.display = isMuted ? 'block' : 'none';
    icMuteOn.style.display  = isMuted ? 'none'  : 'block';
  }});

  // Chống double-fire trên iOS (ontouchend + onclick cùng lúc)
  var lastTouchTime = 0;
  function debounce(fn) {{
    var now = Date.now();
    if (now - lastTouchTime < 300) return;
    lastTouchTime = now;
    fn();
  }}

  function handlePlay() {{ debounce(togglePlay); }}
  function handleMute() {{ debounce(toggleMute); }}
  function handleFs()   {{ debounce(toggleFullscreen); }}

  function togglePlay() {{
    if (v.paused) {{ v.play(); window.location.href = 'videostatus://playing'; }}
    else          {{ v.pause(); window.location.href = 'videostatus://paused'; }}
    resetHideTimer();
  }}

  function toggleMute() {{
    isMuted = !isMuted;
    v.muted = isMuted;
    // iOS fallback: nếu muted không thay đổi sau 50ms thì reload source
    setTimeout(function() {{
      if (v.muted !== isMuted) {{
        var t = v.currentTime;
        v.load();
        v.currentTime = t;
        v.muted = isMuted;
        if (!v.paused) v.play();
      }}
    }}, 50);
    resetHideTimer();
  }}

  function toggleFullscreen() {{
    if (!isFullscreen) {{
      isFullscreen = true;
      icFsIn.style.display = 'none';
      icFsOut.style.display = 'block';
      if (v.requestFullscreen) v.requestFullscreen();
      else if (v.webkitRequestFullscreen) v.webkitRequestFullscreen();
      else if (v.webkitEnterFullscreen) v.webkitEnterFullscreen();
    }} else {{
      isFullscreen = false;
      icFsIn.style.display = 'block';
      icFsOut.style.display = 'none';
      if (document.exitFullscreen) document.exitFullscreen();
      else if (document.webkitExitFullscreen) document.webkitExitFullscreen();
      window.location.href = 'videostatus://exitfullscreen';
    }}
    resetHideTimer();
  }}

  document.addEventListener('fullscreenchange', function() {{
    if (!document.fullscreenElement && !document.webkitFullscreenElement) {{
      isFullscreen = false;
      icFsIn.style.display = 'block';
      icFsOut.style.display = 'none';
    }}
  }});
  document.addEventListener('webkitfullscreenchange', function() {{
    if (!document.fullscreenElement && !document.webkitFullscreenElement) {{
      isFullscreen = false;
      icFsIn.style.display = 'block';
      icFsOut.style.display = 'none';
    }}
  }});

  function seekBy(sec) {{
    v.currentTime = Math.max(0, Math.min(v.duration || 0, v.currentTime + sec));
    resetHideTimer();
  }}

  function onSeekInput() {{
    isSeeking = true;
    curTime.textContent = fmt((progress.value / 100) * (v.duration || 0));
    resetHideTimer();
  }}

  function onSeekChange() {{
    v.currentTime = (progress.value / 100) * (v.duration || 0);
    isSeeking = false;
    resetHideTimer();
  }}

  function showControls() {{
    controls.classList.add('visible');
    resetHideTimer();
  }}

  function resetHideTimer() {{
    clearTimeout(hideTimer);
    hideTimer = setTimeout(function() {{
      controls.classList.remove('visible');
    }}, 5000);
  }}

  document.addEventListener('touchstart', function(e) {{
    if (!e.target.closest('.icon-btn') && !e.target.closest('.small-btn') && e.target !== progress) {{
      showControls();
    }}
  }});
  document.addEventListener('click', function(e) {{
    if (!e.target.closest('.icon-btn') && !e.target.closest('.small-btn') && e.target !== progress) {{
      showControls();
    }}
  }});
</script>
</body>
</html>";
}
