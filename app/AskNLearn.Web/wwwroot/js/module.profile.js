/**
 * ProfileManager - Handles profile management logic
 */
class ProfileManager {
    static init(isOwnProfile, initialTags, initialLinks, userId, isPending) {
        this.userId = userId;
        this.isPending = isPending;

        if (isPending) {
            this.startVerificationPolling();
        }

        if (!isOwnProfile) return;

        // Setup social list
        this.renderSocialLinks(initialLinks);

        // Setup auto-sync for inline fields
        const inputs = document.querySelectorAll('.inline-edit-input, .premium-textarea');
        inputs.forEach(input => {
            input.addEventListener('change', () => this.syncChanges());
        });

        // Setup Reset Password button in modal
        const btnReset = document.getElementById('btnRequestReset');
        if (btnReset) {
            btnReset.addEventListener('click', async () => {
                btnReset.disabled = true;
                btnReset.innerHTML = '<span class="spinner-border spinner-border-sm me-2"></span>Sending...';
                
                try {
                    await axios.post('/identity/auth/request-password-reset');
                    
                    document.getElementById('resetFeedback')?.classList.remove('d-none');
                    btnReset.innerHTML = 'Link Sent!';
                    setTimeout(() => {
                        const modalEl = document.getElementById('resetModal');
                        const modal = bootstrap.Modal.getInstance(modalEl);
                        modal?.hide();
                        setTimeout(() => {
                            btnReset.disabled = false;
                            btnReset.innerHTML = 'Send Reset Link';
                            document.getElementById('resetFeedback')?.classList.add('d-none');
                        }, 500);
                    }, 2000);
                } catch (e) {
                    btnReset.disabled = false;
                    btnReset.innerHTML = 'Send Reset Link';
                }
            });
        }
    }

    static showResetModal() {
        const modalEl = document.getElementById('resetModal');
        if (modalEl) {
            const modal = new bootstrap.Modal(modalEl);
            modal.show();
        }
    }

    static async syncChanges() {
        const form = document.getElementById('linkedInForm');
        if (!form) return;

        const formData = new FormData(form);
        const badge = document.getElementById('syncBadge');
        const status = document.getElementById('syncStatus');

        if (badge && status) {
            badge.style.transform = 'translateY(0) translateX(-50%)';
            badge.style.opacity = '1';
            status.textContent = "Syncing...";
        }

        try {
            await axios.post('/identity/profiles/update', formData);

            // If files were uploaded, we might want to refresh previews
            const avatarInput = document.getElementById('avatarUpload');
            if (avatarInput && avatarInput.files.length > 0) {
                const reader = new FileReader();
                reader.onload = (e) => {
                    document.getElementById('avatarPreview').src = e.target.result;
                    // Also update sidebar avatars if any
                    document.querySelectorAll('.profile-avatar-img').forEach(img => img.src = e.target.result);
                };
                reader.readAsDataURL(avatarInput.files[0]);
            }

            const bannerInput = document.getElementById('bannerUpload');
            if (bannerInput && bannerInput.files.length > 0) {
                const reader = new FileReader();
                reader.onload = (e) => {
                    const banner = document.querySelector('.profile-banner-stage');
                    if (banner) banner.style.backgroundImage = `url('${e.target.result}')`;
                };
                reader.readAsDataURL(bannerInput.files[0]);
            }

            if (status) status.textContent = "Profile Updated";
            setTimeout(() => {
                if (badge) {
                    badge.style.transform = 'translateY(100%) translateX(-50%)';
                    badge.style.opacity = '0';
                }
            }, 2000);
        } catch (e) {
            if (status) status.textContent = "Sync Failed";
        }
    }

    static renderSocialLinks(links) {
        const container = document.getElementById('socialListArea');
        if (!container) return;
        container.innerHTML = links.map(link => `
            <div class="d-flex align-items-center justify-content-between p-2 rounded-3 hover-bg-glass transition-all">
                <a href="${link}" target="_blank" class="text-white text-decoration-none small text-truncate" style="max-width: 80%;">${link}</a>
                <button type="button" onclick="ProfileManager.removeSocialLink('${link}')" class="btn btn-link p-0 text-muted hover-text-danger">
                    <span class="material-symbols-outlined fs-6">close</span>
                </button>
            </div>
        `).join('');
    }

    static async addSocialLink() {
        const input = document.getElementById('socialInputBox');
        const link = input?.value.trim();
        if (!link) return;

        const hidden = document.getElementById('socialLinksHidden');
        let links = hidden.value ? hidden.value.split(';') : [];
        if (!links.includes(link)) {
            links.push(link);
            hidden.value = links.join(';');
            this.renderSocialLinks(links);
            input.value = '';
            await this.syncChanges();
        }
    }

    static async removeSocialLink(link) {
        const hidden = document.getElementById('socialLinksHidden');
        let links = hidden.value.split(';').filter(l => l !== link);
        hidden.value = links.join(';');
        this.renderSocialLinks(links);
        await this.syncChanges();
    }

    static startVerificationPolling() {
        if (this.pollingInterval) clearInterval(this.pollingInterval);
        
        this.pollingInterval = setInterval(async () => {
            try {
                const response = await axios.get(`/identity/profiles/verification-status/${this.userId}`);
                const data = response.data;

                if (data) {
                    this.updateGuardianConsole(data.adminNotes, data.status);
                    
                    // If verified or rejected, stop polling and refresh after a delay
                    if (data.status !== 0) { // Assuming 0 is Pending
                        clearInterval(this.pollingInterval);
                        setTimeout(() => window.location.reload(), 5000);
                    }
                }
            } catch (e) {
                console.error("Verification polling error", e);
            }
        }, 3000);
    }

    static updateGuardianConsole(notes, status) {
        const notesEl = document.getElementById('guardian-current-notes');
        if (notesEl && notes && notes !== notesEl.innerText) {
            // Typing effect simulation
            notesEl.classList.add('animate-pulse');
            notesEl.innerText = notes;
            
            const logs = document.getElementById('guardian-logs');
            if (logs) {
                const log = document.createElement('div');
                log.className = 'text-aurora tiny opacity-50';
                log.innerText = `[${new Date().toLocaleTimeString()}] AI Update detected.`;
                logs.appendChild(log);
                logs.scrollTop = logs.scrollHeight;
            }
            
            setTimeout(() => notesEl.classList.remove('animate-pulse'), 1000);
        }
    }
}

// Expose to window
window.ProfileManager = ProfileManager;
export default ProfileManager;
