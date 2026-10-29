// A temporary reading preference belongs to the current screen, never to saved Audio settings.
const connections = new WeakMap();
export function connectReadingBackground(sphere) {
    connections.get(sphere)?.();
    const button = sphere.querySelector('[data-hit-reading-cover]');
    if (!button) return () => {};
    let screen = '', concealed = false, coverAnimation;
    function setHidden(hide) {
        if ((sphere.dataset.audioReadingHidden === 'true') === hide) return;
        const backdrop = sphere.querySelector(':scope > .sphere-audio-backdrop');
        const current = backdrop && getComputedStyle(backdrop).display !== 'none' ? Number(getComputedStyle(backdrop).opacity) : 0;
        coverAnimation?.cancel(); coverAnimation = null;
        if (hide) sphere.dataset.audioReadingHidden = 'true';
        else sphere.removeAttribute('data-audio-reading-hidden');
        if (!backdrop) return;
        backdrop.style.removeProperty('display');
        if (matchMedia('(prefers-reduced-motion:reduce)').matches || document.getElementById('hit-motion-preference')?.textContent.trim()) return;
        const target = hide ? 0 : Number(getComputedStyle(backdrop).opacity);
        backdrop.style.display = 'block';
        const animation = backdrop.animate([{opacity:current},{opacity:target}],{duration:650,easing:'ease-in-out',fill:'both'});
        coverAnimation = animation;
        animation.finished.then(() => {
            if (coverAnimation !== animation) return;
            backdrop.style.removeProperty('display'); animation.cancel(); coverAnimation = null;
        }).catch(() => {});
    }
    function refresh() {
        const host = sphere.querySelector('.hit-menu-host');
        const panel = host?.querySelector('.hit-menu-panel:not([hidden])');
        const configuration = host?.dataset.hitConfiguration === 'true';
        const views = [...(panel?.querySelectorAll('[data-hit-view]') ?? [])].map(view=>view.dataset.hitView).join('/');
        const next = configuration && panel ? panel.dataset.hitMenu + ':' + views : '';
        if (screen !== next) { screen = next; concealed = false; }
        const available = sphere.dataset.audioCover === 'true';
        if (button.hidden !== (!configuration || sphere.dataset.hitShowCoverButton==='false')) button.hidden = !configuration || sphere.dataset.hitShowCoverButton==='false';
        const forced = configuration && sphere.dataset.hitHideSettingsArtwork === 'true';
        if (button.disabled !== (!available || forced)) button.disabled = !available || forced;
        const hide = configuration && available && (concealed || sphere.dataset.hitHideSettingsArtwork==='true');
        setHidden(hide);
        const label = !available ? 'No artwork background' : hide ? 'Show artwork for this screen' : 'Hide artwork for this screen';
        button.setAttribute('aria-label',label);
        button.setAttribute('aria-pressed',String(available && !hide));
        if (button.hasAttribute('data-hit-tooltip')) button.dataset.hitTooltip = label;
        else if (button.getAttribute('title') !== label) button.title = label;
    }
    const toggle = event => { event.stopPropagation(); if (!button.disabled) { concealed = !concealed; refresh(); } };
    button.addEventListener('click',toggle);
    const observer = new MutationObserver(refresh);
    observer.observe(sphere,{subtree:true,childList:true,attributes:true,attributeFilter:['hidden','data-hit-menu','data-hit-view','data-hit-configuration','data-audio-cover','data-hit-hide-settings-artwork','data-hit-show-cover-button']});
    refresh();
    const disconnect = () => {
        observer.disconnect(); button.removeEventListener('click',toggle);
        coverAnimation?.cancel(); sphere.querySelector(':scope > .sphere-audio-backdrop')?.style.removeProperty('display');
        sphere.removeAttribute('data-audio-reading-hidden'); button.hidden = true;
        connections.delete(sphere);
    };
    connections.set(sphere,disconnect);
    return disconnect;
}
