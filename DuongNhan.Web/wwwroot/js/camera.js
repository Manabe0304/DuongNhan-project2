// Camera capture + client-side image quality analysis for skin scanning.
// Keeping the analysis in the browser means an image can be rejected before it
// is uploaded, which saves bandwidth and avoids a wasted AI run.

const IDEAL_MIN_SIDE = 1024;
const ANALYSIS_MAX_SIDE = 480;

function loadImage(dataUrl) {
    return new Promise((resolve, reject) => {
        const image = new Image();
        image.onload = () => resolve(image);
        image.onerror = () => reject(new Error('Không đọc được dữ liệu ảnh.'));
        image.src = dataUrl;
    });
}

export async function startCamera(videoElement) {
    const stream = await navigator.mediaDevices.getUserMedia({
        video: {
            facingMode: 'user',
            width: { ideal: 2048 },
            height: { ideal: 2048 }
        },
        audio: false
    });
    videoElement.srcObject = stream;
    await videoElement.play();
    return {
        width: videoElement.videoWidth,
        height: videoElement.videoHeight
    };
}

export function stopCamera(videoElement) {
    const stream = videoElement?.srcObject;
    if (stream) {
        stream.getTracks().forEach(track => track.stop());
        videoElement.srcObject = null;
    }
}

export function isCameraAvailable() {
    return !!(navigator.mediaDevices && navigator.mediaDevices.getUserMedia);
}

export function captureFrame(videoElement) {
    const canvas = document.createElement('canvas');
    canvas.width = videoElement.videoWidth;
    canvas.height = videoElement.videoHeight;
    canvas.getContext('2d').drawImage(videoElement, 0, 0);
    return canvas.toDataURL('image/jpeg', 0.95);
}

// Returns exposure, contrast and focus metrics used to grade the capture.
export async function analyzeImage(dataUrl) {
    const image = await loadImage(dataUrl);
    const scale = Math.min(1, ANALYSIS_MAX_SIDE / Math.max(image.width, image.height));
    const width = Math.max(1, Math.round(image.width * scale));
    const height = Math.max(1, Math.round(image.height * scale));

    const canvas = document.createElement('canvas');
    canvas.width = width;
    canvas.height = height;
    const context = canvas.getContext('2d', { willReadFrequently: true });
    context.drawImage(image, 0, 0, width, height);

    const { data } = context.getImageData(0, 0, width, height);
    const pixels = width * height;
    const luminance = new Float32Array(pixels);

    let sum = 0;
    let sumSquares = 0;
    let underexposed = 0;
    let overexposed = 0;

    for (let i = 0, p = 0; i < data.length; i += 4, p++) {
        const value = 0.299 * data[i] + 0.587 * data[i + 1] + 0.114 * data[i + 2];
        luminance[p] = value;
        sum += value;
        sumSquares += value * value;
        if (value < 40) underexposed++;
        else if (value > 220) overexposed++;
    }

    const brightness = sum / pixels;
    const contrast = Math.sqrt(Math.max(0, sumSquares / pixels - brightness * brightness));

    // Variance of the Laplacian is a standard focus / blur proxy:
    // sharp edges produce strong second derivatives, blurred ones do not.
    let laplacianSum = 0;
    let laplacianSquares = 0;
    let laplacianCount = 0;

    for (let y = 1; y < height - 1; y++) {
        for (let x = 1; x < width - 1; x++) {
            const p = y * width + x;
            const value = 4 * luminance[p]
                - luminance[p - 1]
                - luminance[p + 1]
                - luminance[p - width]
                - luminance[p + width];
            laplacianSum += value;
            laplacianSquares += value * value;
            laplacianCount++;
        }
    }

    const laplacianMean = laplacianCount ? laplacianSum / laplacianCount : 0;
    const sharpness = laplacianCount
        ? Math.sqrt(Math.max(0, laplacianSquares / laplacianCount - laplacianMean * laplacianMean))
        : 0;

    return {
        width: image.width,
        height: image.height,
        brightness,
        contrast,
        sharpness,
        underexposedRatio: underexposed / pixels,
        overexposedRatio: overexposed / pixels,
        meetsMinimumResolution: Math.min(image.width, image.height) >= IDEAL_MIN_SIDE
    };
}

// Centre crop to a square, normalised to at most 1280px, for a consistent input.
export async function cropToSquare(dataUrl, zoom = 1) {
    const image = await loadImage(dataUrl);
    const side = Math.min(image.width, image.height) / Math.max(1, zoom);
    const sx = (image.width - side) / 2;
    const sy = (image.height - side) / 2;
    const output = Math.min(1280, Math.round(side));

    const canvas = document.createElement('canvas');
    canvas.width = output;
    canvas.height = output;
    canvas.getContext('2d').drawImage(image, sx, sy, side, side, 0, 0, output, output);
    return canvas.toDataURL('image/jpeg', 0.92);
}

// Caps the longest side so uploads stay small (and fast) without visible loss.
export async function downscale(dataUrl, maxSide = 2048) {
    const image = await loadImage(dataUrl);
    const scale = Math.min(1, maxSide / Math.max(image.width, image.height));
    if (scale >= 1) return dataUrl;

    const canvas = document.createElement('canvas');
    canvas.width = Math.round(image.width * scale);
    canvas.height = Math.round(image.height * scale);
    canvas.getContext('2d').drawImage(image, 0, 0, canvas.width, canvas.height);
    return canvas.toDataURL('image/jpeg', 0.92);
}
