function play() {
    videojs(document.getElementById("player"), { language: "zh-Hans" });
}

function newPage(target) {
    open(target, "_blank");
}