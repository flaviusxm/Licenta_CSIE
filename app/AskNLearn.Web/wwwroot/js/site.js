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

window.toggleLazyComments = async function(postId, communityId) {
    const container = document.getElementById(`comments-${postId}`);
    if (!container) return;

    const isHidden = container.classList.contains('d-none');
    container.classList.toggle('d-none', !isHidden);
    
    if (isHidden && !window.commentsLoaded[postId]) {
        try {
            console.log(`[Comments] Loading for post: ${postId}, community: ${communityId}`);
            // Using absolute path with params object for reliable binding
            const resp = await axios.get('/hubs/communities/v1/comments/retrieve', {
                params: { postId, communityId }
            });
            container.innerHTML = resp.data;
            window.commentsLoaded[postId] = true;
        } catch (e) {
            let errorDetail = e.message;
            if (e.response) {
                errorDetail = `Status: ${e.response.status}. Data: ${JSON.stringify(e.response.data)}`;
            }
            container.innerHTML = `<div class="alert alert-danger p-2 m-3 small border-0 bg-danger bg-opacity-10 text-danger rounded-3">
                <div class="fw-bold mb-1">Failed to load comments</div>
                <div style="font-size: 0.75rem; word-break: break-all; opacity: 0.8;">${errorDetail}</div>
            </div>`;
            console.error('[Comments Load Error]', e);
        }
    }
};

window.submitCommentAsync = async function(event, form) {
    event.preventDefault();
    const formData = new FormData(form);
    const postId = formData.get('PostId');
    
    try {
        // Match the route in ForumController: [HttpPost("v1/discussions/comments/add")]
        const resp = await axios.post('/hubs/communities/v1/discussions/comments/add', formData);
        
        const container = document.getElementById(`comments-${postId}`);
        if (container) {
            container.innerHTML = resp.data;
            window.commentsLoaded[postId] = true;
            if (container.classList.contains('d-none')) {
                container.classList.remove('d-none');
            }
        }
        window.Notify?.success("Comment added successfully");
    } catch (e) {
        window.Notify?.error("Failed to post comment");
        console.error('[Comment Submit Error]', e);
    }
};

window.deleteCommentAsync = async function(commentId, postId, communityId) {
    if (!confirm('Are you sure you want to delete this comment?')) return;
    
    try {
        // Match the route in ForumController: [HttpPost("v1/comments/delete")]
        // Note: Controller expects 'id' and 'communityId'
        await axios.post(`/hubs/communities/v1/comments/delete?id=${commentId}&communityId=${communityId}`);
        
        // Force reload comments for this post
        window.commentsLoaded[postId] = false;
        await window.toggleLazyComments(postId, communityId);
        window.Notify?.success("Comment deleted");
    } catch (e) {
        window.Notify?.error("Failed to delete comment");
        console.error('[Comment Delete Error]', e);
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

// --- Axios Global Configuration ---
if (window.axios) {
    // 1. Request Interceptor: Automatically add Anti-Forgery Token
    axios.interceptors.request.use(config => {
        const token = window.getAntiForgeryToken();
        if (token && ['post', 'put', 'delete', 'patch'].includes(config.method?.toLowerCase())) {
            config.headers['RequestVerificationToken'] = token;
        }
        return config;
    }, error => Promise.reject(error));

    // 2. Response Interceptor: Unified Error Handling
    axios.interceptors.response.use(
        response => response,
        error => {
            const message = error.response?.data?.message || error.message || "A network error occurred.";
            // window.Notify.error(message); // Removed as per user request for custom handling
            console.error('[Axios Error]', error);
            return Promise.reject(error);
        }
    );
}
