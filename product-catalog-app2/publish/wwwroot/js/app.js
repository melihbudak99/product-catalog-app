// Global error handler for Chrome extension related errors
(function() {
    'use strict';
    
    // Prevent Chrome extension message errors from appearing in console
    const originalError = console.error;
    console.error = function(...args) {
        const message = args.join(' ');
        
        // Filter out Chrome extension related errors
        if (message.includes('message channel closed') || 
            message.includes('Extension context invalidated') ||
            message.includes('Could not establish connection')) {
            return; // Suppress these errors
        }
        
        // Allow other errors to be displayed (strict mode compatible)
        originalError.call(console, ...args);
    };

    // Handle unhandled promise rejections
    window.addEventListener('unhandledrejection', function(event) {
        const message = event.reason?.message || event.reason || '';
        if (typeof message === 'string' && 
            (message.includes('message channel closed') ||
             message.includes('Extension context invalidated'))) {
            event.preventDefault(); // Prevent the error from appearing in console
        }
    });
})();

// Global function declarations
let changePage;

document.addEventListener('DOMContentLoaded', function() {
    const productList = document.getElementById('product-list');

    function fetchProducts(search = '', category = '', brand = '', page = 1) {
        const params = new URLSearchParams();
        if (search) params.append('search', search);
        if (category) params.append('category', category);
        if (brand) params.append('brand', brand);
        params.append('page', page.toString());
        params.append('pageSize', '20');

        fetch(`/api/products?${params.toString()}`)
            .then(response => {
                if (!response.ok) {
                    throw new Error(`HTTP error! status: ${response.status}`);
                }
                return response.json();
            })
            .then(data => {
                displayProducts(data.products || data);
                updatePagination(data);
            })
            .catch(error => {
                console.error('Error fetching products:', error);
                showErrorMessage('Failed to load products. Please try again later.');
            });
    }

    function displayProducts(products) {
        if (!productList) return;
        
        productList.innerHTML = '';
        
        if (!Array.isArray(products) || products.length === 0) {
            productList.innerHTML = '<p class="no-products">No products available.</p>';
            return;
        }
        
        products.forEach(product => {
            const productItem = document.createElement('div');
            productItem.className = 'product-item';
            
            // Create elements safely to prevent XSS
            const title = document.createElement('h2');
            title.textContent = product.name || 'Unknown Product';
            
            const image = document.createElement('img');
            image.src = product.imageUrl || 'data:image/svg+xml;base64,PHN2ZyB3aWR0aD0iMTAwIiBoZWlnaHQ9IjEwMCIgdmlld0JveD0iMCAwIDEwMCAxMDAiIGZpbGw9Im5vbmUiIHhtbG5zPSJodHRwOi8vd3d3LnczLm9yZy8yMDAwL3N2ZyI+CjxyZWN0IHdpZHRoPSIxMDAiIGhlaWdodD0iMTAwIiBmaWxsPSIjZjhmOWZhIiBzdHJva2U9IiNkZWUyZTYiIHN0cm9rZS13aWR0aD0iMiIgc3Ryb2tlLWRhc2hhcnJheT0iNSw1Ii8+Cjx0ZXh0IHg9IjUwIiB5PSI0NSIgZm9udC1mYW1pbHk9InN5c3RlbS11aSIgZm9udC1zaXplPSIyNCIgZmlsbD0iIzZjNzU3ZCIgdGV4dC1hbmNob3I9Im1pZGRsZSI+8J+WvO+4jzwvdGV4dD4KPHRleHQgeD0iNTAiIHk9IjY1IiBmb250LWZhbWlseT0ic3lzdGVtLXVpIiBmb250LXNpemU9IjgiIGZpbGw9IiM2Yzc1N2QiIHRleHQtYW5jaG9yPSJtaWRkbGUiPkfDtnJzZWwgWW9rPC90ZXh0Pgo8L3N2Zz4K';
            image.alt = product.name || 'Product Image';
            image.onerror = function() {
                // Create CSS-based placeholder instead of loading another image
                this.style.display = 'none';
                const container = this.parentElement;
                if (container && !container.querySelector('.app-error-placeholder')) {
                    const errorDiv = document.createElement('div');
                    errorDiv.className = 'app-error-placeholder';
                    errorDiv.innerHTML = `
                        <div style="
                            display: flex;
                            flex-direction: column;
                            align-items: center;
                            justify-content: center;
                            background: linear-gradient(135deg, #f8f9fa 0%, #e9ecef 100%);
                            border: 2px dashed #dee2e6;
                            border-radius: 6px;
                            width: 100px;
                            height: 100px;
                            color: #6c757d;
                            font-family: system-ui, -apple-system, sans-serif;
                        ">
                            <span style="font-size: 24px; margin-bottom: 4px;">🖼️</span>
                            <p style="margin: 0; font-size: 8px; text-align: center; font-weight: 500;">Görsel Yok</p>
                        </div>
                    `;
                    container.appendChild(errorDiv);
                }
            };
            
            const features = document.createElement('p');
            features.textContent = Array.isArray(product.features) ? 
                product.features.join(', ') : 
                (product.features || 'No features available');
            
            const link = document.createElement('a');
            link.href = `/Product/Details/${encodeURIComponent(product.id)}`;
            link.textContent = 'View Details';
            link.className = 'btn btn-primary';
            
            productItem.appendChild(title);
            productItem.appendChild(image);
            productItem.appendChild(features);
            productItem.appendChild(link);
            
            productList.appendChild(productItem);
        });
    }

    function showErrorMessage(message) {
        // Use centralized notification system if available
        if (window.showError) {
            window.showError('Hata', message);
        } else if (window.notificationSystem) {
            window.notificationSystem.error('Hata', message);
        } else {
            // Fallback for old system
            console.error('Error:', message);
            if (productList) {
                productList.innerHTML = `<div class="alert alert-error">${message}</div>`;
            }
        }
    }

    function updatePagination(data) {
        // Update pagination controls if they exist
        const paginationContainer = document.getElementById('pagination-container');
        if (paginationContainer && data.totalPages > 1) {
            let paginationHtml = '<div class="pagination">';
            
            // Previous button
            if (data.page > 1) {
                paginationHtml += `<button onclick="changePage(${data.page - 1})" class="btn btn-secondary">Previous</button>`;
            }
            
            // Page numbers
            const startPage = Math.max(1, data.page - 2);
            const endPage = Math.min(data.totalPages, data.page + 2);
            
            for (let i = startPage; i <= endPage; i++) {
                const activeClass = i === data.page ? 'btn-primary' : 'btn-secondary';
                paginationHtml += `<button onclick="changePage(${i})" class="btn ${activeClass}">${i}</button>`;
            }
            
            // Next button
            if (data.page < data.totalPages) {
                paginationHtml += `<button onclick="changePage(${data.page + 1})" class="btn btn-secondary">Next</button>`;
            }
            
            paginationHtml += '</div>';
            paginationContainer.innerHTML = paginationHtml;
        }
    }

    function changePage(page) {
        const searchInput = document.getElementById('search-input');
        const categorySelect = document.getElementById('category-select');
        const brandSelect = document.getElementById('brand-select');
        
        const search = searchInput ? searchInput.value : '';
        const category = categorySelect ? categorySelect.value : '';
        const brand = brandSelect ? brandSelect.value : '';
        
        fetchProducts(search, category, brand, page);
    }

    // Assign the local function to the global variable
    window.changePage = changePage;

    function setupSearchControls() {
        const searchInput = document.getElementById('search-input');
        const categorySelect = document.getElementById('category-select');
        const brandSelect = document.getElementById('brand-select');
        
        if (searchInput) {
            searchInput.addEventListener('input', debounce(() => {
                const search = searchInput.value;
                const category = categorySelect ? categorySelect.value : '';
                const brand = brandSelect ? brandSelect.value : '';
                fetchProducts(search, category, brand, 1);
            }, 300));
        }
        
        if (categorySelect) {
            categorySelect.addEventListener('change', () => {
                const search = searchInput ? searchInput.value : '';
                const category = categorySelect.value;
                const brand = brandSelect ? brandSelect.value : '';
                fetchProducts(search, category, brand, 1);
            });
        }
        
        if (brandSelect) {
            brandSelect.addEventListener('change', () => {
                const search = searchInput ? searchInput.value : '';
                const category = categorySelect ? categorySelect.value : '';
                const brand = brandSelect.value;
                fetchProducts(search, category, brand, 1);
            });
        }
    }

    function debounce(func, delay) {
        let timeoutId;
        return function (...args) {
            clearTimeout(timeoutId);
            timeoutId = setTimeout(() => func.call(this, ...args), delay);
        };
    }

    // Initialize
    if (productList) {
        fetchProducts();
        setupSearchControls();
    }

    // Make changePage available globally
    window.changePage = changePage;
});

// Character Counter Functionality - Optimized Version
function initializeCharacterCounter() {
    const characterCounterInputs = document.querySelectorAll('.character-counter');
    
    characterCounterInputs.forEach(input => {
        const maxChars = parseInt(input.getAttribute('maxlength')) || 200;
        let lastClassName = '';
        
        function updateCounter() {
            const text = input.value;
            const charCount = text.length;
            
            const counterContainer = input.parentNode.querySelector('.character-count');
            if (counterContainer) {
                const charCountSpan = counterContainer.querySelector('.char-count');
                
                if (charCountSpan) {
                    charCountSpan.textContent = `${charCount}/${maxChars}`;
                }
                
                // Optimized: Only change className if needed
                let newClassName = 'character-count';
                if (charCount > maxChars) {
                    newClassName += ' danger';
                } else if (charCount >= maxChars * 0.9) {
                    newClassName += ' warning';
                }
                
                // Only update if className changed - Performance optimization
                if (lastClassName !== newClassName) {
                    counterContainer.className = newClassName;
                    lastClassName = newClassName;
                }
                
                // Optimized border and validation handling
                if (charCount > maxChars) {
                    if (input.style.borderColor !== '#dc3545') {
                        input.style.borderColor = '#dc3545';
                        input.setCustomValidity(`Maksimum ${maxChars} karakter kullanabilirsiniz. Şu anda ${charCount} karakter var.`);
                    }
                } else {
                    if (input.style.borderColor !== '') {
                        input.style.borderColor = '';
                        input.setCustomValidity('');
                    }
                }
            }
        }
        
        // İlk yüklemede sayacı güncelle
        updateCounter();
        
        // Debounced input event for better performance
        let inputTimeout;
        input.addEventListener('input', function() {
            clearTimeout(inputTimeout);
            inputTimeout = setTimeout(updateCounter, 50); // Debounced for performance
        });
        
        input.addEventListener('paste', function() {
            // Paste işleminden sonra sayacı güncelle
            setTimeout(updateCounter, 10);
        });
    });
}

// DOMContentLoaded event listener'ına karakter sayacı başlatmayı ekle
document.addEventListener('DOMContentLoaded', function() {
    // ...existing code...
    
    // Karakter sayacını başlat
    initializeCharacterCounter();
    
    // ...existing code...
});

// Global Collapsible Section Toggle Function
function toggleSection(sectionId) {
    const section = document.getElementById(sectionId);
    if (!section) return;
    
    const header = section.querySelector('.section-header');
    const content = section.querySelector('.collapsible-content');
    const icon = header.querySelector('.toggle-icon');
    
    // CSS sınıf tabanlı kontrol - hidden sınıfını kontrol et
    const isHidden = content.classList.contains('hidden');
    
    if (isHidden) {
        // Expand - Göster
        content.classList.remove('hidden');
        content.classList.add('show');
        header.classList.add('active');
        if (icon) icon.style.transform = 'rotate(180deg)';
        
        // Smooth transition için animasyon
        content.style.maxHeight = content.scrollHeight + 'px';
        setTimeout(() => {
            content.style.maxHeight = 'none';
        }, 250); // CSS transition süresi
    } else {
        // Collapse - Gizle
        content.style.maxHeight = content.scrollHeight + 'px';
        header.classList.remove('active');
        if (icon) icon.style.transform = 'rotate(0deg)';
        
        setTimeout(() => {
            content.style.maxHeight = '0';
        }, 10);
        
        setTimeout(() => {
            content.classList.add('hidden');
            content.classList.remove('show');
        }, 250); // CSS transition süresi
    }
}

// Mobile Menu Toggle Function  
function toggleMobileMenu() {
    const nav = document.querySelector('.main-nav');
    if (nav) {
        nav.classList.toggle('mobile-open');
    }
}

// Note: All bulk operations have been moved to Index.cshtml to avoid conflicts

// Individual Product Actions - Archive/Unarchive functions moved to Index.cshtml
// These are just placeholders for backwards compatibility

function deleteProduct(productId, productName) {
    if (confirm(`"${productName}" ürünü kalıcı olarak silmek istediğinizden emin misiniz? Bu işlem geri alınamaz!`)) {
        // Show loading indicator
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
}

// Initialize collapsible sections on page load
document.addEventListener('DOMContentLoaded', function() {
    // Add keyboard support for accessibility
    const headers = document.querySelectorAll('.collapsible .section-header');
    headers.forEach(header => {
        header.setAttribute('tabindex', '0');
        header.setAttribute('role', 'button');
        header.setAttribute('aria-expanded', 'false');
    });
    
    // Auto-expand first section (basic info) and keep others collapsed
    const collapsibleSections = document.querySelectorAll('.form-section.collapsible');
    collapsibleSections.forEach((section, index) => {
        const content = section.querySelector('.collapsible-content');
        if (content) {
            // Keep all collapsed by default for better UX
            content.style.display = 'none';
        }
    });
    
    // If there are any validation errors, expand the relevant sections
    const validationErrors = document.querySelectorAll('.field-validation-error, .validation-summary-errors');
    if (validationErrors.length > 0) {
        // Find and expand sections with errors
        validationErrors.forEach(error => {
            const section = error.closest('.form-section.collapsible');
            if (section) {
                const sectionId = section.id;
                if (sectionId) {
                    toggleSection(sectionId);
                }
            }
        });
        
        // Smooth scroll to first error section
        const firstError = validationErrors[0];
        const errorSection = firstError.closest('.form-section');
        if (errorSection) {
            setTimeout(() => {
                errorSection.scrollIntoView({ 
                    behavior: 'smooth', 
                    block: 'start' 
                });
            }, 500);
        }
    }
    
    // Initialize bulk selection functionality
    // Bulk selection handled in Index.cshtml
    
    // Initialize other components
    initializeCharacterCounter();
    
    // Enhanced image handling
    enhanceProductImages();
});

// Make functions globally available
window.toggleSection = toggleSection;
// toggleMediaExpansion is handled by _ProductFormScripts.cshtml - removed to avoid conflicts
// window.changePage = changePage; - Already made global inside DOMContentLoaded
window.toggleMobileMenu = toggleMobileMenu;
window.enhanceProductImages = enhanceProductImages;

// Global marketplace gallery functions for Index.cshtml
window.openMarketplaceGallery = function(productId, productName, imageUrls) {
    // This function is defined in Index.cshtml for proper access to DOM elements
    console.log('Marketplace gallery function should be called from Index.cshtml');
    console.log('Product ID:', productId, 'Name:', productName, 'Images:', imageUrls);
    
    // Fallback functionality if the main function is not available
    if (typeof openMarketplaceGallery !== 'undefined') {
        return openMarketplaceGallery(productId, productName, imageUrls);
    }
};

window.openImageModal = function(imageSrc, productName) {
    // This function is defined in Index.cshtml for proper access to DOM elements
    console.log('Image modal function should be called from Index.cshtml');
    console.log('Image src:', imageSrc, 'Product name:', productName);
    
    // Enhanced fallback modal functionality
    if (imageSrc && productName) {
        // Create simple modal if main one is not available
        const existingModal = document.getElementById('imagePreviewModal');
        if (!existingModal) {
            createFallbackImageModal(imageSrc, productName);
        }
    }
};

// Enhanced fallback image modal creator
function createFallbackImageModal(imageSrc, productName) {
    // Remove existing fallback modal if any
    const existingFallback = document.getElementById('fallbackImageModal');
    if (existingFallback) {
        existingFallback.remove();
    }
    
    // Create modal elements
    const modal = document.createElement('div');
    modal.id = 'fallbackImageModal';
    modal.style.cssText = `
        position: fixed;
        top: 0;
        left: 0;
        right: 0;
        bottom: 0;
        background: rgba(0, 0, 0, 0.9);
        display: flex;
        align-items: center;
        justify-content: center;
        z-index: 9999;
        padding: 20px;
        box-sizing: border-box;
    `;
    
    const content = document.createElement('div');
    content.style.cssText = `
        background: white;
        border-radius: 12px;
        max-width: 90vw;
        max-height: 90vh;
        padding: 20px;
        position: relative;
        display: flex;
        flex-direction: column;
        align-items: center;
    `;
    
    const title = document.createElement('h3');
    title.textContent = productName + ' - Ürün Görseli';
    title.style.cssText = `
        margin: 0 0 15px 0;
        color: #333;
        text-align: center;
        font-size: 18px;
    `;
    
    const img = document.createElement('img');
    img.src = imageSrc;
    img.alt = productName;
    img.style.cssText = `
        max-width: 100%;
        max-height: 70vh;
        object-fit: contain;
        border-radius: 8px;
        box-shadow: 0 8px 32px rgba(0, 0, 0, 0.3);
    `;
    
    const closeBtn = document.createElement('button');
    closeBtn.innerHTML = '❌ Kapat';
    closeBtn.style.cssText = `
        position: absolute;
        top: 10px;
        right: 10px;
        background: #dc3545;
        color: white;
        border: none;
        border-radius: 6px;
        padding: 8px 12px;
        cursor: pointer;
        font-size: 14px;
        transition: background-color 0.3s;
    `;
    closeBtn.onmouseover = () => closeBtn.style.backgroundColor = '#c82333';
    closeBtn.onmouseout = () => closeBtn.style.backgroundColor = '#dc3545';
    
    // Close modal function
    const closeFallbackModal = () => {
        modal.remove();
        document.body.style.overflow = 'auto';
        document.removeEventListener('keydown', handleEscKey);
    };
    
    const handleEscKey = (e) => {
        if (e.key === 'Escape') {
            closeFallbackModal();
        }
    };
    
    // Event listeners
    closeBtn.onclick = closeFallbackModal;
    modal.onclick = (e) => {
        if (e.target === modal) {
            closeFallbackModal();
        }
    };
    document.addEventListener('keydown', handleEscKey);
    
    // Assemble modal
    content.appendChild(title);
    content.appendChild(img);
    content.appendChild(closeBtn);
    modal.appendChild(content);
    
    // Show modal
    document.body.appendChild(modal);
    document.body.style.overflow = 'hidden';
}

// Enhanced image loading and error handling - CSS handles hover effects
function enhanceProductImages() {
    const productImages = document.querySelectorAll('.product-thumbnail, .clickable-image');
    
    productImages.forEach(img => {
        // Enhanced hover effects - delegated to CSS classes for better performance
        if (img.classList.contains('clickable-image')) {
            // CSS will handle hover effects automatically - no JS needed
            img.style.cursor = 'pointer';
        }
        
        // Error handling with enhanced fallback
        img.addEventListener('error', function() {
            this.style.display = 'none';
            const container = this.parentElement;
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
                        border: 2px dashed #dee2e6;
                        border-radius: 6px;
                        width: 50px;
                        height: 50px;
                        font-size: 10px;
                        color: #6c757d;
                    ">
                        <span style="font-size: 14px; margin-bottom: 2px;">🖼️</span>
                        <p style="margin: 2px 0 0 0; font-size: 8px; text-align: center; font-weight: 500;">Yüklenemedi</p>
                    </div>
                `;
                container.appendChild(errorDiv);
            }
        });
    });
}

// window.initializeWordCounter = initializeWordCounter; - Function not defined

// Note: Bulk operations are now handled in Index.cshtml

// Global exports for individual product actions - archive functions handled in Index.cshtml
window.deleteProduct = deleteProduct;