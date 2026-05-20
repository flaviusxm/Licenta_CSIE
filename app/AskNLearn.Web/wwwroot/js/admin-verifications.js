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

        // Live Update Polling (Only for page 1 and pending queue)
        if (config.onlyPending && config.currentPage === 1) {
            setInterval(pollUpdates, 5000);
        }
    }

    function pollUpdates() {
        if (isLoading) return;
        const url = `${config.verificationUrl}?pageNumber=1&onlyPending=${config.onlyPending}`;
        
        fetch(url, { headers: { 'X-Requested-With': 'XMLHttpRequest' } })
        .then(response => response.text())
        .then(html => {
            const container = document.getElementById(config.containerId);
            if (container && html.trim().length > 0) {
                // If it's the exact same content, we don't necessarily want to replace it to avoid flickering, 
                // but since it's simple, we'll just replace innerHTML.
                container.innerHTML = html;
            }
        })
        .catch(err => console.error('Polling error:', err));
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
        let content = decodeURIComponent(encodedContent);
        
        // Formatează Markdown basic pentru o afișare frumoasă
        // 1. Text îngroșat: **text** -> <strong>text</strong>
        content = content.replace(/\*\*(.*?)\*\*/g, '<strong class="text-white fw-bold">$1</strong>');
        
        // 2. Capete de secțiune: [Secțiune] -> culoare aurora
        content = content.replace(/^\[(.*?)\]/gm, '<span class="text-aurora fw-bold">[$1]</span>');

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
