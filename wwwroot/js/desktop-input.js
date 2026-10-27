// Install before Blazor so page zoom cannot consume a menu gesture.
(() => {
    function wheel(event) {if(event.ctrlKey||event.metaKey){event.preventDefault();event.stopImmediatePropagation();}}
    function key(event) {
        if((event.ctrlKey||event.metaKey)&&(['+','-','=','0'].includes(event.key)||['NumpadAdd','NumpadSubtract','Numpad0'].includes(event.code))){
            event.preventDefault();event.stopImmediatePropagation();
        }
    }
    window.addEventListener('wheel',wheel,{capture:true,passive:false});
    window.addEventListener('keydown',key,true);
    window.addEventListener('gesturestart',event=>event.preventDefault(),{passive:false});
})();
