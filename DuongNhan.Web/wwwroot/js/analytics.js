// Google Analytics 4 helpers called from Blazor (see Services/AnalyticsService.cs).
// gtag.js itself is injected in Components/App.razor only when Analytics:Ga4MeasurementId is configured;
// without it every call below is a harmless no-op.
window.ga4 = {
    pageView: function (url, title) {
        if (typeof window.gtag !== "function") return;
        window.gtag("event", "page_view", {
            page_location: url,
            page_title: title || document.title
        });
    },

    event: function (eventName, parameters) {
        if (typeof window.gtag !== "function") return;
        window.gtag("event", eventName, parameters || {});
    }
};
