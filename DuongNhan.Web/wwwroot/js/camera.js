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

// Try the richest constraints first, then relax them. Some devices (older Android, some webcams)
// reject a width/height or facingMode hint, and a single strict request would leave a black preview.
const CAMERA_ATTEMPTS = [
    { video: { facingMode: 'user', width: { ideal: 1920 }, height: { ideal: 1080 } }, audio: false },
    { video: { facingMode: 'user' }, audio: false },
    { video: true, audio: false }
];

const CAPTURE_MAX_SIDE = 2048;

function describeCameraError(error) {
    switch (error?.name) {
        case 'NotAllowedError':
        case 'PermissionDeniedError':
        case 'SecurityError':
            return 'Trình duyệt đang chặn quyền camera. Hãy bấm biểu tượng ổ khóa cạnh thanh địa chỉ, chọn cho phép Camera rồi bấm "Bật camera" lại.';
        case 'NotFoundError':
        case 'DevicesNotFoundError':
            return 'Không tìm thấy camera trên thiết bị này.';
        case 'NotReadableError':
        case 'TrackStartError':
            return 'Camera đang được ứng dụng khác sử dụng (Zoom, Meet, camera app...). Hãy đóng ứng dụng đó rồi thử lại.';
        case 'OverconstrainedError':
        case 'ConstraintNotSatisfiedError':
            return 'Camera không đáp ứng được cấu hình yêu cầu. Hãy thử lại hoặc dùng camera của điện thoại.';
        default:
            return `Không mở được camera${error?.name ? ` (${error.name})` : ''}.`;
    }
}

function waitForVideoSize(video, timeoutMs) {
    return new Promise(resolve => {
        if (video.videoWidth > 0) { resolve(true); return; }

        const startedAt = Date.now();
        const timer = setInterval(() => {
            if (video.videoWidth > 0) {
                clearInterval(timer);
                resolve(true);
            } else if (Date.now() - startedAt > timeoutMs) {
                clearInterval(timer);
                resolve(false);
            }
        }, 100);
    });
}

const DEVICE_KEY = 'dn-camera-device';
// Virtual cameras (OBS, ManyCam, NVIDIA Broadcast...) show a logo/black frame when their app is idle
// and never light up the real webcam, so they must not be picked automatically.
const VIRTUAL_CAMERA = /obs|virtual|manycam|snap camera|droidcam|xsplit|broadcast|iriun|epoccam|camo|ndi|splitcam|youcam|vcam/i;

function readSavedDevice() {
    try { return localStorage.getItem(DEVICE_KEY); } catch { return null; }
}

function saveDevice(deviceId) {
    try {
        if (deviceId) localStorage.setItem(DEVICE_KEY, deviceId);
        else localStorage.removeItem(DEVICE_KEY);
    } catch { /* storage may be blocked; the choice just is not remembered */ }
}

async function listVideoInputs() {
    try {
        const devices = await navigator.mediaDevices.enumerateDevices();
        return devices
            .filter(d => d.kind === 'videoinput')
            .map((d, i) => ({ deviceId: d.deviceId, label: d.label || `Camera ${i + 1}` }));
    } catch {
        return [];
    }
}

function exactDevice(deviceId) {
    return {
        video: { deviceId: { exact: deviceId }, width: { ideal: 1920 }, height: { ideal: 1080 } },
        audio: false
    };
}

// Never throws: returns { ok, width, height, deviceId, cameras, code, message } so the UI can show a
// precise reason instead of a generic failure (and so the error text is not lost across SignalR).
// `chosenDeviceId` is a camera the user picked explicitly; otherwise a remembered pick is used, and
// failing that the browser's default (replaced by a real webcam if the default is a virtual camera).
export async function startCamera(videoElement, chosenDeviceId) {
    stopCamera(videoElement);

    if (!navigator.mediaDevices || !navigator.mediaDevices.getUserMedia) {
        return {
            ok: false,
            code: window.isSecureContext ? 'unsupported' : 'insecure',
            message: window.isSecureContext
                ? 'Trình duyệt này không hỗ trợ truy cập camera.'
                : 'Camera chỉ hoạt động trên kết nối HTTPS. Hãy mở trang bằng địa chỉ https://.'
        };
    }

    const userPicked = !!chosenDeviceId;
    const preferred = chosenDeviceId || readSavedDevice();

    let stream = null;
    let lastError = null;

    if (preferred) {
        try {
            stream = await navigator.mediaDevices.getUserMedia(exactDevice(preferred));
        } catch (error) {
            lastError = error;
            if (!userPicked) saveDevice(null); // the remembered camera is gone; forget it
        }
    }

    if (!stream) {
        for (const constraints of CAMERA_ATTEMPTS) {
            try {
                stream = await navigator.mediaDevices.getUserMedia(constraints);
                break;
            } catch (error) {
                lastError = error;
                // A permission refusal will not be fixed by relaxing constraints.
                if (['NotAllowedError', 'PermissionDeniedError', 'SecurityError', 'NotFoundError', 'NotReadableError'].includes(error?.name)) break;
            }
        }
    }

    if (!stream) {
        return { ok: false, code: lastError?.name ?? 'unknown', message: describeCameraError(lastError) };
    }

    // Device labels are only available after permission is granted, so the virtual-camera check
    // has to happen now. If the browser handed us OBS & co., switch to a real webcam when one exists.
    let cameras = await listVideoInputs();
    let track = stream.getVideoTracks()[0];
    if (!userPicked && track && VIRTUAL_CAMERA.test(track.label)) {
        const real = cameras.find(c => c.deviceId !== track.getSettings().deviceId && !VIRTUAL_CAMERA.test(c.label));
        if (real) {
            try {
                const realStream = await navigator.mediaDevices.getUserMedia(exactDevice(real.deviceId));
                stream.getTracks().forEach(t => t.stop());
                stream = realStream;
                track = stream.getVideoTracks()[0];
            } catch {
                // Real webcam busy or blocked: keep the virtual one rather than failing outright.
            }
        }
    }

    const currentDeviceId = track?.getSettings().deviceId ?? null;
    if (userPicked) saveDevice(currentDeviceId);

    // iOS Safari only plays inline when both the property and the attribute are set.
    videoElement.muted = true;
    videoElement.playsInline = true;
    videoElement.setAttribute('playsinline', '');
    videoElement.setAttribute('muted', '');
    videoElement.srcObject = stream;

    try {
        await videoElement.play();
    } catch (error) {
        // AbortError just means a newer load interrupted play(); anything else is a real failure.
        if (error?.name !== 'AbortError') {
            stopCamera(videoElement);
            return { ok: false, code: error?.name ?? 'play', message: 'Không phát được hình từ camera. Hãy thử lại.' };
        }
    }

    const hasFrames = await waitForVideoSize(videoElement, 5000);
    if (!hasFrames) {
        stopCamera(videoElement);
        return { ok: false, code: 'no-frames', message: 'Camera đã mở nhưng không có hình. Hãy thử lại hoặc chọn camera khác.' };
    }

    return {
        ok: true,
        width: videoElement.videoWidth,
        height: videoElement.videoHeight,
        deviceId: currentDeviceId,
        cameras
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

// Returns null when the video has no frame yet (videoWidth is 0), instead of a blank/black image.
export function captureFrame(videoElement) {
    const sourceWidth = videoElement.videoWidth;
    const sourceHeight = videoElement.videoHeight;
    if (!sourceWidth || !sourceHeight) return null;

    const scale = Math.min(1, CAPTURE_MAX_SIDE / Math.max(sourceWidth, sourceHeight));
    const canvas = document.createElement('canvas');
    canvas.width = Math.round(sourceWidth * scale);
    canvas.height = Math.round(sourceHeight * scale);
    canvas.getContext('2d').drawImage(videoElement, 0, 0, canvas.width, canvas.height);
    return canvas.toDataURL('image/jpeg', 0.92);
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
