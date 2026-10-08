import { useEffect, useRef, useState } from 'react';

function QrCodeScanner({ onDetected, onClose }) {
    const videoRef = useRef(null);
    const onDetectedRef = useRef(onDetected);
    const [error, setError] = useState('');

    useEffect(() => {
        onDetectedRef.current = onDetected;
    }, [onDetected]);

    useEffect(() => {
        let stopped = false;
        let controls;

        async function startCamera() {
            if (!navigator.mediaDevices?.getUserMedia) {
                setError('El navegador no permite usar la cámara en esta dirección. Abre PAQTERIA en localhost o mediante HTTPS.');
                return;
            }

            try {
                const { BrowserQRCodeReader } = await import('@zxing/browser');
                if (stopped) return;
                const reader = new BrowserQRCodeReader();
                const scannerControls = await reader.decodeFromVideoDevice(
                    undefined,
                    videoRef.current,
                    (result, _decodeError, activeControls) => {
                        if (!result || stopped) return;
                        const content = result.getText().trim();
                        if (!content) {
                            setError('El código QR está vacío.');
                            return;
                        }
                        if (content.length > 4096) {
                            stopped = true;
                            activeControls.stop();
                            setError('El contenido del QR supera el límite de lectura de PAQTERIA.');
                            return;
                        }

                        stopped = true;
                        activeControls.stop();
                        const validationError = onDetectedRef.current(content);
                        if (validationError) {
                            setError(validationError);
                            return;
                        }
                    },
                );
                controls = scannerControls;
                if (stopped) controls.stop();
            } catch (cause) {
                if (stopped) return;
                setError(cause?.name === 'NotAllowedError'
                    ? 'Permite el acceso a la cámara en el navegador y vuelve a intentarlo.'
                    : 'No se pudo iniciar la cámara. Comprueba que esté disponible y vuelve a intentarlo.');
            }
        }

        void startCamera();
        return () => {
            stopped = true;
            controls?.stop();
        };
    }, []);

    return <section className="qr-scanner-panel" aria-label="Escáner de código QR">
        <video ref={videoRef} autoPlay muted playsInline aria-label="Vista de la cámara para escanear el código QR" />
        <div className="qr-scanner-controls">
            <p role={error ? 'alert' : 'status'}>{error || 'Escanea un folio de texto o un JSON de paquete versión 1.'}</p>
            <button className="secondary-button" onClick={onClose} type="button">Cerrar cámara</button>
        </div>
    </section>;
}

export default QrCodeScanner;
