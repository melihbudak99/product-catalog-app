/**
 * BULK OPERATIONS MODULE - Clean Architecture
 * Handles bulk selection and operations for products
 */

class BulkOperationsManager {
    constructor() {
        this.selectedItems = new Set();
        this.isInitialized = false;
        console.log('🏗️ BulkOperationsManager constructor called');
    }

    init() {
        if (this.isInitialized) {
            console.log('⚠️ BulkOperationsManager already initialized');
            return;
        }
        
        console.log('🚀 Initializing BulkOperationsManager...');
        this.setupEventListeners();
        this.isInitialized = true;
        console.log('✅ BulkOperationsManager initialized successfully');
    }

    setupEventListeners() {
        console.log('🎯 Setting up bulk operations event listeners...');
        
        // Checkbox event delegation
        document.addEventListener('change', (e) => {
            if (e.target.matches('#selectAll')) {
                console.log('📋 Select All checkbox changed:', e.target.checked);
                this.toggleSelectAll(e.target.checked);
            } else if (e.target.matches('.product-checkbox')) {
                console.log('☑️ Product checkbox changed:', e.target.value, e.target.checked);
                this.handleItemSelection(e.target);
            }
        });

        // Data-action based event delegation for bulk operations
        document.addEventListener('click', (e) => {
            const actionElement = e.target.closest('[data-action]');
            if (!actionElement) return;

            const action = actionElement.dataset.action;
            
            // Only handle bulk operation actions
            if (['export-excel', 'bulk-archive', 'bulk-unarchive', 'bulk-delete', 'clear-selection'].includes(action)) {
                console.log('🎯 Bulk action triggered:', action);
                e.preventDefault();
                e.stopPropagation();
                
                switch (action) {
                    case 'export-excel':
                        this.exportSelectedToExcel();
                        break;
                    case 'bulk-archive':
                        this.performBulkAction('archive');
                        break;
                    case 'bulk-unarchive':
                        this.performBulkAction('unarchive');
                        break;
                    case 'bulk-delete':
                        this.performBulkAction('delete');
                        break;
                    case 'clear-selection':
                        this.clearSelection();
                        break;
                }
            }
        });
        
        console.log('✅ Bulk operations event listeners setup completed');
    }

    toggleSelectAll(checked) {
        console.log(`🔄 Toggle all checkboxes to: ${checked}`);
        const checkboxes = document.querySelectorAll('.product-checkbox');
        console.log(`📊 Found ${checkboxes.length} product checkboxes`);
        
        checkboxes.forEach(checkbox => {
            checkbox.checked = checked;
            this.updateItemSelection(checkbox.value, checked);
        });
        this.updateBulkActionsVisibility();
    }

    handleItemSelection(checkbox) {
        this.updateItemSelection(checkbox.value, checkbox.checked);
        this.updateSelectAllState();
        this.updateBulkActionsVisibility();
    }

    updateItemSelection(itemId, selected) {
        if (selected) {
            this.selectedItems.add(itemId);
        } else {
            this.selectedItems.delete(itemId);
        }
        console.log(`📊 Selected items count: ${this.selectedItems.size}`);
    }

    updateSelectAllState() {
        const selectAllCheckbox = document.getElementById('selectAll');
        const checkboxes = document.querySelectorAll('.product-checkbox');
        const checkedCount = document.querySelectorAll('.product-checkbox:checked').length;
        
        if (selectAllCheckbox) {
            selectAllCheckbox.checked = checkedCount === checkboxes.length;
            selectAllCheckbox.indeterminate = checkedCount > 0 && checkedCount < checkboxes.length;
        }
    }

    updateBulkActionsVisibility() {
        const bulkActionsPanel = document.getElementById('bulkActionsPanel');
        const selectedCount = this.selectedItems.size;
        
        console.log(`🔍 Updating bulk actions visibility. Selected: ${selectedCount}`);
        
        if (bulkActionsPanel) {
            if (selectedCount > 0) {
                bulkActionsPanel.style.display = 'block';
                const countElement = bulkActionsPanel.querySelector('.selected-count');
                if (countElement) {
                    countElement.textContent = `${selectedCount} ürün seçildi`;
                }
                console.log('✅ Bulk actions panel shown');
            } else {
                bulkActionsPanel.style.display = 'none';
                console.log('❌ Bulk actions panel hidden');
            }
        } else {
            console.warn('⚠️ Bulk actions panel (#bulkActionsPanel) not found in DOM');
        }
    }

    async performBulkAction(action) {
        if (this.selectedItems.size === 0) {
            if (typeof window.showWarning === 'function') {
                window.showWarning('Uyarı', 'Lütfen işlem yapmak için ürün seçiniz.');
            } else {
                alert('Lütfen işlem yapmak için ürün seçiniz.');
            }
            return;
        }

        const selectedIds = Array.from(this.selectedItems);
        console.log(`🔄 Performing bulk ${action} on items:`, selectedIds);
        
        // Confirmation dialog
        const confirmed = await this.showConfirmationDialog(action, selectedIds.length);
        if (!confirmed) return;

        try {
            if (typeof window.showInfo === 'function') {
                window.showInfo('İşlem', `${selectedIds.length} ürün işleniyor...`);
            }
            
            const response = await fetch('/Product/BulkOperation', {
                method: 'POST',
                headers: {
                    'Content-Type': 'application/json',
                    'RequestVerificationToken': document.querySelector('input[name="__RequestVerificationToken"]').value
                },
                body: JSON.stringify({
                    action: action,
                    productIds: selectedIds
                })
            });

            if (response.ok) {
                const result = await response.json();
                if (result.success) {
                    if (typeof window.showSuccess === 'function') {
                        window.showSuccess('Başarılı', `${selectedIds.length} ürün başarıyla ${this.getActionText(action)}.`);
                    }
                    this.selectedItems.clear();
                    location.reload(); // Refresh page to show changes
                } else {
                    if (typeof window.showError === 'function') {
                        window.showError('Hata', result.message || 'İşlem başarısız oldu.');
                    }
                }
            } else {
                throw new Error('Server error');
            }
        } catch (error) {
            console.error('Bulk operation error:', error);
            if (typeof window.showError === 'function') {
                window.showError('Hata', 'İşlem sırasında bir hata oluştu.');
            }
        }
    }

    showConfirmationDialog(action, count) {
        return new Promise((resolve) => {
            const actionText = this.getActionText(action);
            const message = `${count} ürünü ${actionText} istediğinizden emin misiniz?`;
            resolve(confirm(message));
        });
    }

    getActionText(action) {
        const actionTexts = {
            'delete': 'silindi',
            'archive': 'arşivlendi',
            'unarchive': 'arşivden çıkarıldı'
        };
        return actionTexts[action] || 'işlendi';
    }

    clearSelection() {
        console.log('🧹 Clearing selection...');
        this.selectedItems.clear();
        document.querySelectorAll('.product-checkbox').forEach(cb => cb.checked = false);
        const selectAllCheckbox = document.getElementById('selectAll');
        if (selectAllCheckbox) {
            selectAllCheckbox.checked = false;
            selectAllCheckbox.indeterminate = false;
        }
        this.updateBulkActionsVisibility();
    }

    // Individual product archive/unarchive methods (Professional approach)
    async archiveProduct(productId, productName) {
        return this.performSingleProductAction('archive', productId, productName);
    }

    async unarchiveProduct(productId, productName) {
        return this.performSingleProductAction('unarchive', productId, productName);
    }

    async deleteProduct(productId, productName) {
        return this.performSingleProductAction('delete', productId, productName);
    }

    // DRY principle - Unified single product action handler
    async performSingleProductAction(action, productId, productName) {
        const actionTexts = {
            'archive': { verb: 'arşivle', past: 'arşivlendi' },
            'unarchive': { verb: 'arşivden çıkar', past: 'arşivden çıkarıldı' },
            'delete': { verb: 'sil', past: 'silindi' }
        };

        const actionText = actionTexts[action];
        if (!actionText) {
            console.error(`Unknown action: ${action}`);
            return false;
        }

        // Confirmation dialog
        const confirmed = confirm(`"${productName}" ürününü ${actionText.verb}mak istediğinizden emin misiniz?`);
        if (!confirmed) return false;

        try {
            if (typeof window.showInfo === 'function') {
                window.showInfo('İşlem', `${productName} ${actionText.verb}ılıyor...`);
            }

            const response = await fetch('/Product/BulkOperation', {
                method: 'POST',
                headers: {
                    'Content-Type': 'application/json',
                    'RequestVerificationToken': document.querySelector('input[name="__RequestVerificationToken"]')?.value || ''
                },
                body: JSON.stringify({
                    action: action,
                    productIds: [parseInt(productId)]
                })
            });

            if (response.ok) {
                const result = await response.json();
                if (result.success) {
                    if (typeof window.showSuccess === 'function') {
                        window.showSuccess('Başarılı', `${productName} başarıyla ${actionText.past}.`);
                    }
                    
                    // Refresh page to show changes
                    setTimeout(() => {
                        location.reload();
                    }, 1000);
                    
                    return true;
                } else {
                    if (typeof window.showError === 'function') {
                        window.showError('Hata', result.message || 'İşlem başarısız oldu.');
                    }
                    return false;
                }
            } else {
                throw new Error('Server error');
            }
        } catch (error) {
            console.error(`${action} operation error:`, error);
            if (typeof window.showError === 'function') {
                window.showError('Hata', `İşlem sırasında bir hata oluştu: ${error.message}`);
            }
            return false;
        }
    }

    // Export selected products to Excel using advanced export system
    async exportSelectedToExcel() {
        console.log('📊 Excel export requested...');
        
        if (this.selectedItems.size === 0) {
            if (typeof window.showWarning === 'function') {
                window.showWarning('Uyarı', 'Lütfen en az bir ürün seçin.');
            } else {
                alert('Lütfen en az bir ürün seçin.');
            }
            return;
        }

        const selectedIds = Array.from(this.selectedItems).map(id => parseInt(id));
        
        try {
            console.log('📤 Starting Excel export for items:', selectedIds);
            
            if (typeof window.showInfo === 'function') {
                window.showInfo('Excel Export', `${selectedIds.length} ürün Excel'e aktarılıyor (tüm sütunlarla)...`);
            }
            
            // Tüm sütunları al
            const columnsResponse = await fetch('/api/ExportImport/columns');
            if (!columnsResponse.ok) {
                throw new Error('Sütun bilgileri alınamadı');
            }
            
            const columnsData = await columnsResponse.json();
            if (!columnsData.success) {
                throw new Error('Sütun bilgileri geçersiz');
            }
            
            // Tüm sütunları seçili olarak işaretle
            const allColumns = [];
            columnsData.categories.forEach(category => {
                category.columns.forEach(column => {
                    allColumns.push(column.propertyName);
                });
            });
            
            console.log(`📋 Using ${allColumns.length} columns for export`);
            
            // Export filter'ı hazırla
            const exportFilter = {
                status: 'all',
                category: '',
                brand: '',
                searchTerm: '',
                selectedColumns: allColumns,
                includeHtmlDescription: true,
                includePlainTextDescription: true,
                includeMarketplaceBarcodes: true,
                includeSpecialFeatures: true,
                exportFormat: 'xlsx',
                selectedProductIds: selectedIds // Seçili ürün ID'lerini ekle
            };
            
            // Export işlemini başlat
            const response = await fetch('/api/ExportImport/export', {
                method: 'POST',
                headers: {
                    'Content-Type': 'application/json',
                    'RequestVerificationToken': document.querySelector('input[name="__RequestVerificationToken"]')?.value || ''
                },
                body: JSON.stringify(exportFilter)
            });

            if (response.ok) {
                // File download
                const blob = await response.blob();
                const url = window.URL.createObjectURL(blob);
                const a = document.createElement('a');
                a.href = url;
                a.download = `secili-urunler-${selectedIds.length}adet-${new Date().toISOString().split('T')[0]}.xlsx`;
                document.body.appendChild(a);
                a.click();
                document.body.removeChild(a);
                window.URL.revokeObjectURL(url);
                
                console.log('✅ Excel export completed successfully');
                
                if (typeof window.showSuccess === 'function') {
                    window.showSuccess('Başarılı', `${selectedIds.length} ürün Excel'e aktarıldı (${allColumns.length} sütun ile).`);
                } else {
                    alert(`${selectedIds.length} ürün Excel'e aktarıldı.`);
                }
            } else {
                const errorText = await response.text();
                throw new Error(errorText || 'Server error');
            }
        } catch (error) {
            console.error('Excel export error:', error);
            if (typeof window.showError === 'function') {
                window.showError('Hata', 'Excel export sırasında bir hata oluştu: ' + error.message);
            } else {
                alert('Excel export sırasında bir hata oluştu: ' + error.message);
            }
        }
    }

    getSelectedItems() {
        return Array.from(this.selectedItems);
    }

    getSelectedCount() {
        return this.selectedItems.size;
    }
}

// Export globally only if not already defined
if (!window.BulkOperationsManager) {
    window.BulkOperationsManager = BulkOperationsManager;
    console.log('🌍 BulkOperationsManager class exported to window');
}

// Create global instance only if not already exists
if (!window.bulkOperationsManager) {
    window.bulkOperationsManager = new BulkOperationsManager();
    console.log('✅ BulkOperationsManager instance created');
}

// Auto-initialize when DOM is ready
if (document.readyState === 'loading') {
    document.addEventListener('DOMContentLoaded', () => {
        if (window.bulkOperationsManager && !window.bulkOperationsManager.isInitialized) {
            window.bulkOperationsManager.init();
        }
    });
} else {
    // DOM already loaded
    if (window.bulkOperationsManager && !window.bulkOperationsManager.isInitialized) {
        window.bulkOperationsManager.init();
    }
}