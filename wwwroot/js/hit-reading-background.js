// A temporary reading preference belongs to the current screen, never to saved Audio settings.
const connections = new WeakMap();
export function connectReadingBackground(sphere) {
    connections.get(sphere)?.();
    const button = sphere.querySelector('[data-hit-reading-cover]');
    if (!button) return () => {};
    let screen = '', concealed = false;
    function refresh() {
        const host = sphere.querySelector('.hit-menu-host');
        const panel = host?.querySelector('.hit-menu-panel:not([hidden])');
        const configuration = host?.dataset.hitConfiguration === 'true';
        const views = [...(panel?.querySelectorAll('[data-hit-view]') ?? [])].map(view=>view.dataset.hitView).join('/');
        const next = configuration && panel ? panel.dataset.hitMenu + ':' + views : '';
        if (screen !== next) { screen = next; concealed = false; }
        const available = sphere.dataset.audioCover === 'true';
        if (button.hidden !== !configuration) button.hidden = !configuration;
        if (button.disabled !== !available) button.disabled = !available;
        const hide = configuration && available && concealed;
        if (hide) { if (sphere.dataset.audioReadingHidden !== 'true') sphere.dataset.audioReadingHidden = 'true'; }
        else sphere.removeAttribute('data-audio-reading-hidden');
        const label = !available ? 'No artwork background' : hide ? 'Show artwork for this screen' : 'Hide artwork for this screen';
        button.setAttribute('aria-label',label);
        button.setAttribute('aria-pressed',String(available && !hide));
        if (button.hasAttribute('data-hit-tooltip')) button.dataset.hitTooltip = label;
        else if (button.getAttribute('title') !== label) button.title = label;
    }
    const toggle = event => { event.stopPropagation(); if (!button.disabled) { concealed = !concealed; refresh(); } };
    button.addEventListener('click',toggle);
    const observer = new MutationObserver(refresh);
    observer.observe(sphere,{subtree:true,childList:true,attributes:true,attributeFilter:['hidden','data-hit-menu','data-hit-view','data-hit-configuration','data-audio-cover']});
    refresh();
    const disconnect = () => {
        observer.disconnect(); button.removeEventListener('click',toggle);
        sphere.removeAttribute('data-audio-reading-hidden'); button.hidden = true;
        connections.delete(sphere);
    };
    connections.set(sphere,disconnect);
    return disconnect;
}
