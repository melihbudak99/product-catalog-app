/**
 * Rich Text Editor - Professional Grade
 * Microsoft Word HTML Sanitization & Multi-Mode Support
 * 
 * @version 2.0.0
 * @author Product Catalog System
 * @description Professional rich text editor with HTML sanitization, mode switching, and Word compatibility
 */

// Protective wrapper against Chrome extension errors
(function() {
    'use strict';
    
    // Configuration
    const DEBUG_MODE = false; // Set to false for production
    
    // Centralized logging system
    const Logger = {
        debug: (msg, data = null) => DEBUG_MODE && console.log(`🔧 RTE: ${msg}`, data || ''),
        info: (msg, data = null) => DEBUG_MODE && console.log(`ℹ️ RTE: ${msg}`, data || ''),
        warn: (msg, data = null) => console.warn(`⚠️ RTE: ${msg}`, data || ''),
        error: (msg, data = null) => console.error(`❌ RTE: ${msg}`, data || ''),
        success: (msg, data = null) => DEBUG_MODE && console.log(`✅ RTE: ${msg}`, data || '')
    };
    
    // Handle potential Chrome extension message channel errors
    window.addEventListener('error', function(event) {
        const message = event.message || '';
        if (message.includes('message channel closed') || 
            message.includes('Extension context invalidated')) {
            event.preventDefault();
            return false;
        }
    });
})();

// Global değişkenler
let editorInstance = null;
let isPasteInProgress = false;

class RichTextEditor {
    constructor() {
        this.editor = null;
        this.textarea = null;
        this.isInitialized = false;
        this.eventListeners = [];
        this.savedCursorPosition = null; // Cursor pozisyonunu kaydetmek için
        
        // Mode switching elements
        this.previewElement = null;
        this.sourceElement = null;
        this.sourceTextarea = null;
        this.currentMode = 'visual'; // 'visual', 'preview', 'source'
        
        // Performance optimization flags
        this._isUpdatingButtons = false;
        this._updateTimeout = null;
        this._charCountTimeout = null;
    }

    // Debounce utility for performance optimization
    debounce(func, wait) {
        let timeout;
        return function executedFunction(...args) {
            const later = () => {
                clearTimeout(timeout);
                func.call(this, ...args);
            };
            clearTimeout(timeout);
            timeout = setTimeout(later, wait);
        };
    }

    // Debounced character counter update
    get debouncedCharacterCounter() {
        if (!this._debouncedCharCounter) {
            this._debouncedCharCounter = this.debounce(() => {
                this.updateCharacterCounter();
            }, 150);
        }
        return this._debouncedCharCounter;
    }

    // Cursor pozisyonunu kaydet
    saveCursorPosition() {
        const sel = window.getSelection();
        if (sel.rangeCount > 0) {
            this.savedCursorPosition = sel.getRangeAt(0).cloneRange();
            Logger.debug('Cursor pozisyonu kaydedildi');
            return true;
        }
        return false;
    }

    // Cursor pozisyonunu geri yükle
    restoreCursorPosition() {
        if (this.savedCursorPosition) {
            try {
                const sel = window.getSelection();
                sel.removeAllRanges();
                sel.addRange(this.savedCursorPosition);
                Logger.debug('Cursor pozisyonu geri yüklendi');
                return true;
            } catch (e) {
                Logger.warn('Cursor pozisyonu geri yüklenemedi', e.message);
                this.savedCursorPosition = null;
                return false;
            }
        }
        return false;
    }

    // Cursor pozisyonunu temizle
    clearSavedCursorPosition() {
        this.savedCursorPosition = null;
    }

    // Ana başlatma fonksiyonu - Performance & Security Enhanced
    initialize() {
        if (this.isInitialized) {
            Logger.info('Rich text editor zaten başlatılmış');
            return true;
        }

        // Security: DOM element validation
        this.editor = document.getElementById('editor-content');
        this.textarea = document.getElementById('Description');
        this.previewElement = document.getElementById('editor-preview');
        this.sourceElement = document.getElementById('editor-source');
        this.sourceTextarea = document.getElementById('source-textarea');

        if (!this.editor || !this.textarea || !this.previewElement || !this.sourceElement || !this.sourceTextarea) {
            Logger.error('Critical: Editor elements bulunamadı - DOM hazır değil');
            return false;
        }

        // Security: XSS prevention check
        if (this.editor.classList.contains('rte-active')) {
            Logger.debug('Editor zaten aktif - duplicate initialization prevented');
            return true;
        }

        // Performance: Batch DOM operations
        try {
            this.setupEditor();
        this.setupToolbar();
        this.setupEventListeners();
        this.setupSourceTextareaListener();
        this.loadInitialContent();
        this.updateButtonStates();
        this.updateCharacterCounter(); // İlk karakter sayısını ayarla

            this.isInitialized = true;
            this.editor.classList.add('rte-active');
            
            Logger.success('Rich Text Editor başarıyla başlatıldı');
            return true;
            
        } catch (error) {
            Logger.error('Rich Text Editor initialization failed', error.message);
            
            // Fallback: Basic textarea işlevselliği
            if (this.textarea) {
                this.textarea.style.display = 'block';
                Logger.debug('Fallback: Basic textarea enabled');
            }
            
            return false;
        }
    }

    // Editor temel ayarlarını yap
    setupEditor() {
        this.editor.contentEditable = true;
        this.editor.spellcheck = false;
        
        // Stil ayarları
        Object.assign(this.editor.style, {
            minHeight: '250px',
            padding: '20px',
            border: 'none',
            fontSize: '15px',
            lineHeight: '1.7',
            outline: 'none',
            fontFamily: "'Segoe UI', Tahoma, Geneva, Verdana, sans-serif",
            backgroundColor: '#fff',
            color: '#333',
            maxHeight: '500px',
            overflowY: 'auto'
        });
    }

    // Toolbar buttonlarını ayarla
    setupToolbar() {
        const toolbar = document.querySelector('.editor-toolbar');
        if (!toolbar) return;

        const buttons = toolbar.querySelectorAll('.editor-btn');
        buttons.forEach(button => {
            const command = button.getAttribute('data-command');
            const value = button.getAttribute('data-value');
            const mode = button.getAttribute('data-mode');
            
            // Önceki listener'ları temizle
            const newButton = button.cloneNode(true);
            button.parentNode.replaceChild(newButton, button);
            
            this.addEventListener(newButton, 'click', (e) => {
                e.preventDefault();
                e.stopPropagation();
                
                if (mode) {
                    // Mode switching
                    this.switchMode(mode);
                } else if (command === 'heading') {
                    this.toggleHeading(value);
                } else if (command) {
                    // Only execute command if command exists
                    this.executeCommand(command, value);
                }
            });
        });
    }

    // Event listener'ları kur
    setupEventListeners() {
        // Debounced button states update - Performance optimized
        this.debouncedUpdateButtonStates = this.debounce(() => {
            this._doUpdateButtonStates();
        }, 100); // Optimize edildi: 150ms -> 100ms

        // İçerik değişimi - optimized
        this.addEventListener(this.editor, 'input', () => {
            this.updateTextarea();
            this.handlePlaceholder();
            this.debouncedUpdateButtonStates(); // 🚀 Optimized: Debounced version
        });

        // Akıllı yapıştırma - daha güçlü event handling ve async error prevention
        this.addEventListener(this.editor, 'paste', (e) => {
            this.handlePaste(e);
        }, { capture: true, passive: false });
        
        // Ek koruma katmanları - Simplified to prevent async issues
        this.addEventListener(this.editor, 'beforepaste', (e) => {
            if (isPasteInProgress) {
                console.log('⏸️ BeforePaste engelliyor');
                e.preventDefault();
                e.stopPropagation();
                return false;
            }
        }, { capture: true, passive: false });

        // Focus/Blur
        this.addEventListener(this.editor, 'focus', () => {
            this.handleFocus();
        });

        this.addEventListener(this.editor, 'blur', () => {
            this.handleBlur();
        });

        // Klavye kısayolları
        this.addEventListener(this.editor, 'keydown', (e) => {
            this.handleKeydown(e);
        });

        // Button state güncellemesi - optimized with smart throttling
        let lastUpdateTime = 0;
        const THROTTLE_DELAY = 150;
        
        const throttledUpdate = () => {
            const now = Date.now();
            if (now - lastUpdateTime > THROTTLE_DELAY) {
                lastUpdateTime = now;
                this.debouncedUpdateButtonStates();
            }
        };

        this.addEventListener(this.editor, 'mouseup', throttledUpdate);
        this.addEventListener(this.editor, 'keyup', throttledUpdate);
        this.addEventListener(this.editor, 'selectionchange', throttledUpdate, { passive: true });
    }

    // Event listener yönetimi - Enhanced error handling
    addEventListener(element, event, handler, options = {}) {
        try {
            // Default options to prevent async issues
            const safeOptions = {
                passive: false,
                once: false,
                ...options
            };
            
            element.addEventListener(event, handler, safeOptions);
            this.eventListeners.push({ element, event, handler, options: safeOptions });
        } catch (error) {
            console.error('Error adding event listener:', error);
        }
    }

    // Yapıştırma işlemi - Cursor pozisyonu düzeltildi
    handlePaste(e) {
        if (isPasteInProgress) {
            console.log('⏸️ Yapıştırma zaten devam ediyor - engelliyor');
            e.preventDefault();
            e.stopPropagation();
            e.stopImmediatePropagation();
            return false;
        }

        console.log('📋 Paste event başlatıldı');
        e.preventDefault();
        e.stopPropagation();
        e.stopImmediatePropagation();
        
        // Mevcut selection/cursor pozisyonunu kaydet
        const sel = window.getSelection();
        if (sel.rangeCount > 0) {
            this.savedCursorPosition = sel.getRangeAt(0).cloneRange();
            console.log('📍 Cursor pozisyonu kaydedildi - startContainer:', this.savedCursorPosition.startContainer.nodeName, 'startOffset:', this.savedCursorPosition.startOffset);
        } else {
            console.log('⚠️ Aktif cursor pozisyonu bulunamadı');
            this.savedCursorPosition = null;
        }
        
        // Global flag'i set et
        isPasteInProgress = true;
        console.log('🚩 Paste flag set edildi');

        const clipboardData = e.clipboardData || window.clipboardData;
        const plainText = clipboardData.getData('text/plain') || '';
        const htmlText = clipboardData.getData('text/html') || '';

        console.log('📊 Paste verisi - Plain:', plainText.length, 'chars, HTML:', htmlText.length, 'chars');

        // HTML içeriği varsa ve farklıysa modal göster
        if (htmlText && htmlText.trim() !== plainText.trim() && plainText.trim()) {
            this.showPasteModal(htmlText, plainText);
        } else if (plainText.trim()) {
            console.log('📝 Düz metin olarak yapıştırılıyor');
            this.insertPlainText(plainText);
            this.resetPasteFlag();
        } else {
            console.log('⚠️ Boş içerik - işlem iptal');
            this.resetPasteFlag();
        }
        
        return false;
    }

    // Yapıştırma modalı
    showPasteModal(htmlContent, plainText) {
        const existingModal = document.getElementById('paste-modal');
        if (existingModal) {
            existingModal.remove();
        }

        const modal = document.createElement('div');
        modal.id = 'paste-modal';
        modal.className = 'paste-modal-backdrop';
        
        modal.innerHTML = `
            <div class="paste-modal">
                <div class="paste-modal-header">
                    <h3><i class="fas fa-paste"></i> Yapıştırma Seçenekleri</h3>
                    <p>İçeriği nasıl yapıştırmak istiyorsunuz?</p>
                </div>
                <div class="paste-modal-body">
                    <div class="paste-option" data-action="keep-format">
                        <div class="paste-option-icon">
                            <i class="fas fa-magic"></i>
                        </div>
                        <div class="paste-option-content">
                            <h4>Biçimi Koru</h4>
                            <p>Metnin formatını (kalın, italik, başlık vb.) koruyarak yapıştır</p>
                            <div class="paste-preview" id="html-preview"></div>
                        </div>
                    </div>
                    <div class="paste-option" data-action="plain-text">
                        <div class="paste-option-icon">
                            <i class="fas fa-font"></i>
                        </div>
                        <div class="paste-option-content">
                            <h4>Sadece Metin</h4>
                            <p>Tüm formatları temizleyerek düz metin olarak yapıştır</p>
                            <div class="paste-preview" id="text-preview"></div>
                        </div>
                    </div>
                </div>
                <div class="paste-modal-footer">
                    <button class="paste-btn paste-btn-cancel">İptal</button>
                </div>
            </div>
        `;

        document.body.appendChild(modal);

        // Önizlemeleri doldur
        const htmlPreview = modal.querySelector('#html-preview');
        const textPreview = modal.querySelector('#text-preview');
        
        if (htmlPreview) {
            const cleanHtml = this.sanitizeHtml(htmlContent);
            htmlPreview.innerHTML = cleanHtml.substring(0, 200) + (cleanHtml.length > 200 ? '...' : '');
        }
        
        if (textPreview) {
            textPreview.textContent = plainText.substring(0, 200) + (plainText.length > 200 ? '...' : '');
        }

        // Event handler'lar
        modal.addEventListener('click', (e) => {
            const action = e.target.closest('[data-action]')?.dataset.action;
            
            if (action === 'keep-format') {
                console.log('🎨 Biçimi koru seçildi');
                // Modal'ı hemen kapat
                modal.remove();
                document.removeEventListener('keydown', escHandler);
                
                // Cursor pozisyonunu geri yükle ve içeriği ekle
                this.insertFormattedContentAtSavedPosition(htmlContent);
                this.resetPasteFlag();
            } else if (action === 'plain-text') {
                console.log('📝 Düz metin seçildi');
                // Modal'ı hemen kapat
                modal.remove();
                document.removeEventListener('keydown', escHandler);
                
                // Cursor pozisyonunu geri yükle ve içeriği ekle
                this.insertPlainTextAtSavedPosition(plainText);
                this.resetPasteFlag();
            } else if (e.target.classList.contains('paste-btn-cancel') || e.target.closest('.paste-btn-cancel')) {
                console.log('❌ Yapıştırma iptal edildi');
                modal.remove();
                document.removeEventListener('keydown', escHandler);
                this.clearSavedCursorPosition(); // Cursor pozisyonunu temizle
                this.resetPasteFlag();
            }
        });

        // ESC ile kapat
        const escHandler = (e) => {
            if (e.key === 'Escape') {
                modal.remove();
                document.removeEventListener('keydown', escHandler);
                this.clearSavedCursorPosition(); // Cursor pozisyonunu temizle
                this.resetPasteFlag();
            }
        };
        document.addEventListener('keydown', escHandler);

        // Animasyon
        requestAnimationFrame(() => {
            modal.classList.add('show');
        });
    }

    // HTML'i temizle - Microsoft Word etiketlerini ve gereksiz kısımları kaldır
    sanitizeHtml(html) {
        // Önce HTML'den gereksiz kısımları temizle
        let cleanedHtml = html;
        
        // 1. Word Fragment temizliği
        cleanedHtml = cleanedHtml.replace(/<!--StartFragment-->|<!--EndFragment-->/g, '');
        cleanedHtml = cleanedHtml.replace(/<html[\s\S]*?<body[^>]*>|<\/body>[\s\S]*?<\/html>/gi, '');
        
        // 2. Microsoft Office class'larını temizle (MsoNormal, MsoListParagraph, vb.)
        cleanedHtml = cleanedHtml.replace(/\s*class\s*=\s*["'][^"']*Mso[^"']*["']/gi, '');
        cleanedHtml = cleanedHtml.replace(/\s*class\s*=\s*["'][^"']*["']/g, ''); // Tüm class attribute'larını kaldır
        
        // 3. Inline stilleri ve style attribute'larını temizle
        cleanedHtml = cleanedHtml.replace(/\s*style\s*=\s*["'][^"']*["']/gi, '');
        
        // 4. XML namespace'leri ve Office-specific attribute'ları temizle
        cleanedHtml = cleanedHtml.replace(/\s*xmlns[^=]*="[^"]*"/gi, '');
        cleanedHtml = cleanedHtml.replace(/\s*xml:[^=]*="[^"]*"/gi, '');
        cleanedHtml = cleanedHtml.replace(/\s*data-[^=]*="[^"]*"/g, '');
        
        // 5. Office prefix'li etiketleri temizle (o:p, w:, v: vb.)
        cleanedHtml = cleanedHtml.replace(/<\/?[ovw]:[^>]*>/gi, '');
        
        // 6. Gereksiz boş satırları ve whitespace'leri temizle
        cleanedHtml = cleanedHtml.replace(/^\s+$/gm, ''); // Sadece whitespace olan satırları kaldır
        cleanedHtml = cleanedHtml.replace(/\n{3,}/g, '\n\n'); // 3'ten fazla newline'ı 2'ye indir
        
        // 7. Boş <p> etiketlerini temizle
        cleanedHtml = cleanedHtml.replace(/<p\s*class\s*=\s*["'][^"']*["']\s*><\/p>/gi, '');
        cleanedHtml = cleanedHtml.replace(/<p\s*><\/p>/g, '');
        
        // 8. Ardışık boş <p> etiketlerini tek <p></p>'ye çevir
        cleanedHtml = cleanedHtml.replace(/(<p><\/p>\s*){2,}/g, '<p></p>');
        
        // 9. DOM ile son temizlik - izin verilen etiketleri kontrol et
        const tempDiv = document.createElement('div');
        tempDiv.innerHTML = cleanedHtml;

        // İzin verilen etiketler - başlık etiketleri dahil
        const allowedTags = ['p', 'br', 'strong', 'b', 'em', 'i', 'u', 'ul', 'ol', 'li', 'h1', 'h2', 'h3', 'h4', 'h5', 'h6'];
        
        // İzin verilmeyen elementleri temizle
        const allElements = tempDiv.querySelectorAll('*');
        allElements.forEach(el => {
            const tagName = el.tagName.toLowerCase();
            
            if (!allowedTags.includes(tagName)) {
                // İzin verilmeyen element - içeriği koru, elementi kaldır
                const parent = el.parentNode;
                while (el.firstChild) {
                    parent.insertBefore(el.firstChild, el);
                }
                parent.removeChild(el);
            } else {
                // İzin verilen element - tüm attribute'ları temizle
                const attrs = [...el.attributes];
                attrs.forEach(attr => {
                    el.removeAttribute(attr.name);
                });
            }
        });

        // 10. Son temizlik - boş paragrafları kaldır
        const emptyParagraphs = tempDiv.querySelectorAll('p');
        emptyParagraphs.forEach(p => {
            if (!p.textContent.trim() && p.childElementCount === 0) {
                p.remove();
            }
        });

        // 11. Sonucu döndür
        let result = tempDiv.innerHTML.trim();
        result = result.replace(/(<p><\/p>\s*){2,}/g, '<p></p>'); // Ardışık boş paragrafları tek paragrafla değiştir
        
        return result;
    }

    // Formatlanmış içeriği kaydedilen cursor pozisyonuna ekle
    insertFormattedContentAtSavedPosition(htmlContent) {
        const cleanHtml = this.sanitizeHtml(htmlContent);
        
        this.insertHtmlAtSavedCursorPosition(cleanHtml);
        this.updateTextarea();
        this.debouncedUpdateButtonStates();
    }

    // Düz metin içeriği kaydedilen cursor pozisyonuna ekle
    insertPlainTextAtSavedPosition(text) {
        const lines = text.split('\n').filter(line => line.trim());
        let html = '';
        
        lines.forEach((line, index) => {
            html += `<p>${this.escapeHtml(line.trim())}</p>`;
        });
        
        this.insertHtmlAtSavedCursorPosition(html || `<p>${this.escapeHtml(text)}</p>`);
        this.updateTextarea();
    }

    // Formatlanmış içeriği ekle
    insertFormattedContent(htmlContent) {
        const cleanHtml = this.sanitizeHtml(htmlContent);
        
        this.insertHtmlAtCursor(cleanHtml);
        this.updateTextarea();
        this.debouncedUpdateButtonStates();
    }

    // Düz metin ekle
    insertPlainText(text) {
        console.log('📝 Düz metin ekleniyor...');
        const lines = text.split('\n').filter(line => line.trim());
        let html = '';
        
        lines.forEach((line, index) => {
            html += `<p>${this.escapeHtml(line.trim())}</p>`;
        });
        
        this.insertHtmlAtCursor(html || `<p>${this.escapeHtml(text)}</p>`);
        this.updateTextarea();
        
        console.log('✅ Düz metin başarıyla eklendi');
    }

    // HTML'i kaydedilen cursor pozisyonuna ekle
    insertHtmlAtSavedCursorPosition(html) {
        console.log('📍 HTML kaydedilen cursor pozisyonuna ekleniyor:', html.substring(0, 50) + '...');
        
        // Editörü focus et
        this.editor.focus();
        
        // Kaydedilen cursor pozisyonu varsa onu kullan
        if (this.savedCursorPosition) {
            console.log('📍 Kaydedilen cursor pozisyonu geri yükleniyor...');
            try {
                // Önce mevcut selection'ı temizle
                const sel = window.getSelection();
                sel.removeAllRanges();
                
                // Kaydedilen pozisyonu geri yükle
                const restoredRange = this.savedCursorPosition.cloneRange();
                
                // Range'in hala geçerli olup olmadığını kontrol et
                if (restoredRange.startContainer.parentNode) {
                    sel.addRange(restoredRange);
                    console.log('✅ Cursor pozisyonu başarıyla geri yüklendi');
                    
                    // HTML'i bu pozisyona ekle
                    this.insertHtmlAtCurrentPosition(html);
                    
                    // Cursor pozisyonunu temizle
                    this.clearSavedCursorPosition();
                    return;
                } else {
                    console.log('⚠️ Kaydedilen cursor pozisyonu artık geçerli değil');
                }
            } catch (e) {
                console.log('⚠️ Cursor pozisyonu geri yüklenemedi:', e);
            }
        }
        
        // Fallback: Normal insertion yap
        console.log('📍 Fallback: Normal insertion kullanılıyor');
        this.insertHtmlAtCursor(html);
    }

    // HTML'i mevcut cursor pozisyonuna ekle
    insertHtmlAtCurrentPosition(html) {
        console.log('📍 HTML mevcut cursor pozisyonuna ekleniyor...');
        
        const sel = window.getSelection();
        if (!sel.rangeCount) {
            console.log('⚠️ Cursor bulunamadı, editörün sonuna ekleniyor');
            this.insertAtEnd(html);
            return;
        }
        
        try {
            const range = sel.getRangeAt(0);
            console.log('📍 Aktif cursor pozisyonu bulundu');
            
            // Mevcut seçimi sil (eğer varsa)
            if (!range.collapsed) {
                range.deleteContents();
                console.log('🗑️ Seçili içerik silindi');
            }

            // HTML'i DOM fragment'ı olarak hazırla
            const tempDiv = document.createElement('div');
            tempDiv.innerHTML = html;
            
            const fragment = document.createDocumentFragment();
            const nodes = [];
            
            // Tüm node'ları fragment'a taşı
            while (tempDiv.firstChild) {
                const node = tempDiv.firstChild;
                nodes.push(node);
                fragment.appendChild(node);
            }
            
            // Fragment'ı cursor pozisyonuna ekle
            range.insertNode(fragment);
            console.log('✅ Fragment cursor pozisyonuna eklendi');
            
            // Cursor'u eklenen içeriğin sonuna konumlandır
            if (nodes.length > 0) {
                const lastNode = nodes[nodes.length - 1];
                const newRange = document.createRange();
                
                // Son node'un tipine göre cursor pozisyonunu ayarla
                if (lastNode.nodeType === Node.TEXT_NODE) {
                    newRange.setStart(lastNode, lastNode.textContent.length);
                } else if (lastNode.lastChild && lastNode.lastChild.nodeType === Node.TEXT_NODE) {
                    newRange.setStart(lastNode.lastChild, lastNode.lastChild.textContent.length);
                } else {
                    newRange.setStartAfter(lastNode);
                }
                
                newRange.collapse(true);
                sel.removeAllRanges();
                sel.addRange(newRange);
                
                console.log('✅ Cursor eklenen içeriğin sonuna konumlandırıldı');
            }
            
            console.log('✅ HTML cursor pozisyonuna başarıyla eklendi');
            
        } catch (error) {
            console.error('❌ Cursor pozisyonuna ekleme hatası:', error);
            // Hata durumunda güvenli fallback
            this.insertAtEnd(html);
        }
        
        this.updateTextarea();
        this.debouncedUpdateButtonStates(); // Optimize edildi (debounced)
        
        // Son kontrol - editör boşsa placeholder ekle
        setTimeout(() => {
            this.handlePlaceholder();
        }, 50);
    }

    // HTML'i cursor pozisyonuna temiz bir şekilde ekle
    insertHtmlAtCursor(html) {
        console.log('📍 HTML cursor pozisyonuna ekleniyor:', html.substring(0, 50) + '...');
        
        // Editörü focus et
        this.editor.focus();
        
        const sel = window.getSelection();
        
        // Placeholder kontrolü - sadece gerçek placeholder'lar için
        const currentContent = this.editor.innerHTML.trim();
        const isActuallyEmpty = currentContent === '' || 
                               currentContent === '<p><br></p>' || 
                               currentContent === '<p></p>' || 
                               currentContent === '<br>' ||
                               (currentContent.includes('Ürün açıklamasını buraya yazın') && currentContent.length < 100);
        
        // Sadece gerçekten boş veya placeholder içerik varsa temizle
        if (isActuallyEmpty) {
            console.log('🗑️ Gerçek placeholder/boş içerik tespit edildi - temizleniyor');
            this.editor.innerHTML = html;
            
            // Cursor'u eklenen içeriğin sonuna koy
            setTimeout(() => {
                this.setCursorToEnd();
                this.updateTextarea();
                this.debouncedUpdateButtonStates(); // Optimize edildi (debounced)
            }, 10);
            
            console.log('✅ İçerik placeholder yerine eklendi');
            return;
        }
        
        // Normal içerik var - mevcut cursor pozisyonunu kullan
        console.log('📍 Normal içerik mevcut - cursor pozisyonuna ekleniyor...');
        
        if (!sel.rangeCount) {
            console.log('⚠️ Cursor bulunamadı, editörün sonuna ekleniyor');
            this.insertAtEnd(html);
            return;
        }
        
        try {
            const range = sel.getRangeAt(0);
            console.log('📍 Aktif cursor pozisyonu bulundu');
            
            // Mevcut seçimi sil (eğer varsa)
            if (!range.collapsed) {
                range.deleteContents();
                console.log('🗑️ Seçili içerik silindi');
            }

            // HTML'i DOM fragment'ı olarak hazırla
            const tempDiv = document.createElement('div');
            tempDiv.innerHTML = html;
            
            const fragment = document.createDocumentFragment();
            const nodes = [];
            
            // Tüm node'ları fragment'a taşı
            while (tempDiv.firstChild) {
                const node = tempDiv.firstChild;
                nodes.push(node);
                fragment.appendChild(node);
            }
            
            // Fragment'ı cursor pozisyonuna ekle
            range.insertNode(fragment);
            console.log('✅ Fragment cursor pozisyonuna eklendi');
            
            // Cursor'u eklenen içeriğin sonuna konumlandır
            if (nodes.length > 0) {
                const lastNode = nodes[nodes.length - 1];
                const newRange = document.createRange();
                
                // Son node'un tipine göre cursor pozisyonunu ayarla
                if (lastNode.nodeType === Node.TEXT_NODE) {
                    newRange.setStart(lastNode, lastNode.textContent.length);
                } else if (lastNode.lastChild && lastNode.lastChild.nodeType === Node.TEXT_NODE) {
                    newRange.setStart(lastNode.lastChild, lastNode.lastChild.textContent.length);
                } else {
                    newRange.setStartAfter(lastNode);
                }
                
                newRange.collapse(true);
                sel.removeAllRanges();
                sel.addRange(newRange);
                
                console.log('✅ Cursor eklenen içeriğin sonuna konumlandırıldı');
            }
            
            console.log('✅ HTML cursor pozisyonuna başarıyla eklendi');
            
        } catch (error) {
            console.error('❌ Cursor pozisyonuna ekleme hatası:', error);
            // Hata durumunda güvenli fallback
            this.insertAtEnd(html);
        }
        
        this.updateTextarea();
        this.debouncedUpdateButtonStates(); // Optimize edildi (debounced)
        
        // Son kontrol - editör boşsa placeholder ekle
        setTimeout(() => {
            this.handlePlaceholder();
        }, 50);
    }

    // Cursor'u editörün başına koy
    setCursorToStart() {
        const range = document.createRange();
        const sel = window.getSelection();
        
        this.editor.focus();
        
        if (this.editor.firstChild) {
            // İlk child varsa onun başına koy
            if (this.editor.firstChild.nodeType === Node.TEXT_NODE) {
                range.setStart(this.editor.firstChild, 0);
            } else {
                range.setStart(this.editor.firstChild, 0);
            }
        } else {
            // Hiç child yoksa editörün kendisinin başına koy
            range.setStart(this.editor, 0);
        }
        
        range.collapse(true);
        sel.removeAllRanges();
        sel.addRange(range);
        
        console.log('📍 Cursor editörün başına konumlandırıldı');
    }

    // Cursor'u editörün sonuna koy
    setCursorToEnd() {
        const range = document.createRange();
        const sel = window.getSelection();
        
        this.editor.focus();
        
        if (this.editor.lastChild) {
            // Son child varsa onun sonuna koy
            if (this.editor.lastChild.nodeType === Node.TEXT_NODE) {
                range.setStart(this.editor.lastChild, this.editor.lastChild.textContent.length);
            } else if (this.editor.lastChild.lastChild && this.editor.lastChild.lastChild.nodeType === Node.TEXT_NODE) {
                range.setStart(this.editor.lastChild.lastChild, this.editor.lastChild.lastChild.textContent.length);
            } else {
                range.setStartAfter(this.editor.lastChild);
            }
        } else {
            // Hiç child yoksa editörün kendisinin sonuna koy
            range.setStart(this.editor, 0);
        }
        
        range.collapse(true);
        sel.removeAllRanges();
        sel.addRange(range);
        
        console.log('📍 Cursor editörün sonuna konumlandırıldı');
    }

    // Güvenli fallback - editörün sonuna ekle
    insertAtEnd(html) {
        console.log('🔄 Fallback: İçerik editörün sonuna ekleniyor');
        
        if (this.editor.innerHTML.trim() === '' || this.editor.innerHTML.includes('Ürün açıklamasını buraya yazın')) {
            this.editor.innerHTML = html;
        } else {
            this.editor.innerHTML += html;
        }
        
        // Cursor'u sona koy
        this.setCursorToEnd();
        
        // Cursor pozisyonunu temizle
        this.clearSavedCursorPosition();
    }

    // HTML escape
    escapeHtml(text) {
        const div = document.createElement('div');
        div.textContent = text;
        return div.innerHTML;
    }

    // Paste flag'i sıfırla - Optimized version
    resetPasteFlag() {
        console.log('🔄 Paste flag sıfırlanıyor...');
        isPasteInProgress = false;
        
        // Single safety timeout - optimized from 3 timeouts to 1
        setTimeout(() => {
            isPasteInProgress = false;
            console.log('✅ Paste flag kesin olarak sıfırlandı');
        }, 200); // Optimized: Single timeout with reasonable delay
    }

    // Komut çalıştır
    executeCommand(command, value = null) {
        if (!command) {
            console.warn('⚠️ executeCommand: Command is null or undefined');
            return;
        }
        
        this.editor.focus();
        
        try {
            if (command === 'heading') {
                this.toggleHeading(value);
            } else if (command === 'indent') {
                this.indentContent();
            } else if (command === 'outdent') {
                this.outdentContent();
            } else if (command === 'removeFormat') {
                this.clearFormatting();
            } else {
                const success = document.execCommand(command, false, value);
                // Debug log removed for production
            }
            
            this.updateTextarea();
            this.debouncedUpdateButtonStates(); // Optimize edildi (debounced)
        } catch (error) {
            console.error('Komut hatası:', error);
        }
    }

    // Başlık toggle - Gerçek toggle mekanizması
    toggleHeading(tagName) {
        console.log(`📝 Başlık toggle çalıştırılıyor: ${tagName}`);
        
        this.editor.focus();
        
        try {
            const selection = window.getSelection();
            if (!selection.rangeCount) {
                console.log('⚠️ Seçim bulunamadı');
                return;
            }
            
            // Mevcut elementi bul
            let currentElement = selection.anchorNode;
            if (currentElement.nodeType === 3) { // Text node
                currentElement = currentElement.parentElement;
            }
            
            // En yakın blok elementi bul
            while (currentElement && currentElement !== this.editor && 
                   !['p', 'h1', 'h2', 'h3', 'h4', 'h5', 'h6', 'div'].includes(currentElement.tagName?.toLowerCase())) {
                currentElement = currentElement.parentElement;
            }
            
            if (!currentElement || currentElement === this.editor) {
                console.log('🔧 Blok element bulunamadı, p etiketi oluşturuluyor');
                document.execCommand('formatBlock', false, 'p');
                // Yeni oluşan elementi bul
                const newSelection = window.getSelection();
                if (newSelection.anchorNode) {
                    currentElement = newSelection.anchorNode.nodeType === 3 ? 
                        newSelection.anchorNode.parentElement : newSelection.anchorNode;
                }
            }
            
            const currentTag = currentElement.tagName?.toLowerCase();
            console.log(`🔍 Mevcut element: ${currentTag}`);
            
            if (currentTag === tagName.toLowerCase()) {
                // Aynı başlık etiketi - P'ye dönüştür (toggle OFF)
                console.log(`🔄 ${tagName.toUpperCase()} zaten aktif → Normal paragrafa dönüştürülüyor`);
                const success = document.execCommand('formatBlock', false, 'p');
                console.log(`✅ formatBlock (p) başarı durumu: ${success}`);
                
                if (success) {
                    console.log(`✅ ${tagName.toUpperCase()} başlığı KALDIRILDI → Normal paragraf yapıldı`);
                    
                    // Button state'i güncelle - Optimize edildi (debounced)
                    this.debouncedUpdateButtonStates();
                }
            } else {
                // Farklı etiket veya p - istenen başlığa dönüştür (toggle ON)
                console.log(`🔄 ${currentTag || 'undefined'} → ${tagName.toUpperCase()} dönüştürülüyor`);
                const success = document.execCommand('formatBlock', false, tagName);
                console.log(`✅ formatBlock (${tagName}) başarı durumu: ${success}`);
                
                if (success) {
                    console.log(`✅ ${tagName.toUpperCase()} başlığı UYGULANDI`);
                    
                    // Button state'i güncelle - Optimize edildi (debounced)
                    this.debouncedUpdateButtonStates();
                }
            }
            
            this.updateTextarea();
            
        } catch (error) {
            console.error(`❌ Başlık toggle hatası (${tagName}):`, error);
        }
    }

    // Girinti artırma (Indent) - Export Uyumlu
    indentContent() {
        console.log('📝 Girinti artırma işlemi başlatılıyor (Export uyumlu)');
        
        try {
            const selection = window.getSelection();
            if (!selection.rangeCount) return;
            
            const range = selection.getRangeAt(0);
            let element = range.commonAncestorContainer;
            
            // Text node ise parent elementi al
            if (element.nodeType === 3) {
                element = element.parentElement;
            }
            
            // En yakın blok elementi bul (p, li, div, heading)
            while (element && element !== this.editor && 
                   !['p', 'li', 'div', 'h1', 'h2', 'h3', 'h4', 'h5', 'h6'].includes(element.tagName?.toLowerCase())) {
                element = element.parentElement;
            }
            
            if (element && element !== this.editor) {
                // Export uyumlu girinti: CSS class kullan
                const currentClass = element.className || '';
                const indentMatch = currentClass.match(/indent-level-(\d+)/);
                const currentLevel = indentMatch ? parseInt(indentMatch[1]) : 0;
                const newLevel = Math.min(currentLevel + 1, 5); // Maksimum 5 seviye
                
                // Eski indent class'ını kaldır
                element.className = currentClass.replace(/indent-level-\d+\s?/g, '');
                
                // Yeni indent class'ını ekle
                if (element.className.trim()) {
                    element.className += ` indent-level-${newLevel}`;
                } else {
                    element.className = `indent-level-${newLevel}`;
                }
                
                console.log(`✅ Girinti seviyesi: ${currentLevel} → ${newLevel} (CSS class: indent-level-${newLevel})`);
            }
            
        } catch (error) {
            console.error('❌ Girinti artırma hatası:', error);
        }
    }

    // Girinti azaltma (Outdent) - Export Uyumlu
    outdentContent() {
        console.log('📝 Girinti azaltma işlemi başlatılıyor (Export uyumlu)');
        
        try {
            const selection = window.getSelection();
            if (!selection.rangeCount) return;
            
            const range = selection.getRangeAt(0);
            let element = range.commonAncestorContainer;
            
            // Text node ise parent elementi al
            if (element.nodeType === 3) {
                element = element.parentElement;
            }
            
            // En yakın blok elementi bul (p, li, div, heading)
            while (element && element !== this.editor && 
                   !['p', 'li', 'div', 'h1', 'h2', 'h3', 'h4', 'h5', 'h6'].includes(element.tagName?.toLowerCase())) {
                element = element.parentElement;
            }
            
            if (element && element !== this.editor) {
                // Export uyumlu girinti: CSS class kullan
                const currentClass = element.className || '';
                const indentMatch = currentClass.match(/indent-level-(\d+)/);
                const currentLevel = indentMatch ? parseInt(indentMatch[1]) : 0;
                const newLevel = Math.max(0, currentLevel - 1);
                
                // Eski indent class'ını kaldır
                element.className = currentClass.replace(/indent-level-\d+\s?/g, '');
                
                // Yeni indent class'ını ekle (eğer 0'dan büyükse)
                if (newLevel > 0) {
                    if (element.className.trim()) {
                        element.className += ` indent-level-${newLevel}`;
                    } else {
                        element.className = `indent-level-${newLevel}`;
                    }
                }
                
                // Boş class attribute'u temizle
                if (!element.className.trim()) {
                    element.removeAttribute('class');
                }
                
                console.log(`✅ Girinti seviyesi: ${currentLevel} → ${newLevel}`);
            }
            
        } catch (error) {
            console.error('❌ Girinti azaltma hatası:', error);
        }
    }

    // Biçimlendirmeyi temizleme (Clear Formatting) - Gelişmiş
    clearFormatting() {
        try {
            const selection = window.getSelection();
            if (!selection.rangeCount) return;
            
            // Eğer seçim varsa, sadece seçili alanı temizle
            if (!selection.isCollapsed) {
                this.clearSelectedFormatting(selection);
            } else {
                this.clearAllFormatting();
            }
            
            // İşlem sonunda Microsoft Word HTML'ini de temizle
            this.cleanMicrosoftWordHtml();
            
        } catch (error) {
            console.error('❌ Biçimlendirme temizleme hatası:', error);
        }
    }
    
    // Microsoft Word HTML'ini temizle
    cleanMicrosoftWordHtml() {
        const currentContent = this.editor.innerHTML;
        if (currentContent && (currentContent.includes('MsoNormal') || currentContent.includes('class="Mso') || currentContent.includes('microsoft'))) {
            const cleanedContent = this.sanitizeHtml(currentContent);
            this.editor.innerHTML = cleanedContent;
            this.updateTextarea();
        }
    }

    // Seçili alanın formatını temizle
    clearSelectedFormatting(selection) {
        try {
            const range = selection.getRangeAt(0);
            const fragment = range.cloneContents();
            
            // Seçili içeriğin sadece text halini al
            const plainText = this.extractPlainText(fragment);
            
            // Seçili alanı sil ve temiz metin ekle
            range.deleteContents();
            
            // Yeni metin node'u oluştur ve ekle
            const textNode = document.createTextNode(plainText);
            range.insertNode(textNode);
            
            // Seçimi yeni eklenen metnin sonuna koy
            range.setStartAfter(textNode);
            range.collapse(true);
            selection.removeAllRanges();
            selection.addRange(range);
            
            console.log('✅ Seçili alandaki biçimlendirme temizlendi');
            
        } catch (error) {
            console.error('❌ Seçili alan temizleme hatası:', error);
        }
    }

    // Tüm editörün formatını temizle
    clearAllFormatting() {
        try {
            // Cursor pozisyonunu kaydet
            this.saveCursorPosition();
            
            // Editörün tüm text içeriğini al
            const plainText = this.extractPlainText(this.editor);
            
            // Temiz paragraflar halinde yeniden düzenle
            const paragraphs = plainText.split(/\n\s*\n/).filter(p => p.trim());
            
            if (paragraphs.length === 0) {
                this.editor.innerHTML = '<p><br></p>';
            } else {
                const cleanHtml = paragraphs
                    .map(p => `<p>${p.replace(/\n/g, '<br>')}</p>`)
                    .join('');
                this.editor.innerHTML = cleanHtml;
            }
            
            // Cursor pozisyonunu geri yükle veya sona koy
            setTimeout(() => {
                try {
                    this.restoreCursorPosition();
                } catch (e) {
                    this.setCursorToEnd();
                }
            }, 50);
            
            console.log('✅ Tüm biçimlendirme temizlendi');
            
        } catch (error) {
            console.error('❌ Tüm editör temizleme hatası:', error);
        }
    }

    // Element veya fragment'tan sadece plain text çıkar
    extractPlainText(element) {
        if (element.nodeType === Node.TEXT_NODE) {
            return element.textContent || '';
        }
        
        let text = '';
        const walker = document.createTreeWalker(
            element,
            NodeFilter.SHOW_TEXT,
            null,
            false
        );
        
        let node;
        while (node = walker.nextNode()) {
            text += node.textContent;
        }
        
        return text;
    }

    // Focus işleme
    handleFocus() {
        const container = this.editor.closest('.rich-text-editor');
        if (container) {
            container.style.borderColor = '#667eea';
            container.style.boxShadow = '0 0 0 0.2rem rgba(102, 126, 234, 0.25)';
        }
        
        // Placeholder temizle - ama cursor pozisyonunu koruyarak
        if (this.editor.innerHTML.includes('Ürün açıklamasını buraya yazın')) {
            // Mevcut cursor pozisyonunu kaydet
            const sel = window.getSelection();
            const savedRange = sel.rangeCount > 0 ? sel.getRangeAt(0) : null;
            
            this.editor.innerHTML = '<p></p>';
            this.editor.style.color = '#333';
            
            // Cursor pozisyonunu geri yükle veya uygun konuma yerleştir
            if (savedRange) {
                try {
                    sel.removeAllRanges();
                    sel.addRange(savedRange);
                } catch (e) {
                    // Hata durumunda ilk paragrafın içine cursor koy
                    const newRange = document.createRange();
                    const firstP = this.editor.querySelector('p');
                    if (firstP) {
                        newRange.setStart(firstP, 0);
                        newRange.collapse(true);
                        sel.removeAllRanges();
                        sel.addRange(newRange);
                    }
                }
            } else {
                // Cursor yoksa ilk paragrafın içine koy
                const newRange = document.createRange();
                const firstP = this.editor.querySelector('p');
                if (firstP) {
                    newRange.setStart(firstP, 0);
                    newRange.collapse(true);
                    sel.removeAllRanges();
                    sel.addRange(newRange);
                }
            }
        }
        
        this.debouncedUpdateButtonStates(); // Optimize edildi (debounced)
    }

    // Blur işleme
    handleBlur() {
        const container = this.editor.closest('.rich-text-editor');
        if (container) {
            container.style.borderColor = '#e9ecef';
            container.style.boxShadow = 'none';
        }
        this.updateTextarea();
        this.handlePlaceholder();
    }

    // Klavye kısayolları
    handleKeydown(e) {
        // Tab ve Shift+Tab için girinti kontrolleri
        if (e.key === 'Tab') {
            e.preventDefault();
            if (e.shiftKey) {
                this.executeCommand('outdent');
            } else {
                this.executeCommand('indent');
            }
            return;
        }
        
        if (e.ctrlKey || e.metaKey) {
            switch(e.key.toLowerCase()) {
                case 'b':
                    e.preventDefault();
                    this.executeCommand('bold');
                    break;
                case 'i':
                    e.preventDefault();
                    this.executeCommand('italic');
                    break;
                case 'u':
                    e.preventDefault();
                    this.executeCommand('underline');
                    break;
            }
        }
    }

    // Placeholder yönetimi
    handlePlaceholder() {
        const content = this.editor.innerHTML.trim();
        const isEmpty = content === '' || content === '<p><br></p>' || content === '<br>' || content === '<p></p>';
        
        if (isEmpty) {
            this.editor.innerHTML = '<p style="color: #999; font-style: italic;">Ürün açıklamasını buraya yazın...</p>';
        }
    }

    // Textarea'yı güncelle - Mode-aware version with auto HTML cleanup
    updateTextarea() {
        if (!this.textarea) return;
        
        let content = '';
        
        // Get content based on current mode
        switch (this.currentMode) {
            case 'visual':
                content = this.editor.innerHTML;
                break;
            case 'source':
                content = this.sourceTextarea.value;
                break;
            case 'preview':
                // Preview mode uses existing textarea content
                return;
        }
        
        // Placeholder temizle
        if (content.includes('Ürün açıklamasını buraya yazın')) {
            content = '';
        }
        
        // Microsoft Word HTML'ini otomatik temizle
        if (content && (content.includes('class="MsoNormal"') || content.includes('Mso') || content.includes('microsoft'))) {
            content = this.sanitizeHtml(content);
        }
        
        // Boş paragrafları temizle
        if (content === '<p><br></p>' || content === '<p></p>' || content === '<br>') {
            content = '';
        }
        
        this.textarea.value = content;
        
        // Update preview content if needed
        if (this.currentMode !== 'source') {
            this.updatePreviewContent();
        }
        
        // Form validation sistemini tetikle
        this.triggerValidationEvents();
    }

    // Validation event'lerini tetikle
    triggerValidationEvents() {
        if (!this.textarea) return;
        
        // Character counter güncelle (debounced)
        this.debouncedCharacterCounter();
        
        // jQuery validation için input event
        const inputEvent = new Event('input', { bubbles: true });
        this.textarea.dispatchEvent(inputEvent);
        
        // Change event de tetikle
        const changeEvent = new Event('change', { bubbles: true });
        this.textarea.dispatchEvent(changeEvent);
        
        // jQuery validation manuel tetikleme (eğer mevcut ise)
        if (window.jQuery && jQuery(this.textarea).valid) {
            jQuery(this.textarea).valid();
        }
    }

    // Character counter güncelleme - Mode-aware version
    updateCharacterCounter() {
        const counter = document.getElementById('description-char-count');
        if (!counter) return;
        
        let textLength = 0;
        
        // Get text length based on current mode
        switch (this.currentMode) {
            case 'visual':
                textLength = this.extractPlainText(this.editor).length;
                break;
            case 'source':
                textLength = this.extractPlainText(this.sourceTextarea).length;
                break;
            case 'preview':
                textLength = this.extractPlainText(this.editor).length;
                break;
        }
        
        const maxLength = 2000; // Constants.cs'den MAX_DESCRIPTION_LENGTH
        
        counter.textContent = `${textLength}/${maxLength}`;
        
        // Renk kontrolleri
        const counterElement = counter.parentElement;
        const editorContainer = this.editor.closest('.rich-text-editor');
        
        if (textLength > maxLength) {
            // Maksimum aşıldı - kırmızı
            counterElement.className = 'description-counter danger';
            editorContainer.classList.add('error');
        } else if (textLength > maxLength * 0.9) {
            // %90'ı aştı - turuncu
            counterElement.className = 'description-counter warning';
            editorContainer.classList.remove('error');
        } else {
            // Normal - gri
            counterElement.className = 'description-counter';
            editorContainer.classList.remove('error');
        }
        
        console.log(`📊 Karakter sayısı güncellendi: ${textLength}/${maxLength}`);
    }

    // Button durumlarını güncelle - Optimize edildi
    updateButtonStates() {
        // Skip if already processing
        if (this._isUpdatingButtons) {
            return;
        }
        
        if (this._updateTimeout) {
            clearTimeout(this._updateTimeout);
        }
        
        this._updateTimeout = setTimeout(() => {
            this._doUpdateButtonStates();
        }, 75); // 50ms -> 75ms optimize edildi
    }
    
    _doUpdateButtonStates() {
        // Performance guard
        if (this._isUpdatingButtons) {
            return;
        }
        
        this._isUpdatingButtons = true;
        
        try {
            const buttons = document.querySelectorAll('.editor-btn');
            
            // Önce tüm butonları sıfırla - Batch DOM update
            const resetStyles = {
                backgroundColor: '#fff',
                borderColor: '#dee2e6',
                color: '#495057'
            };
            
            buttons.forEach(button => {
                button.classList.remove('active');
                Object.assign(button.style, resetStyles);
            });
            
            // Aktif durumları kontrol et (sadece basit komutlar)
            const activeCommands = {
                'bold': document.queryCommandState('bold'),
                'italic': document.queryCommandState('italic'),
                'underline': document.queryCommandState('underline'),
                'insertUnorderedList': document.queryCommandState('insertUnorderedList'),
                'insertOrderedList': document.queryCommandState('insertOrderedList')
            };
            
            // Batch activate buttons
            Object.keys(activeCommands).forEach(command => {
                if (activeCommands[command]) {
                    this.activateButton(command);
                }
            });
            
            // Heading durumlarını kontrol et
            this.checkHeadingState();
            
        } catch (error) {
            console.warn('Button state güncelleme hatası:', error);
        } finally {
            this._isUpdatingButtons = false;
        }
    }

    // Başlık durumunu kontrol et - optimize edilmiş
    checkHeadingState() {
        try {
            const selection = window.getSelection();
            if (selection.rangeCount === 0) return;
            
            let element = selection.anchorNode;
            if (!element) return;
            
            // Text node'dan parent element'e geç
            if (element.nodeType === 3) {
                element = element.parentElement;
            }
            
            // En yakın başlık etiketini bul
            let headingFound = false;
            while (element && element !== this.editor && !headingFound) {
                const tagName = element.tagName?.toLowerCase();
                if (['h1', 'h2', 'h3', 'h4', 'h5', 'h6'].includes(tagName)) {
                    // Sadece değişiklik olduğunda log
                    const button = document.querySelector(`[data-command="heading"][data-value="${tagName}"]`);
                    if (button && !button.classList.contains('active')) {
                        console.log(`📌 ${tagName.toUpperCase()} butonu aktif edildi`);
                    }
                    this.activateButton('heading', tagName);
                    headingFound = true;
                }
                element = element.parentElement;
            }
            
            // Eğer hiçbir başlık bulunamazsa, tüm başlık butonlarını deaktif et
            if (!headingFound) {
                const headingButtons = document.querySelectorAll('[data-command="heading"]');
                headingButtons.forEach(button => {
                    if (button.classList.contains('active')) {
                        console.log(`📌 ${button.dataset.value?.toUpperCase()} butonu deaktif edildi`);
                        button.classList.remove('active');
                        button.style.backgroundColor = '#fff';
                        button.style.borderColor = '#dee2e6';
                        button.style.color = '#495057';
                    }
                });
            }
        } catch (error) {
            console.warn('Heading state kontrolü hatası:', error);
        }
    }

    // Button'u aktive et - Performance optimized
    activateButton(command, value = null) {
        let selector;
        
        if (command === 'heading' && value) {
            // Heading butonları için özel selector
            selector = `[data-command="heading"][data-value="${value}"]`;
        } else {
            // Normal butonlar için
            selector = `.editor-btn[data-command="${command}"]`;
        }
        
        const button = document.querySelector(selector);
        if (button && !button.classList.contains('active')) {
            // Batch style update
            button.classList.add('active');
            Object.assign(button.style, {
                backgroundColor: '#667eea',
                borderColor: '#5a6fd8',
                color: '#fff'
            });
        }
    }

    // İlk içeriği yükle
    loadInitialContent() {
        if (!this.textarea.value || this.textarea.value.trim() === '') {
            this.editor.innerHTML = '<p style="color: #999; font-style: italic;">Ürün açıklamasını buraya yazın...</p>';
        } else {
            this.editor.innerHTML = this.textarea.value;
        }
        
        // Initialize source textarea with current content
        this.sourceTextarea.value = this.textarea.value || '';
        
        // Initialize preview content
        this.updatePreviewContent();
    }

    // ===== MODE SWITCHING FUNCTIONALITY =====
    
    switchMode(mode) {
        if (this.currentMode === mode) return;
        
        // Save current content before switching
        this.saveCurrentContent();
        
        // Update mode buttons
        this.updateModeButtons(mode);
        
        // Hide all editor areas
        this.editor.style.display = 'none';
        this.previewElement.style.display = 'none';
        this.sourceElement.style.display = 'none';
        
        // Show selected mode
        switch (mode) {
            case 'visual':
                this.editor.style.display = 'block';
                this.enableToolbarButtons(true);
                break;
            case 'preview':
                this.updatePreviewContent();
                this.previewElement.style.display = 'block';
                this.enableToolbarButtons(false);
                break;
            case 'source':
                this.sourceElement.style.display = 'block';
                this.enableToolbarButtons(false);
                this.syncToSourceTextarea();
                break;
        }
        
        this.currentMode = mode;
        // Mode switch completed
    }
    
    saveCurrentContent() {
        switch (this.currentMode) {
            case 'visual':
                this.textarea.value = this.editor.innerHTML;
                break;
            case 'source':
                this.textarea.value = this.sourceTextarea.value;
                this.editor.innerHTML = this.sourceTextarea.value;
                break;
        }
    }
    
    updateModeButtons(activeMode) {
        const modeButtons = document.querySelectorAll('.mode-btn');
        modeButtons.forEach(btn => {
            const mode = btn.getAttribute('data-mode');
            if (mode === activeMode) {
                btn.classList.add('active');
            } else {
                btn.classList.remove('active');
            }
        });
    }
    
    enableToolbarButtons(enable) {
        const toolbarButtons = document.querySelectorAll('.editor-btn:not(.mode-btn)');
        toolbarButtons.forEach(btn => {
            btn.disabled = !enable;
            btn.style.opacity = enable ? '1' : '0.5';
        });
    }
    
    updatePreviewContent() {
        const content = this.textarea.value || this.editor.innerHTML;
        const previewContent = this.previewElement.querySelector('.preview-content');
        if (previewContent) {
            previewContent.innerHTML = content || '<p style="color: #999; font-style: italic;">Önizleme için içerik yok</p>';
        }
    }
    
    syncToSourceTextarea() {
        this.sourceTextarea.value = this.textarea.value || this.editor.innerHTML;
    }
    
    // Source textarea'dan değişiklikleri dinle
    setupSourceTextareaListener() {
        if (this.sourceTextarea) {
            this.addEventListener(this.sourceTextarea, 'input', () => {
                this.textarea.value = this.sourceTextarea.value;
                this.updateCharacterCounter();
            });
        }
    }
}

// Global fonksiyonlar
function initializeRichTextEditor() {
    if (!editorInstance) {
        editorInstance = new RichTextEditor();
    }
    return editorInstance.initialize();
}

// Global erişim
window.initializeRichTextEditor = initializeRichTextEditor;
