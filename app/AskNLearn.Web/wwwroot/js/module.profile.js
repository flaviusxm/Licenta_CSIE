/**
 * ProfileManager - Handles profile management logic
 */
class ProfileManager {
    static rankThresholds = [
        { name: "Newcomer", minPoints: 0 },
        { name: "Apprentice", minPoints: 200 },
        { name: "Scholar", minPoints: 500 },
        { name: "Contributor", minPoints: 1000 },
        { name: "Expert", minPoints: 2000 },
        { name: "Mentor", minPoints: 4000 },
        { name: "Legend", minPoints: 8000 }
    ];

    static init(isOwnProfile, userId, isPending, currentPoints) {
        this.userId = userId;
        this.isOwnProfile = isOwnProfile;
        this.isPending = isPending;
        this.originalBio = null;
        this.currentPoints = currentPoints;

        // Load rank progress
        this.loadRankProgress();
        this.loadRecentActivity();

        if (isPending) {
            this.startVerificationPolling();
        }

        if (!isOwnProfile) return;

        // Setup name edit
        const nameInput = document.getElementById('fullNameInput');
        if (nameInput) {
            nameInput.addEventListener('input', () => this.onDataChanged());
        }

        // Setup Reset Password button
        const btnReset = document.getElementById('btnRequestReset');
        if (btnReset) {
            btnReset.addEventListener('click', async () => {
                btnReset.disabled = true;
                btnReset.innerHTML = '<span class="spinner-border spinner-border-sm me-2"></span>Sending...';
                try {
                    await axios.post('/identity/auth/request-password-reset');
                    window.Notify.success('Reset link sent to your email');
                    const modalEl = document.getElementById('resetModal');
                    const modal = bootstrap.Modal.getInstance(modalEl);
                    if (modal) modal.hide();
                } catch (e) {
                    window.Notify.error('Failed to send reset link');
                } finally {
                    btnReset.disabled = false;
                    btnReset.innerHTML = 'Send Reset Link';
                }
            });
        }
    }

    static onDataChanged() {
        const banner = document.getElementById('saveChangesBanner');
        if (banner) banner.classList.remove('d-none');
    }

    static editBio() {
        if (!this.isOwnProfile) return;

        const display = document.getElementById('bioDisplay');
        const edit = document.getElementById('bioEdit');
        const textarea = document.getElementById('bioTextarea');

        if (display && edit && textarea) {
            this.originalBio = display.innerText.trim();
            textarea.value = this.originalBio === 'No bio yet. Click edit to add one.' ? '' : this.originalBio;
            display.classList.add('d-none');
            edit.classList.remove('d-none');
        }
    }

    static cancelBioEdit() {
        const display = document.getElementById('bioDisplay');
        const edit = document.getElementById('bioEdit');

        if (display && edit) {
            display.classList.remove('d-none');
            edit.classList.add('d-none');
        }
    }

    static async saveBio() {
        const textarea = document.getElementById('bioTextarea');
        const newBio = textarea?.value || '';

        const hidden = document.getElementById('bioHidden');
        if (hidden) hidden.value = newBio;

        await this.saveChanges();
        this.cancelBioEdit();

        const display = document.getElementById('bioDisplay');
        if (display) {
            display.innerText = newBio || 'No bio yet. Click edit to add one.';
        }
    }

    static async saveChanges() {
        const form = document.getElementById('profileUpdateForm');
        const fullNameInput = document.getElementById('fullNameInput');
        const fullNameHidden = document.getElementById('fullNameHidden');

        if (fullNameInput && fullNameHidden) {
            fullNameHidden.value = fullNameInput.value;
        }

        const formData = new FormData(form);

        try {
            const response = await axios.post('/identity/profiles/update', formData);

            if (response.data && response.data.length === 0) {
                window.Notify.success('Profile updated');
                const banner = document.getElementById('saveChangesBanner');
                if (banner) banner.classList.add('d-none');

                // Refresh page to show updated data
                setTimeout(() => location.reload(), 1000);
            } else {
                window.Notify.error('Failed to update profile');
            }
        } catch (e) {
            window.Notify.error('Failed to update profile');
        }
    }

    static async discardChanges() {
        const banner = document.getElementById('saveChangesBanner');
        if (banner) banner.classList.add('d-none');
        location.reload();
    }

    static showResetModal() {
        const modalEl = document.getElementById('resetModal');
        if (modalEl) {
            const modal = new bootstrap.Modal(modalEl);
            modal.show();
        }
    }

    static handleFileSelect(input) {
        const display = input.parentElement.querySelector('.file-name-display');
        if (input.files && input.files[0]) {
            display.innerText = input.files[0].name;
            display.classList.add('text-primary');
            input.parentElement.style.borderColor = '#3b82f6';

            // Auto-submit the form
            const form = input.closest('form');
            if (form) form.submit();
        }
    }

    static loadRankProgress() {
        const currentPoints = this.currentPoints;
        const thresholds = this.rankThresholds;

        // Find current rank
        let currentRank = thresholds[0];
        let nextRank = null;

        for (let i = thresholds.length - 1; i >= 0; i--) {
            if (currentPoints >= thresholds[i].minPoints) {
                currentRank = thresholds[i];
                nextRank = thresholds[i + 1] || null;
                break;
            }
        }

        const currentRankNameEl = document.getElementById('currentRankName');
        const nextRankNameEl = document.getElementById('nextRankName');
        const progressBar = document.getElementById('rankProgressBar');
        const nextThresholdEl = document.getElementById('nextRankThreshold');
        const currentPointsEl = document.getElementById('currentRankPoints');

        if (currentRankNameEl) {
            currentRankNameEl.textContent = currentRank.name;
        }

        if (nextRank) {
            if (nextRankNameEl) nextRankNameEl.textContent = nextRank.name;

            const pointsToNext = nextRank.minPoints - currentPoints;
            const totalForRank = nextRank.minPoints - currentRank.minPoints;
            const progress = Math.floor(((currentPoints - currentRank.minPoints) / totalForRank) * 100);

            if (progressBar) {
                progressBar.style.width = `${Math.min(progress, 100)}%`;
                progressBar.setAttribute('aria-valuenow', progress);
            }

            if (nextThresholdEl) {
                nextThresholdEl.textContent = `${pointsToNext} pts to ${nextRank.name}`;
            }
        } else {
            // Max rank reached
            if (nextRankNameEl) nextRankNameEl.textContent = "Legend (Max)";
            if (progressBar) {
                progressBar.style.width = "100%";
                progressBar.classList.add('bg-success');
            }
            if (nextThresholdEl) nextThresholdEl.textContent = "Maximum rank achieved!";
        }

        if (currentPointsEl) {
            currentPointsEl.textContent = `${currentPoints} pts`;
        }
    }

    
   
static async loadRecentActivity() {
    try {
        const response = await axios.get(`/identity/profiles/recent-activity/${this.userId}`);
        const activities = response.data;
        
        const container = document.getElementById('recentActivityList');
        const totalEarned = document.getElementById('recentPointsEarned');
        
        if (container && activities && activities.length > 0) {
            let totalPoints = 0;
            container.innerHTML = activities.map(act => {
                totalPoints += act.points;
                const sign = act.points > 0 ? '+' : '';
                return `
                    <div class="d-flex align-items-center justify-content-between py-2 border-bottom border-glass">
                        <div class="d-flex align-items-center gap-2">
                            <span class="material-symbols-outlined text-primary fs-6">
                                ${act.action === 'Created post' ? 'article' : act.action === 'Added comment' ? 'chat' : 'thumb_up'}
                            </span>
                            <span class="text-white small">${act.action}</span>
                        </div>
                        <span class="text-primary small fw-semibold">${sign}${act.points} pts</span>
                    </div>
                `;
            }).join('');
            
            if (totalEarned) {
                totalEarned.textContent = `+${totalPoints} pts`;
            }
        } else if (container) {
            container.innerHTML = '<div class="text-center py-3"><span class="text-muted small">No recent activity yet</span></div>';
        }
    } catch (e) {
        console.error('Failed to load recent activity:', e);
        const container = document.getElementById('recentActivityList');
        if (container) {
            container.innerHTML = '<div class="text-center py-3"><span class="text-muted small">Unable to load activity</span></div>';
        }
    }
}
    static startVerificationPolling() {
        if (this.pollingInterval) clearInterval(this.pollingInterval);

        this.pollingInterval = setInterval(async () => {
            try {
                const response = await axios.get(`/identity/profiles/verification-status/${this.userId}`);
                const data = response.data;

                if (data) {
                    const isFinished = data.status !== 0;

                    if (isFinished) {
                        clearInterval(this.pollingInterval);
                        setTimeout(() => window.location.reload(), 2000);
                    }
                }
            } catch (e) {
                console.error("Verification polling error", e);
            }
        }, 3000);
    }
}

// Expose to window
window.ProfileManager = ProfileManager;
export default ProfileManager;