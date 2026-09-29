(function(){
    let throttled = false;
    function post(e){
        if (e.clientY <= 10) {
            if (window.chrome && window.chrome.webview && !throttled) {
                window.chrome.webview.postMessage('toolbar_mousemove');
                throttled = true;
                setTimeout(() => { throttled = false; }, 100);
            }
        }
    }
    document.addEventListener('mousemove', post, { passive: true });
})();
