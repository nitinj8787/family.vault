/**
 * Triggers a file download in the browser using a base64-encoded byte array.
 * Called from Blazor via IJSRuntime.InvokeVoidAsync.
 *
 * @param {string} fileName  - Suggested file name for the download dialog.
 * @param {string} mimeType  - MIME type of the file (e.g. "application/pdf").
 * @param {string} base64    - Base64-encoded file content.
 */
window.downloadFileFromBytes = function (fileName, mimeType, base64) {
    const byteCharacters = atob(base64);
    const byteArrays = [];

    for (let offset = 0; offset < byteCharacters.length; offset += 512) {
        const slice = byteCharacters.slice(offset, offset + 512);
        const byteNumbers = new Array(slice.length);
        for (let i = 0; i < slice.length; i++) {
            byteNumbers[i] = slice.charCodeAt(i);
        }
        byteArrays.push(new Uint8Array(byteNumbers));
    }

    const blob = new Blob(byteArrays, { type: mimeType });
    const url = URL.createObjectURL(blob);

    const anchor = document.createElement("a");
    anchor.href = url;
    anchor.download = fileName;
    anchor.style.display = "none";
    document.body.appendChild(anchor);
    anchor.click();
    document.body.removeChild(anchor);

    // Release the object URL after a short delay to allow the download to start.
    setTimeout(() => URL.revokeObjectURL(url), 10000);
};
