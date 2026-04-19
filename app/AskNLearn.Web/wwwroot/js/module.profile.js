/**
 * ProfileManager - Handles profile management logic
 */
class ProfileManager {
    static init(isOwnProfile, initialTags, initialLinks) {
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
                    const resp = await fetch('/identity/auth/request-password-reset', {
                        method: 'POST',
                        headers: { 'RequestVerificationToken': window.getAntiForgeryToken() }
                    });
                    
                    if (resp.ok) {
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
                    } else {
                        window.Notify.error("Failed to request reset.");
                        btnReset.disabled = false;
                        btnReset.innerHTML = 'Send Reset Link';
                    }
                } catch (e) {
                    console.error(e);
                    window.Notify.error("Network error.");
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
            const resp = await fetch('/identity/profiles/update', {
                method: 'POST',
                body: formData
            });

            if (resp.ok) {
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
            } else {
                if (status) status.textContent = "Sync Failed";
                window.Notify.error("Failed to save changes.");
            }
        } catch (e) {
            console.error(e);
            if (status) status.textContent = "Network Error";
            window.Notify.error("Network error during sync.");
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
}

// Expose to window
window.ProfileManager = ProfileManager;
export default ProfileManager;
