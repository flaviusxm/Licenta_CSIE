/**
 * AuthManager - Handles authentication UI logic and validation toasts
 */
class AuthManager {
    static init() {
        this.handleValidationErrors();
        this.setupPasswordToggles();
        this.setupSignupFeatures();
    }

    static handleValidationErrors() {
        const summary = document.getElementById('validation-summary');
        if (summary) {
            const errors = summary.querySelectorAll('li');
            errors.forEach(err => {
                if (err.innerText && err.innerText !== "ModelOnly") {
                    window.Notify.error(err.innerText);
                }
            });
            
            const directText = summary.innerText.trim();
            if (directText && errors.length === 0) {
                window.Notify.error(directText);
            }
        }
    }

    static setupPasswordToggles() {
        const setups = [
            { input: 'PasswordSignIn', btn: 'togglePasswordSignIn' },
            { input: 'PasswordSignUp', btn: 'togglePasswordSignUp' },
            { input: 'ConfirmPasswordSignUp', btn: 'toggleConfirmSignUp' }
        ];

        setups.forEach(s => {
            const input = document.getElementById(s.input);
            const btn = document.getElementById(s.btn);
            if (input && btn) {
                btn.addEventListener('click', () => {
                    const isPass = input.type === 'password';
                    input.type = isPass ? 'text' : 'password';
                    btn.innerHTML = isPass 
                        ? '<span class="material-symbols-outlined fs-5">visibility_off</span>' 
                        : '<span class="material-symbols-outlined fs-5">visibility</span>';
                });
            }
        });
    }

    static setupSignupFeatures() {
        const passwordInput = document.querySelector("#PasswordSignUp");
        const confirmInput = document.querySelector("#ConfirmPasswordSignUp");
        const strengthBar = document.querySelector("#passwordStrengthBar");
        const strengthText = document.querySelector("#passwordStrengthText");
        const confirmText = document.querySelector("#confirmMatchText");

        if (passwordInput && strengthBar) {
            passwordInput.addEventListener("input", () => {
                let value = passwordInput.value;
                let strength = 0;
                if (value.length >= 8) strength++;
                if (/[A-Z]/.test(value)) strength++;
                if (/[a-z]/.test(value)) strength++;
                if (/[0-9]/.test(value)) strength++;
                if (/[^A-Za-z0-9]/.test(value)) strength++;

                switch(strength) {
                    case 0:
                    case 1:
                        strengthBar.style.width = "20%";
                        strengthBar.className = "progress-bar bg-danger";
                        strengthText.textContent = "Weak password";
                        break;
                    case 2:
                    case 3:
                        strengthBar.style.width = "60%";
                        strengthBar.className = "progress-bar bg-warning";
                        strengthText.textContent = "Medium password";
                        break;
                    case 4:
                    case 5:
                        strengthBar.style.width = "100%";
                        strengthBar.className = "progress-bar bg-success";
                        strengthText.textContent = "Strong password";
                        break;
                }

                if (confirmInput && confirmText) {
                    confirmText.textContent = passwordInput.value === confirmInput.value ? "Passwords match ✅" : "Passwords do not match ❌";
                }
            });

            if (confirmInput && confirmText) {
                confirmInput.addEventListener("input", () => {
                    confirmText.textContent = passwordInput.value === confirmInput.value ? "Passwords match ✅" : "Passwords do not match ❌";
                });
            }
        }
    }
}

// Expose to window for Razor views
window.AuthManager = AuthManager;
export default AuthManager;
