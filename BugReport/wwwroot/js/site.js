document.querySelectorAll('.toast').forEach(toastEl => {
    const toast = new bootstrap.Toast(toastEl, {delay: 4000 });
    toast.show();
});
// Password visibility toggle: swaps input type and button icon
(() => {
    const eye = '<svg class="icon-eye" xmlns="http://www.w3.org/2000/svg" width="18" height="18" viewBox="0 0 16 16" role="img" aria-hidden="true">' +
        '<path class="eye" d="M16 8s-3-5.5-8-5.5S0 8 0 8s3 5.5 8 5.5S16 8 16 8z"/>' +
        '<circle class="pupil" cx="8" cy="8" r="2.2" />' +
    '</svg>';

    const eyeSlash = '<svg class="icon-eye-slash" xmlns="http://www.w3.org/2000/svg" width="18" height="18" viewBox="0 0 16 16" role="img" aria-hidden="true">' +
        '<path class="eye" d="M16 8s-3-5.5-8-5.5S0 8 0 8s3 5.5 8 5.5S16 8 16 8z"/>' +
        '<circle class="pupil" cx="8" cy="8" r="2.2" />' +
        '<path class="slash" d="M3 3l10 10" />' +
    '</svg>';

    document.addEventListener('click', function (e) {
        const btn = e.target.closest('.toggle-password');
        if (!btn) return;

        const group = btn.closest('.input-group');
        if (!group) return;

        const input = group.querySelector('input');
        if (!input) return;

        if (input.type === 'password') {
            input.type = 'text';
            btn.innerHTML = eyeSlash;
            btn.setAttribute('aria-pressed', 'true');
            btn.title = 'Hide password';
            btn.setAttribute('aria-label', 'Hide password');
        } else {
            input.type = 'password';
            btn.innerHTML = eye;
            btn.setAttribute('aria-pressed', 'false');
            btn.title = 'Show password';
            btn.setAttribute('aria-label', 'Show password');
        }
    });
})();