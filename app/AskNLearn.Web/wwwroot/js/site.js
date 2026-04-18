// Global State for Lazy Loading Comments
window.commentsLoaded = {};
window.commentsOpen = {};

// --- Lazy Comments Engine ---
async function toggleLazyComments(postId, commId) {
    const container = document.getElementById(`comments-${postId}`);
    if (!container) return;

    const isCurrentlyHidden = container.classList.contains('d-none');
    
    if (!isCurrentlyHidden) {
        container.classList.add('d-none');
        window.commentsOpen[postId] = false;
        return;
    }

    container.classList.remove('d-none');
    window.commentsOpen[postId] = true;

    if (!window.commentsLoaded[postId]) {
        window.commentsLoaded[postId] = true;
        try {
            const response = await fetch(`/hubs/communities/v1/comments/retrieve?postId=${postId}&communityId=${commId}`, {
                headers: { 'X-Requested-With': 'XMLHttpRequest' }
            });
            if (!response.ok) throw new Error('Error loading comments');
            const html = await response.text();
            container.innerHTML = html;
        } catch (err) {
            console.error('Error loading comments:', err);
            container.innerHTML = '<p class="text-danger p-3 mb-0">Failed to load comments.</p>';
            window.commentsLoaded[postId] = false;
        }
    }
}

// --- Reply Form Toggle ---
function toggleReplyForm(commentId) {
    const el = document.getElementById(`reply-form-${commentId}`);
    if (el) {
        el.classList.toggle('d-none');
        if (!el.classList.contains('d-none')) {
            const input = el.querySelector('input[type="text"]');
            if (input) input.focus();
        }
    }
}

// --- File Name Display ---
function updateFileName(input) {
    const fileName = input.files[0] ? input.files[0].name : '';
    const container = input.closest('form');
    const nameSpan = container ? container.querySelector('.file-name') : null;
    if (nameSpan) nameSpan.textContent = fileName;
}

// --- Comment Submission (AJAX) ---
async function submitCommentAsync(e, form) {
    e.preventDefault();
    const formData = new FormData(form);
    const postId = formData.get('PostId');
    const container = document.getElementById(`comments-${postId}`);
    
    // Show loading state
    const btn = form.querySelector('button[type="submit"]');
    const originalContent = btn.innerHTML;
    btn.disabled = true;
    btn.innerHTML = '<span class="spinner-border spinner-border-sm"></span>';

    try {
        const response = await fetch(form.action, {
            method: 'POST',
            body: formData,
            headers: { 
                'X-Requested-With': 'XMLHttpRequest',
                'Accept': 'text/html'
            }
        });

        if (response.ok) {
            const html = await response.text();
            
            // Check if it's a full page (redirect fallback)
            if (html.includes('<!DOCTYPE html>')) {
                console.warn("Full page received. Reloading...");
                window.location.reload();
                return;
            }

            if (container) {
                container.innerHTML = html;
                form.reset();
                const fileNameSpan = form.querySelector('.file-name');
                if (fileNameSpan) fileNameSpan.textContent = '';
            }
        } else {
            const errorText = await response.text();
            if (window.Notify) window.Notify.error("Failed to post comment. " + (errorText || "Please check your status."));
        }
    } catch (err) {
        console.error("Comment submission error:", err);
        if (window.Notify) window.Notify.error("An unexpected error occurred.");
    } finally {
        btn.disabled = false;
        btn.innerHTML = originalContent;
    }
}

// --- Global Voting Engine ---
async function votePost(postId, communityId, value) {
    const token = document.querySelector('input[name="__RequestVerificationToken"]')?.value ?? '';
    
    // Select elements to update (compatible with both Home and Forum views)
    const countEls = document.querySelectorAll(`.vote-count-${postId}`);
    const upIcons = document.querySelectorAll(`.vote-icon-up-${postId}, .vote-btn-up-${postId}`);
    const downIcons = document.querySelectorAll(`.vote-icon-down-${postId}, .vote-btn-down-${postId}`);

    try {
        const response = await fetch(`/hubs/communities/v1/interactions/vote?postId=${postId}&communityId=${communityId}&value=${value}`, {
            method: 'POST',
            headers: {
                'Accept': 'application/json',
                'X-Requested-With': 'XMLHttpRequest',
                'RequestVerificationToken': token
            }
        });
        
        const data = await response.json();
        
        if (data.success) {
            // Update counts
            countEls.forEach(el => {
                el.textContent = data.voteCount;
                el.classList.remove('text-mint', 'text-danger', 'text-white');
                el.classList.add(data.userVote === 1 ? 'text-mint' : data.userVote === -1 ? 'text-danger' : 'text-white');
            });

            // Update icons/buttons
            upIcons.forEach(el => {
                const icon = el.tagName === 'SPAN' ? el : el.querySelector('.material-symbols-outlined');
                if (icon) icon.style.color = data.userVote === 1 ? 'var(--color-mint-bright)' : '';
                el.classList.toggle('text-mint', data.userVote === 1);
                el.classList.toggle('font-weight-bold', data.userVote === 1);
                el.classList.toggle('text-muted', data.userVote !== 1);
            });

            downIcons.forEach(el => {
                const icon = el.tagName === 'SPAN' ? el : el.querySelector('.material-symbols-outlined');
                if (icon) icon.style.color = data.userVote === -1 ? '#ef4444' : '';
                el.classList.toggle('text-danger', data.userVote === -1);
                el.classList.toggle('font-weight-bold', data.userVote === -1);
                el.classList.toggle('text-muted', data.userVote !== -1);
            });
        }
    } catch (err) {
        console.error("Voting error:", err);
    }
}

async function deleteCommentAsync(commentId, postId, communityId) {
    if (!confirm('Sigur vrei să ștergi acest comentariu?')) return;

    try {
        const token = document.querySelector('input[name="__RequestVerificationToken"]')?.value ?? '';
        const response = await fetch(`/hubs/communities/v1/comments/delete?id=${commentId}&communityId=${communityId}`, {
            method: 'POST',
            headers: {
                'X-Requested-With': 'XMLHttpRequest',
                'RequestVerificationToken': token
            }
        });

        if (response.ok) {
            // Re-fetch comments for this post
            const container = document.getElementById(`comments-${postId}`);
            if (container) {
                const refreshResp = await fetch(`/hubs/communities/v1/comments/retrieve?postId=${postId}&communityId=${communityId}`);
                if (refreshResp.ok) {
                    container.innerHTML = await refreshResp.text();
                }
            }
        }
    } catch (err) {
        console.error('deleteCommentAsync error:', err);
    }
}
