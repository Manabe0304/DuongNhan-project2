export async function startCamera(videoElement) {
    const stream = await navigator.mediaDevices.getUserMedia({
        video: { facingMode: 'user', width: { ideal: 1920 }, height: { ideal: 1080 } }
    });
    videoElement.srcObject = stream;
    await videoElement.play();
    return true;
}

export function stopCamera(videoElement) {
    const stream = videoElement.srcObject;
    if (stream) {
        stream.getTracks().forEach(t => t.stop());
        videoElement.srcObject = null;
    }
}

export function captureFrame(videoElement) {
    const canvas = document.createElement('canvas');
    canvas.width = videoElement.videoWidth;
    canvas.height = videoElement.videoHeight;
    canvas.getContext('2d').drawImage(videoElement, 0, 0);
    return canvas.toDataURL('image/jpeg', 0.95);
}