export function showPrompt(message) {
  return prompt(message, '');
}

async function gameLoop(timeStamp) {
    window.requestAnimationFrame(gameLoop);
    await game.instance.invokeMethodAsync('GameLoop');
}

async function onKeyDown(e) {
    await game.instance.invokeMethodAsync('OnKeyDown', e.keyCode);
}

async function onMouseDown(e) {
    await game.instance.invokeMethodAsync('OnMouseDown', e.button);
}

async function onMouseMove(e) {
    await game.instance.invokeMethodAsync('OnMouseMove', e.clientX, e.clientY);
}

async function onResize() {
    if (!window.game.canvas)
        return;

    game.canvas.width = window.innerWidth;
    game.canvas.height = window.innerHeight;

    await game.instance.invokeMethodAsync('OnResize', window.innerWidth, window.innerHeight)
}

export function initGame(instance) {
    var canvasContainer = document.getElementById('canvasContainer'),
        canvases = canvasContainer.getElementsByTagName('canvas') || [];
    window.game = {
        instance: instance,
        canvas: canvases.length ? canvases[0] : null
    };

    window.addEventListener("keydown", onKeyDown)
    window.addEventListener("mousedown", onMouseDown);
    window.addEventListener("mousemove", onMouseMove);
    window.addEventListener("resize", onResize);

    // Call once to set initial state.
    onResize();

    window.requestAnimationFrame(gameLoop);
};
