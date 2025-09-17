/**
 * PRODUCT INDEX MODULE
 * Extracted from Index.cshtml for better maintainability and performance
 * Version: 2.0.0 - Production Ready
 */

// Prevent duplicate class definitions
if (typeof window.ProductIndexManager === 'undefined') {

class ProductIndexManager {
    constructor() {
        console.log('🔧 ProductIndexManager initializing...');
        
        // Global error handler for debugging
        window.addEventListener('error', (e) => {
            if (e.error && e.error.message && e.error.message.includes('Maximum call stack size exceeded')) {
                console.error('🚨 INFINITE RECURSION DETECTED!', {
                    message: e.error.message,
                    filename: e.filename,
                    lineno: e.lineno,
                    colno: e.colno,
                    stack: e.error.stack
                });
                
                // Try to break the cycle
                if (window.productIndexManager) {
                    window.productIndexManager.state.isLoading = false;
                    window.productIndexManager._showingIndicator = false;
                    window.productIndexManager.forceHideAllLoadingIndicators();
                }
            }
        });
        
        // Configuration - OPTIMIZED for performance
        this.config = {
            pageSize: 50,
            maxPageSize: 200,
            searchDelay: 450, // Increased from 300ms to reduce server load
            maxRetries: 3,
            cacheTimeout: 10 * 60 * 1000 // Increased to 10 minutes for better caching
        };

        // State management
        this.state = {
            currentPage: 1,
            totalPages: 1,
            totalCount: 0,
            isLoading: false,
            searchTimeout: null,
            cache: new Map(),
            selectedProducts: new Set(),
            lastSearchParams: null,
            lastSearchTime: 0,
            lastSearchParamsHash: null,
            userTriggeredSearch: false
        };

        // DOM elements
        this.elements = {};
        
        // Event listeners
        this.eventListeners = [];
        
        // Recursion protection flag
        this._showingIndicator = false;
        
        // Initialize user interaction tracking
        this.initializeUserInteraction();
    }

    /**
     * Initialize the product index functionality
     */
    init() {
        try {
            console.log('🚀 ProductIndexManager init started...');
            
            // Ensure state is properly initialized
            if (!this.state) {
                console.warn('⚠️ State was not initialized, re-initializing...');
                this.state = {
                    currentPage: 1,
                    totalPages: 1,
                    totalCount: 0,
                    isLoading: false,
                    searchTimeout: null,
                    cache: new Map(),
                    selectedProducts: new Set(),
                    lastSearchParams: null,
                    lastSearchTime: 0,
                    lastSearchParamsHash: null,
                    userTriggeredSearch: false
                };
            }
            
            // Initialize dependencies first
            this.initializeDependencies();
            
            // Setup global helper functions
            this.setupGlobalHelpers();
            
            // Initialize core functionality
            this.cacheElements();
            this.setupEventListeners();
            this.initializeBulkOperations();
            this.initializeSearch();
            this.initializePagination();
            this.initializeImageHandling();
            this.initializeProgressIndicator();
            
            console.log('✅ ProductIndexManager initialized successfully');
        } catch (error) {
            console.error('❌ Failed to initialize ProductIndexManager:', error);
            this.showErrorNotification('Sayfa yüklenirken bir hata oluştu');
        }
    }

    /**
     * Initialize all dependencies (NotificationSystem, BulkOperationsManager)
     */
    initializeDependencies() {
        // Initialize NotificationSystem
        if (typeof window.NotificationSystem !== 'undefined' && !window.notificationSystem) {
            window.notificationSystem = new NotificationSystem();
            console.log('✅ NotificationSystem initialized');
        }

        // BulkOperationsManager is self-initializing from bulk-operations.js
        // No need to initialize here to avoid conflicts
    }

    /**
     * Setup global helper functions for backward compatibility
     */
    setupGlobalHelpers() {
        // Notification helper functions
        window.showSuccess = (title, message) => {
            if (window.notificationSystem) {
                window.notificationSystem.success(title, message);
            } else {
                alert(`${title}: ${message}`);
            }
        };

        window.showError = (title, message) => {
            if (window.notificationSystem) {
                window.notificationSystem.error(title, message);
            } else {
                alert(`${title}: ${message}`);
            }
        };

        window.showWarning = (title, message) => {
            if (window.notificationSystem) {
                window.notificationSystem.warning(title, message);
            } else {
                alert(`${title}: ${message}`);
            }
        };

        window.showInfo = (title, message) => {
            if (window.notificationSystem) {
                window.notificationSystem.info(title, message);
            } else {
                alert(`${title}: ${message}`);
            }
        };

        window.showProgress = (message, details) => {
            if (window.progressIndicator) {
                window.progressIndicator.show(message, details);
            }
        };

        window.hideProgress = () => {
            if (window.progressIndicator) {
                window.progressIndicator.hide();
            }
        };

        // Archive/Unarchive functions - delegate to BulkOperationsManager
        window.archiveProduct = (productId, productName) => {
            if (window.bulkOperationsManager) {
                return window.bulkOperationsManager.archiveProduct(productId, productName);
            } else {
                console.error('BulkOperationsManager not available');
                return Promise.resolve(false);
            }
        };

        window.unarchiveProduct = (productId, productName) => {
            if (window.bulkOperationsManager) {
                return window.bulkOperationsManager.unarchiveProduct(productId, productName);
            } else {
                console.error('BulkOperationsManager not available');
                return Promise.resolve(false);
            }
        };

        // Bulk operations functions - delegate to BulkOperationsManager
        window.bulkDelete = () => {
            if (window.bulkOperationsManager) {
                return window.bulkOperationsManager.performBulkAction('delete');
            } else {
                console.error('BulkOperationsManager not available');
            }
        };

        window.bulkArchive = () => {
            if (window.bulkOperationsManager) {
                return window.bulkOperationsManager.performBulkAction('archive');
            } else {
                console.error('BulkOperationsManager not available');
            }
        };

        window.bulkUnarchive = () => {
            if (window.bulkOperationsManager) {
                return window.bulkOperationsManager.performBulkAction('unarchive');
            } else {
                console.error('BulkOperationsManager not available');
            }
        };

        window.exportSelectedToExcel = () => {
            if (window.bulkOperationsManager) {
                return window.bulkOperationsManager.exportSelectedToExcel();
            } else {
                console.error('BulkOperationsManager not available');
            }
        };

        window.clearSelection = () => {
            if (window.bulkOperationsManager) {
                return window.bulkOperationsManager.clearSelection();
            } else {
                console.error('BulkOperationsManager not available');
            }
        };

        // Image modal functions
        window.openImageModal = window.openImageModal || ((imageSrc, productName) => {
            console.log('Opening image modal:', imageSrc, productName);
            // Implementation delegated to app.js if available
        });

        window.openMarketplaceGallery = window.openMarketplaceGallery || ((productId, productName, imageUrls) => {
            console.log('Opening marketplace gallery:', productId, productName, imageUrls);
            // Implementation delegated to app.js if available
        });

        console.log('✅ Global helper functions setup complete');
    }

    /**
     * Cache frequently used DOM elements
     */
    cacheElements() {
        this.elements = {
            // Search elements
            searchInput: document.getElementById('searchInput'),
            categorySelect: document.getElementById('categoryFilter'),
            brandSelect: document.getElementById('brandFilter'),
            statusSelect: document.getElementById('statusFilter'),
            
            // Filter elements
            materialSelect: document.getElementById('materialFilter'),
            colorSelect: document.getElementById('colorFilter'),
            eanCodeInput: document.getElementById('eanCodeFilter'),
            
            // Weight filters
            minWeightInput: document.getElementById('minWeight'),
            maxWeightInput: document.getElementById('maxWeight'),
            
            // Desi filters
            minDesiInput: document.getElementById('minDesi'),
            maxDesiInput: document.getElementById('maxDesi'),
            
            // Warranty filters
            minWarrantyInput: document.getElementById('minWarranty'),
            maxWarrantyInput: document.getElementById('maxWarranty'),
            
            // Sorting
            sortBySelect: document.getElementById('sortBy'),
            sortDirectionSelect: document.getElementById('sortDirection'),
            
            // Special filters
            hasImageCheckbox: document.getElementById('hasImage'),
            hasEanCheckbox: document.getElementById('hasEan'),
            hasBarcodeCheckbox: document.getElementById('hasBarcode'),
            barcodeTypeSelect: document.getElementById('barcodeType'),
            
            // Pagination
            paginationContainer: document.querySelector('.pagination-container'),
            pageSizeSelect: document.getElementById('pageSize'),
            
            // Product grid
            productGrid: document.querySelector('.product-grid'),
            productCards: document.querySelectorAll('.product-card'),
            
            // Bulk operations
            selectAllCheckbox: document.getElementById('selectAllProducts'),
            bulkPanel: document.querySelector('.bulk-operations-panel'),
            bulkButtons: document.querySelectorAll('.bulk-action-btn'),
            
            // Results info
            resultsInfo: document.querySelector('.results-info'),
            
            // Loading indicator
            loadingIndicator: document.querySelector('.loading-indicator')
        };

        // Remove null elements to avoid errors
        Object.keys(this.elements).forEach(key => {
            if (!this.elements[key]) {
                delete this.elements[key];
            }
        });
    }

    /**
     * Initialize user interaction tracking
     */
    initializeUserInteraction() {
        window.userHasInteracted = false;

        const markInteraction = () => {
            window.userHasInteracted = true;
        };

        document.addEventListener('click', markInteraction, { once: true });
        document.addEventListener('keydown', markInteraction, { once: true });
        document.addEventListener('touchstart', markInteraction, { once: true });
    }

    /**
     * Initialize search functionality
     */
    initializeSearch() {
        // Search input focus handling
        if (this.elements.searchInput) {
            this.elements.searchInput.addEventListener('focus', () => {
                this.elements.searchInput.select();
            });
            
            // Handle paste events - just cleanup, no automatic search
            this.elements.searchInput.addEventListener('paste', (e) => {
                console.log('Paste event detected in search input');
                
                // Force cleanup any stuck loading states
                this.forceHideAllLoadingIndicators();
                
                // Note: No automatic search on paste
                // User must click the search button after pasting
                console.log('📝 Paste completed - user should click search button to search');
            });
        }

        // Restore search state from URL parameters
        const urlParams = new URLSearchParams(window.location.search);
        if (urlParams.get('search') && this.elements.searchInput) {
            this.elements.searchInput.value = urlParams.get('search');
        }

        // Initialize search suggestions if available
        this.initializeSearchSuggestions();
        
        console.log('Search functionality initialized');
    }

    /**
     * Initialize search suggestions
     */
    initializeSearchSuggestions() {
        if (!this.elements.searchInput) return;
        
        let suggestionTimeout;
        const suggestionsContainer = document.getElementById('searchSuggestions');
        
        if (!suggestionsContainer) return;

        // Handle input for search suggestions
        this.elements.searchInput.addEventListener('input', (e) => {
            clearTimeout(suggestionTimeout);
            const query = e.target.value.trim();
            
            if (query.length < 2) {
                this.hideSuggestions();
                return;
            }
            
            suggestionTimeout = setTimeout(() => {
                this.fetchSearchSuggestions(query);
            }, 300);
        });

        // Hide suggestions when clicking outside
        document.addEventListener('click', (e) => {
            if (!this.elements.searchInput.contains(e.target) && !suggestionsContainer.contains(e.target)) {
                this.hideSuggestions();
            }
        });

        // Handle escape key
        this.elements.searchInput.addEventListener('keydown', (e) => {
            if (e.key === 'Escape') {
                this.hideSuggestions();
            }
        });
    }

    /**
     * Fetch search suggestions from server
     */
    async fetchSearchSuggestions(query) {
        try {
            const response = await fetch(`/Product/SearchSuggestions?query=${encodeURIComponent(query)}`);
            const suggestions = await response.json();
            this.showSuggestions(suggestions);
        } catch (error) {
            console.error('Error fetching search suggestions:', error);
            this.hideSuggestions();
        }
    }

    /**
     * Show search suggestions
     */
    showSuggestions(suggestions) {
        const suggestionsContainer = document.getElementById('searchSuggestions');
        if (!suggestionsContainer) return;

        if (suggestions.length === 0) {
            this.hideSuggestions();
            return;
        }

        suggestionsContainer.innerHTML = '';
        
        suggestions.forEach(suggestion => {
            const suggestionItem = document.createElement('div');
            suggestionItem.className = 'suggestion-item';
            suggestionItem.innerHTML = `
                <span class="suggestion-text">${suggestion.highlight || suggestion.text}</span>
                <span class="suggestion-type">${suggestion.type === 'product' ? 'Ürün' : 'Marka'}</span>
            `;
            
            suggestionItem.addEventListener('click', () => {
                this.elements.searchInput.value = suggestion.text;
                this.hideSuggestions();
                this.handleSearch(); // Trigger search when suggestion is clicked
            });
            
            suggestionsContainer.appendChild(suggestionItem);
        });

        suggestionsContainer.classList.remove('hidden');
    }

    /**
     * Hide search suggestions
     */
    hideSuggestions() {
        const suggestionsContainer = document.getElementById('searchSuggestions');
        if (suggestionsContainer) {
            suggestionsContainer.classList.add('hidden');
        }
    }

    /**
     * Setup all event listeners
     */
    setupEventListeners() {
        // Search input - Optimized single event handler
        if (this.elements.searchInput && !this.elements.searchInput.hasAttribute('data-search-listener-added')) {
            let lastSearchValue = '';
            
            // Create optimized debounced handlers
            const debouncedSuggestions = this.debounce((value) => {
                this.fetchSearchSuggestions(value);
            }, 200);
            
            const debouncedSearch = this.debounce(() => {
                this.handleSearch();
            }, 600); // Optimized: Reduced from 800ms to 600ms
            
            // Single unified input handler - Performance optimized
            const inputHandler = (e) => {
                const currentValue = e.target.value.trim();
                
                // Only process if value changed
                if (currentValue === lastSearchValue) return;
                lastSearchValue = currentValue;
                
                // Handle suggestions for 2+ characters
                if (currentValue.length >= 2) {
                    debouncedSuggestions(currentValue);
                } else {
                    this.hideSuggestions();
                }
                
                // Handle search for 3+ characters or empty (clear)
                if (currentValue.length >= 3 || currentValue.length === 0) {
                    debouncedSearch();
                }
            };

            this.addEventListenerWithCleanup(this.elements.searchInput, 'input', inputHandler);
            this.elements.searchInput.setAttribute('data-search-listener-added', 'true');
        }        // Filter changes
        const filterElements = [
            'categorySelect', 'brandSelect', 'statusSelect', 'materialSelect', 
            'colorSelect', 'sortBySelect', 'sortDirectionSelect', 'barcodeTypeSelect'
        ];

        filterElements.forEach(elementKey => {
            if (this.elements[elementKey] && !this.elements[elementKey].hasAttribute('data-filter-listener-added')) {
                this.addEventListenerWithCleanup(this.elements[elementKey], 'change', 
                    this.handleFilterChange.bind(this));
                this.elements[elementKey].setAttribute('data-filter-listener-added', 'true');
            }
        });

        // Numeric filters
        const numericElements = [
            'minWeightInput', 'maxWeightInput', 'minDesiInput', 'maxDesiInput',
            'minWarrantyInput', 'maxWarrantyInput', 'eanCodeInput'
        ];

        numericElements.forEach(elementKey => {
            if (this.elements[elementKey] && !this.elements[elementKey].hasAttribute('data-numeric-listener-added')) {
                this.addEventListenerWithCleanup(this.elements[elementKey], 'input',
                    this.debounce(this.handleFilterChange.bind(this), this.config.searchDelay));
                this.elements[elementKey].setAttribute('data-numeric-listener-added', 'true');
            }
        });

        // Checkbox filters
        const checkboxElements = ['hasImageCheckbox', 'hasEanCheckbox', 'hasBarcodeCheckbox'];
        checkboxElements.forEach(elementKey => {
            if (this.elements[elementKey] && !this.elements[elementKey].hasAttribute('data-checkbox-listener-added')) {
                this.addEventListenerWithCleanup(this.elements[elementKey], 'change',
                    this.handleFilterChange.bind(this));
                this.elements[elementKey].setAttribute('data-checkbox-listener-added', 'true');
            }
        });

        // Page size change
        if (this.elements.pageSizeSelect && !this.elements.pageSizeSelect.hasAttribute('data-pagesize-listener-added')) {
            this.addEventListenerWithCleanup(this.elements.pageSizeSelect, 'change',
                this.handlePageSizeChange.bind(this));
            this.elements.pageSizeSelect.setAttribute('data-pagesize-listener-added', 'true');
        }

        // Bulk operations
        if (this.elements.selectAllCheckbox && !this.elements.selectAllCheckbox.hasAttribute('data-selectall-listener-added')) {
            this.addEventListenerWithCleanup(this.elements.selectAllCheckbox, 'change',
                this.handleSelectAll.bind(this));
            this.elements.selectAllCheckbox.setAttribute('data-selectall-listener-added', 'true');
        }

        // Global keyboard shortcuts
        this.addEventListenerWithCleanup(document, 'keydown', this.handleKeyboardShortcuts.bind(this));

        // Auto-submit form elements (modern event delegation)
        this.addEventListenerWithCleanup(document, 'change', (e) => {
            if (e.target.matches('[data-auto-submit="true"]')) {
                const form = e.target.closest('form');
                if (form) {
                    // Mark as user triggered for tracking
                    this.state.userTriggeredSearch = true;
                    
                    // Performance optimized submit with minimal processing
                    requestAnimationFrame(() => {
                        form.submit();
                    });
                }
            }
        });

        // Modern event delegation for all data-action attributes (CLEAN CODE)
        this.addEventListenerWithCleanup(document, 'click', (e) => {
            const action = e.target.closest('[data-action]')?.dataset.action;
            if (!action) return;

            console.log('🎯 Action triggered:', action);

            switch (action) {
                case 'clear-filters':
                    if (typeof window.clearAllFilters === 'function') {
                        window.clearAllFilters();
                    }
                    break;
                    
                // Individual product actions (not bulk operations)
                case 'archive-product':
                case 'unarchive-product':
                case 'delete-product':
                    {
                        const target = e.target.closest('[data-action]');
                        const productId = target.dataset.productId;
                        const productName = target.dataset.productName;
                        
                        if (action === 'archive-product' && typeof window.archiveProduct === 'function') {
                            window.archiveProduct(productId, productName);
                        } else if (action === 'unarchive-product' && typeof window.unarchiveProduct === 'function') {
                            window.unarchiveProduct(productId, productName);
                        } else if (action === 'delete-product' && typeof window.deleteProduct === 'function') {
                            window.deleteProduct(productId, productName);
                        }
                    }
                    break;
                    
                case 'open-image':
                    {
                        console.log('🖼️ open-image action triggered');
                        const target = e.target.closest('[data-action]');
                        const imageUrl = target.dataset.imageUrl;
                        const productName = target.dataset.productName;
                        
                        console.log('🖼️ Image URL:', imageUrl);
                        console.log('🖼️ Product Name:', productName);
                        console.log('🖼️ openImageModal function exists:', typeof window.openImageModal === 'function');
                        
                        if (typeof window.openImageModal === 'function') {
                            window.openImageModal(imageUrl, productName);
                        } else {
                            console.error('❌ openImageModal function not found');
                        }
                    }
                    break;
                    
                case 'open-gallery':
                    {
                        console.log('🎬 open-gallery action triggered');
                        const target = e.target.closest('[data-action]');
                        const productId = target.dataset.productId;
                        const productName = target.dataset.productName;
                        const images = target.dataset.images;
                        
                        console.log('🎬 Product ID:', productId);
                        console.log('🎬 Product Name:', productName);
                        console.log('🎬 Images data:', images);
                        console.log('🎬 openMarketplaceGallery function exists:', typeof window.openMarketplaceGallery === 'function');
                        
                        if (typeof window.openMarketplaceGallery === 'function') {
                            try {
                                // Split the pipe-delimited string
                                const imageArray = images.split('|').filter(url => url.trim() !== '');
                                console.log('🎬 Parsed image array:', imageArray);
                                window.openMarketplaceGallery(productId, productName, imageArray);
                            } catch (error) {
                                console.error('❌ Error parsing images data:', error);
                                console.error('❌ Raw images data:', images);
                            }
                        } else {
                            console.error('❌ openMarketplaceGallery function not found');
                        }
                    }
                    break;
                    
                case 'close-modal':
                    {
                        const modalType = e.target.closest('[data-action]').dataset.modal;
                        if (modalType === 'image' && typeof window.closeImageModal === 'function') {
                            window.closeImageModal();
                        } else if (modalType === 'gallery' && typeof window.closeMarketplaceGallery === 'function') {
                            window.closeMarketplaceGallery();
                        }
                    }
                    break;
                    
                case 'gallery-prev':
                    if (typeof window.previousImage === 'function') {
                        window.previousImage();
                    }
                    break;
                    
                case 'gallery-next':
                    if (typeof window.nextImage === 'function') {
                        window.nextImage();
                    }
                    break;
                    
                default:
                    // Other actions are handled by their respective modules
                    break;
            }
        });

        // Search form submit handler - Mark user triggered searches (OPTIMIZED)
        const searchForm = document.getElementById('searchForm');
        if (searchForm && !searchForm.hasAttribute('data-form-listener-added')) {
            this.addEventListenerWithCleanup(searchForm, 'submit', (e) => {
                // Fast path - minimal processing for better performance
                this.state.userTriggeredSearch = true;
                console.log('🔍 Search form submitted - performance optimized');
                
                // Let form submit naturally - no expensive operations
            });
            searchForm.setAttribute('data-form-listener-added', 'true');
        }

        // Window events
        this.addEventListenerWithCleanup(window, 'beforeunload', this.cleanup.bind(this));
        this.addEventListenerWithCleanup(window, 'popstate', this.handlePopState.bind(this));
    }

    /**
     * Add event listener with automatic cleanup tracking
     */
    addEventListenerWithCleanup(element, event, handler, options = {}) {
        element.addEventListener(event, handler, options);
        this.eventListeners.push({ element, event, handler, options });
    }

    /**
     * Debounce function for search input - Enhanced to prevent excessive calls
     */
    debounce(func, wait) {
        let timeout;
        const self = this; // Capture the correct context
        return function executedFunction(...args) {
            const later = () => {
                clearTimeout(timeout);
                timeout = null;
                
                // Skip if loading to prevent race conditions - add null checks
                if (self && self.state && self.state.isLoading) {
                    console.log('Debounced function skipped - operation in progress');
                    return;
                }
                
                func.call(self, ...args);
            };
            
            if (timeout) {
                clearTimeout(timeout);
            }
            timeout = setTimeout(later, wait);
        };
    }

    /**
     * Handle search input - Now only called programmatically when needed
     */
    async handleSearch() {
        // Force cleanup any stuck loading states first
        if (this.state.isLoading) {
            console.log('Search already in progress, force cleaning and aborting...');
            this.forceHideAllLoadingIndicators();
            this.state.isLoading = false;
            return;
        }
        
        this.state.lastSearchTime = Date.now();
        
        try {
            await this.performSearch(1); // Reset to first page on new search
        } catch (error) {
            console.error('Search error:', error);
            this.forceHideAllLoadingIndicators();
            this.state.isLoading = false;
            this.showErrorNotification('Arama sırasında bir hata oluştu');
        }
    }

    /**
     * Handle filter changes
     */
    async handleFilterChange() {
        console.log('🔥 Filter change started - isLoading:', this.state.isLoading);
        
        if (this.state.isLoading) {
            console.warn('⚠️ Filter change blocked - already loading');
            return;
        }
        
        try {
            console.log('🔄 Starting filter search...');
            await this.performSearch(1); // Reset to first page on filter change
            console.log('✅ Filter search completed');
        } catch (error) {
            console.error('❌ Filter error:', error);
            console.trace('Filter error stack trace');
            this.showErrorNotification('Filtre uygulanırken bir hata oluştu');
        }
    }

    /**
     * Handle page size change
     */
    async handlePageSizeChange() {
        const newPageSize = parseInt(this.elements.pageSizeSelect.value);
        if (newPageSize > this.config.maxPageSize) {
            this.elements.pageSizeSelect.value = this.config.maxPageSize;
            this.showWarningNotification(`Maksimum sayfa boyutu ${this.config.maxPageSize} ile sınırlıdır`);
            return;
        }
        
        this.config.pageSize = newPageSize;
        await this.performSearch(1); // Reset to first page with new page size
    }

    /**
     * Perform search with current parameters
     */
    async performSearch(page = null) {
        console.log('🔍 performSearch called - page:', page, 'isLoading:', this.state.isLoading);
        
        // Prevent multiple concurrent searches
        if (this.state.isLoading) {
            console.log('🚫 Search already in progress, aborting...');
            return;
        }

        this.state.isLoading = true;
        console.log('🏁 Starting search - loading indicator will show');
        this.showLoadingIndicator(true);

        try {
            const searchParams = this.collectSearchParameters();
            searchParams.page = page || this.state.currentPage;
            searchParams.pageSize = this.config.pageSize;
            
            console.log('📋 Search params collected:', JSON.stringify(searchParams, null, 2));

            // Skip if parameters haven't changed
            const currentParamsHash = JSON.stringify(searchParams);
            if (this.state.lastSearchParamsHash === currentParamsHash) {
                console.log('⏭️ Search parameters unchanged, skipping search');
                this.state.isLoading = false;
                this.showLoadingIndicator(false);
                return;
            }
            
            this.state.lastSearchParamsHash = currentParamsHash;

            // Check cache first
            const cacheKey = this.generateCacheKey(searchParams);
            const cachedResult = this.getFromCache(cacheKey);
            
            if (cachedResult) {
                this.handleSearchResults(cachedResult, searchParams);
                this.state.isLoading = false;
                this.showLoadingIndicator(false);
                return;
            }

            // Make API request
            const response = await this.makeSearchRequest(searchParams);
            
            if (response.success) {
                // Cache the results
                this.addToCache(cacheKey, response);
                this.handleSearchResults(response, searchParams);
            } else {
                throw new Error(response.message || 'Search failed');
            }

        } catch (error) {
            console.error('Search error:', error);
            this.showErrorNotification('Arama sırasında bir hata oluştu');
        } finally {
            this.state.isLoading = false;
            this.showLoadingIndicator(false);
        }
    }

    /**
     * Collect current search parameters
     */
    collectSearchParameters() {
        return {
            search: this.elements.searchInput?.value || '',
            category: this.elements.categorySelect?.value || '',
            brand: this.elements.brandSelect?.value || '',
            status: this.elements.statusSelect?.value || '',
            material: this.elements.materialSelect?.value || '',
            color: this.elements.colorSelect?.value || '',
            eanCode: this.elements.eanCodeInput?.value || '',
            minWeight: this.elements.minWeightInput?.value || '',
            maxWeight: this.elements.maxWeightInput?.value || '',
            minDesi: this.elements.minDesiInput?.value || '',
            maxDesi: this.elements.maxDesiInput?.value || '',
            minWarranty: this.elements.minWarrantyInput?.value || '',
            maxWarranty: this.elements.maxWarrantyInput?.value || '',
            sortBy: this.elements.sortBySelect?.value || 'name',
            sortDirection: this.elements.sortDirectionSelect?.value || 'asc',
            hasImage: this.elements.hasImageCheckbox?.checked || false,
            hasEan: this.elements.hasEanCheckbox?.checked || false,
            hasBarcode: this.elements.hasBarcodeCheckbox?.checked || false,
            barcodeType: this.elements.barcodeTypeSelect?.value || ''
        };
    }

    /**
     * Generate cache key for search parameters
     * Fixed: Using encodeURIComponent instead of btoa for Turkish character support
     */
    generateCacheKey(params) {
        try {
            return encodeURIComponent(JSON.stringify(params));
        } catch (error) {
            console.warn('Cache key generation failed:', error);
            return `cache_${Date.now()}_${Math.random()}`;
        }
    }

    /**
     * Get result from cache
     */
    getFromCache(key) {
        const cached = this.state.cache.get(key);
        if (cached && (Date.now() - cached.timestamp) < this.config.cacheTimeout) {
            return cached.data;
        }
        this.state.cache.delete(key);
        return null;
    }

    /**
     * Add result to cache
     */
    addToCache(key, data) {
        // Limit cache size
        if (this.state.cache.size > 50) {
            const firstKey = this.state.cache.keys().next().value;
            this.state.cache.delete(firstKey);
        }
        
        this.state.cache.set(key, {
            data: data,
            timestamp: Date.now()
        });
    }

    /**
     * Make search request to server
     */
    async makeSearchRequest(params) {
        const url = new URL(window.location.pathname, window.location.origin);
        
        // Add parameters to URL
        Object.keys(params).forEach(key => {
            if (params[key] !== '' && params[key] !== false) {
                url.searchParams.append(key, params[key]);
            }
        });

        const response = await fetch(url.toString(), {
            method: 'GET',
            headers: {
                'X-Requested-With': 'XMLHttpRequest',
                'Accept': 'text/html,application/xhtml+xml'
            }
        });

        if (!response.ok) {
            throw new Error(`HTTP ${response.status}: ${response.statusText}`);
        }

        const html = await response.text();
        return { success: true, html: html };
    }

    /**
     * Handle search results
     */
    handleSearchResults(response, searchParams) {
        try {
            // Update page content with new results
            this.updatePageContent(response.html);
            
            // Update state
            this.state.currentPage = searchParams.page;
            this.state.lastSearchParams = searchParams;
            
            // Update URL without page reload
            this.updateBrowserHistory(searchParams);
            
            // Re-initialize components for new content
            this.reinitializeComponents();
            
            // Only show notification if user explicitly triggered the search
            if (this.state.userTriggeredSearch) {
                this.showSuccessNotification('Sonuçlar güncellendi');
                this.state.userTriggeredSearch = false; // Reset flag
            } else {
                console.log('🔕 Skipping notification - automated search');
            }
            
        } catch (error) {
            console.error('Error handling search results:', error);
            this.showErrorNotification('Sonuçlar yüklenirken bir hata oluştu');
        }
    }

    /**
     * Update page content with new HTML - OPTIMIZED with smooth transitions
     */
    updatePageContent(html) {
        // Create temporary container to parse new HTML
        const tempDiv = document.createElement('div');
        tempDiv.innerHTML = html;
        
        // Extract and update specific sections
        const newProductGrid = tempDiv.querySelector('.product-grid');
        const newPagination = tempDiv.querySelector('.pagination-container');
        const newResultsInfo = tempDiv.querySelector('.results-info');
        
        // Smooth content updates with fade effects to prevent "black screen"
        if (newProductGrid && this.elements.productGrid) {
            this.smoothUpdateElement(this.elements.productGrid, newProductGrid.innerHTML);
        }
        
        if (newPagination && this.elements.paginationContainer) {
            this.smoothUpdateElement(this.elements.paginationContainer, newPagination.innerHTML);
        }
        
        if (newResultsInfo && this.elements.resultsInfo) {
            this.smoothUpdateElement(this.elements.resultsInfo, newResultsInfo.innerHTML);
        }
    }

    /**
     * Smooth element update with fade transition to prevent flashing
     */
    smoothUpdateElement(element, newContent) {
        if (!element) return;
        
        // Quick opacity transition to prevent "black screen" effect
        element.style.transition = 'opacity 0.15s ease';
        element.style.opacity = '0.7';
        
        // Use requestAnimationFrame for smooth update
        requestAnimationFrame(() => {
            element.innerHTML = newContent;
            
            // Restore opacity quickly
            requestAnimationFrame(() => {
                element.style.opacity = '1';
                
                // Clean up transition after animation
                setTimeout(() => {
                    element.style.transition = '';
                }, 200);
            });
        });
    }

    /**
     * Re-initialize components after content update
     */
    reinitializeComponents() {
        // Re-cache elements that might have changed
        this.cacheElements();
        
        // Re-initialize bulk operations
        this.initializeBulkOperations();
        
        // Re-initialize image handling
        this.initializeImageHandling();
        
        // Re-setup pagination
        this.initializePagination();
    }

    /**
     * Update browser history
     */
    updateBrowserHistory(params) {
        const url = new URL(window.location);
        
        // Clear existing parameters
        url.search = '';
        
        // Add new parameters
        Object.keys(params).forEach(key => {
            if (params[key] !== '' && params[key] !== false && params[key] !== null) {
                url.searchParams.set(key, params[key]);
            }
        });
        
        // Update history without reload
        window.history.pushState(params, '', url.toString());
    }

    /**
     * Handle browser back/forward
     */
    handlePopState(event) {
        if (event.state) {
            this.restoreStateFromHistory(event.state);
        }
    }

    /**
     * Restore state from browser history
     */
    restoreStateFromHistory(state) {
        // Update form fields
        Object.keys(state).forEach(key => {
            const element = this.elements[key + 'Input'] || this.elements[key + 'Select'] || this.elements[key + 'Checkbox'];
            if (element) {
                if (element.type === 'checkbox') {
                    element.checked = state[key];
                } else {
                    element.value = state[key];
                }
            }
        });
        
        // Perform search with historical state
        this.performSearch(state.page);
    }

    /**
     * Initialize bulk operations
     */
    initializeBulkOperations() {
        // Use existing bulk operations manager if available
        if (window.bulkOperationsManager) {
            window.bulkOperationsManager.init();
            return;
        }

        // Fallback implementation
        this.setupBulkOperationsFallback();
    }

    /**
     * Fallback bulk operations setup
     */
    setupBulkOperationsFallback() {
        // Handle select all
        if (this.elements.selectAllCheckbox) {
            this.elements.selectAllCheckbox.addEventListener('change', (e) => {
                const checkboxes = document.querySelectorAll('.product-checkbox');
                checkboxes.forEach(cb => cb.checked = e.target.checked);
                this.updateBulkPanel();
            });
        }

        // Handle individual checkboxes
        document.addEventListener('change', (e) => {
            if (e.target.classList.contains('product-checkbox')) {
                this.updateBulkPanel();
            }
        });

        // Setup bulk action buttons
        this.elements.bulkButtons?.forEach(btn => {
            btn.addEventListener('click', this.handleBulkAction.bind(this));
        });
    }

    /**
     * Update bulk operations panel
     */
    updateBulkPanel() {
        const selectedCheckboxes = document.querySelectorAll('.product-checkbox:checked');
        const selectedCount = selectedCheckboxes.length;
        
        if (this.elements.bulkPanel) {
            if (selectedCount > 0) {
                this.elements.bulkPanel.style.display = 'block';
                const countElement = this.elements.bulkPanel.querySelector('.selected-count');
                if (countElement) {
                    countElement.textContent = selectedCount;
                }
            } else {
                this.elements.bulkPanel.style.display = 'none';
            }
        }
    }

    /**
     * Handle bulk actions
     */
    async handleBulkAction(event) {
        const action = event.target.dataset.action;
        const selectedIds = Array.from(document.querySelectorAll('.product-checkbox:checked'))
            .map(cb => cb.value);

        if (selectedIds.length === 0) {
            this.showWarningNotification('Lütfen en az bir ürün seçin');
            return;
        }

        if (!confirm(`Seçilen ${selectedIds.length} ürün için bu işlemi yapmak istediğinizden emin misiniz?`)) {
            return;
        }

        try {
            this.showLoadingIndicator(true, `İşlem gerçekleştiriliyor... (${selectedIds.length} ürün)`);
            
            const response = await this.submitBulkOperation(action, selectedIds);
            
            if (response.success) {
                this.showSuccessNotification(`İşlem başarıyla tamamlandı (${selectedIds.length} ürün)`);
                await this.performSearch(); // Refresh results
            } else {
                throw new Error(response.message || 'Bulk operation failed');
            }
            
        } catch (error) {
            console.error('Bulk operation error:', error);
            this.showErrorNotification('Toplu işlem sırasında bir hata oluştu');
        } finally {
            this.showLoadingIndicator(false);
        }
    }

    /**
     * Submit bulk operation to server
     */
    async submitBulkOperation(action, productIds) {
        const formData = new FormData();
        formData.append('action', action);
        productIds.forEach(id => formData.append('productIds', id));

        const response = await fetch('/Product/BulkOperation', {
            method: 'POST',
            body: formData,
            headers: {
                'RequestVerificationToken': this.getAntiForgeryToken()
            }
        });

        if (!response.ok) {
            throw new Error(`HTTP ${response.status}: ${response.statusText}`);
        }

        return await response.json();
    }

    /**
     * Get anti-forgery token
     */
    getAntiForgeryToken() {
        const token = document.querySelector('input[name="__RequestVerificationToken"]');
        return token ? token.value : '';
    }

    /**
     * Initialize pagination
     */
    initializePagination() {
        // Setup pagination click handlers
        document.addEventListener('click', (e) => {
            if (e.target.matches('.page-link')) {
                e.preventDefault();
                const page = parseInt(e.target.dataset.page);
                if (page && !isNaN(page)) {
                    this.performSearch(page);
                }
            }
        });
    }

    /**
     * Initialize image handling
     */
    initializeImageHandling() {
        // Lazy loading for images
        const images = document.querySelectorAll('img[data-src]');
        if ('IntersectionObserver' in window) {
            const imageObserver = new IntersectionObserver((entries, observer) => {
                entries.forEach(entry => {
                    if (entry.isIntersecting) {
                        const img = entry.target;
                        img.src = img.dataset.src;
                        img.removeAttribute('data-src');
                        observer.unobserve(img);
                    }
                });
            });

            images.forEach(img => imageObserver.observe(img));
        } else {
            // Fallback for older browsers
            images.forEach(img => {
                img.src = img.dataset.src;
                img.removeAttribute('data-src');
            });
        }

        // Initialize global error counter for user notification
        if (!window.imageErrorCount) {
            window.imageErrorCount = 0;
            window.imageErrorNotificationShown = false;
        }

        // Enhanced image error handling - DRY approach
        const handleImageError = (img) => {
            window.imageErrorCount++;
            console.warn('📷 Image failed to load:', img.src);
            console.log('📊 Total failed images:', window.imageErrorCount);
            img.style.display = 'none';
            
            const container = img.parentElement;
            if (container && !container.querySelector('.image-error')) {
                const errorDiv = document.createElement('div');
                errorDiv.className = 'image-error';
                errorDiv.innerHTML = `
                    <div style="
                        display: flex;
                        flex-direction: column;
                        align-items: center;
                        justify-content: center;
                        background: linear-gradient(135deg, #f8f9fa 0%, #e9ecef 100%);
                        border: 2px dashed #ced4da;
                        border-radius: 8px;
                        width: 50px;
                        height: 50px;
                        color: #6c757d;
                        font-family: system-ui, -apple-system, sans-serif;
                        cursor: pointer;
                        transition: all 0.3s ease;
                    "
                    title="Ürün görseli bulunamadı veya kaldırılmış"
                    onclick="this.style.transform='scale(0.95)'; setTimeout(() => this.style.transform='scale(1)', 150);">
                        <span style="font-size: 18px; margin-bottom: 2px;">🖼️</span>
                        <p style="margin: 0; font-size: 7px; text-align: center; font-weight: 500; line-height: 1;">Görsel Yok</p>
                    </div>
                `;
                container.appendChild(errorDiv);
                
                // Add hover effect
                const errorElement = errorDiv.querySelector('div');
                errorElement.addEventListener('mouseenter', function() {
                    this.style.background = 'linear-gradient(135deg, #e9ecef 0%, #dee2e6 100%)';
                    this.style.borderColor = '#adb5bd';
                });
                errorElement.addEventListener('mouseleave', function() {
                    this.style.background = 'linear-gradient(135deg, #f8f9fa 0%, #e9ecef 100%)';
                    this.style.borderColor = '#ced4da';
                });
            }
            
            // Show user notification after multiple failures
            if (window.imageErrorCount >= 3 && !window.imageErrorNotificationShown) {
                window.imageErrorNotificationShown = true;
                setTimeout(() => window.showImageErrorNotification(window.imageErrorCount), 1000);
            }
        };

        // Apply to existing thumbnails
        const thumbnails = document.querySelectorAll('.product-thumbnail');
        thumbnails.forEach(img => {
            img.addEventListener('error', () => handleImageError(img));
        });

        console.log('✅ Image error handling initialized for', thumbnails.length, 'thumbnails');

        // Global error handler for dynamically loaded images
        document.addEventListener('error', (e) => {
            if (e.target.tagName === 'IMG' && e.target.classList.contains('product-thumbnail')) {
                // Only handle if not already processed by specific handler
                if (!e.target.hasAttribute('data-error-handled')) {
                    e.target.setAttribute('data-error-handled', 'true');
                }
            }
        }, true);
    }

    /**
     * Initialize progress indicator
     */
    initializeProgressIndicator() {
        if (!window.progressIndicator) {
            window.progressIndicator = {
                show: (title, message) => this.showLoadingIndicator(true, `${title}: ${message}`),
                hide: () => this.hideLoadingIndicatorDirect(),
                update: (message) => this.updateLoadingMessage(message)
            };
        }
    }

    /**
     * Show/hide loading indicator - OPTIMIZED for fast searches
     */
    showLoadingIndicator(show, message = 'Yükleniyor...') {
        console.log('💡 showLoadingIndicator called - show:', show, 'message:', message);
        
        // Recursive protection
        if (this._showingIndicator) {
            console.warn('⚠️ Recursive showLoadingIndicator call detected, aborting!');
            return;
        }
        
        this._showingIndicator = true;
        
        try {
            if (show) {
                // For fast searches, use minimal loading indicator
                if (!this.elements.loadingIndicator) {
                    this.elements.loadingIndicator = this.createMinimalLoadingIndicator();
                }
                
                this.elements.loadingIndicator.querySelector('.loading-message').textContent = message;
                this.elements.loadingIndicator.style.display = 'flex';
                
                // Don't lock body overflow for fast operations
                // document.body.style.overflow = 'hidden'; // Removed for better UX
            } else {
                this.hideLoadingIndicatorDirect();
            }
        } finally {
            this._showingIndicator = false;
        }
    }

    /**
     * Direct hide method - prevents infinite recursion
     */
    hideLoadingIndicatorDirect() {
        if (this.elements.loadingIndicator) {
            this.elements.loadingIndicator.style.display = 'none';
        }
        
        // Quick cleanup of common loading overlays
        const quickSelectors = [
            '.loading-overlay', 
            '.minimal-loading-overlay',
            '.progress-overlay', 
            '.notification-modal-backdrop', 
            '.paste-modal-backdrop'
        ];
        
        quickSelectors.forEach(selector => {
            const elements = document.querySelectorAll(selector);
            elements.forEach(el => el.style.display = 'none');
        });
        
        // Restore body overflow
        document.body.style.overflow = '';
    }

    /**
     * Force hide all loading indicators (cleanup) - FIXED RECURSION BUG
     */
    forceHideAllLoadingIndicators() {
        // Use direct hide method to prevent recursion
        this.hideLoadingIndicatorDirect();
        
        console.log('🧹 Force cleaned all loading indicators');
    }

    /**
     * Create loading indicator element
     */
    createLoadingIndicator() {
        const indicator = document.createElement('div');
        indicator.className = 'loading-overlay';
        indicator.innerHTML = `
            <div class="loading-content">
                <div class="loading-spinner"></div>
                <div class="loading-message">Yükleniyor...</div>
            </div>
        `;
        
        // Add styles
        const style = document.createElement('style');
        style.textContent = `
            .loading-overlay {
                position: fixed;
                top: 0;
                left: 0;
                width: 100%;
                height: 100%;
                background: rgba(0, 0, 0, 0.7);
                display: none;
                align-items: center;
                justify-content: center;
                z-index: 9999;
                backdrop-filter: blur(2px);
            }
            .loading-content {
                background: white;
                padding: 30px;
                border-radius: 12px;
                text-align: center;
                box-shadow: 0 10px 30px rgba(0, 0, 0, 0.3);
                max-width: 300px;
            }
            .loading-spinner {
                width: 40px;
                height: 40px;
                border: 4px solid #f3f3f3;
                border-top: 4px solid #007bff;
                border-radius: 50%;
                animation: spin 1s linear infinite;
                margin: 0 auto 15px;
            }
            .loading-message {
                color: #333;
                font-weight: 500;
            }
            @keyframes spin {
                0% { transform: rotate(0deg); }
                100% { transform: rotate(360deg); }
            }
        `;
        document.head.appendChild(style);
        
        document.body.appendChild(indicator);
        return indicator;
    }

    /**
     * Create minimal loading indicator for fast searches - PERFORMANCE OPTIMIZED
     */
    createMinimalLoadingIndicator() {
        const indicator = document.createElement('div');
        indicator.className = 'minimal-loading-overlay';
        indicator.innerHTML = `
            <div class="minimal-loading-content">
                <div class="minimal-spinner"></div>
                <div class="loading-message">Aranıyor...</div>
            </div>
        `;

        // Add minimal CSS if not already present
        if (!document.getElementById('minimal-loading-styles')) {
            const style = document.createElement('style');
            style.id = 'minimal-loading-styles';
            style.textContent = `
                .minimal-loading-overlay {
                    position: fixed;
                    top: 0;
                    right: 20px;
                    width: 200px;
                    height: 60px;
                    background: rgba(255, 255, 255, 0.95);
                    border: 1px solid #e0e0e0;
                    border-radius: 8px;
                    box-shadow: 0 4px 12px rgba(0, 0, 0, 0.15);
                    display: flex;
                    align-items: center;
                    justify-content: center;
                    z-index: 9999;
                    backdrop-filter: blur(3px);
                    transform: translateY(-80px);
                    transition: transform 0.2s ease;
                }
                .minimal-loading-overlay[style*="flex"] {
                    transform: translateY(20px);
                }
                .minimal-loading-content {
                    display: flex;
                    align-items: center;
                    gap: 10px;
                }
                .minimal-spinner {
                    width: 20px;
                    height: 20px;
                    border: 2px solid #f3f3f3;
                    border-top: 2px solid #007bff;
                    border-radius: 50%;
                    animation: minimal-spin 0.8s linear infinite;
                }
                .minimal-loading-overlay .loading-message {
                    color: #555;
                    font-size: 13px;
                    font-weight: 500;
                }
                @keyframes minimal-spin {
                    0% { transform: rotate(0deg); }
                    100% { transform: rotate(360deg); }
                }
            `;
            document.head.appendChild(style);
        }

        document.body.appendChild(indicator);
        return indicator;
    }

    /**
     * Update loading message
     */
    updateLoadingMessage(message) {
        const messageElement = this.elements.loadingIndicator?.querySelector('.loading-message');
        if (messageElement) {
            messageElement.textContent = message;
        }
    }

    /**
     * Handle keyboard shortcuts
     */
    handleKeyboardShortcuts(event) {
        // Ctrl+F: Focus search
        if (event.ctrlKey && event.key === 'f') {
            event.preventDefault();
            this.elements.searchInput?.focus();
        }
        
        // Escape: Clear search/close modals
        if (event.key === 'Escape') {
            if (this.elements.searchInput === document.activeElement) {
                this.elements.searchInput.value = '';
                this.handleSearch();
            }
        }
        
        // Ctrl+A: Select all products
        if (event.ctrlKey && event.key === 'a' && this.elements.selectAllCheckbox) {
            event.preventDefault();
            this.elements.selectAllCheckbox.click();
        }
    }

    /**
     * Notification methods
     */
    showSuccessNotification(message) {
        this.showNotification(message, 'success');
    }

    showErrorNotification(message) {
        this.showNotification(message, 'error');
    }

    showWarningNotification(message) {
        this.showNotification(message, 'warning');
    }

    showNotification(message, type = 'info') {
        // Use existing notification system if available
        if (window.notificationSystem) {
            window.notificationSystem[type](type === 'error' ? 'Hata' : 'Bilgi', message);
            return;
        }

        // Fallback notification
        const notification = document.createElement('div');
        notification.className = `notification notification-${type}`;
        notification.textContent = message;
        
        Object.assign(notification.style, {
            position: 'fixed',
            top: '20px',
            right: '20px',
            padding: '12px 20px',
            borderRadius: '6px',
            color: 'white',
            backgroundColor: type === 'success' ? '#28a745' : type === 'error' ? '#dc3545' : '#ffc107',
            zIndex: '10000',
            boxShadow: '0 4px 12px rgba(0,0,0,0.15)',
            maxWidth: '400px',
            wordWrap: 'break-word'
        });

        document.body.appendChild(notification);

        setTimeout(() => {
            notification.remove();
        }, 5000);
    }

    /**
     * Cleanup method
     */
    cleanup() {
        // Remove event listeners
        this.eventListeners.forEach(({ element, event, handler, options }) => {
            element.removeEventListener(event, handler, options);
        });
        this.eventListeners = [];

        // Clear cache
        this.state.cache.clear();

        // Clear timeouts
        if (this.state.searchTimeout) {
            clearTimeout(this.state.searchTimeout);
        }

        console.log('ProductIndexManager cleaned up');
    }

    /**
     * Destroy instance
     */
    destroy() {
        this.cleanup();
        
        // Remove loading indicator
        if (this.elements.loadingIndicator) {
            this.elements.loadingIndicator.remove();
        }

        // Reset window variables
        if (window.productIndexManager === this) {
            window.productIndexManager = null;
        }
    }
}

// Global functions for backward compatibility
window.getSelectedProductIds = function() {
    return Array.from(document.querySelectorAll('.product-checkbox:checked')).map(cb => cb.value);
};

window.changePage = function(page) {
    if (window.productIndexManager) {
        window.productIndexManager.performSearch(page);
    }
};

// Export for module systems
if (typeof module !== 'undefined' && module.exports) {
    module.exports = ProductIndexManager;
}

// Global instance
window.ProductIndexManager = ProductIndexManager;

// Auto-initialize when DOM is ready (only if not already initialized)
if (document.readyState === 'loading') {
    document.addEventListener('DOMContentLoaded', () => {
        if (!window.productIndexManager) {
            console.log('🚀 Auto-initializing ProductIndexManager from DOMContentLoaded');
            window.productIndexManager = new ProductIndexManager();
            window.productIndexManager.init();
        }
    });
} else {
    if (!window.productIndexManager) {
        console.log('🚀 Auto-initializing ProductIndexManager immediately');
        window.productIndexManager = new ProductIndexManager();
        window.productIndexManager.init();
    }
}

// Also expose ProductIndexManager globally for compatibility
window.ProductIndexManager = ProductIndexManager;

// Archive/Unarchive functions are now handled by BulkOperationsManager
// Global functions are set up in setupGlobalHelpers() method

// Emergency cleanup function - expose globally for manual intervention
window.emergencyCleanup = function() {
    console.log('🚨 Emergency cleanup initiated...');
    
    // Force reset all loading states
    if (window.productIndexManager) {
        window.productIndexManager.state.isLoading = false;
        window.productIndexManager.state.isPasteInProgress = false;
        window.productIndexManager.forceHideAllLoadingIndicators();
    }
    
    // Remove all loading overlays and modals
    const overlays = document.querySelectorAll(
        '.loading-overlay, .progress-overlay, .notification-modal-backdrop, .paste-modal-backdrop, .modal-backdrop'
    );
    overlays.forEach(overlay => overlay.remove());
    
    // Reset body overflow
    document.body.style.overflow = '';
    
    console.log('🧹 Emergency cleanup completed');
};

// Global clearAllFilters function
window.clearAllFilters = function() {
    console.log('🧹 Clearing all filters...');
    
    // Find the form
    const form = document.getElementById('searchForm');
    if (!form) {
        console.error('❌ Search form not found!');
        return;
    }
    
    // Clear all text and number inputs
    form.querySelectorAll('input[type="text"], input[type="number"]').forEach(input => {
        if (input.name !== 'page') { // Don't clear page field
            input.value = '';
        }
    });
    
    // Reset all select elements to first option (except pageSize)
    form.querySelectorAll('select').forEach(select => {
        if (select.name !== 'pageSize') {
            select.selectedIndex = 0;
        }
    });
    
    // Clear all checkboxes
    form.querySelectorAll('input[type="checkbox"]').forEach(checkbox => {
        checkbox.checked = false;
    });
    
    // Reset page to 1
    const pageInput = form.querySelector('input[name="page"]');
    if (pageInput) {
        pageInput.value = '1';
    } else {
        // If no page input exists, create one
        const hiddenPageInput = document.createElement('input');
        hiddenPageInput.type = 'hidden';
        hiddenPageInput.name = 'page';
        hiddenPageInput.value = '1';
        form.appendChild(hiddenPageInput);
    }
    
    console.log('✅ All filters cleared, submitting form...');
    
    // Submit the form
    form.submit();
};

} // Close the duplicate prevention block

// Global helper function for image error notifications
window.showImageErrorNotification = function(failedCount) {
    console.log('🚨 Showing image error notification for', failedCount, 'failed images');
    
    // Create toast notification
    const toast = document.createElement('div');
    toast.style.cssText = `
        position: fixed;
        top: 20px;
        right: 20px;
        background: linear-gradient(135deg, #ffeeba 0%, #fff3cd 100%);
        border: 1px solid #ffeaa7;
        border-radius: 8px;
        padding: 16px 20px;
        box-shadow: 0 4px 12px rgba(0, 0, 0, 0.15);
        z-index: 9999;
        max-width: 350px;
        font-family: system-ui, -apple-system, sans-serif;
        animation: slideInRight 0.3s ease;
    `;
    
    toast.innerHTML = `
        <div style="display: flex; align-items: center; gap: 12px;">
            <span style="font-size: 24px;">📷</span>
            <div style="flex: 1;">
                <div style="font-weight: 600; color: #856404; margin-bottom: 4px;">
                    Bazı Görseller Bulunamadı
                </div>
                <div style="font-size: 13px; color: #856404; line-height: 1.4;">
                    ${failedCount} ürün görseli yüklenemedi. Görseller kaldırılmış veya taşınmış olabilir.
                </div>
            </div>
            <button onclick="this.parentElement.parentElement.remove()" 
                    style="background: none; border: none; font-size: 18px; color: #856404; cursor: pointer; padding: 4px;">
                ×
            </button>
        </div>
    `;
    
    // Add CSS animation
    const style = document.createElement('style');
    style.textContent = `
        @keyframes slideInRight {
            from { transform: translateX(100%); opacity: 0; }
            to { transform: translateX(0); opacity: 1; }
        }
    `;
    document.head.appendChild(style);
    
    document.body.appendChild(toast);
    
    // Auto-remove after 8 seconds
    setTimeout(() => {
        if (toast.parentElement) {
            toast.style.animation = 'slideInRight 0.3s ease reverse';
            setTimeout(() => toast.remove(), 300);
        }
    }, 8000);
};

// Individual Product Actions - Must be outside the class definition
window.deleteProduct = function(productId, productName) {
    if (confirm(`"${productName}" ürünü kalıcı olarak silmek istediğinizden emin misiniz? Bu işlem geri alınamaz!`)) {
        // Show loading indicator if available
        if (window.showProgress) {
            window.showProgress('Ürün Siliniyor', `"${productName}" siliniyor...`);
        }
        
        const form = document.createElement('form');
        form.method = 'POST';
        form.action = `/Product/DeleteProduct/${productId}`;
        
        // Add anti-forgery token
        const token = document.querySelector('input[name="__RequestVerificationToken"]');
        if (token) {
            const tokenInput = document.createElement('input');
            tokenInput.type = 'hidden';
            tokenInput.name = '__RequestVerificationToken';
            tokenInput.value = token.value;
            form.appendChild(tokenInput);
        } else {
            console.error('Anti-forgery token not found!');
            if (window.hideProgress) window.hideProgress();
            if (window.showError) {
                window.showError('Hata', 'Güvenlik token\'ı bulunamadı. Sayfayı yenileyin.');
            } else {
                alert('Güvenlik token\'ı bulunamadı. Sayfayı yenileyin.');
            }
            return;
        }
        
        document.body.appendChild(form);
        form.submit();
    }
};
