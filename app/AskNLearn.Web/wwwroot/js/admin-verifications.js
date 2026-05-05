/**
 * Admin Verifications Management
 * Handles infinite scroll, rejection modals, and AI report viewing.
 */

const VerificationManager = (function() {
    let config = {
        currentPage: 1,
        totalPages: 1,
        onlyPending: true,
        verificationUrl: '',
        containerId: 'verification-container',
        sentinelId: 'loading-sentinel'
    };

    let isLoading = false;

    function init(options) {
        config = { ...config, ...options };
        
        const sentinel = document.getElementById(config.sentinelId);
        if (sentinel) {
            const observer = new IntersectionObserver((entries) => {
                if (entries[0].isIntersecting && !isLoading && config.currentPage < config.totalPages) {
                    loadNextPage();
                }
            }, { threshold: 0.1 });
            observer.observe(sentinel);
        }
    }

    function loadNextPage() {
        isLoading = true;
        config.currentPage++;

        const url = `${config.verificationUrl}?pageNumber=${config.currentPage}&onlyPending=${config.onlyPending}`;
        
        fetch(url, {
            headers: { 'X-Requested-With': 'XMLHttpRequest' }
        })
        .then(response => response.text())
        .then(html => {
            const container = document.getElementById(config.containerId);
            if (container) {
                container.insertAdjacentHTML('beforeend', html);
            }
            isLoading = false;
            
            if (config.currentPage >= config.totalPages) {
                const sentinel = document.getElementById(config.sentinelId);
                if (sentinel) sentinel.classList.add('d-none');
            }
        })
        .catch(err => {
            console.error('Error loading verifications:', err);
            isLoading = false;
        });
    }

    // Modal Handlers
    function openRejectModal(id) {
        const input = document.getElementById('rejectId');
        const modal = document.getElementById('rejectModal');
        if (input && modal) {
            input.value = id;
            modal.classList.remove('d-none');
            modal.classList.add('d-flex');
        }
    }

    function closeRejectModal() {
        const modal = document.getElementById('rejectModal');
        if (modal) {
            modal.classList.add('d-none');
            modal.classList.remove('d-flex');
        }
    }

    function viewAiReport(encodedContent) {
        const content = decodeURIComponent(encodedContent);
        const display = document.getElementById('aiReportContent');
        const modal = document.getElementById('aiReportModal');
        if (display && modal) {
            display.innerHTML = content;
            modal.classList.remove('d-none');
            modal.classList.add('d-flex');
        }
    }

    function closeAiReportModal() {
        const modal = document.getElementById('aiReportModal');
        if (modal) {
            modal.classList.add('d-none');
            modal.classList.remove('d-flex');
        }
    }

    // Expose public methods
    return {
        init,
        openRejectModal,
        closeRejectModal,
        viewAiReport,
        closeAiReportModal
    };
})();

// Attach to window for global access (needed for inline onclick handlers)
window.openRejectModal = VerificationManager.openRejectModal;
window.closeRejectModal = VerificationManager.closeRejectModal;
window.viewAiReport = VerificationManager.viewAiReport;
window.closeAiReportModal = VerificationManager.closeAiReportModal;
