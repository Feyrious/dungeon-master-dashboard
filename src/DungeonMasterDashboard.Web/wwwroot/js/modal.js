// Lets an open dialog react to Escape wherever focus is on the page.
let handler = null;

export function listenForEscape(dotnet) {
    stopListening();
    handler = event => {
        if (event.key === 'Escape') {
            dotnet.invokeMethodAsync('OnEscape');
        }
    };
    document.addEventListener('keydown', handler);
}

export function stopListening() {
    if (handler) {
        document.removeEventListener('keydown', handler);
        handler = null;
    }
}
