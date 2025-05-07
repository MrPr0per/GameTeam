const debugMode = true;

export function showServerError(message, ...debugInfo) {
    if (debugMode) {
        console.log(message, debugInfo);
    } else {
        console.log(message);
    }
}