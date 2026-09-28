// Thin wrapper around Google Identity Services (GIS) so Blazor components can
// trigger a Google sign-in prompt and receive the resulting ID token back in .NET.
window.dnGoogleAuth = {
    _dotNetRef: null,

    init: function (clientId, dotNetRef) {
        this._dotNetRef = dotNetRef;

        if (!clientId || clientId.indexOf('REPLACE_WITH_YOUR_GOOGLE_OAUTH_CLIENT_ID') === 0) {
            console.warn('Dưỡng Nhan: Google Sign-In is not configured yet (Google:ClientId in appsettings.json).');
            return false;
        }

        if (!window.google || !window.google.accounts || !window.google.accounts.id) {
            console.warn('Dưỡng Nhan: Google Identity Services script has not loaded yet.');
            return false;
        }

        window.google.accounts.id.initialize({
            client_id: clientId,
            callback: (response) => {
                if (this._dotNetRef && response && response.credential) {
                    this._dotNetRef.invokeMethodAsync('OnGoogleCredential', response.credential);
                }
            }
        });

        return true;
    },

    prompt: function () {
        if (window.google && window.google.accounts && window.google.accounts.id) {
            window.google.accounts.id.prompt();
        }
    }
};
