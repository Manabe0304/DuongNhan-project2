// Small UI helpers called from Blazor.
window.dnCopyText = async function (text) {
    try {
        await navigator.clipboard.writeText(text);
        return true;
    } catch (e) {
        return false;
    }
};


// Scrolls an element into view after a page change (smooth unless the visitor prefers reduced motion).
window.dnScrollToId = function (id) {
    var el = document.getElementById(id);
    if (!el) return;
    var reduce = window.matchMedia && window.matchMedia("(prefers-reduced-motion: reduce)").matches;
    el.scrollIntoView({ behavior: reduce ? "auto" : "smooth", block: "start" });
};

window.dnSetTitle = function (title) { document.title = title; };
