// Small UI helpers called from Blazor.
window.dnCopyText = async function (text) {
    try {
        await navigator.clipboard.writeText(text);
        return true;
    } catch (e) {
        return false;
    }
};
