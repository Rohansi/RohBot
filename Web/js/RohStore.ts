
class RohStore {

    static get(key: string): string {
        return RohStore.storage[key];
    }

    static set(key: string, value: string) {
        RohStore.storage[key] = value;
    }

    static remove(key: string) {
        delete RohStore.storage[key];
    }

    private static storage = (() => {
        var store = window.localStorage;
        var key = "localStorage test";

        if (store != null) {
            try {
                var data = Date.now() + "a";
                store[key] = data;
                if (data !== store[key])
                    store = null;
                if (store != null)
                    delete store[key];
            } catch (e) {
                store = null;
            }
        }

        if (store == null)
            return {};
        return store;
    })();

}
