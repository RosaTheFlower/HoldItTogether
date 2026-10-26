// Theme format: @layer sphere { ... } and/or @layer menus { ... }.
// Rules are parsed by the browser, then scoped before entering the live document.
let style;
let motionStyle;


function selectors(value) {
    const result = [];
    let start = 0, depth = 0, quote = "", escaped = false;
    for (let i = 0; i < value.length; i++) {
        const character = value[i];
        if (escaped) { escaped = false; continue; }
        if (character === "\\") { escaped = true; continue; }
        if (quote) { if (character === quote) quote = ""; continue; }
        if (character === '"' || character === "'") { quote = character; continue; }
        if (character === "(" || character === "[") depth++;
        if (character === ")" || character === "]") depth--;
        if (character === "," && depth === 0) { result.push(value.slice(start, i).trim()); start = i + 1; }
    }
    result.push(value.slice(start).trim());
    return result;
}

function scopeSelector(selector, target) {
    const root = target === "menus" ? '.hit-menu-panel[data-hit-menu]' : '.sphere-area';
    if (/^(:root|:scope|html|body)(?=$|[\s.#:[>+~])/.test(selector))
        return selector.replace(/^(:root|:scope|html|body)/, root);
    return root + " " + selector;
}

function namesIn(rules, target, names = new Map()) {
    for (const rule of rules) {
        if (rule.type === CSSRule.KEYFRAMES_RULE)
            names.set(rule.name, "hit-theme-" + target + "-" + rule.name);
        if (rule.type === CSSRule.FONT_FACE_RULE) {
            const name = rule.style.getPropertyValue("font-family").trim().replace(/^["']|["']$/g, "");
            if (name) names.set(name, "HIT Theme " + target + " " + name);
        }
        if (rule.cssRules && rule.type !== CSSRule.KEYFRAMES_RULE) namesIn(rule.cssRules, target, names);
    }
    return names;
}

function rewriteValue(value, names) {
    for (const [name, replacement] of names) {
        const escaped = name.replace(/[.*+?^$()|[\]\\]/g, "\\$&");
        value = value.replace(new RegExp("(?<![\\w-])" + escaped + "(?![\\w-])", "g"), () => replacement);
    }
    return value;
}

function declarations(style, names) {
    return [...style].map(property => {
        let value = style.getPropertyValue(property);
        if (/^(animation|font)(-|$)/.test(property) || property.startsWith("--")) value = rewriteValue(value, names);
        return property + ":" + value + (style.getPropertyPriority(property) ? "!important" : "") + ";";
    }).join("");
}

function compileRules(rules, target, names = namesIn(rules, target)) {
    return [...rules].map(rule => {
        if (rule.type === CSSRule.STYLE_RULE) {
            // CSS nesting is deliberately excluded: all selectors must pass through this boundary.
            if (rule.cssRules?.length) throw new Error("Use seletores completos em vez de CSS aninhado.");
            const scoped = selectors(rule.selectorText).map(value => scopeSelector(value, target)).join(",");
            return scoped + "{" + declarations(rule.style, names) + "}";
        }
        if (rule.type === CSSRule.KEYFRAMES_RULE)
            return "@keyframes " + names.get(rule.name) + "{" +
                [...rule.cssRules].map(frame => frame.keyText + "{" + declarations(frame.style, names) + "}").join("") + "}";
        if (rule.type === CSSRule.FONT_FACE_RULE)
            return "@font-face{" + declarations(rule.style, names) + "}";
        if (rule.type === CSSRule.MEDIA_RULE || rule.type === CSSRule.SUPPORTS_RULE)
            return (rule.type === CSSRule.MEDIA_RULE ? "@media " : "@supports ") + rule.conditionText + "{" + compileRules(rule.cssRules, target, names) + "}";
        throw new Error("Regra não suportada no tema. Use seletores, @media, @supports, @keyframes ou @font-face.");
    }).join("\n");
}

export function compile(css) {
    if (!css.trim()) return "";
    const sheet = new CSSStyleSheet();
    sheet.replaceSync(css);
    // replaceSync ignores @import. Reject it explicitly so downloaded CSS cannot escape scoping.
    if (/(^|[;}])\s*@import\b/i.test(css.replace(/\/\*[\s\S]*?\*\//g, ""))) throw new Error("Inclua o CSS no próprio arquivo; @import não é aceito em temas.");
    const rules = [...sheet.cssRules];
    if (!rules.length) throw new Error("O arquivo não contém um tema CSS válido.");
    const compiled = [];
    for (const rule of rules) {
        if (rule.constructor.name !== "CSSLayerBlockRule" || (rule.name !== "sphere" && rule.name !== "menus"))
            throw new Error("Organize o tema em @layer sphere { ... } e @layer menus { ... }.");
        compiled.push(compileRules(rule.cssRules, rule.name));
    }
    return compiled.join("\n");
}

export function apply(css, readError = null) {
    try {
        const compiled = compile(css);
        if (!style) {
            style = document.createElement("style");
            style.id = "hit-custom-theme";
            document.head.append(style);
        }
        style.textContent = compiled;
        document.documentElement.dataset.hitThemeError = readError || "";
    } catch (error) {
        document.documentElement.dataset.hitThemeError = "Tema não aplicado: " + error.message;
    }
    document.dispatchEvent(new CustomEvent("hit-theme-status"));
    // Preserve the final override after every theme change.
    if (motionStyle) document.head.append(motionStyle);
    return document.documentElement.dataset.hitThemeError || null;
}

// This is a separate, last stylesheet. Themes (including !important rules) cannot re-enable motion.
export function setMotion(disabled) {
    if (!motionStyle) {
        motionStyle = document.createElement("style");
        motionStyle.id = "hit-motion-preference";
    }
    motionStyle.textContent = disabled
        ? '@layer hit-motion{.sphere-area,.sphere-area *,.sphere-area::before,.sphere-area::after,.sphere-area *::before,.sphere-area *::after' +
          '{animation:none!important;transition:none!important;scroll-behavior:auto!important;}}'
        : "";
    document.head.append(motionStyle);
    document.documentElement.dataset.hitMotion = disabled ? "off" : "on";
}
// Native select popups should not retain wheel focus after a theme is selected.
export function releaseFocus(element) {
    if (element instanceof HTMLElement) element.blur();
}

// A real modal dialog provides focus trapping, Escape cancellation and focus restoration.
export function setDeleteDialog(dialog, visible) {
    if (!(dialog instanceof HTMLDialogElement)) return;
    if (!dialog.dataset.hitCancelBound) {
        dialog.addEventListener("cancel", event => {
            event.preventDefault();
            dialog.querySelector("[data-dialog-cancel]")?.click();
        });
        dialog.dataset.hitCancelBound = "true";
    }
    if (visible && !dialog.open) {
        dialog.showModal();
        dialog.querySelector("[data-dialog-cancel]")?.focus();
    } else if (!visible && dialog.open) {
        dialog.close();
    }
}

// The canonical theme remains beneath custom overrides, including partial themes.
export function applyBase(css) {
    const compiled = compile(css);
    let baseline = document.getElementById("hit-base-theme");
    if (!baseline) {
        baseline = document.createElement("style");
        baseline.id = "hit-base-theme";
    }
    baseline.textContent = compiled;
    document.head.insertBefore(baseline, style || motionStyle || null);
    document.dispatchEvent(new CustomEvent("hit-theme-status"));
}

// Preserve accessible names. Only native hover tooltips are optional.
let tooltipObserver;
function stripTitles() {
    for (const element of document.querySelectorAll(".sphere-area[title], .sphere-area [title]")) {
        element.dataset.hitTooltip = element.getAttribute("title");
        element.removeAttribute("title");
    }
}
export function setTooltips(enabled) {
    tooltipObserver?.disconnect();
    if (enabled) {
        for (const element of document.querySelectorAll(".sphere-area[data-hit-tooltip], .sphere-area [data-hit-tooltip]")) {
            element.setAttribute("title", element.dataset.hitTooltip);
            delete element.dataset.hitTooltip;
        }
        return;
    }
    stripTitles();
    tooltipObserver ??= new MutationObserver(records => {
        tooltipObserver.disconnect();
        for (const record of records)
            if (record.type === "attributes" && !record.target.hasAttribute("title"))
                delete record.target.dataset.hitTooltip;
        stripTitles();
        tooltipObserver.observe(document.documentElement, {subtree:true, childList:true, attributes:true, attributeFilter:["title"]});
    });
    tooltipObserver.observe(document.documentElement, {subtree:true, childList:true, attributes:true, attributeFilter:["title"]});
}
