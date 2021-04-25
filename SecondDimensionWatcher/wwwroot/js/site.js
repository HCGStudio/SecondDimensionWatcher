function play() {
    videojs(document.getElementById("player"), { language: "zh-Hans" });
}

function newPage(target) {
    open(target, "_blank");
}

function performSearch() {
    const search = document.getElementById("searchContent");
    if (search.value !== "")
        open(`/search/${encodeURIComponent(search.value)}`, "_blank");
}

function gotoTop() {
    window.scrollTo(0, 0);
}