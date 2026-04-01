document.addEventListener("DOMContentLoaded", function () {
    const deleteForms = document.querySelectorAll(".delete-form");

    deleteForms.forEach(function (form) {
        form.addEventListener("submit", function (e) {
            const confirmed = confirm("Ви впевнені, що хочете видалити цей запис?");
            if (!confirmed) {
                e.preventDefault();
            }
        });
    });
});