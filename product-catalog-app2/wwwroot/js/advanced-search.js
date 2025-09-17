/**
 * ADVANCED SEARCH MODULE
 * Extracted from Index.cshtml for better maintainability
 */

class AdvancedSearchManager {
    constructor() {
        this.toggleTimeout = null;
        this.isInitialized = false;
    }

    init() {
        if (this.isInitialized) {
            console.log('AdvancedSearchManager already initialized');
            return;
        }
        
        console.log('🔍 Initializing AdvancedSearchManager...');
        this.setupToggleHandler();
        this.isInitialized = true;
        console.log('✅ AdvancedSearchManager initialized successfully');
    }

    setupToggleHandler() {
        console.log('🎛️ Setting up toggle handler...');
        const toggleBtn = document.querySelector('.btn-toggle-advanced');
        
        if (!toggleBtn) {
            console.error('❌ Advanced search toggle button not found! Selector: .btn-toggle-advanced');
            return;
        }
        
        console.log('✅ Toggle button found:', toggleBtn);
        
        // Event delegation sistemine güven - kendi listener'ı ekleme
        // Event delegation.js dosyası bu click'i yakalayacak ve toggle() metodunu çağıracak
        console.log('✅ Advanced search handler delegated to event-delegation.js');
    }

    toggle() {
        console.log('🔄 Toggle method called');
        
        // Debounce ile çoklu çağrıları engelle
        if (this.toggleTimeout) {
            clearTimeout(this.toggleTimeout);
        }
        
        this.toggleTimeout = setTimeout(() => {
            try {
                const advancedFilters = document.getElementById('advancedFilters');
                const toggleBtn = document.querySelector('.btn-toggle-advanced');
                
                if (!advancedFilters) {
                    console.error('❌ Advanced filters element not found! ID: advancedFilters');
                    return;
                }
                
                if (!toggleBtn) {
                    console.error('❌ Toggle button not found!');
                    return;
                }
                
                console.log('✅ Both elements found:', { advancedFilters, toggleBtn });
                
                // Global .hidden utility sınıfını kontrol et
                const isHidden = advancedFilters.classList.contains('hidden');
                
                console.log('🔍 Current state - isHidden:', isHidden);
                console.log('🔍 Current classes:', advancedFilters.className);
                
                if (isHidden) {
                    console.log('👆 Showing advanced filters...');
                    this.show(advancedFilters, toggleBtn);
                } else {
                    console.log('👇 Hiding advanced filters...');
                    this.hide(advancedFilters, toggleBtn);
                }
            } catch (error) {
                console.error('❌ Advanced search toggle error:', error);
            }
        }, 100);
    }

    show(advancedFilters, toggleBtn) {
        console.log('👆 Show method called');
        
        // Global .hidden utility sınıfını kullan
        advancedFilters.classList.remove('hidden');
        
        // Smooth animation için CSS transition
        advancedFilters.style.opacity = '0';
        advancedFilters.style.transform = 'translateY(20px)';
        
        setTimeout(() => {
            advancedFilters.style.opacity = '1';
            advancedFilters.style.transform = 'translateY(0)';
        }, 10);
        
        toggleBtn.innerHTML = '<i class="fas fa-cogs"></i> Gelişmiş Arama ▲';
        localStorage.setItem('advancedSearchOpen', 'true');
        console.log('✅ Advanced filters shown');
    }

    hide(advancedFilters, toggleBtn) {
        console.log('👇 Hide method called');
        
        // Animation önce
        advancedFilters.style.opacity = '0';
        advancedFilters.style.transform = 'translateY(-20px)';
        
        setTimeout(() => {
            // Global .hidden utility sınıfını kullan
            advancedFilters.classList.add('hidden');
        }, 300);
        
        toggleBtn.innerHTML = '<i class="fas fa-cogs"></i> Gelişmiş Arama ▼';
        localStorage.setItem('advancedSearchOpen', 'false');
        console.log('✅ Advanced filters hidden');
    }
}


// Export globally only if not already defined
if (!window.AdvancedSearchManager) {
    window.AdvancedSearchManager = AdvancedSearchManager;
    console.log('🌍 AdvancedSearchManager class exported');
}

// Create global instance
if (!window.advancedSearchManager) {
    window.advancedSearchManager = new AdvancedSearchManager();
    console.log('✅ AdvancedSearchManager instance ready');
}

// Auto-initialize when DOM is ready
if (document.readyState === 'loading') {
    console.log('📄 Document still loading, adding DOMContentLoaded listener');
    document.addEventListener('DOMContentLoaded', () => {
        console.log('🎯 DOMContentLoaded fired, initializing AdvancedSearchManager');
        if (window.advancedSearchManager && !window.advancedSearchManager.isInitialized) {
            window.advancedSearchManager.init();
        }
    });
} else {
    console.log('📄 Document already loaded, initializing AdvancedSearchManager immediately');
    if (window.advancedSearchManager && !window.advancedSearchManager.isInitialized) {
        window.advancedSearchManager.init();
    }
}