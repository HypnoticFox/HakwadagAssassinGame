let themeMode = localStorage.getItem("theme-mode");
if (themeMode === null) {
    themeMode = "dark";
    localStorage.setItem("theme-mode", themeMode);
}

if (themeMode === "dark") {
    document.getElementById("app-loading-progress-div").classList.add("dark-mode");
}