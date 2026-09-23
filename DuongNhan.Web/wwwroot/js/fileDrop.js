// Drag & drop image reading for the upload form.
// Blazor's DragEventArgs only exposes file names (not content), so the actual
// bytes are read here and handed back to .NET as a data URL.

export function registerDropZone(element, dotNetRef, maxBytes) {
    const stop = event => {
        event.preventDefault();
        event.stopPropagation();
    };

    const onDragOver = event => {
        stop(event);
        element.classList.add('dragging');
    };

    const onDragLeave = event => {
        stop(event);
        element.classList.remove('dragging');
    };

    const onDrop = event => {
        stop(event);
        element.classList.remove('dragging');

        const file = event.dataTransfer?.files?.[0];
        if (!file) return;

        if (!file.type.startsWith('image/')) {
            dotNetRef.invokeMethodAsync('OnDropError', 'Tệp đã thả không phải là ảnh.');
            return;
        }

        if (maxBytes > 0 && file.size > maxBytes) {
            dotNetRef.invokeMethodAsync('OnDropError', 'Ảnh vượt quá 10 MB. Vui lòng chọn ảnh nhỏ hơn.');
            return;
        }

        const reader = new FileReader();
        reader.onload = () => dotNetRef.invokeMethodAsync('OnDropImage', reader.result, file.name);
        reader.onerror = () => dotNetRef.invokeMethodAsync('OnDropError', 'Không đọc được tệp đã thả.');
        reader.readAsDataURL(file);
    };

    element.addEventListener('dragover', onDragOver);
    element.addEventListener('dragleave', onDragLeave);
    element.addEventListener('drop', onDrop);

    return {
        dispose: () => {
            element.removeEventListener('dragover', onDragOver);
            element.removeEventListener('dragleave', onDragLeave);
            element.removeEventListener('drop', onDrop);
        }
    };
}
