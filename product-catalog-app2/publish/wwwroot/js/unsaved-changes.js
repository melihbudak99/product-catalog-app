/**
 * PROFESSIONAL UNSAVED CHANGES DETECTION SYSTEM
 * =============================================
 * Advanced form state management with user experience focus
 * Features: Dirty state tracking, beforeunload protection, visual feedback
 * Version: 1.0 - Production Ready
 */

(function() {
    'use strict';

    // ===== CONFIGURATION =====
    const CONFIG = {
        DEBUG_MODE: false, // Set to true for development
        LOG_CHANGES: false // Set to true for development debugging
    };

    // ===== LOGGING UTILITY =====
    const log = {
        info: (msg) => CONFIG.DEBUG_MODE && console.log(`🔧 UnsavedChanges: ${msg}`),
        success: (msg) => CONFIG.DEBUG_MODE && console.log(`✅ UnsavedChanges: ${msg}`),
        warn: (msg) => console.warn(`⚠️ UnsavedChanges: ${msg}`),
        error: (msg) => console.error(`❌ UnsavedChanges: ${msg}`),
        change: (msg) => CONFIG.LOG_CHANGES && console.log(`🔄 UnsavedChanges: ${msg}`)
    };

    // ===== UNSAVED CHANGES MANAGER =====
    class UnsavedChangesManager {
        constructor(formSelector = '#productForm') {
            this.form = null;
            this.formSelector = formSelector;
            this.isDirty = false;
            this.originalData = new Map();
            this.ignoredFields = new Set(['__RequestVerificationToken']);
            this.isSubmitting = false;
            this.hasUnsavedIndicator = false;
            
            // Event handlers bound to this instance
            this.boundHandlers = {
                beforeUnload: this.handleBeforeUnload.bind(this),
                formChange: this.handleFormChange.bind(this),
                formSubmit: this.handleFormSubmit.bind(this),
                keyDown: this.handleKeyDown.bind(this)
            };

            log.info('Manager initialized');
        }

        // ===== PUBLIC METHODS =====
        
        init() {
            this.form = document.querySelector(this.formSelector);
            if (!this.form) {
                log.warn(`Form not found: ${this.formSelector}`);
                return false;
            }

            this.setupEventListeners();
            this.captureOriginalState();
            this.createUnsavedIndicator();
            
            log.success(`System activated for form: ${this.formSelector}`);
            return true;
        }

        destroy() {
            this.removeEventListeners();
            this.removeUnsavedIndicator();
            this.isDirty = false;
            this.originalData.clear();
            log.info('System deactivated');
        }

        // Check if form has unsaved changes
        hasUnsavedChanges() {
            return this.isDirty && !this.isSubmitting;
        }

        // Mark form as clean (saved)
        markAsSaved() {
            this.isDirty = false;
            this.isSubmitting = false;
            this.captureOriginalState(); // Update baseline
            this.updateVisualFeedback();
            console.log('✅ UnsavedChanges: Form marked as saved');
        }

        // Force mark as dirty (useful for programmatic changes)
        markAsDirty() {
            if (!this.isDirty) {
                this.isDirty = true;
                this.updateVisualFeedback();
                console.log('🔧 UnsavedChanges: Form marked as dirty (forced)');
            }
        }

        // ===== PRIVATE METHODS =====

        setupEventListeners() {
            // Form change detection
            this.form.addEventListener('input', this.boundHandlers.formChange);
            this.form.addEventListener('change', this.boundHandlers.formChange);
            
            // Form submission
            this.form.addEventListener('submit', this.boundHandlers.formSubmit);
            
            // Page unload protection
            window.addEventListener('beforeunload', this.boundHandlers.beforeUnload);
            
            // Enhanced keyboard shortcuts
            document.addEventListener('keydown', this.boundHandlers.keyDown, true);
        }

        removeEventListeners() {
            if (this.form) {
                this.form.removeEventListener('input', this.boundHandlers.formChange);
                this.form.removeEventListener('change', this.boundHandlers.formChange);
                this.form.removeEventListener('submit', this.boundHandlers.formSubmit);
            }
            
            window.removeEventListener('beforeunload', this.boundHandlers.beforeUnload);
            document.removeEventListener('keydown', this.boundHandlers.keyDown, true);
        }

        captureOriginalState() {
            this.originalData.clear();
            
            if (!this.form) return;

            // Capture all form elements
            const elements = this.form.querySelectorAll('input, textarea, select');
            elements.forEach(element => {
                if (this.ignoredFields.has(element.name)) return;
                
                let value = '';
                if (element.type === 'checkbox' || element.type === 'radio') {
                    value = element.checked;
                } else if (element.type === 'file') {
                    value = element.files.length > 0 ? Array.from(element.files).map(f => f.name).join(',') : '';
                } else {
                    value = element.value;
                }
                
                this.originalData.set(element.name || element.id, value);
            });

            console.log('📸 UnsavedChanges: Original state captured (' + this.originalData.size + ' fields)');
        }

        detectChanges() {
            if (!this.form) return false;

            const elements = this.form.querySelectorAll('input, textarea, select');
            
            for (const element of elements) {
                if (this.ignoredFields.has(element.name)) continue;
                
                const key = element.name || element.id;
                const originalValue = this.originalData.get(key);
                
                let currentValue = '';
                if (element.type === 'checkbox' || element.type === 'radio') {
                    currentValue = element.checked;
                } else if (element.type === 'file') {
                    currentValue = element.files.length > 0 ? Array.from(element.files).map(f => f.name).join(',') : '';
                } else {
                    currentValue = element.value;
                }
                
                // Compare values (handle different types)
                if (originalValue !== currentValue) {
                    return true;
                }
            }
            
            return false;
        }

        updateVisualFeedback() {
            const indicator = document.querySelector('.unsaved-changes-indicator');
            const saveButton = this.form?.querySelector('button[type="submit"]');
            
            if (this.isDirty) {
                // Show unsaved changes
                if (indicator) {
                    indicator.classList.add('visible');
                    indicator.querySelector('.indicator-text').textContent = 'Kaydedilmemiş değişiklikler var';
                }
                
                if (saveButton) {
                    saveButton.classList.add('has-changes');
                    if (!saveButton.querySelector('.unsaved-dot')) {
                        const dot = document.createElement('span');
                        dot.className = 'unsaved-dot';
                        dot.innerHTML = '●';
                        saveButton.appendChild(dot);
                    }
                }
                
                // Update page title
                if (!document.title.startsWith('● ')) {
                    document.title = '● ' + document.title;
                }
            } else {
                // Hide indicators
                if (indicator) {
                    indicator.classList.remove('visible');
                }
                
                if (saveButton) {
                    saveButton.classList.remove('has-changes');
                    const dot = saveButton.querySelector('.unsaved-dot');
                    if (dot) dot.remove();
                }
                
                // Clean page title
                if (document.title.startsWith('● ')) {
                    document.title = document.title.substring(2);
                }
            }
        }

        createUnsavedIndicator() {
            if (this.hasUnsavedIndicator) return;
            
            const indicator = document.createElement('div');
            indicator.className = 'unsaved-changes-indicator';
            indicator.innerHTML = `
                <div class="indicator-content">
                    <i class="fas fa-exclamation-triangle"></i>
                    <span class="indicator-text">Kaydedilmemiş değişiklikler var</span>
                    <small>Ctrl+S ile kaydedin</small>
                </div>
            `;
            
            document.body.appendChild(indicator);
            this.hasUnsavedIndicator = true;
        }

        removeUnsavedIndicator() {
            const indicator = document.querySelector('.unsaved-changes-indicator');
            if (indicator) {
                indicator.remove();
                this.hasUnsavedIndicator = false;
            }
        }

        // ===== EVENT HANDLERS =====

        handleFormChange(event) {
            // Debounce change detection to avoid excessive checks
            clearTimeout(this.changeTimeout);
            this.changeTimeout = setTimeout(() => {
                const hasChanges = this.detectChanges();
                
                if (hasChanges !== this.isDirty) {
                    this.isDirty = hasChanges;
                    this.updateVisualFeedback();
                    
                    if (hasChanges) {
                        console.log('🔄 UnsavedChanges: Changes detected in field:', event.target.name || event.target.id);
                    } else {
                        console.log('✅ UnsavedChanges: All changes reverted');
                    }
                }
            }, 300);
        }

        handleFormSubmit(event) {
            this.isSubmitting = true;
            
            // Show loading state
            const submitButton = event.target.querySelector('button[type="submit"]');
            if (submitButton) {
                submitButton.disabled = true;
                const originalText = submitButton.innerHTML;
                submitButton.innerHTML = '<i class="fas fa-spinner fa-spin"></i> Kaydediliyor...';
                
                // Restore button state if submission fails
                setTimeout(() => {
                    if (this.isSubmitting) {
                        submitButton.disabled = false;
                        submitButton.innerHTML = originalText;
                        this.isSubmitting = false;
                    }
                }, 10000);
            }
            
            console.log('💾 UnsavedChanges: Form submission started');
        }

        handleBeforeUnload(event) {
            if (this.hasUnsavedChanges()) {
                const message = 'Kaydedilmemiş değişiklikleriniz var. Sayfadan ayrılmak istediğinizden emin misiniz?';
                event.returnValue = message;
                return message;
            }
        }

        handleKeyDown(event) {
            // Enhanced Ctrl+S with unsaved changes check
            if ((event.ctrlKey || event.metaKey) && event.key === 's') {
                event.preventDefault();
                event.stopPropagation();
                
                if (!this.form) {
                    console.warn('UnsavedChanges: No form found for Ctrl+S');
                    return;
                }
                
                // Visual feedback for Ctrl+S
                this.showCtrlSFeedback();
                
                if (this.isDirty) {
                    console.log('💾 UnsavedChanges: Ctrl+S - Saving changes...');
                    this.form.submit();
                } else {
                    console.log('ℹ️ UnsavedChanges: Ctrl+S - No changes to save');
                    this.showNoChangesMessage();
                }
                
                return false;
            }
        }

        showCtrlSFeedback() {
            const saveButton = this.form?.querySelector('button[type="submit"]');
            if (saveButton) {
                saveButton.style.transform = 'scale(0.95)';
                saveButton.style.boxShadow = '0 0 0 3px rgba(0, 123, 255, 0.3)';
                
                setTimeout(() => {
                    saveButton.style.transform = 'scale(1)';
                    saveButton.style.boxShadow = '';
                }, 200);
            }
        }

        showNoChangesMessage() {
            const indicator = document.querySelector('.unsaved-changes-indicator');
            if (indicator) {
                const originalText = indicator.querySelector('.indicator-text').textContent;
                indicator.classList.add('visible', 'info');
                indicator.querySelector('.indicator-text').textContent = 'Değişiklik yok - Zaten kaydedildi';
                
                setTimeout(() => {
                    indicator.classList.remove('visible', 'info');
                    indicator.querySelector('.indicator-text').textContent = originalText;
                }, 2000);
            }
        }
    }

    // ===== GLOBAL INITIALIZATION =====
    window.UnsavedChangesManager = UnsavedChangesManager;

    // Auto-initialize when DOM is ready
    let globalManager = null;
    
    document.addEventListener('DOMContentLoaded', function() {
        // Only initialize on form pages
        const productForm = document.querySelector('#productForm');
        if (productForm) {
            globalManager = new UnsavedChangesManager();
            globalManager.init();
            
            // Expose to global scope for external access
            window.unsavedChangesManager = globalManager;
        }
    });

    // Clean up on page unload
    window.addEventListener('unload', function() {
        if (globalManager) {
            globalManager.destroy();
        }
    });

})();