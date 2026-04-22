/**
 * MessagingManager - Handles communication hub logic
 */
class MessagingManager {
    static config = {
        conversationId: null,
        userId: null,
        msgContainer: null
    };

    static emojiList = ["😀","😃","😄","😁","😆","😅","😂","🤣","😊","😇","🙂","🙃","😉","😌","😍","🥰","😘","😗","😙","😚","😋","😛","😝","😜","🤪","🤨","🧐","🤓","😎","🤩","🥳","😏","😒","😞","😔","😟","😕","🙁","☹️","😣","😖","😫","😩","🥺","😢","😭","😤","😠","😡","🤬","🤯","😳","🥵","🥶","😱","😨","😰","😥","😓","🤗","🤔","🤭","🤫","🤥","😶","😐","😑","😬","🙄","😯","😦","😧","😮","😲","🥱","😴","🤤","😪","😵","🤐","🥴","🤢","🤮","🤧","😷","🤒","🤕","🤑","🤠","😈","👿","👹","👺","🤡","💩","👻","💀","☠️","👽","👾","🤖","🎃","😺","😸","😹","😻","😼","😽","🙀","😿","😾","❤️","🧡","💛","💚","💙","💜","🖤","🤍","🤎","💔","❣️","💕","💞","💓","💗","💖","💘","💝","💟","✨","⭐","🌟","🔥","💨","💦","💧","💤","💢","✨"];
    static searchTimeout = null;

    static init(convId, userId, recentConvIds) {
        this.config.conversationId = convId;
        this.config.userId = userId;
        this.config.msgContainer = document.getElementById('messages-container');

        if (this.config.msgContainer) {
            this.config.msgContainer.scrollTop = this.config.msgContainer.scrollHeight;
        }

        this.setupSignalR(recentConvIds);
        this.restoreSectionStates();
        this.setupEventListeners();
    }

    static restoreSectionStates() {
        ['messages', 'connections', 'requests'].forEach(id => {
            const state = localStorage.getItem(`inbox_section_${id}`);
            if (state) {
                const content = document.getElementById(`content-${id}`);
                const header = document.querySelector(`.section-header[onclick*="${id}"]`);
                if (content && header) {
                    const shouldOpen = state === 'open';
                    content.classList.toggle('show', shouldOpen);
                    header.classList.toggle('collapsed', !shouldOpen);
                }
            }
        });
    }

    static setupSignalR(recentConvIds) {
        if (!this.config.conversationId) return;

        const joinChannels = () => {
            if (window.connection.state === "Connected") {
                window.connection.invoke("JoinChannel", this.config.conversationId);
                recentConvIds.forEach(id => {
                    window.connection.invoke("JoinChannel", id);
                });
            }
        };

        if (window.connection.state === "Connected") {
            joinChannels();
        } else {
            window.onSignalRConnected = joinChannels;
        }
        
        window.connection.on("ReceiveMessage", (msg) => {
            const incomingId = (msg.conversationId || msg.channelId || "").toString().toLowerCase();
            const activeId = (this.config.conversationId || "").toLowerCase();

            const sidebarMsg = document.getElementById(`sidebar-msg-${incomingId}`);
            if (sidebarMsg) {
                sidebarMsg.innerText = msg.content;
                sidebarMsg.classList.add('fw-bold', 'text-light');
                sidebarMsg.classList.remove('text-muted');
            }

            if (incomingId === activeId) {
                this.appendMessage(msg);
            } else {
                window.Notify.info(`New message from ${msg.authorName}`);
            }
        });
    }

    static setupEventListeners() {
        document.addEventListener('click', (e) => {
            const overlay = document.getElementById('search-results-overlay');
            if (overlay && !overlay.contains(e.target) && e.target.id !== 'global-search-input') {
                overlay.classList.add('d-none');
            }

            const picker = document.getElementById('emoji-picker');
            const btn = document.getElementById('emoji-btn');
            if (picker && !picker.contains(e.target) && !btn?.contains(e.target)) {
                picker.classList.add('d-none');
            }
        });

        document.getElementById('message-input')?.addEventListener('keypress', (e) => {
            if (e.key === 'Enter') {
                this.sendMessage();
                document.getElementById('emoji-picker')?.classList.add('d-none');
            }
        });
    }

    static toggleSection(sectionId) {
        const content = document.getElementById(`content-${sectionId}`);
        const header = document.querySelector(`.section-header[onclick*="${sectionId}"]`);
        
        if (content) {
            const isOpen = content.classList.contains('show');
            content.classList.toggle('show', !isOpen);
            header?.classList.toggle('collapsed', isOpen);
            localStorage.setItem(`inbox_section_${sectionId}`, !isOpen ? 'open' : 'closed');
        }
    }

    static async handleGlobalSearch(term) {
        const overlay = document.getElementById('search-results-overlay');
        const content = document.getElementById('search-results-content');
        
        if (!term || term.length < 2) {
            if (overlay) overlay.classList.add('d-none');
            return;
        }

        if (this.searchTimeout) clearTimeout(this.searchTimeout);
        this.searchTimeout = setTimeout(async () => {
            try {
                const resp = await axios.get(`/Connections/SearchPeople?term=${encodeURIComponent(term)}`);
                const users = resp.data;
                if (overlay) overlay.classList.remove('d-none');
                
                if (users.length === 0) {
                    if (content) content.innerHTML = '<div class="p-4 text-center text-muted small">No people found</div>';
                } else {
                    if (content) {
                        content.innerHTML = users.map(u => `
                            <form action="/communication/messaging/conversations/initialize" method="post" class="m-0">
                                <input type="hidden" name="userId" value="${u.id}" />
                                <button type="submit" class="d-flex align-items-center gap-3 p-3 w-100 bg-transparent border-0 text-start hover-bg-glass transition-all rounded-3">
                                    <div class="avatar-sm rounded-circle overflow-hidden border border-glass" style="width: 32px; height: 32px;">
                                        <img src="${u.avatarUrl || `https://api.dicebear.com/7.x/avataaars/svg?seed=${u.userName}`}" class="w-100 h-100 object-cover" />
                                    </div>
                                    <div class="flex-grow-1 min-w-0">
                                        <div class="fw-bold text-white small text-truncate">${u.fullName || u.userName}</div>
                                        <div class="text-muted" style="font-size: 0.6rem;">@${u.userName}</div>
                                    </div>
                                </button>
                            </form>
                        `).join('');
                    }
                }
            } catch (e) { 
                // Error handled globally
            }
        }, 300);
    }

    static async handleFriendRequest(userId, action) {
        try {
            window.Notify.system("Processing...");
            await axios.post(`/Connections/${action}?userId=${userId}`);
            
            window.Notify.success(action === 'AcceptRequest' ? "Connection accepted!" : "Request declined.");
            const el = document.getElementById(`sidebar-request-${userId}`);
            if (el) {
                el.style.opacity = '0';
                el.style.transform = 'translateX(-20px)';
                setTimeout(() => {
                    el.remove();
                    const container = document.getElementById('content-requests');
                    if (container && container.querySelectorAll('[id^="sidebar-request-"]').length === 0) {
                        container.innerHTML = '<div class="p-4 text-center opacity-50"><p class="text-muted small mb-0">No pending requests</p></div>';
                    }
                }, 400);
            }
        } catch (e) { 
            // Error handled globally
        }
    }

    static appendMessage(msg) {
        if (!this.config.msgContainer) return;
        const isMine = msg.authorId === this.config.userId;
        const html = `
            <div class="d-flex w-100 ${isMine ? 'justify-content-end' : 'justify-content-start'} animate-slide-up">
                <div class="d-flex ${isMine ? 'flex-row-reverse' : ''} gap-2 align-items-end" style="max-width: 75%;">
                    <div class="d-flex flex-column gap-1 ${isMine ? 'align-items-end' : 'align-items-start'}">
                        <div class="px-3 py-2 shadow-sm" style="border-radius: 18px; ${isMine ? 'background: var(--color-accent); color: white;' : 'background: rgba(255,255,255,0.06); border: 1px solid rgba(255,255,255,0.05); color: white;'}">
                            <p class="mb-0" style="font-size: 0.9rem; line-height: 1.4;">${msg.content}</p>
                        </div>
                        <span class="text-muted px-1" style="font-size: 0.55rem;">${new Date().toLocaleTimeString([], {hour: '2-digit', minute:'2-digit'})}</span>
                    </div>
                </div>
            </div>
        `;
        this.config.msgContainer.insertAdjacentHTML('beforeend', html);
        this.config.msgContainer.scrollTop = this.config.msgContainer.scrollHeight;
    }

    static async sendMessage() {
        const input = document.getElementById('message-input');
        const content = input.value.trim();
        if (!content || !this.config.conversationId) return;

        input.value = '';
        try {
            await window.connection.invoke("SendMessage", this.config.conversationId, content);
        } catch (err) {
            console.error(err);
            window.Notify.error("Failed to send message.");
        }
    }

    static async deleteDM(msgId) {
        if (!confirm('Ștergi acest mesaj?')) return;
        try {
            await axios.post(`/communication/messaging/messages/delete?id=${msgId}`);
            const bubble = document.getElementById(`msg-bubble-${msgId}`)?.closest('.animate-slide-up');
            if (bubble) {
                bubble.style.opacity = '0';
                bubble.style.transform = 'scale(0.9)';
                setTimeout(() => bubble.remove(), 300);
            }
        } catch (e) { 
            // Error handled globally
        }
    }

    static toggleEmojiPicker() {
        const picker = document.getElementById('emoji-picker');
        const list = document.getElementById('emoji-list');
        if (!picker || !list) return;

        picker.classList.toggle('d-none');
        
        if (!picker.classList.contains('d-none') && list.children.length === 0) {
            this.emojiList.forEach(emoji => {
                const span = document.createElement('span');
                span.innerText = emoji;
                span.style.cursor = 'pointer';
                span.style.fontSize = '1.25rem';
                span.style.padding = '5px';
                span.className = 'hover-scale transition-all d-inline-block';
                span.onclick = () => {
                    const input = document.getElementById('message-input');
                    if (input) {
                        input.value += emoji;
                        input.focus();
                    }
                };
                list.appendChild(span);
            });
        }
    }
}

// Expose to window
window.MessagingManager = MessagingManager;
export default MessagingManager;
