/**
 * GLOBAL EVENT DELEGATION - Performance Optimized
 * Centralized event handling to prevent duplicate listeners and improve performance
 */

(function() {
    'use strict';
    
    // Prevent multiple initializations
    if (window.eventDelegationInitialized) return;
    window.eventDelegationInitialized = true;

    // Global click delegation
    document.addEventListener('click', function(e) {
        const target = e.target;
        
        // Mobile menu toggle
        if (target.closest('.mobile-menu-btn')) {
            e.preventDefault();
            if (window.toggleMobileMenu) {
                window.toggleMobileMenu();
            }
        }
        
        // Advanced search toggle - Priority handler
        if (target.closest('.btn-toggle-advanced')) {
            e.preventDefault();
            e.stopPropagation();
            e.stopImmediatePropagation();
            console.log('🎯 Advanced search toggle clicked (event-delegation.js)');
            
            // Use AdvancedSearchManager if available
            if (window.advancedSearchManager && typeof window.advancedSearchManager.toggle === 'function') {
                window.advancedSearchManager.toggle();
            } else {
                console.error('❌ AdvancedSearchManager not available');
            }
            return; // Prevent further processing
        }
        
        // Product card actions
        if (target.closest('.product-card .btn-archive')) {
            e.preventDefault();
            const productId = target.dataset.productId;
            const productName = target.dataset.productName;
            if (window.archiveProduct) {
                window.archiveProduct(productId, productName);
            }
        }
        
        if (target.closest('.product-card .btn-unarchive')) {
            e.preventDefault();
            const productId = target.dataset.productId;
            const productName = target.dataset.productName;
            if (window.unarchiveProduct) {
                window.unarchiveProduct(productId, productName);
            }
        }
    }, { passive: false });

    // Global paste delegation - Optimized
    let isPasteInProgress = false;
    document.addEventListener('paste', function(e) {
        // Prevent overlapping paste operations
        if (isPasteInProgress) {
            console.log('⏸️ Global paste delegation blocked overlapping operation');
            e.preventDefault();
            return;
        }
        
        isPasteInProgress = true;
        
        // Reset flag after reasonable delay
        setTimeout(() => {
            isPasteInProgress = false;
        }, 500);
        
        // Rich text editor paste
        if (e.target.closest('.rich-text-editor')) {
            // Let rich text editor handle this
            return;
        }
        
        // Character counter inputs
        if (e.target.classList.contains('character-counter')) {
            // Debounced counter update
            setTimeout(() => {
                if (window.initializeCharacterCounter) {
                    // Trigger counter update
                    const event = new Event('input', { bubbles: true });
                    e.target.dispatchEvent(event);
                }
            }, 10);
        }
    }, { capture: true });

    // Global input delegation for performance
    let inputTimeout;
    document.addEventListener('input', function(e) {
        const target = e.target;
        
        // Debounced processing for heavy operations
        clearTimeout(inputTimeout);
        inputTimeout = setTimeout(() => {
            // Character counter inputs
            if (target.classList.contains('character-counter')) {
                // Already handled by character counter optimization
                return;
            }
            
            // Search inputs
            if (target.id === 'searchInput' || target.classList.contains('search-input')) {
                // Already handled by ProductIndexManager
                return;
            }
        }, 50);
    }, { passive: true });

    // Global keyboard delegation
    document.addEventListener('keydown', function(e) {
        // ESC key handling
        if (e.key === 'Escape') {
            // Close mobile menu
            const navMenu = document.querySelector('.nav-menu.show');
            if (navMenu) {
                navMenu.classList.remove('show');
            }
            
            // Close modals
            const modals = document.querySelectorAll('.modal.show, .modal-overlay');
            modals.forEach(modal => {
                modal.classList.remove('show');
                if (modal.classList.contains('modal-overlay')) {
                    modal.remove();
                }
            });
        }
        
        // Ctrl+S prevention for forms
        if (e.ctrlKey && e.key === 's') {
            const activeForm = document.activeElement.closest('form');
            if (activeForm) {
                e.preventDefault();
                // Auto-save if available
                if (window.autoSave) {
                    window.autoSave();
                }
            }
        }
    });

    console.log('✅ Global event delegation initialized - Performance optimized');
})();
