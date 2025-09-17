/**
 * PROFESSIONAL NOTIFICATION SYSTEM ENHANCEMENT
 * ===========================================
 * Advanced feedback system for form operations
 * Features: Success/Error states, Auto-dismiss, Accessibility
 */

(function() {
    'use strict';

    // ===== NOTIFICATION MANAGER =====
    class NotificationManager {
        constructor() {
            this.notifications = new Map();
            this.container = null;
            this.autoHideTimeout = 5000; // 5 seconds
            this.maxNotifications = 5;
            
            this.createContainer();
            console.log('🔧 NotificationManager: Initialized');
        }

        createContainer() {
            if (this.container) return;
            
            this.container = document.createElement('div');
            this.container.className = 'notification-container';
            this.container.setAttribute('role', 'region');
            this.container.setAttribute('aria-label', 'Bildirimler');
            this.container.setAttribute('aria-live', 'polite');
            
            document.body.appendChild(this.container);
        }

        show(message, type = 'info', options = {}) {
            const id = 'notification_' + Date.now() + '_' + Math.random().toString(36).substr(2, 9);
            const notification = this.createNotification(id, message, type, options);
            
            // Remove old notifications if we have too many
            if (this.notifications.size >= this.maxNotifications) {
                const oldestId = this.notifications.keys().next().value;
                this.remove(oldestId);
            }
            
            this.notifications.set(id, notification);
            this.container.appendChild(notification.element);
            
            // Trigger animation
            requestAnimationFrame(() => {
                notification.element.classList.add('show');
            });
            
            // Auto-hide (unless persistent)
            if (!options.persistent && type !== 'error') {
                notification.timeout = setTimeout(() => {
                    this.remove(id);
                }, options.autoHide !== undefined ? options.autoHide : this.autoHideTimeout);
            }
            
            return id;
        }

        createNotification(id, message, type, options) {
            const element = document.createElement('div');
            element.className = `notification notification-${type}`;
            element.setAttribute('role', 'alert');
            element.setAttribute('data-notification-id', id);
            
            const iconMap = {
                success: 'fas fa-check-circle',
                error: 'fas fa-exclamation-circle',
                warning: 'fas fa-exclamation-triangle',
                info: 'fas fa-info-circle'
            };
            
            element.innerHTML = `
                <div class="notification-content">
                    <i class="${iconMap[type] || iconMap.info}"></i>
                    <span class="notification-message">${message}</span>
                    <button type="button" class="notification-close" title="Kapat" aria-label="Bildirimi kapat">
                        <i class="fas fa-times"></i>
                    </button>
                </div>
                <div class="notification-progress"></div>
            `;
            
            // Add close handler
            const closeBtn = element.querySelector('.notification-close');
            closeBtn.addEventListener('click', () => {
                this.remove(id);
            });
            
            return {
                element,
                type,
                message,
                options,
                timeout: null
            };
        }

        remove(id) {
            const notification = this.notifications.get(id);
            if (!notification) return;
            
            // Clear timeout
            if (notification.timeout) {
                clearTimeout(notification.timeout);
            }
            
            // Animate out
            notification.element.classList.add('hide');
            
            setTimeout(() => {
                if (notification.element.parentNode) {
                    notification.element.parentNode.removeChild(notification.element);
                }
                this.notifications.delete(id);
            }, 300);
        }

        clear() {
            this.notifications.forEach((notification, id) => {
                this.remove(id);
            });
        }

        // Convenience methods
        success(message, options = {}) {
            return this.show(message, 'success', options);
        }

        error(message, options = {}) {
            return this.show(message, 'error', { persistent: true, ...options });
        }

        warning(message, options = {}) {
            return this.show(message, 'warning', options);
        }

        info(message, options = {}) {
            return this.show(message, 'info', options);
        }
    }

    // ===== FORM FEEDBACK INTEGRATION =====
    class FormFeedbackManager {
        constructor() {
            this.notificationManager = new NotificationManager();
            this.setupFormInterception();
            console.log('🔧 FormFeedback: Manager initialized');
        }

        setupFormInterception() {
            // Listen for form submissions
            document.addEventListener('submit', (event) => {
                const form = event.target;
                if (form.id === 'productForm') {
                    this.handleFormSubmission(form, event);
                }
            });

            // Listen for unsaved changes manager events
            document.addEventListener('DOMContentLoaded', () => {
                if (window.unsavedChangesManager) {
                    this.integrateWithUnsavedChanges();
                }
            });
        }

        handleFormSubmission(form, event) {
            const submitButton = form.querySelector('button[type="submit"]');
            const isEdit = form.action.includes('EditProduct');
            
            // Show saving notification
            const savingId = this.notificationManager.info(
                isEdit ? 'Değişiklikler kaydediliyor...' : 'Ürün kaydediliyor...',
                { persistent: true }
            );

            // Enhanced button feedback
            if (submitButton) {
                const originalHTML = submitButton.innerHTML;
                const originalDisabled = submitButton.disabled;
                
                submitButton.disabled = true;
                submitButton.innerHTML = '<i class="fas fa-spinner fa-spin"></i> Kaydediliyor...';
                submitButton.classList.add('saving');

                // Setup cleanup for failed submissions
                const cleanup = () => {
                    submitButton.disabled = originalDisabled;
                    submitButton.innerHTML = originalHTML;
                    submitButton.classList.remove('saving');
                    this.notificationManager.remove(savingId);
                };

                // Cleanup after timeout (in case of server issues)
                setTimeout(cleanup, 15000);
                
                // Store cleanup function for success handling
                window.formSubmissionCleanup = cleanup;
            }

            console.log('💾 FormFeedback: Form submission started');
        }

        integrateWithUnsavedChanges() {
            const manager = window.unsavedChangesManager;
            if (!manager) return;

            // Override the markAsSaved method to show success feedback
            const originalMarkAsSaved = manager.markAsSaved.bind(manager);
            manager.markAsSaved = () => {
                originalMarkAsSaved();
                
                // Show success notification
                this.notificationManager.success('✅ Değişiklikler başarıyla kaydedildi!');
                
                // Clean up submission UI if needed
                if (window.formSubmissionCleanup) {
                    window.formSubmissionCleanup();
                    delete window.formSubmissionCleanup;
                }
                
                console.log('✅ FormFeedback: Save success feedback shown');
            };
        }

        // Public methods for external use
        showSaveSuccess(message = 'Değişiklikler başarıyla kaydedildi!') {
            return this.notificationManager.success(message);
        }

        showSaveError(message = 'Kaydetme sırasında bir hata oluştu. Lütfen tekrar deneyin.') {
            return this.notificationManager.error(message);
        }

        showValidationError(message = 'Lütfen tüm gerekli alanları doğru şekilde doldurun.') {
            return this.notificationManager.warning(message);
        }
    }

    // ===== GLOBAL INITIALIZATION =====
    window.NotificationManager = NotificationManager;
    window.FormFeedbackManager = FormFeedbackManager;

    // Auto-initialize on form pages
    document.addEventListener('DOMContentLoaded', function() {
        const productForm = document.querySelector('#productForm');
        if (productForm) {
            window.formFeedbackManager = new FormFeedbackManager();
            console.log('✅ FormFeedback: System activated');
        }
    });

})();