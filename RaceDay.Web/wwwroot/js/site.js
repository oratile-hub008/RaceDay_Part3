// RaceDay - small enhancements. All data still comes from the API through the server.
(function () {
    // Ask before destructive actions: <form data-confirm="Are you sure?">
    document.querySelectorAll("form[data-confirm]").forEach(function (form) {
        form.addEventListener("submit", function (event) {
            if (!window.confirm(form.getAttribute("data-confirm"))) event.preventDefault();
        });
    });

    // Block oversized or non-image files before they are sent.
    document.querySelectorAll("input[type=file][data-max-mb]").forEach(function (input) {
        input.addEventListener("change", function () {
            var file = input.files[0];
            if (!file) return;

            var maxBytes = parseFloat(input.getAttribute("data-max-mb")) * 1024 * 1024;
            if (!file.type.startsWith("image/")) {
                window.alert("Please choose an image file (JPG, PNG, GIF or WEBP).");
                input.value = "";
            } else if (file.size > maxBytes) {
                window.alert("That image is too large. Please choose one under " + input.getAttribute("data-max-mb") + " MB.");
                input.value = "";
            }
        });
    });

    // Organiser code is only needed when registering as an organiser.
    var group = document.getElementById("organiser-code-group");
    if (group) {
        var update = function () {
            var organiser = document.getElementById("role-organiser");
            group.style.display = organiser && organiser.checked ? "" : "none";
        };
        document.querySelectorAll("input[name=Role]").forEach(function (radio) {
            radio.addEventListener("change", update);
        });
        update();
    }

    // Success messages fade away on their own.
    document.querySelectorAll(".alert-success").forEach(function (alert) {
        setTimeout(function () {
            if (window.bootstrap) bootstrap.Alert.getOrCreateInstance(alert).close();
        }, 6000);
    });
})();
