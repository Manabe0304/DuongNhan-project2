export function renderTurnstile(elementId, siteKey, callback) {
    return window.turnstile.render(`#${elementId}`, {
        sitekey: siteKey,
        callback: (token) => window.DotNet.invokeMethodAsync(callback.assembly, callback.method, token)
    });
}

export function resetTurnstile(widgetId) {
    window.turnstile.reset(widgetId);
}