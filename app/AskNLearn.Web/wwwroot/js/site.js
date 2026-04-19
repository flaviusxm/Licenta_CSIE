/**
 * site.js
 * Core platform utilities, notification engine, and real-time connectivity.
 */

// --- Global Security Helper ---
window.getAntiForgeryToken = function() {
    const input = document.querySelector('input[name="__RequestVerificationToken"]');
    return input ? input.value : '';
};

// --- Global UI Helpers ---
window.updateFileName = function(input) {
    const fileName = input.files[0] ? input.files[0].name : '';
    // Support multiple label/span structures
    const container = input.closest('form') || input.closest('label')?.parentElement;
    const nameSpan = container ? container.querySelector('.file-name') : null;
    if (nameSpan) nameSpan.textContent = fileName;
};

window.toggleReplyForm = function(commentId) {
    const el = document.getElementById(`reply-form-${commentId}`);
    if (el) {
        el.classList.toggle('d-none');
        if (!el.classList.contains('d-none')) {
            const input = el.querySelector('input[type="text"]');
            if (input) input.focus();
        }
    }
};

// --- Shared State ---
window.commentsLoaded = {};
window.commentsOpen = {};

// --- Notification Engine ---
window.Notify = {
    container: document.getElementById('toast-container'),
    
    show: function(message, type = 'info', icon = 'info', duration = 5000) {
        if (!this.container) {
            // Fallback if toast container isn't rendered yet
            this.container = document.getElementById('toast-container');
            if (!this.container) return;
        }
        
        const toast = document.createElement('div');
        toast.className = 'glass-card p-3 shadow-lg d-flex align-items-center gap-3 animate-entrance system-toast';
        
        let color = 'var(--text-muted)';
        let defaultIcon = icon;
        
        switch(type) {
            case 'success': 
                color = 'var(--color-mint-bright)'; 
                defaultIcon = icon || 'check_circle';
                break;
            case 'error': 
                color = '#ff4b5c'; 
                defaultIcon = icon || 'error';
                break;
            case 'warning': 
                color = '#ffb300'; 
                defaultIcon = icon || 'warning';
                break;
            case 'system': 
                color = 'var(--color-teal)'; 
                defaultIcon = icon || 'guardian';
                break;
        }

        toast.style.cssText = `
            width: 340px;
            pointer-events: auto;
            background: rgba(10, 24, 18, 0.95);
            border-left: 4px solid ${color};
            backdrop-filter: blur(25px);
            margin-bottom: 12px;
            box-shadow: 0 10px 40px -10px rgba(0,0,0,0.5);
        `;
        
        toast.innerHTML = `
            <div class="rounded-circle d-flex align-items-center justify-content-center flex-shrink-0" style="width: 40px; height: 40px; background: rgba(255,255,255,0.05);">
                <span class="material-symbols-outlined" style="color: ${color}; font-size: 22px;">${defaultIcon}</span>
            </div>
            <div class="flex-grow-1">
                <p class="text-white fw-bold mb-0" style="font-size: 0.85rem; line-height: 1.3;">${message}</p>
            </div>
            <button onclick="this.closest('.system-toast').remove()" class="btn btn-link p-1 text-muted hover-text-white text-decoration-none shadow-none">
                <span class="material-symbols-outlined fs-6">close</span>
            </button>
        `;

        this.container.appendChild(toast);
        
        if (duration > 0) {
            setTimeout(() => {
                toast.style.opacity = '0';
                toast.style.transform = 'translateY(20px) scale(0.95)';
                setTimeout(() => toast.remove(), 600);
            }, duration);
        }
        return toast;
    },
    success: (msg) => window.Notify.show(msg, 'success'),
    error: (msg) => window.Notify.show(msg, 'error'),
    warning: (msg) => window.Notify.show(msg, 'warning'),
    system: (msg) => window.Notify.show(msg, 'system')
};
