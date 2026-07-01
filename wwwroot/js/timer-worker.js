let targetTime = null;
let pausedRemaining = null;
let tickInterval = null;

function tick() {
    if (targetTime === null) return;
    const remaining = Math.max(0, targetTime - Date.now());
    postMessage({ type: 'tick', remaining });
    if (remaining <= 0) {
        clearInterval(tickInterval);
        tickInterval = null;
        targetTime = null;
        postMessage({ type: 'done' });
    }
}

onmessage = function (e) {
    const { cmd, durationMs, remainingMs } = e.data;

    if (cmd === 'start') {
        const ms = durationMs || remainingMs || 0;
        targetTime = Date.now() + ms;
        pausedRemaining = null;
        if (tickInterval) clearInterval(tickInterval);
        tickInterval = setInterval(tick, 250);
        tick();
    } else if (cmd === 'pause') {
        if (targetTime !== null) {
            pausedRemaining = Math.max(0, targetTime - Date.now());
        }
        targetTime = null;
        if (tickInterval) { clearInterval(tickInterval); tickInterval = null; }
        postMessage({ type: 'paused', remaining: pausedRemaining });
    } else if (cmd === 'resume') {
        if (pausedRemaining != null && pausedRemaining > 0) {
            targetTime = Date.now() + pausedRemaining;
            pausedRemaining = null;
            if (tickInterval) clearInterval(tickInterval);
            tickInterval = setInterval(tick, 250);
            tick();
        }
    } else if (cmd === 'stop') {
        targetTime = null;
        pausedRemaining = null;
        if (tickInterval) { clearInterval(tickInterval); tickInterval = null; }
        postMessage({ type: 'stopped' });
    }
};
