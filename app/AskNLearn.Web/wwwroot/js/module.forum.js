/**
 * ForumManager - Handles forum interactions
 */
class ForumManager {
    static init() {
        // Any general forum initialization
    }

    static toggleInlinePost(show) {
        const form = document.getElementById('inline-post-form');
        const container = document.getElementById('quick-post-container');
        if (show) {
            if (form) {
                form.classList.remove('d-none');
                form.classList.add('animate-slide-down');
            }
            if (container) container.classList.add('d-none');
            if (form) {
                setTimeout(() => {
                    form.scrollIntoView({ behavior: 'smooth', block: 'center' });
                    form.querySelector('input[name="Title"]')?.focus();
                }, 100);
            }
        } else {
            if (form) {
                form.style.opacity = '0';
                form.style.transform = 'translateY(-10px)';
                setTimeout(() => {
                    form.classList.add('d-none');
                    form.style.opacity = '';
                    form.style.transform = '';
                    if (container) container.classList.remove('d-none');
                }, 300);
            }
        }
    }

    static async vote(postId, communityId, value) {
        try {
            const resp = await axios.post(`/hubs/communities/v1/interactions/vote?postId=${postId}&communityId=${communityId}&value=${value}`);
            const data = resp.data;
            
            if (data.success) {
                const scoreEl = document.querySelector(`.vote-count-${postId}`);
                if (scoreEl) {
                    scoreEl.innerText = data.voteCount;
                    
                    // Update colors based on vote
                    scoreEl.classList.remove('text-accent', 'text-danger', 'text-white');
                    if (data.userVote === 1) scoreEl.classList.add('text-accent');
                    else if (data.userVote === -1) scoreEl.classList.add('text-danger');
                    else scoreEl.classList.add('text-white');
                }
                
                // Toggle active states on buttons
                const upIcon = document.querySelector(`.vote-icon-up-${postId}`);
                const downIcon = document.querySelector(`.vote-icon-down-${postId}`);
                
                upIcon?.parentElement.classList.toggle('text-accent', data.userVote === 1);
                upIcon?.parentElement.classList.toggle('text-muted', data.userVote !== 1);
                
                downIcon?.parentElement.classList.toggle('text-danger', data.userVote === -1);
                downIcon?.parentElement.classList.toggle('text-muted', data.userVote !== -1);
            }
        } catch (e) { 
            // Error handled by global axios interceptor
        }
    }
}

// Global bridge for legacy onclick handlers in views
window.votePost = (postId, communityId, value) => ForumManager.vote(postId, communityId, value);

// Expose to window
window.ForumManager = ForumManager;
export default ForumManager;
