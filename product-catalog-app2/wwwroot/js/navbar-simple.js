// Sade ve Temiz Navbar JavaScript - Optimized Version
(function() {
    'use strict';

    // Single global mobile menu toggle with throttling
    let isToggling = false;
    window.toggleMobileMenu = function() {
        if (isToggling) return; // Prevent rapid clicks
        isToggling = true;
        
        const navMenu = document.querySelector('.nav-menu');
        if (navMenu) {
            navMenu.classList.toggle('show');
        }
        
        // Reset throttle after animation
        setTimeout(() => {
            isToggling = false;
        }, 300);
    };

    // Sayfa yüklendiğinde
    document.addEventListener('DOMContentLoaded', function() {
        const navbar = document.querySelector('.navbar');
        if (!navbar) return;

        let lastScrollY = window.scrollY;
        let isScrolling = false;

        // Optimized scroll event with throttling
        let scrollTimeout;
        function handleScroll() {
            // Throttle scroll events for better performance
            if (scrollTimeout) return;
            
            scrollTimeout = setTimeout(() => {
                const currentScrollY = window.scrollY;
                
                // Batch DOM updates for better performance
                requestAnimationFrame(() => {
                    // Scroll durumuna göre navbar stilini değiştir
                    if (currentScrollY > 50) {
                        navbar.classList.add('scrolled');
                    } else {
                        navbar.classList.remove('scrolled');
                    }
                    
                    // Navbar gizleme/gösterme
                    if (currentScrollY > lastScrollY && currentScrollY > 100) {
                        navbar.classList.add('hidden');
                    } else {
                        navbar.classList.remove('hidden');
                    }
                    
                    lastScrollY = currentScrollY;
                });
                
                scrollTimeout = null;
            }, 16); // ~60fps throttling
        }

        // Scroll dinleyicisi
        window.addEventListener('scroll', handleScroll, { passive: true });

        // Mobil menü dış tıklama
        document.addEventListener('click', function(e) {
            const navMenu = document.querySelector('.nav-menu');
            const mobileBtn = document.querySelector('.mobile-menu-btn');
            
            if (navMenu && navMenu.classList.contains('show')) {
                if (!navMenu.contains(e.target) && !mobileBtn.contains(e.target)) {
                    navMenu.classList.remove('show');
                }
            }
        });

        // Keyboard navigation
        document.addEventListener('keydown', function(e) {
            if (e.key === 'Escape') {
                const navMenu = document.querySelector('.nav-menu');
                if (navMenu && navMenu.classList.contains('show')) {
                    navMenu.classList.remove('show');
                }
            }
        });
    });

})();
