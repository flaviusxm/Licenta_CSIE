/**
 * LeaderboardManager - Handles filtering, sorting and pagination
 */
class LeaderboardManager {
    static debounceTimer = null;

    static init() {
        const searchInput = document.querySelector('input[name="searchTerm"]');
        const institutionInput = document.querySelector('input[name="institution"]');
        const sortBySelect = document.querySelector('select[name="sortBy"]');

        if (searchInput) {
            searchInput.addEventListener('input', () => {
                clearTimeout(this.debounceTimer);
                this.debounceTimer = setTimeout(() => this.loadLeaderboard(1), 300);
            });
        }

        if (institutionInput) {
            institutionInput.addEventListener('input', () => {
                clearTimeout(this.debounceTimer);
                this.debounceTimer = setTimeout(() => this.loadLeaderboard(1), 300);
            });
        }

        if (sortBySelect) {
            sortBySelect.addEventListener('change', () => this.loadLeaderboard(1));
        }

        window.addEventListener('popstate', () => {
            location.reload();
        });
    }

    static async loadLeaderboard(page = 1, pageSize = null) {
        const searchTerm = document.querySelector('input[name="searchTerm"]')?.value || '';
        const institution = document.querySelector('input[name="institution"]')?.value || '';
        const sortBy = document.querySelector('select[name="sortBy"]')?.value || 'PointsDesc';
        
        if (!pageSize) {
            const pageSizeBottom = document.querySelector('select.form-select-glass[onchange*="loadLeaderboard"]');
            pageSize = pageSizeBottom ? pageSizeBottom.value : 12;
        }

        const url = `/Leaderboard?page=${page}&pageSize=${pageSize}&searchTerm=${encodeURIComponent(searchTerm)}&institution=${encodeURIComponent(institution)}&sortBy=${sortBy}`;
        
        const container = document.getElementById('leaderboard-data-container');
        if (container) {
            container.style.opacity = '0.6';
            container.style.filter = 'blur(2px)';
            container.style.pointerEvents = 'none';
        }

        try {
            const resp = await axios.get(url, {
                headers: { 'X-Requested-With': 'XMLHttpRequest' }
            });
            
            if (container) {
                container.innerHTML = resp.data;
                container.style.opacity = '1';
                container.style.filter = 'none';
                container.style.pointerEvents = 'auto';
                
                if (page > 1) {
                    window.scrollTo({ top: container.offsetTop - 100, behavior: 'smooth' });
                }
            }
            window.history.pushState({ path: url }, '', url);
        } catch (err) {
            if (container) {
                container.style.opacity = '1';
                container.style.filter = 'none';
                container.style.pointerEvents = 'auto';
            }
        }
    }
}

// Expose to window
window.LeaderboardManager = LeaderboardManager;
export default LeaderboardManager;
