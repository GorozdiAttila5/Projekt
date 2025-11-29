document.querySelectorAll('.toast').forEach(toastEl => {
    const toast = new bootstrap.Toast(toastEl, {delay: 4000 });
    toast.show();
});
2