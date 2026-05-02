/**
 * ForumManager - Handles forum interactions
 */
class ForumManager {
    static init(communityId, hasMore) {
        this.communityId = communityId;
        this.hasMore = hasMore;
        this.currentPage = 1;
        this.isLoading = false;
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

    static initInfiniteScroll(communityId, initialHasMore) {
        this.communityId = communityId;
        this.hasMore = initialHasMore;
        this.currentPage = 1;
        this.isLoading = false;

        const sentinel = document.getElementById('posts-sentinel');
        if (!sentinel) return;

        const observer = new IntersectionObserver(async (entries) => {
            if (entries[0].isIntersecting && this.hasMore && !this.isLoading) {
                await this.loadMorePosts();
            }
        }, { threshold: 0.1 });

        observer.observe(sentinel);
    }

    static async loadMorePosts() {
        if (this.isLoading || !this.hasMore) return;

        this.isLoading = true;
        const spinner = document.getElementById('posts-loading-spinner');
        if (spinner) spinner.classList.remove('d-none');

        try {
            this.currentPage++;
            const response = await axios.get(`/hubs/communities/v1/discussions/batch?communityId=${this.communityId}&page=${this.currentPage}`);
            
            if (response.data.trim()) {
                const container = document.getElementById('posts-container');
                container.insertAdjacentHTML('beforeend', response.data);
                
                // The partial view _PostListPartial might contain its own hasMore indicator logic
                // But for simplicity, we check if we got content.
                // In a real app, you'd return JSON with metadata.
            } else {
                this.hasMore = false;
            }
        } catch (error) {
            console.error('Error loading more posts:', error);
            this.currentPage--; // Reset page on error
        } finally {
            this.isLoading = false;
            if (spinner) spinner.classList.add('d-none');
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
                const upBtn = upIcon?.parentElement;
                const downBtn = downIcon?.parentElement;
                
                if (upBtn) {
                    upBtn.classList.toggle('text-accent', data.userVote === 1);
                    upBtn.classList.toggle('font-weight-bold', data.userVote === 1);
                    upBtn.classList.toggle('text-muted', data.userVote !== 1);
                    upBtn.classList.toggle('hover-glow', data.userVote !== 1);
                }
                
                if (downBtn) {
                    downBtn.classList.toggle('text-danger', data.userVote === -1);
                    downBtn.classList.toggle('font-weight-bold', data.userVote === -1);
                    downBtn.classList.toggle('text-muted', data.userVote !== -1);
                    downBtn.classList.toggle('hover-text-danger', data.userVote !== -1);
                }
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
