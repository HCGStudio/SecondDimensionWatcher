const RootComponent = {
    data() {
        return {
            remoteList: [],
            localList: [],
            selector: "remote",
            scrolledToBottom: false,
            isLoading: true,
            processing: false
        };
    },
    methods: {
        async calculateBottom() {
            const bottomOfWindow =
                Math.max(window.pageYOffset, document.documentElement.scrollTop, document.body.scrollTop) +
                    window.innerHeight >=
                    document.documentElement.offsetHeight * 0.98;
            if (bottomOfWindow === true && this.processing === false) {
                this.processing = true;
                if (this.selector !== "remote") {
                    const response = await fetch(`/api/Feed/Tracked?after=${this.localList.length}`);
                    const value = await response.json();
                    Array.prototype.push.apply(this.localList, value);
                } else {
                    const response = await fetch(`/api/Feed/UnTracked?after=${this.remoteList.length}`);
                    const value = await response.json();
                    Array.prototype.push.apply(this.remoteList, value);
                }
                this.processing = false;
            }
        },
        async setSelector(selector) {
            this.selector = selector;
            if (selector !== "remote" && this.localList.length === 0) {
                this.isLoading = true;
                const localResponse = await fetch("/api/Feed/Tracked");
                this.localList = await localResponse.json();
                this.isLoading = false;
            }
        },
        async forceReload() {
            await fetch("/api/Feed/Refresh");
            window.location.reload(false);
        }
    },
    async mounted() {
        const remoteResponse = await fetch("/api/Feed/UnTracked");
        this.remoteList = await remoteResponse.json();
        this.isLoading = false;
        await this.calculateBottom();
        window.onscroll = async () => {
            await this.calculateBottom();
        };
    }
};


const app = Vue.createApp(RootComponent);
app.component("animation-view",
    {
        data() {
            return {
                connection: null,
                info: {
                },
                progress: "width: 0%",
                isFinished: false,
                isPaused: false,
                isError: false,
                isRunning: false
            };
        },
        props: ["animation", "selector"],
        template: "#animation-view",
        methods: {
            async pause() {
                await fetch(`/api/Torrent/Pause/${encodeURIComponent(this.animation.hash)}`);
            },
            async resume() {
                await fetch(`/api/Torrent/Resume/${encodeURIComponent(this.animation.hash)}`);
            },
            canShow() {

                if (this.selector === "remote") {
                    return this.animation.isTracked === false;
                }
                if (this.selector === "local") {
                    return this.animation.isTracked === true;
                }
                if (this.selector === "downloading") {
                    return this.isRunning === true;
                }
                if (this.selector === "paused") {
                    return this.isPaused === true;
                }
                if (this.selector === "finished") {
                    return this.isFinished === true;
                }
                return false;
            },
            async beginDownload() {
                await fetch(`/api/Torrent/Download/${encodeURIComponent(this.animation.id)}`);
                this.animation.isTracked = true;
                await this.beginTrack();
            },
            printInfo() {
                console.log(this.animation.id);
            },
            async beginTrack() {
                const url = new URL(`/api/Torrent/Query/${this.animation.hash}`, window.location.href);
                url.protocol = url.protocol.replace("http", "ws");
                this.connection = new WebSocket(url.href);
                this.connection.onmessage = (event => {
                    this.info = JSON.parse(event.data)[0];
                    this.progress = `width: ${parseFloat(this.info.Progress * 100).toFixed(2)}%`;
                    this.isFinished =
                        this.info.State === "uploading" ||
                        this.info.State.includes("UP");
                    this.isPaused =
                        this.info.State === "pausedDL" ||
                        this.info.State === "checkingResumeData";
                    this.isError =
                        this.info.State === "error" ||
                        this.info.State === "unknown" ||
                        this.info.State === "missingFiles";
                    this.isRunning =
                        this.info.State === "downloading" ||
                        (this.info.State.includes("DL") && this.info.State !== "pausedDL");

                    if (this.isFinished) {
                        //Close websocket when finishing task
                        this.connection.close();
                    }
                });
            }
        },
        async mounted() {
            if (this.animation.isTracked === true) {
                await this.beginTrack();
            }
        },
        beforeUnmount() {
            if (this.connection !== null)
                this.connection.close();
        }
    });

app.component("breadcrumb-items",
    {
        props: ["name"],
        template: "#breadcrumb-items"
    });

app.component("folder-detail",
    {
        props: ["name", "parent"],
        template: "#folder-detail",
        methods: {
            async navigateToThis() {
                await this.parent.goNextRoute(this.name);
            }
        }
    });

app.component("file-detail",
    {
        props: ["name", "relativePath", "hash"],
        template: "#file-detail",
        methods: {
            playOnline() {
                window.open(
                    `/PlayBack/?hash=${this.hash}&path=${encodeURIComponent(this.relativePath + "/" + this.name)}`,
                    "_blank").focus();
            },
            downloadFile() {
                window.open(
                    `/api/Torrent/File/${this.hash}?relativePath=${encodeURIComponent(this.relativePath +
                        "/" +
                        this.name)}`,
                    "_blank");
            }
        }
    });

app.component("file-view",
    {
        data() {
            return {
                pastRoutes: [],
                currentRoute: ".",
                folders: [],
                files: []
            };
        },
        methods: {
            displayRouteName(routeName) {
                if (routeName === ".") {
                    return this.name;
                }
                return routeName;
            },
            async goNextRoute(nextRoute) {
                if (nextRoute === "..") {
                    this.currentRoute = this.pastRoutes.pop();
                } else {
                    this.pastRoutes.push(this.currentRoute);
                    this.currentRoute = nextRoute;
                }
                await this.updateData();
            },
            buildUpRoute() {
                if (this.currentRoute === ".")
                    return ".";
                return this.pastRoutes.join("/") + `/${currentRoute}`;
            },
            async updateData() {
                const folderRequest =
                    await fetch(
                        `/api/Torrent/DirectoryName/${this.hash}?relativePath=encodeURIComponent${this
                        .buildUpRoute()}`);
                const fileRequest =
                    await fetch(`/api/Torrent/FileName/${this.hash}?relativePath=${this.buildUpRoute()}`);
                if (this.currentRoute !== ".") {
                    this.folders = [".."].concat(await folderRequest.json());
                }
                this.files = await fileRequest.json();
            }
        },
        props: ["name", "hash"],
        template: "#file-view",
        async mounted() {
            await this.updateData();
        }
    });

app.mount("#app");