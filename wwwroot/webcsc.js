(function () {
    window.webcsc = {
        started: false,
        ensureStarted: async function () {
            if (this.started) return;
            await this.start();
            this.started = true;
        },
        start: async function () {
            await Blazor.start();
        },
        run: async function (code, options) {
            return await DotNet.invokeMethodAsync(
                'WebCSC', 'Run', code, options
            );
        },
        check: async function (code,options) {
            return await DotNet.invokeMethodAsync(
                'WebCSC', 'Check', code, options
            );
        },
    };
})();