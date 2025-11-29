class AssigneeSelector {
    constructor() {
        this.container = document.getElementById("assignee-tag-container");
        this.input = document.getElementById("assignee-input");
        this.dropdown = document.getElementById("assignee-dropdown");
        this.hidden = document.getElementById("assignee-hidden");

        this.users = [];
        this.selected = new Map();
        this.filtered = [];
        this.highlightIndex = -1;

        this.initialize();
    }

    async initialize() {
        await this.fetchUsers();
        this.restoreFromStorage?.(); // Safely call if exists

        this.input.addEventListener("input", () => {
            this.updatePlaceholder();
            this.renderDropdown();
        });

        this.container.addEventListener("click", () => this.input.focus());
        this.input.addEventListener("keydown", (e) => this.handleKeyDown(e));

        // Delay blur hiding to allow mousedown to register
        this.input.addEventListener("blur", () =>
            setTimeout(() => this.hideDropdown(), 150)
        );

        // Hide when clicking outside
        document.addEventListener("click", (e) => {
            const inside = this.container.contains(e.target) || this.dropdown.contains(e.target);
            if (!inside) this.hideDropdown();
        });

        this.updatePlaceholder();
    }

    async fetchUsers() {
        try {
            const res = await fetch("/User/GetUsers");
            if (!res.ok) throw new Error("Network error while fetching users");

            const users = await res.json();

            // Defensive normalization
            this.users = Array.isArray(users)
                ? users.map(u => ({
                    id: u.id ?? u.Id,
                    fullName: u.fullName ?? u.fullname ?? u.FullName ?? ""
                }))
                : [];

        } catch (e) {
            console.error("Failed to fetch users.", e);
        }

        this.renderDropdown();
    }

    /* ============================
       DROPDOWN RENDERING
    ============================ */

    renderDropdown() {
        const query = this.input.value.trim().toLowerCase();
        this.dropdown.innerHTML = "";

        this.filtered = this.users
            .filter(u => !this.selected.has(u.id))
            .filter(u => u.fullName.toLowerCase().includes(query));

        if (this.filtered.length === 0) {
            this.hideDropdown();
            return;
        }

        this.filtered.forEach((user, i) => {
            this.dropdown.appendChild(this.createDropdownItem(user, i));
        });

        this.highlightIndex = 0;
        this.applyHighlight();

        this.dropdown.classList.add("show");
    }

    createDropdownItem(user, index) {
        const item = document.createElement("div");
        item.className = "dropdown-item";
        item.textContent = user.fullName;
        item.dataset.index = index;

        // Use mousedown so click doesn't blur the input before selection happens
        item.addEventListener("mousedown", (e) => {
            e.preventDefault(); // Prevent blur
            this.addAssignee(user);
            this.hideDropdown();
        });

        return item;
    }

    hideDropdown() {
        this.dropdown.classList.remove("show");
    }

    /* ============================
       KEYBOARD SUPPORT
    ============================ */

    handleKeyDown(e) {
        if (!this.dropdown.classList.contains("show")) return;

        const len = this.filtered.length;
        if (!len) return;

        switch (e.key) {
            case "ArrowDown":
                e.preventDefault();
                this.highlightIndex = (this.highlightIndex + 1) % len;
                this.applyHighlight();
                break;

            case "ArrowUp":
                e.preventDefault();
                this.highlightIndex = (this.highlightIndex - 1 + len) % len;
                this.applyHighlight();
                break;

            case "Enter":
                e.preventDefault();
                const choice = this.filtered[this.highlightIndex];
                if (choice) {
                    this.addAssignee(choice);
                    this.hideDropdown();
                }
                break;

            case "Backspace":
                if (this.input.value === "") {
                    const lastId = [...this.selected.keys()].pop();
                    if (lastId) {
                        const tag = this.container.querySelector(`[data-id='${lastId}']`);
                        if (tag) this.removeAssignee(lastId, tag);
                    }
                }
                break;
        }
    }

    applyHighlight() {
        [...this.dropdown.children].forEach((item, i) =>
            item.classList.toggle("active", i === this.highlightIndex)
        );
    }

    /* ============================
       TAG MANAGEMENT
    ============================ */

    addAssignee(user, save = true) {
        if (!user?.id || this.selected.has(user.id)) return;

        this.selected.set(user.id, user.fullName);

        const tag = this.createTag(user);
        this.container.insertBefore(tag, this.input);

        const hidden = this.createHiddenInput(user.id);
        this.hidden.appendChild(hidden);

        if (save) this.saveToStorage?.(); // Optional if implemented

        this.input.value = "";
        this.updatePlaceholder();
        this.renderDropdown();
    }

    createTag(user) {
        const el = document.createElement("span");
        el.className = "tag";
        el.dataset.id = user.id;
        el.innerHTML = `${user.fullName} <span class="remove-tag">&times;</span>`;

        el.querySelector(".remove-tag").addEventListener("click", () => {
            this.removeAssignee(user.id, el);
        });

        return el;
    }

    createHiddenInput(id) {
        const input = document.createElement("input");
        input.type = "hidden";
        input.name = "Assignees[]";
        input.value = id;
        input.dataset.id = id;
        return input;
    }

    removeAssignee(id, tagEl) {
        this.selected.delete(id);
        tagEl?.remove();

        const hidden = this.hidden.querySelector(`[data-id='${id}']`);
        hidden?.remove();

        this.saveToStorage?.();
        this.renderDropdown();
    }

    updatePlaceholder() {
        this.input.placeholder = this.selected.size === 0 ? "Add assignees..." : "";
    }
}

class AttachmentManager {
    constructor() {
        this.dropzone = document.getElementById("file-dropzone");
        this.input = document.getElementById("file-input");
        this.preview = document.getElementById("file-preview");
        this.files = [];

        if (this.dropzone && this.input) this.init();
    }

    init() {
        this.dropzone.onclick = () => this.input.click();
        this.input.onchange = e => this.addFiles([...e.target.files]);

        this.dropzone.ondragover = e => {
            e.preventDefault();
            this.dropzone.classList.add("dragover");
        };

        this.dropzone.ondragleave = () => this.dropzone.classList.remove("dragover");
        this.dropzone.ondrop = e => {
            e.preventDefault();
            this.dropzone.classList.remove("dragover");
            this.addFiles([...e.dataTransfer.files]);
        };
    }

    addFiles(newFiles) {
        newFiles.forEach(file => {
            if (!file.type.startsWith("image/")) return;
            if (this.files.some(f => f.name === file.name && f.size === file.size)) return;

            this.files.push(file);
            this.createPreview(file);
        });
        this.syncInput();
    }

    createPreview(file) {
        const reader = new FileReader();
        reader.onload = e => {
            const wrapper = document.createElement("div");
            wrapper.className = "preview-wrapper";

            wrapper.innerHTML = `
                <img src="${e.target.result}" />
                <div class="preview-remove">&times;</div>
            `;

            wrapper.querySelector(".preview-remove").onclick = () => {
                this.files = this.files.filter(f => f !== file);
                wrapper.remove();
                this.syncInput();
            };

            this.preview.appendChild(wrapper);
        };
        reader.readAsDataURL(file);
    }

    syncInput() {
        const dt = new DataTransfer();
        this.files.forEach(f => dt.items.add(f));
        this.input.files = dt.files;
    }
}

document.addEventListener("DOMContentLoaded", () => {
    new AssigneeSelector();
    new AttachmentManager();
});
