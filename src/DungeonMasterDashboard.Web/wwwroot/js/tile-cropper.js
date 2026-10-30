// Crops an uploaded image to the shape of a tile (3:2): drag to move, slider or wheel to zoom.
// The crop is previewed live on the preview tile and exported at a fixed size.

const OUTPUT_WIDTH = 1200;
const OUTPUT_HEIGHT = 800;
const PREVIEW_WIDTH = 600;
const PREVIEW_HEIGHT = 400;
const MAX_FILE_BYTES = 25 * 1024 * 1024;
const ACCEPTED_TYPES = ['image/png', 'image/jpeg', 'image/webp'];

const croppers = new WeakMap();

export function attach(input, frame, zoom, preview, dotnet) {
    const state = {
        input, frame, zoom, preview, dotnet,
        image: frame.querySelector('img'),
        source: null, url: null, drag: null, exported: null, previewTimer: 0,
        scale: 1, minScale: 1, x: 0, y: 0, frameWidth: 0,
    };

    state.onFile = () => loadFile(state);
    state.onZoom = () => setZoom(state, Number(zoom.value), frameCenter(state));
    state.onWheel = event => {
        if (!state.source) return;
        event.preventDefault();
        const factor = clamp(Number(zoom.value) * (event.deltaY < 0 ? 1.08 : 1 / 1.08), 1, Number(zoom.max));
        zoom.value = factor;
        setZoom(state, factor, pointInFrame(state, event));
    };
    state.onPointerDown = event => {
        if (!state.source) return;
        frame.setPointerCapture(event.pointerId);
        state.drag = { x: event.clientX, y: event.clientY, startX: state.x, startY: state.y };
    };
    state.onPointerMove = event => {
        if (!state.drag) return;
        state.x = state.drag.startX + event.clientX - state.drag.x;
        state.y = state.drag.startY + event.clientY - state.drag.y;
        clampPosition(state);
        render(state);
    };
    state.onPointerUp = () => {
        state.drag = null;
        schedulePreview(state);
    };
    state.onResize = () => refit(state);

    input.addEventListener('change', state.onFile);
    zoom.addEventListener('input', state.onZoom);
    frame.addEventListener('wheel', state.onWheel, { passive: false });
    frame.addEventListener('pointerdown', state.onPointerDown);
    frame.addEventListener('pointermove', state.onPointerMove);
    frame.addEventListener('pointerup', state.onPointerUp);
    frame.addEventListener('pointercancel', state.onPointerUp);
    window.addEventListener('resize', state.onResize);
    croppers.set(frame, state);
}

export function detach(frame) {
    const state = croppers.get(frame);
    if (!state) return;
    state.input.removeEventListener('change', state.onFile);
    state.zoom.removeEventListener('input', state.onZoom);
    state.frame.removeEventListener('wheel', state.onWheel);
    state.frame.removeEventListener('pointerdown', state.onPointerDown);
    state.frame.removeEventListener('pointermove', state.onPointerMove);
    state.frame.removeEventListener('pointerup', state.onPointerUp);
    state.frame.removeEventListener('pointercancel', state.onPointerUp);
    window.removeEventListener('resize', state.onResize);
    reset(state);
    croppers.delete(frame);
}

// Called once the crop frame is visible, so its size is known.
export function fit(frame) {
    const state = croppers.get(frame);
    if (!state?.source) return;
    const { width, height } = frameSize(state);
    state.minScale = Math.max(width / state.source.naturalWidth, height / state.source.naturalHeight);
    state.scale = state.minScale;
    state.zoom.value = 1;
    state.x = (width - state.source.naturalWidth * state.scale) / 2;
    state.y = (height - state.source.naturalHeight * state.scale) / 2;
    state.frameWidth = width;
    render(state);
    schedulePreview(state);
}

// Renders the crop and keeps it until takeExport is called. Returns its content type.
export async function exportCrop(frame) {
    const state = croppers.get(frame);
    if (!state?.source) return null;
    const canvas = drawCrop(state, OUTPUT_WIDTH, OUTPUT_HEIGHT);
    let blob = await toBlob(canvas, 'image/webp', 0.85);
    if (!blob || blob.type !== 'image/webp') {
        // Not every browser can encode WebP.
        blob = await toBlob(canvas, 'image/jpeg', 0.88);
    }
    state.exported = blob;
    return blob?.type ?? null;
}

export function takeExport(frame) {
    const state = croppers.get(frame);
    const blob = state?.exported;
    if (state) state.exported = null;
    return blob;
}

// Ends cropping but keeps the cropped image on the preview tile.
export function finish(frame) {
    const state = croppers.get(frame);
    if (state) reset(state);
}

// Ends cropping and puts the preview tile back to its saved image.
export function cancel(frame) {
    const state = croppers.get(frame);
    if (!state) return;
    reset(state);
    clearPreview(frame);
}

export function clearPreview(frame) {
    const state = croppers.get(frame);
    const layer = state?.preview.querySelector('.system-tile-image');
    if (layer) layer.style.backgroundImage = '';
}

function loadFile(state) {
    const file = state.input.files?.[0];
    if (!file) return;
    if (!ACCEPTED_TYPES.includes(file.type)) {
        state.input.value = '';
        state.dotnet.invokeMethodAsync('OnCropError', 'Choose a PNG, JPEG or WebP image.');
        return;
    }
    if (file.size > MAX_FILE_BYTES) {
        state.input.value = '';
        state.dotnet.invokeMethodAsync('OnCropError', 'That image is too large (max 25 MB).');
        return;
    }

    reset(state);
    state.url = URL.createObjectURL(file);
    const source = new Image();
    source.onload = () => {
        state.source = source;
        state.image.src = state.url;
        state.image.style.width = `${source.naturalWidth}px`;
        state.image.style.height = `${source.naturalHeight}px`;
        state.dotnet.invokeMethodAsync('OnImageLoaded');
    };
    source.onerror = () => {
        reset(state);
        state.dotnet.invokeMethodAsync('OnCropError', 'That file could not be read as an image.');
    };
    source.src = state.url;
}

function reset(state) {
    clearTimeout(state.previewTimer);
    if (state.url) URL.revokeObjectURL(state.url);
    state.url = null;
    state.source = null;
    state.drag = null;
    state.input.value = '';
    state.image.removeAttribute('src');
}

function setZoom(state, factor, point) {
    if (!state.source) return;
    const scale = state.minScale * factor;
    // Zoom around the given point so it stays under the cursor (or centered).
    state.x = point.x - (point.x - state.x) * (scale / state.scale);
    state.y = point.y - (point.y - state.y) * (scale / state.scale);
    state.scale = scale;
    clampPosition(state);
    render(state);
    schedulePreview(state);
}

function refit(state) {
    if (!state.source || !state.frameWidth) return;
    const { width } = frameSize(state);
    if (!width) return;
    const ratio = width / state.frameWidth;
    state.scale *= ratio;
    state.minScale *= ratio;
    state.x *= ratio;
    state.y *= ratio;
    state.frameWidth = width;
    clampPosition(state);
    render(state);
}

// Keeps the frame fully covered by the image.
function clampPosition(state) {
    const { width, height } = frameSize(state);
    state.x = clamp(state.x, width - state.source.naturalWidth * state.scale, 0);
    state.y = clamp(state.y, height - state.source.naturalHeight * state.scale, 0);
}

function render(state) {
    state.image.style.transform = `translate(${state.x}px, ${state.y}px) scale(${state.scale})`;
}

function schedulePreview(state) {
    clearTimeout(state.previewTimer);
    state.previewTimer = setTimeout(() => {
        if (!state.source) return;
        const layer = state.preview.querySelector('.system-tile-image');
        if (layer) {
            const url = drawCrop(state, PREVIEW_WIDTH, PREVIEW_HEIGHT).toDataURL('image/jpeg', 0.85);
            layer.style.backgroundImage = `url("${url}")`;
        }
    }, 100);
}

function drawCrop(state, width, height) {
    const frameDimensions = frameSize(state);
    const canvas = document.createElement('canvas');
    canvas.width = width;
    canvas.height = height;
    const context = canvas.getContext('2d');
    context.imageSmoothingQuality = 'high';
    context.drawImage(
        state.source,
        -state.x / state.scale, -state.y / state.scale,
        frameDimensions.width / state.scale, frameDimensions.height / state.scale,
        0, 0, width, height);
    return canvas;
}

function frameSize(state) {
    return { width: state.frame.clientWidth, height: state.frame.clientHeight };
}

function frameCenter(state) {
    const { width, height } = frameSize(state);
    return { x: width / 2, y: height / 2 };
}

function pointInFrame(state, event) {
    const rect = state.frame.getBoundingClientRect();
    return { x: event.clientX - rect.left, y: event.clientY - rect.top };
}

function clamp(value, min, max) {
    return Math.min(Math.max(value, min), max);
}

function toBlob(canvas, type, quality) {
    return new Promise(resolve => canvas.toBlob(resolve, type, quality));
}
