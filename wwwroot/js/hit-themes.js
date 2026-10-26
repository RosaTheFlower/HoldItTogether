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
    const root = typeof target === "object" ? target.root : target === "menus" ? '.hit-menu-panel[data-hit-menu]' : '.sphere-area';
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

function compileRules(rules, target, names = namesIn(rules, typeof target === "object" ? target.key : target)) {
    return [...rules].map(rule => {
        if (rule.type === CSSRule.STYLE_RULE) {
            // CSS nesting is deliberately excluded: all selectors must pass through this boundary.
            if (rule.cssRules?.length) throw new Error("Use complete selectors instead of nested CSS.");
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
        throw new Error("Unsupported theme rule. Use selectors, @media, @supports, @keyframes or @font-face.");
    }).join("\n");
}

export function compile(css) {
    if (!css.trim()) return "";
    const sheet = new CSSStyleSheet();
    sheet.replaceSync(css);
    // replaceSync ignores @import. Reject it explicitly so downloaded CSS cannot escape scoping.
    if (/(^|[;}])\s*@import\b/i.test(css.replace(/\/\*[\s\S]*?\*\//g, ""))) throw new Error("Inline your CSS; @import is not supported in themes.");
    const rules = [...sheet.cssRules];
    if (!rules.length) throw new Error("The file does not contain a valid CSS theme.");
    const compiled = [];
    for (const rule of rules) {
        if (rule.constructor.name !== "CSSLayerBlockRule" || (rule.name !== "sphere" && rule.name !== "menus"))
            throw new Error("Organize the theme into @layer sphere { ... } and @layer menus { ... }.");
        compiled.push(compileRules(rule.cssRules, rule.name));
    }
    return compiled.join("\n");
}

// Public package CSS is scoped independently of the global theme contract.
export function compileExtension(css, id, baseUrl) {
    if (/(^|[;}])\s*@import\b/i.test(css.replace(/\/\*[\s\S]*?\*\//g, ""))) throw new Error("Inline your CSS; @import is not supported.");
    const sheet = new CSSStyleSheet();
    const absolute = css.replace(/url\(\s*(['"]?)([^)'"\s]+)\1\s*\)/gi, (_, quote, url) => `url("${new URL(url, new URL(baseUrl, document.baseURI)).href.replaceAll('"', '%22')}")`);
    sheet.replaceSync(absolute);
    return compileRules(sheet.cssRules, { root: `.hit-menu-panel[data-hit-menu="${CSS.escape(id)}"]`, key: id.replace(/[^a-z0-9-]/gi, "-") });
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
        document.documentElement.dataset.hitThemeError = "Theme not applied: " + error.message;
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
function stripTitles(root = document) {
    const nodes = root instanceof Element && root.hasAttribute("title") ? [root] : [];
    if (root.querySelectorAll) nodes.push(...root.querySelectorAll("[title]"));
    for (const element of nodes) {
        if (!element.closest(".sphere-area")) continue;
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
        for (const record of records) {
            if (record.type === "attributes") {
                if (!record.target.hasAttribute("title")) delete record.target.dataset.hitTooltip;
                else stripTitles(record.target);
            } else {
                for (const node of record.addedNodes) if (node instanceof Element) stripTitles(node);
            }
        }
        tooltipObserver.observe(document.documentElement, {subtree:true, childList:true, attributes:true, attributeFilter:["title"]});
    });
    tooltipObserver.observe(document.documentElement, {subtree:true, childList:true, attributes:true, attributeFilter:["title"]});
}
