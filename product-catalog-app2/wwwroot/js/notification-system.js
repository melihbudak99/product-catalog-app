/**
 * Advanced Notification System - Unified & Optimized
 * Modern toast notifications with multiple display options
 * Singleton pattern to prevent duplicates
 */

class NotificationSystem {
    constructor() {
        // Singleton pattern - prevent duplicate instances
        if (NotificationSystem.instance) {
            console.log('🔔 NotificationSystem: Returning existing instance');
            return NotificationSystem.instance;
        }
        
        this.container = null;
        this.notifications = new Map();
        this.position = 'top-right'; // top-right, top-left, bottom-right, bottom-left, top-center, bottom-center
        this.defaultDuration = 7000; // Increased from 5000 for better visibility
        this.maxNotifications = 5;
        this.soundEnabled = true;
        this.queue = [];
        this.isProcessing = false;
        this.isInitialized = false;
        
        // Enhanced notification features
        this.modalNotifications = new Map(); // For important notifications
        this.tabNotification = { active: false, originalTitle: document.title };
        this.browserNotificationEnabled = false;
        this.vibrationEnabled = true;
        
        NotificationSystem.instance = this;
        this.init();
        console.log('✅ NotificationSystem: New instance created');
    }

    init() {
        // Prevent duplicate initialization
        if (this.isInitialized) {
            console.log('🔔 NotificationSystem: Already initialized');
            return;
        }

        // Create notification container
        this.container = document.createElement('div');
        this.container.className = 'notification-container';
        this.container.id = 'notification-container';
        document.body.appendChild(this.container);

        // Add CSS styles if not already present
        if (!document.getElementById('notification-styles')) {
            this.addStyles();
        }

        // Listen for page visibility changes to handle notifications
        document.addEventListener('visibilitychange', () => {
            if (!document.hidden && this.queue.length > 0) {
                this.processQueue();
            }
            
            // Reset tab notification when user returns
            if (!document.hidden && this.tabNotification.active) {
                this.clearTabNotification();
            }
        });

        // Initialize user interaction tracking for notification permission
        this.initUserInteractionTracking();
        
        // Initialize vibration support
        this.initVibrationSupport();
        
        this.isInitialized = true;
        console.log('✅ NotificationSystem: Initialization complete');
    }

    /**
     * Initialize user interaction tracking for notification permission
     */
    initUserInteractionTracking() {
        const requestPermissionOnInteraction = () => {
            this.requestNotificationPermission();
            // Remove listeners after first interaction
            document.removeEventListener('click', requestPermissionOnInteraction);
            document.removeEventListener('keydown', requestPermissionOnInteraction);
            document.removeEventListener('touchstart', requestPermissionOnInteraction);
        };
        
        // Wait for user interaction before requesting permission
        document.addEventListener('click', requestPermissionOnInteraction, { once: true });
        document.addEventListener('keydown', requestPermissionOnInteraction, { once: true });
        document.addEventListener('touchstart', requestPermissionOnInteraction, { once: true });
    }

    initStickyBanner() {
        // Create sticky banner container at the top of the page
        let stickyContainer = document.getElementById('sticky-banner-container');
        if (!stickyContainer) {
            stickyContainer = document.createElement('div');
            stickyContainer.className = 'sticky-banner-container';
            stickyContainer.id = 'sticky-banner-container';
            
            // Insert after body opening or before main content
            const mainContent = document.querySelector('.container') || document.body.firstElementChild;
            if (mainContent && mainContent.parentNode) {
                mainContent.parentNode.insertBefore(stickyContainer, mainContent);
            } else {
                document.body.insertBefore(stickyContainer, document.body.firstElementChild);
            }
        }
        this.stickyContainer = stickyContainer;
    }

    addStyles() {
        const style = document.createElement('style');
        style.id = 'notification-styles';
        style.textContent = `
            /* Notification Container */
            .notification-container {
                position: fixed;
                z-index: 10000;
                pointer-events: none;
                top: 20px;
                right: 20px;
                max-width: 400px;
                width: 100%;
            }

            .notification-container.top-left {
                top: 20px;
                left: 20px;
                right: auto;
            }

            .notification-container.bottom-right {
                top: auto;
                bottom: 20px;
                right: 20px;
            }

            .notification-container.bottom-left {
                top: auto;
                bottom: 20px;
                left: 20px;
                right: auto;
            }

            .notification-container.top-center {
                top: 20px;
                left: 50%;
                right: auto;
                transform: translateX(-50%);
            }

            .notification-container.bottom-center {
                top: auto;
                bottom: 20px;
                left: 50%;
                right: auto;
                transform: translateX(-50%);
            }

            /* Individual Notification */
            .notification {
                background: white;
                border-radius: 12px;
                box-shadow: 0 8px 32px rgba(0, 0, 0, 0.12);
                margin-bottom: 10px;
                padding: 16px 20px;
                pointer-events: auto;
                position: relative;
                transform: translateX(100%);
                transition: all 0.3s cubic-bezier(0.68, -0.55, 0.265, 1.55);
                border-left: 4px solid #007bff;
                min-height: 60px;
                display: flex;
                align-items: center;
                overflow: hidden;
            }

            .notification.show {
                transform: translateX(0);
            }

            .notification.hide {
                transform: translateX(100%);
                opacity: 0;
            }

            /* Notification Types */
            .notification.success {
                border-left-color: #28a745;
                background: linear-gradient(135deg, #d4edda 0%, #c3e6cb 100%);
            }

            .notification.error {
                border-left-color: #dc3545;
                background: linear-gradient(135deg, #f8d7da 0%, #f5c6cb 100%);
            }

            .notification.warning {
                border-left-color: #ffc107;
                background: linear-gradient(135deg, #fff3cd 0%, #ffeaa7 100%);
            }

            .notification.info {
                border-left-color: #17a2b8;
                background: linear-gradient(135deg, #d1ecf1 0%, #b8daff 100%);
            }

            /* Notification Content */
            .notification-content {
                flex: 1;
                display: flex;
                align-items: center;
            }

            .notification-icon {
                font-size: 20px;
                margin-right: 12px;
                min-width: 20px;
            }

            .notification.success .notification-icon {
                color: #28a745;
            }

            .notification.error .notification-icon {
                color: #dc3545;
            }

            .notification.warning .notification-icon {
                color: #ffc107;
            }

            .notification.info .notification-icon {
                color: #17a2b8;
            }

            .notification-text {
                flex: 1;
            }

            .notification-title {
                font-weight: 600;
                font-size: 14px;
                margin: 0 0 4px 0;
                color: #2c3e50;
            }

            .notification-message {
                font-size: 13px;
                color: #5a6c7d;
                margin: 0;
                line-height: 1.4;
            }

            /* Close Button */
            .notification-close {
                position: absolute;
                top: 8px;
                right: 8px;
                background: none;
                border: none;
                font-size: 16px;
                color: #6c757d;
                cursor: pointer;
                width: 20px;
                height: 20px;
                display: flex;
                align-items: center;
                justify-content: center;
                border-radius: 50%;
                transition: all 0.2s ease;
                padding: 0;
            }

            .notification-close:hover {
                background: rgba(0, 0, 0, 0.1);
                color: #495057;
            }

            /* Progress Bar */
            .notification-progress {
                position: absolute;
                bottom: 0;
                left: 0;
                height: 3px;
                background: rgba(0, 0, 0, 0.1);
                border-radius: 0 0 12px 12px;
                transition: width linear;
            }

            .notification.success .notification-progress {
                background: #28a745;
            }

            .notification.error .notification-progress {
                background: #dc3545;
            }

            .notification.warning .notification-progress {
                background: #ffc107;
            }

            .notification.info .notification-progress {
                background: #17a2b8;
            }

            /* Sticky Banner */
            .sticky-banner-container {
                position: relative;
                z-index: 9999;
                width: 100%;
            }

            .sticky-banner {
                width: 100%;
                padding: 12px 20px;
                display: flex;
                align-items: center;
                justify-content: space-between;
                font-size: 14px;
                font-weight: 500;
                border-bottom: 1px solid rgba(255, 255, 255, 0.2);
                transition: all 0.3s ease;
                position: relative;
                overflow: hidden;
            }

            .sticky-banner::before {
                content: '';
                position: absolute;
                top: 0;
                left: -100%;
                width: 100%;
                height: 100%;
                background: linear-gradient(90deg, transparent, rgba(255, 255, 255, 0.2), transparent);
                transition: left 2s ease;
            }

            .sticky-banner.animate::before {
                left: 100%;
            }

            .sticky-banner.success {
                background: linear-gradient(135deg, #28a745 0%, #20c997 100%);
                color: white;
            }

            .sticky-banner.error {
                background: linear-gradient(135deg, #dc3545 0%, #e74c3c 100%);
                color: white;
            }

            .sticky-banner.warning {
                background: linear-gradient(135deg, #ffc107 0%, #f39c12 100%);
                color: #2c3e50;
            }

            .sticky-banner.info {
                background: linear-gradient(135deg, #17a2b8 0%, #3498db 100%);
                color: white;
            }

            .sticky-banner-content {
                display: flex;
                align-items: center;
                flex: 1;
            }

            .sticky-banner-icon {
                font-size: 18px;
                margin-right: 10px;
            }

            .sticky-banner-close {
                background: none;
                border: none;
                color: inherit;
                font-size: 18px;
                cursor: pointer;
                padding: 4px;
                border-radius: 50%;
                transition: background 0.2s ease;
                margin-left: 10px;
            }

            .sticky-banner-close:hover {
                background: rgba(0, 0, 0, 0.1);
            }

            /* Responsive Design */
            @media (max-width: 768px) {
                .notification-container {
                    left: 10px;
                    right: 10px;
                    top: 10px;
                    max-width: none;
                    width: auto;
                }

                .notification {
                    margin-bottom: 8px;
                    padding: 12px 16px;
                }

                .sticky-banner {
                    padding: 10px 15px;
                    font-size: 13px;
                }
            }

            /* Animation Keyframes */
            @keyframes slideInRight {
                from {
                    transform: translateX(100%);
                    opacity: 0;
                }
                to {
                    transform: translateX(0);
                    opacity: 1;
                }
            }

            @keyframes slideOutRight {
                from {
                    transform: translateX(0);
                    opacity: 1;
                }
                to {
                    transform: translateX(100%);
                    opacity: 0;
                }
            }

            @keyframes slideInLeft {
                from {
                    transform: translateX(-100%);
                    opacity: 0;
                }
                to {
                    transform: translateX(0);
                    opacity: 1;
                }
            }

            @keyframes slideOutLeft {
                from {
                    transform: translateX(0);
                    opacity: 1;
                }
                to {
                    transform: translateX(-100%);
                    opacity: 0;
                }
            }

            @keyframes pulse {
                0% {
                    transform: scale(1);
                }
                50% {
                    transform: scale(1.05);
                }
                100% {
                    transform: scale(1);
                }
            }

            /* Modal Notifications - Enhanced specificity to avoid conflicts */
            .notification-modal-backdrop {
                position: fixed !important;
                top: 0 !important;
                left: 0 !important;
                width: 100% !important;
                height: 100% !important;
                background: rgba(0, 0, 0, 0.7) !important;
                z-index: 99999 !important;
                display: flex !important;
                align-items: center !important;
                justify-content: center !important;
                opacity: 0;
                visibility: hidden;
                transition: all 0.3s ease;
                margin: 0 !important;
                padding: 0 !important;
                box-sizing: border-box !important;
            }

            .notification-modal-backdrop.show {
                opacity: 1 !important;
                visibility: visible !important;
            }

            .notification-modal {
                background: white !important;
                border-radius: 16px !important;
                box-shadow: 0 20px 60px rgba(0, 0, 0, 0.3) !important;
                max-width: 500px !important;
                width: 90% !important;
                max-height: 80vh !important;
                overflow: hidden !important;
                transform: scale(0.7) translateY(-50px);
                transition: all 0.3s cubic-bezier(0.68, -0.55, 0.265, 1.55) !important;
                position: relative !important;
                margin: 0 !important;
                padding: 0 !important;
            }

            .notification-modal-backdrop.show .notification-modal {
                transform: scale(1) translateY(0) !important;
            }

            .modal-header {
                padding: 24px 24px 16px 24px !important;
                display: flex !important;
                align-items: center !important;
                border-bottom: 1px solid #e9ecef !important;
                margin: 0 !important;
            }

            .modal-icon {
                font-size: 32px !important;
                margin-right: 16px !important;
                animation: pulse 2s infinite;
            }

            .notification-modal.success .modal-icon {
                color: #28a745 !important;
            }

            .notification-modal.error .modal-icon {
                color: #dc3545 !important;
            }

            .notification-modal.warning .modal-icon {
                color: #ffc107 !important;
            }

            .notification-modal.info .modal-icon {
                color: #17a2b8 !important;
            }

            .modal-title {
                font-size: 20px !important;
                font-weight: 600 !important;
                color: #2c3e50 !important;
                margin: 0 !important;
            }

            .modal-body {
                padding: 16px 24px !important;
                margin: 0 !important;
            }

            .modal-message {
                font-size: 16px !important;
                line-height: 1.5 !important;
                color: #495057 !important;
                margin: 0 !important;
            }

            .modal-message h4 {
                margin: 0 0 10px 0 !important;
                font-size: 14px !important;
                font-weight: 600 !important;
            }

            .modal-message ul {
                margin: 0 !important;
                padding-left: 20px !important;
            }

            .modal-message li {
                margin-bottom: 8px !important;
            }

            .modal-footer {
                padding: 16px 24px 24px 24px !important;
                display: flex !important;
                justify-content: flex-end !important;
                gap: 12px !important;
                margin: 0 !important;
            }

            .modal-btn {
                padding: 10px 24px !important;
                border: none !important;
                border-radius: 8px !important;
                font-size: 14px !important;
                font-weight: 600 !important;
                cursor: pointer !important;
                transition: all 0.2s ease !important;
                min-width: 80px !important;
                margin: 0 !important;
            }

            .modal-btn-confirm {
                background: linear-gradient(135deg, #007bff 0%, #0056b3 100%) !important;
                color: white !important;
            }

            .modal-btn-confirm:hover {
                background: linear-gradient(135deg, #0056b3 0%, #004085 100%) !important;
                transform: translateY(-1px) !important;
            }

            .modal-btn-cancel {
                background: #f8f9fa !important;
                color: #6c757d !important;
                border: 1px solid #dee2e6 !important;
            }

            .modal-btn-cancel:hover {
                background: #e9ecef !important;
                color: #495057 !important;
            }

            /* Enhanced Sticky Banner */
            .sticky-banner.critical {
                background: linear-gradient(135deg, #dc3545 0%, #c82333 100%);
                animation: shake 0.5s ease-in-out 0s 3;
                box-shadow: 0 4px 20px rgba(220, 53, 69, 0.3);
            }

            @keyframes shake {
                0%, 100% { transform: translateX(0); }
                25% { transform: translateX(-5px); }
                75% { transform: translateX(5px); }
            }

            /* Responsive Modal */
            @media (max-width: 768px) {
                .notification-modal {
                    margin: 20px !important;
                    width: calc(100% - 40px) !important;
                }

                .modal-header {
                    padding: 20px 20px 12px 20px !important;
                }

                .modal-icon {
                    font-size: 28px !important;
                    margin-right: 12px !important;
                }

                .modal-title {
                    font-size: 18px !important;
                }

                .modal-body {
                    padding: 12px 20px !important;
                }

                .modal-footer {
                    padding: 12px 20px 20px 20px !important;
                    flex-direction: column !important;
                }

                .modal-btn {
                    width: 100% !important;
                    margin-bottom: 8px !important;
                }

                .modal-btn:last-child {
                    margin-bottom: 0 !important;
                }
            }
        `;
        document.head.appendChild(style);
    }

    show(options = {}) {
        const config = {
            type: options.type || 'info', // success, error, warning, info
            title: options.title || '',
            message: options.message || '',
            duration: options.duration || this.defaultDuration,
            persistent: options.persistent || false, // Don't auto-hide
            showProgress: options.showProgress !== false,
            allowClose: options.allowClose !== false,
            sound: options.sound !== false,
            sticky: options.sticky || false, // Show as sticky banner
            priority: options.priority || 'normal', // high, normal, low
            action: options.action || null, // { text: 'Action', callback: function() {} }
            id: options.id || this.generateId()
        };

        if (config.sticky) {
            return this.showStickyBanner(config);
        }

        // Check if notification with same ID already exists
        if (this.notifications.has(config.id)) {
            this.hide(config.id);
        }

        // Add to queue if max notifications reached
        if (this.notifications.size >= this.maxNotifications) {
            this.queue.push(config);
            return config.id;
        }

        return this.displayNotification(config);
    }

    displayNotification(config) {
        const notification = this.createNotificationElement(config);
        this.container.appendChild(notification);
        this.notifications.set(config.id, { element: notification, config });

        // Trigger animation
        requestAnimationFrame(() => {
            notification.classList.add('show');
        });

        // Auto-hide if not persistent
        if (!config.persistent) {
            const timeout = setTimeout(() => {
                this.hide(config.id);
            }, config.duration);

            this.notifications.get(config.id).timeout = timeout;
        }

        // Play sound
        if (config.sound && this.soundEnabled) {
            this.playNotificationSound(config.type);
        }

        // Process queue
        this.processQueue();

        return config.id;
    }

    createNotificationElement(config) {
        const notification = document.createElement('div');
        notification.className = `notification ${config.type}`;
        notification.dataset.id = config.id;

        const icons = {
            success: 'fas fa-check-circle',
            error: 'fas fa-exclamation-circle',
            warning: 'fas fa-exclamation-triangle',
            info: 'fas fa-info-circle'
        };

        let actionHtml = '';
        if (config.action) {
            actionHtml = `
                <button class="notification-action" onclick="window.notificationSystem.executeAction('${config.id}')">
                    ${config.action.text}
                </button>
            `;
        }

        notification.innerHTML = `
            <div class="notification-content">
                <i class="notification-icon ${icons[config.type] || icons.info}"></i>
                <div class="notification-text">
                    ${config.title ? `<div class="notification-title">${config.title}</div>` : ''}
                    <div class="notification-message">${config.message}</div>
                </div>
                ${actionHtml}
            </div>
            ${config.allowClose ? '<button class="notification-close" onclick="window.notificationSystem.hide(\'' + config.id + '\')">&times;</button>' : ''}
            ${config.showProgress ? '<div class="notification-progress"></div>' : ''}
        `;

        // Add progress bar animation if enabled
        if (config.showProgress && !config.persistent) {
            const progressBar = notification.querySelector('.notification-progress');
            if (progressBar) {
                progressBar.style.width = '100%';
                progressBar.style.transition = `width ${config.duration}ms linear`;
                requestAnimationFrame(() => {
                    progressBar.style.width = '0%';
                });
            }
        }

        // Store action callback
        if (config.action) {
            this.notifications.set(config.id, { 
                element: notification, 
                config, 
                actionCallback: config.action.callback 
            });
        }

        return notification;
    }

    showStickyBanner(config) {
        // Remove existing sticky banner
        this.hideStickyBanner();

        const banner = document.createElement('div');
        banner.className = `sticky-banner ${config.type}`;
        banner.dataset.id = config.id;

        const icons = {
            success: 'fas fa-check-circle',
            error: 'fas fa-exclamation-circle',
            warning: 'fas fa-exclamation-triangle',
            info: 'fas fa-info-circle'
        };

        banner.innerHTML = `
            <div class="sticky-banner-content">
                <i class="sticky-banner-icon ${icons[config.type] || icons.info}"></i>
                <div class="sticky-banner-text">
                    ${config.title ? `<strong>${config.title}</strong> ` : ''}${config.message}
                </div>
            </div>
            ${config.allowClose ? '<button class="sticky-banner-close" onclick="window.notificationSystem.hideStickyBanner()">&times;</button>' : ''}
        `;

        this.stickyContainer.appendChild(banner);
        
        // Add animation effect
        setTimeout(() => {
            banner.classList.add('animate');
        }, 100);

        // Auto-hide if not persistent
        if (!config.persistent) {
            setTimeout(() => {
                this.hideStickyBanner();
            }, config.duration);
        }

        return config.id;
    }

    hide(id) {
        if (!this.notifications.has(id)) return;

        const notificationData = this.notifications.get(id);
        const element = notificationData.element;
        
        // Clear timeout
        if (notificationData.timeout) {
            clearTimeout(notificationData.timeout);
        }

        // Add hide animation
        element.classList.add('hide');
        
        setTimeout(() => {
            if (element.parentNode) {
                element.parentNode.removeChild(element);
            }
            this.notifications.delete(id);
            this.processQueue();
        }, 300);
    }

    hideStickyBanner() {
        const banner = this.stickyContainer.querySelector('.sticky-banner');
        if (banner) {
            banner.style.transform = 'translateY(-100%)';
            banner.style.opacity = '0';
            setTimeout(() => {
                if (banner.parentNode) {
                    banner.parentNode.removeChild(banner);
                }
            }, 300);
        }
    }

    executeAction(id) {
        if (this.notifications.has(id)) {
            const notificationData = this.notifications.get(id);
            if (notificationData.actionCallback) {
                notificationData.actionCallback();
            }
            this.hide(id);
        }
    }

    processQueue() {
        if (this.isProcessing || this.queue.length === 0) return;
        if (this.notifications.size >= this.maxNotifications) return;

        this.isProcessing = true;
        const config = this.queue.shift();
        
        setTimeout(() => {
            this.displayNotification(config);
            this.isProcessing = false;
            this.processQueue();
        }, 100);
    }

    success(title, message, options = {}) {
        return this.show({
            type: 'success',
            title,
            message,
            ...options
        });
    }

    error(title, message, options = {}) {
        return this.show({
            type: 'error',
            title,
            message,
            duration: options.duration || 8000, // Longer for errors
            ...options
        });
    }

    warning(title, message, options = {}) {
        return this.show({
            type: 'warning',
            title,
            message,
            ...options
        });
    }

    info(title, message, options = {}) {
        return this.show({
            type: 'info',
            title,
            message,
            ...options
        });
    }

    // Bulk operations notifications
    showBulkProgress(operation, total) {
        return this.show({
            type: 'info',
            title: operation,
            message: `0 / ${total} işlendi`,
            persistent: true,
            id: 'bulk-operation',
            showProgress: false
        });
    }

    updateBulkProgress(processed, total, operation = 'İşlem') {
        const id = 'bulk-operation';
        if (this.notifications.has(id)) {
            const element = this.notifications.get(id).element;
            const messageEl = element.querySelector('.notification-message');
            if (messageEl) {
                messageEl.textContent = `${processed} / ${total} ${operation.toLowerCase()} tamamlandı`;
            }
        }
    }

    completeBulkOperation(processed, total, operation = 'İşlem') {
        this.hide('bulk-operation');
        this.success(
            `${operation} Tamamlandı`,
            `${processed} / ${total} öğe başarıyla işlendi`,
            { duration: 6000 }
        );
    }

    playNotificationSound(type) {
        // Only play sound if user has interacted with the page (avoid AudioContext warning)
        if (!window.userHasInteracted) {
            return; // Skip sound if user hasn't interacted yet
        }
        
        // Create audio context and play notification sound
        try {
            const audioContext = new (window.AudioContext || window.webkitAudioContext)();
            
            // Resume context if suspended (required for autoplay policy)
            if (audioContext.state === 'suspended') {
                audioContext.resume();
            }
            
            const oscillator = audioContext.createOscillator();
            const gainNode = audioContext.createGain();
            
            oscillator.connect(gainNode);
            gainNode.connect(audioContext.destination);
            
            // Different frequencies for different types
            const frequencies = {
                success: 800,
                error: 400,
                warning: 600,
                info: 700
            };
            
            oscillator.frequency.setValueAtTime(frequencies[type] || 700, audioContext.currentTime);
            oscillator.type = 'sine';
            
            gainNode.gain.setValueAtTime(0.1, audioContext.currentTime);
            gainNode.gain.exponentialRampToValueAtTime(0.01, audioContext.currentTime + 0.3);
            
            oscillator.start(audioContext.currentTime);
            oscillator.stop(audioContext.currentTime + 0.3);
        } catch (e) {
            // Fallback: no sound if Web Audio API not supported
            console.log('Notification sound not available');
        }
    }

    clearAll() {
        this.notifications.forEach((data, id) => {
            this.hide(id);
        });
        this.queue = [];
        // Remove sticky banner functionality since we disabled it
        // this.hideStickyBanner();
    }

    generateId() {
        return 'notification-' + Date.now() + '-' + Math.random().toString(36).substr(2, 9);
    }

    // Settings
    setPosition(position) {
        this.position = position;
        this.container.className = `notification-container ${position}`;
    }

    enableSound() {
        this.soundEnabled = true;
    }

    disableSound() {
        this.soundEnabled = false;
    }

    setMaxNotifications(max) {
        this.maxNotifications = max;
    }

    // Enhanced notification methods for critical messages
    
    /**
     * Shows a modal notification that requires user acknowledgment
     * Perfect for critical operations like successful imports/exports
     */
    showModal(options = {}) {
        const config = {
            type: options.type || 'info',
            title: options.title || 'Bildirim',
            message: options.message || '',
            confirmText: options.confirmText || 'Tamam',
            cancelText: options.cancelText || 'İptal',
            showCancel: options.showCancel || false,
            onConfirm: options.onConfirm || null,
            onCancel: options.onCancel || null,
            id: options.id || this.generateId()
        };

        // Create modal backdrop
        const modalBackdrop = document.createElement('div');
        modalBackdrop.className = 'notification-modal-backdrop';
        modalBackdrop.id = `modal-${config.id}`;

        // Create modal
        const modal = document.createElement('div');
        modal.className = `notification-modal ${config.type}`;

        const icons = {
            success: 'fas fa-check-circle',
            error: 'fas fa-exclamation-circle',
            warning: 'fas fa-exclamation-triangle',
            info: 'fas fa-info-circle'
        };

        modal.innerHTML = `
            <div class="modal-header">
                <i class="modal-icon ${icons[config.type] || icons.info}"></i>
                <h3 class="modal-title">${config.title}</h3>
            </div>
            <div class="modal-body">
                <div class="modal-message">${config.message}</div>
            </div>
            <div class="modal-footer">
                ${config.showCancel ? `<button class="modal-btn modal-btn-cancel" data-action="cancel">${config.cancelText}</button>` : ''}
                <button class="modal-btn modal-btn-confirm" data-action="confirm">${config.confirmText}</button>
            </div>
        `;

        modalBackdrop.appendChild(modal);
        
        // Force correct positioning with inline styles to override any conflicts
        modalBackdrop.style.cssText = `
            position: fixed !important;
            top: 0 !important;
            left: 0 !important;
            width: 100% !important;
            height: 100% !important;
            display: flex !important;
            align-items: center !important;
            justify-content: center !important;
            z-index: 99999 !important;
            background: rgba(0, 0, 0, 0.7) !important;
            margin: 0 !important;
            padding: 0 !important;
        `;
        
        modal.style.cssText = `
            position: relative !important;
            margin: 0 !important;
            padding: 0 !important;
            max-width: 500px !important;
            width: 90% !important;
            background: white !important;
            border-radius: 16px !important;
            box-shadow: 0 20px 60px rgba(0, 0, 0, 0.3) !important;
        `;
        
        document.body.appendChild(modalBackdrop);
        
        console.log('🔧 Modal created with forced positioning:', {
            backdropPosition: modalBackdrop.style.position,
            backdropDisplay: modalBackdrop.style.display,
            backdropZIndex: modalBackdrop.style.zIndex
        });

        // Add event listeners
        modal.addEventListener('click', (e) => {
            if (e.target.dataset.action === 'confirm') {
                if (config.onConfirm) config.onConfirm();
                this.hideModal(config.id);
            } else if (e.target.dataset.action === 'cancel') {
                if (config.onCancel) config.onCancel();
                this.hideModal(config.id);
            }
        });

        // Add to storage
        this.modalNotifications.set(config.id, { element: modalBackdrop, config });

        // Show with animation
        requestAnimationFrame(() => {
            modalBackdrop.classList.add('show');
        });

        // Play sound and vibrate
        if (this.soundEnabled) this.playNotificationSound(config.type);
        this.vibrateDevice(config.type);

        return config.id;
    }

    /**
     * Hide modal notification
     */
    hideModal(id) {
        if (!this.modalNotifications.has(id)) return;

        const modalData = this.modalNotifications.get(id);
        const element = modalData.element;

        element.classList.remove('show');
        setTimeout(() => {
            if (element.parentNode) {
                element.parentNode.removeChild(element);
            }
            this.modalNotifications.delete(id);
        }, 300);
    }

    /**
     * Shows notification in browser tab title (when tab is not active)
     */
    showTabNotification(message, type = 'info') {
        if (document.hidden) {
            const icons = { success: '✅', error: '❌', warning: '⚠️', info: 'ℹ️' };
            document.title = `${icons[type]} ${message}`;
            this.tabNotification.active = true;
            
            // Flash the title
            let flashCount = 0;
            const flashInterval = setInterval(() => {
                document.title = flashCount % 2 === 0 ? 
                    `${icons[type]} ${message}` : 
                    this.tabNotification.originalTitle;
                flashCount++;
                
                if (flashCount >= 6) {
                    clearInterval(flashInterval);
                    document.title = `${icons[type]} ${message}`;
                }
            }, 1000);
        }
    }

    /**
     * Clear tab notification and restore original title
     */
    clearTabNotification() {
        if (this.tabNotification.active) {
            document.title = this.tabNotification.originalTitle;
            this.tabNotification.active = false;
        }
    }

    /**
     * Show browser notification (if permission granted)
     */
    showBrowserNotification(title, message, options = {}) {
        if (this.browserNotificationEnabled && 'Notification' in window) {
            const notification = new Notification(title, {
                body: message,
                icon: options.icon || '/favicon.ico',
                badge: options.badge || '/favicon.ico',
                tag: options.tag || 'product-catalog',
                requireInteraction: options.requireInteraction || false
            });

            notification.onclick = () => {
                window.focus();
                if (options.onClick) options.onClick();
                notification.close();
            };

            // Auto close after 8 seconds
            setTimeout(() => notification.close(), 8000);
        }
    }

    /**
     * Request browser notification permission
     */
    async requestNotificationPermission() {
        if ('Notification' in window) {
            const permission = await Notification.requestPermission();
            this.browserNotificationEnabled = permission === 'granted';
        }
    }

    /**
     * Initialize vibration support
     */
    initVibrationSupport() {
        this.hasUserInteracted = false;
        
        // Track user interaction for vibration API
        const interactionEvents = ['click', 'touchstart', 'keydown', 'mousedown'];
        const trackInteraction = () => {
            this.hasUserInteracted = true;
            // Remove listeners after first interaction
            interactionEvents.forEach(event => {
                document.removeEventListener(event, trackInteraction);
            });
        };
        
        interactionEvents.forEach(event => {
            document.addEventListener(event, trackInteraction, { once: true });
        });
        
        this.vibrationEnabled = 'vibrate' in navigator;
        if (!this.vibrationEnabled) {
            console.debug('Vibration API not supported');
        }
    }

    /**
     * Vibrate device based on notification type
     */
    vibrateDevice(type) {
        if (!this.vibrationEnabled || !navigator.vibrate) return;
        
        // Check if user has interacted with the page
        if (!this.hasUserInteracted) {
            console.debug('Vibration blocked: User interaction required');
            return;
        }
        
        const patterns = {
            success: [100, 50, 100],
            error: [200, 100, 200, 100, 200],
            warning: [150, 75, 150],
            info: [100]
        };
        
        try {
            navigator.vibrate(patterns[type] || patterns.info);
        } catch (error) {
            console.debug('Vibration blocked by browser policy:', error.message);
        }
    }    /**
     * Enhanced notification for critical operations
     */
    critical(title, message, options = {}) {
        // Show modal
        const modalId = this.showModal({
            type: 'error',
            title: title,
            message: message,
            confirmText: 'Anladım',
            ...options
        });

        // Show tab notification
        this.showTabNotification(title, 'error');

        // Show browser notification
        this.showBrowserNotification(title, message, { requireInteraction: true });

        return modalId;
    }

    /**
     * Enhanced success notification for important operations
     */
    successModal(title, message, options = {}) {
        // Show modal
        const modalId = this.showModal({
            type: 'success',
            title: title,
            message: message,
            confirmText: 'Harika!',
            ...options
        });

        // Show tab notification
        this.showTabNotification(title, 'success');

        // Show browser notification
        this.showBrowserNotification(title, message);

        return modalId;
    }

    /**
     * Enhanced notification that combines multiple methods for maximum visibility
     */
    important(type, title, message, options = {}) {
        // Regular toast notification with longer duration for important messages
        const toastId = this.show({
            type: type,
            title: title,
            message: message,
            duration: 8000, // Longer duration for important messages
            persistent: false,
            ...options
        });

        // Tab notification if page not focused
        this.showTabNotification(title, type);

        // Browser notification
        this.showBrowserNotification(title, message);

        // Extra vibration for mobile
        this.vibrateDevice(type);

        return toastId;
    }
}

// Global instance
window.notificationSystem = new NotificationSystem();

// Helper functions for backward compatibility and convenience
window.showSuccess = (title, message, options = {}) => window.notificationSystem.success(title, message, options);
window.showError = (title, message, options = {}) => window.notificationSystem.error(title, message, options);
window.showWarning = (title, message, options = {}) => window.notificationSystem.warning(title, message, options);
window.showInfo = (title, message, options = {}) => window.notificationSystem.info(title, message, options);

// Enhanced helper functions
window.showSuccessModal = (title, message, options = {}) => window.notificationSystem.successModal(title, message, options);
window.showErrorModal = (title, message, options = {}) => window.notificationSystem.critical(title, message, options);
window.showImportant = (type, title, message, options = {}) => window.notificationSystem.important(type, title, message, options);

// Operation-specific notifications
window.notifyProductAdded = (productName) => {
    window.notificationSystem.important('success', 'Ürün Eklendi', `"${productName}" başarıyla eklendi.`);
};

window.notifyProductUpdated = (productName) => {
    window.notificationSystem.important('success', 'Ürün Güncellendi', `"${productName}" başarıyla güncellendi.`);
};

window.notifyProductDeleted = (productName) => {
    window.notificationSystem.important('warning', 'Ürün Silindi', `"${productName}" başarıyla silindi.`);
};

window.notifyImportCompleted = (count) => {
    window.notificationSystem.successModal(
        'İçe Aktarma Tamamlandı',
        `${count} ürün başarıyla içe aktarıldı.`,
        {
            onConfirm: () => {
                // Optional: Reload page or refresh product list
                // window.location.reload();
            }
        }
    );
};

window.notifyExportCompleted = (format, count) => {
    window.notificationSystem.successModal(
        'Dışa Aktarma Tamamlandı',
        `${count} ürün ${format.toUpperCase()} formatında başarıyla dışa aktarıldı.`
    );
};

window.notifyError = (operation, errorMessage) => {
    window.notificationSystem.critical(
        `${operation} Hatası`,
        errorMessage || 'Beklenmeyen bir hata oluştu. Lütfen tekrar deneyin.'
    );
};

// Integration with TempData notifications
document.addEventListener('DOMContentLoaded', function() {
    // Check for TempData messages in page
    const successMessage = document.querySelector('[data-tempdata="Success"]');
    const errorMessage = document.querySelector('[data-tempdata="Error"]');
    const warningMessage = document.querySelector('[data-tempdata="Warning"]');
    const infoMessage = document.querySelector('[data-tempdata="Info"]');
    
    if (successMessage) {
        const message = successMessage.textContent.trim();
        
        // Check if it's an important operation based on message content
        if (message.includes('aktarıldı') || message.includes('import') || message.includes('export')) {
            // Show modal for import/export operations
            window.notificationSystem.successModal('İşlem Başarılı', message);
        } else if (message.includes('eklendi') || message.includes('güncellendi') || message.includes('silindi')) {
            // Show important notification for CRUD operations
            window.notificationSystem.important('success', 'İşlem Başarılı', message);
        } else {
            // Regular toast notification
            window.notificationSystem.success('Başarılı', message);
        }
        successMessage.remove();
    }
    
    if (errorMessage) {
        const message = errorMessage.textContent.trim();
        
        // Always show critical errors with modal
        window.notificationSystem.critical('Hata Oluştu', message);
        errorMessage.remove();
    }
    
    if (warningMessage) {
        const message = warningMessage.textContent.trim();
        window.notificationSystem.important('warning', 'Uyarı', message);
        warningMessage.remove();
    }
    
    if (infoMessage) {
        const message = infoMessage.textContent.trim();
        window.notificationSystem.info('Bilgi', message);
        infoMessage.remove();
    }
});


// Export globally only if not already defined
if (!window.NotificationSystem) {
    window.NotificationSystem = NotificationSystem;
}

// Create singleton global instance
if (!window.notificationSystem) {
    window.notificationSystem = new NotificationSystem();
    console.log('🌍 NotificationSystem: Global singleton instance ready');
} else {
    console.log('🔄 NotificationSystem: Using existing global instance');
}

// Helper functions for backward compatibility and convenience
window.showSuccess = (title, message, options = {}) => window.notificationSystem.success(title, message, options);
window.showError = (title, message, options = {}) => window.notificationSystem.error(title, message, options);
window.showWarning = (title, message, options = {}) => window.notificationSystem.warning(title, message, options);
window.showInfo = (title, message, options = {}) => window.notificationSystem.info(title, message, options);

// Enhanced helper functions
window.showSuccessModal = (title, message, options = {}) => window.notificationSystem.successModal(title, message, options);
window.showErrorModal = (title, message, options = {}) => window.notificationSystem.critical(title, message, options);
