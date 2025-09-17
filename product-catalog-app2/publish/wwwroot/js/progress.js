// Progress Indicator Utility
// Prevent duplicate class definitions
if (typeof window.ProgressIndicator === 'undefined') {

class ProgressIndicator {
    constructor() {
        this.overlay = null;
    }

    show(message = 'İşlem devam ediyor...', details = '') {
        // Zaten gösteriliyorsa güncelle
        if (this.overlay) {
            this.updateMessage(message, details);
            return;
        }

        // Overlay oluştur
        this.overlay = document.createElement('div');
        this.overlay.className = 'progress-overlay';
        this.overlay.innerHTML = `
            <div class="progress-content">
                <button class="progress-close-btn" onclick="window.progressIndicator.hide()" title="Kapat (ESC)">×</button>
                <div class="progress-spinner"></div>
                <div class="progress-message">${message}</div>
                ${details ? `<div class="progress-details">${details}</div>` : ''}
                <div class="progress-timer">0 saniye</div>
                <div class="progress-tips">
                    <small>💡 İpucu: Büyük dosyalar için işlem zaman alabilir. Sayfayı kapatmayın.</small>
                    <small>⚠️ Lütfen işlem tamamlanana kadar bekleyin.</small>
                </div>
            </div>
        `;

        // CSS stilleri ekle
        if (!document.getElementById('progress-styles')) {
            const style = document.createElement('style');
            style.id = 'progress-styles';
            style.textContent = `
                .progress-overlay {
                    position: fixed;
                    top: 0;
                    left: 0;
                    width: 100vw;
                    height: 100vh;
                    background: rgba(0, 0, 0, 0.8);
                    display: flex;
                    justify-content: center;
                    align-items: center;
                    z-index: 99999;
                }

                .progress-content {
                    background: white;
                    padding: 40px;
                    border-radius: 12px;
                    text-align: center;
                    box-shadow: 0 10px 30px rgba(0, 0, 0, 0.3);
                    min-width: 300px;
                    max-width: 500px;
                    position: relative;
                }

                .progress-close-btn {
                    position: absolute;
                    top: 10px;
                    right: 15px;
                    background: none;
                    border: none;
                    font-size: 24px;
                    color: #999;
                    cursor: pointer;
                    width: 30px;
                    height: 30px;
                    display: flex;
                    align-items: center;
                    justify-content: center;
                    border-radius: 50%;
                    transition: all 0.2s;
                }

                .progress-close-btn:hover {
                    background: #f5f5f5;
                    color: #333;
                }

                .progress-spinner {
                    width: 50px;
                    height: 50px;
                    border: 4px solid #f3f3f3;
                    border-top: 4px solid #007bff;
                    border-radius: 50%;
                    animation: spin 1s linear infinite;
                    margin: 0 auto 20px;
                }

                @keyframes spin {
                    0% { transform: rotate(0deg); }
                    100% { transform: rotate(360deg); }
                }

                .progress-message {
                    font-size: 18px;
                    font-weight: 500;
                    color: #333;
                    margin-bottom: 10px;
                }

                .progress-details {
                    font-size: 14px;
                    color: #666;
                    margin-bottom: 15px;
                }

                .progress-timer {
                    font-size: 12px;
                    color: #999;
                    margin-top: 15px;
                }

                .progress-tips {
                    margin-top: 15px;
                    padding: 10px;
                    background: #f8f9fa;
                    border-radius: 6px;
                    border-left: 3px solid #007bff;
                }

                .progress-tips small {
                    color: #6c757d;
                    font-size: 11px;
                }
            `;
            document.head.appendChild(style);
        }

        document.body.appendChild(this.overlay);

        // Timer başlat
        this.startTime = Date.now();
        this.timerInterval = setInterval(() => {
            const elapsed = Math.floor((Date.now() - this.startTime) / 1000);
            const timerElement = this.overlay.querySelector('.progress-timer');
            if (timerElement) {
                timerElement.textContent = `${elapsed} saniye`;
            }
        }, 1000);
    }

    updateMessage(message, details = '') {
        if (!this.overlay) return;
        
        const messageEl = this.overlay.querySelector('.progress-message');
        const detailsEl = this.overlay.querySelector('.progress-details');
        
        if (messageEl) messageEl.textContent = message;
        if (detailsEl && details) detailsEl.textContent = details;
    }

    hide() {
        if (this.overlay) {
            if (this.timerInterval) {
                clearInterval(this.timerInterval);
            }
            this.overlay.remove();
            this.overlay = null;
        }
    }
}

// Global instance
window.progressIndicator = new ProgressIndicator();

// Global progress function
window.showProgress = function(message, details = '') {
    window.progressIndicator.show(message, details);
};

// Manual progress kapatma
window.hideProgress = function() {
    window.progressIndicator.hide();
};

// Expose ProgressIndicator globally for compatibility
window.ProgressIndicator = ProgressIndicator;

} // End of ProgressIndicator class definition guard