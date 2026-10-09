// Smooth section transitions: fades/slides [data-reveal] elements in as they enter the viewport.
// Blazor re-renders and inserts DOM dynamically, so a MutationObserver picks up new elements.
// Visibility state lives in a data attribute (data-revealed) because Blazor overwrites `class`.
(function () {
    var root = document.documentElement;
    var reduce = window.matchMedia && window.matchMedia("(prefers-reduced-motion: reduce)").matches;
    if (reduce || !("IntersectionObserver" in window)) return; // content simply stays visible

    root.classList.add("dn-js");

    var io = new IntersectionObserver(function (entries) {
        entries.forEach(function (entry) {
            if (!entry.isIntersecting) return;
            var el = entry.target;
            el.setAttribute("data-revealed", "");
            io.unobserve(el);
        });
    }, { rootMargin: "0px 0px -8% 0px", threshold: 0.08 });

    function watch(node) {
        if (node.nodeType !== 1) return;
        if (node.hasAttribute("data-reveal") && !node.hasAttribute("data-revealed")) io.observe(node);
        var kids = node.querySelectorAll ? node.querySelectorAll("[data-reveal]:not([data-revealed])") : [];
        for (var i = 0; i < kids.length; i++) io.observe(kids[i]);
    }

    function start() {
        watch(document.body);
        new MutationObserver(function (muts) {
            muts.forEach(function (m) { m.addedNodes.forEach(watch); });
        }).observe(document.body, { childList: true, subtree: true });
    }

    if (document.body) start(); else document.addEventListener("DOMContentLoaded", start);

    // Safety net: never leave content hidden if something goes wrong.
    setTimeout(function () {
        document.querySelectorAll("[data-reveal]:not([data-revealed])").forEach(function (el) {
            var r = el.getBoundingClientRect();
            if (r.top < window.innerHeight) el.setAttribute("data-revealed", "");
        });
    }, 2500);

    // Smooth in-page scrolling for step navigators (#anchor links) and highlight of the active one.
    document.addEventListener("click", function (e) {
        var a = e.target.closest && e.target.closest("a[data-scroll]");
        if (!a) return;
        var id = (a.getAttribute("href") || "").replace(/^.*#/, "");
        var target = id && document.getElementById(id);
        if (!target) return;
        e.preventDefault();
        target.scrollIntoView({ behavior: "smooth", block: "start" });
    });
})();

window.dnScrollSpy = {
    _io: null,
    start: function (containerSelector) {
        this.stop();
        var links = document.querySelectorAll(containerSelector + " a[data-scroll]");
        var map = {};
        links.forEach(function (a) { map[(a.getAttribute("href") || "").replace(/^.*#/, "")] = a; });
        this._io = new IntersectionObserver(function (entries) {
            entries.forEach(function (en) {
                if (!en.isIntersecting) return;
                links.forEach(function (l) { l.removeAttribute("data-active"); });
                var link = map[en.target.id];
                if (link) link.setAttribute("data-active", "");
            });
        }, { rootMargin: "-30% 0px -60% 0px" });
        Object.keys(map).forEach(function (id) {
            var el = document.getElementById(id);
            if (el) window.dnScrollSpy._io.observe(el);
        });
    },
    stop: function () { if (this._io) { this._io.disconnect(); this._io = null; } }
};
