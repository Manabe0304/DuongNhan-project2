// Thin wrapper around Google Identity Services (GIS). Renders Google's official
// "Sign in with Google" button (which opens a reliable popup) and hands the signed
// ID token back to the Blazor component.
window.dnGoogleAuth = {
    _waitForGoogle: function (timeoutMs) {
        return new Promise((resolve) => {
            const started = Date.now();
            const tick = () => {
                if (window.google && window.google.accounts && window.google.accounts.id) {
                    resolve(true);
                } else if (Date.now() - started > timeoutMs) {
                    resolve(false);
                } else {
                    setTimeout(tick, 100);
                }
            };
            tick();
        });
    },

    // Returns "ok" | "not-configured" | "script-blocked"
    renderButton: async function (elementId, clientId, dotNetRef, text, width) {
        if (!clientId || clientId.indexOf('52076919259-cmjvm8mbul607hum1h1clcos1oq2qsal.apps.googleusercontent.com') === 0) {
            return 'not-configured';
        }

        const loaded = await this._waitForGoogle(8000);
        if (!loaded) return 'script-blocked';

        const el = document.getElementById(elementId);
        if (!el) return 'ok';

        window.google.accounts.id.initialize({
            client_id: clientId,
            callback: (response) => {
                if (response && response.credential) {
                    dotNetRef.invokeMethodAsync('OnGoogleCredential', response.credential);
                }
            }
        });

        el.innerHTML = '';
        window.google.accounts.id.renderButton(el, {
            type: 'standard',
            theme: 'outline',
            size: 'large',
            text: text,
            shape: 'rectangular',
            logo_alignment: 'center',
            width: width
        });
        return 'ok';
    }
};
