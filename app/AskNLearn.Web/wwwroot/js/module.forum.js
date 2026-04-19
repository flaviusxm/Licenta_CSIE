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

    static async vote(postId, isUpvote) {
        try {
            const resp = await fetch(`/Forum/VotePost?postId=${postId}&isUpvote=${isUpvote}`, {
                method: 'POST',
                headers: { 'RequestVerificationToken': window.getAntiForgeryToken() }
            });
            if (resp.ok) {
                const data = await resp.json();
                const scoreEl = document.getElementById(`post-score-${postId}`);
                if (scoreEl) scoreEl.innerText = data.newScore;
                
                // Toggle active classes on buttons
                const upBtn = document.querySelector(`button[onclick*="vote('${postId}', true)"]`);
                const downBtn = document.querySelector(`button[onclick*="vote('${postId}', false)"]`);
                
                if (isUpvote) {
                    upBtn?.classList.toggle('text-accent');
                    downBtn?.classList.remove('text-danger');
                } else {
                    downBtn?.classList.toggle('text-danger');
                    upBtn?.classList.remove('text-accent');
                }
            }
        } catch (e) { console.error(e); }
    }
}

// Expose to window
window.ForumManager = ForumManager;
export default ForumManager;
