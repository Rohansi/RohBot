
declare var Notification;

class Notifications {

    private static supported: boolean;
    private static enabled: boolean;

    private static construct = (() => {
        Notifications.supported = "Notification" in window;
        Notifications.enabled = RohStore.get("notifications-enabled") === "true";
    })();

    static enable() {
        Notifications.requestPermission();
        Notifications.enabled = true;
        RohStore.set("notifications-enabled", "true");
    }

    static disable() {
        Notifications.enabled = false;
        RohStore.set("notifications-enabled", "false");
    }

    static areEnabled() {
        return RohStore.get("notifications-enabled") === "true";
    }

    static areSupported() {
        return Notifications.supported;
    }

    static hasPermission() {
        return Notification.permission === "granted";
    }

    static requestPermission() {
        if (!Notifications.areSupported() || Notification.permission === "denied")
            return;

        Notification.requestPermission(permission => {
            if (!("permission" in Notification)) {
                Notification.permission = permission;
            }
        });
    }

    static create(title: string, body: string, clicked: () => void = null) {
        Notifications.requestPermission();

        if (!Notifications.areEnabled() || !Notifications.areSupported() || !Notifications.hasPermission())
            return;

        var notification = new Notification(title, {
            icon: "rohbot.png",
            body: body
        });

        var closeNotification = () => {
            notification.close();
        };

        notification.onclick = () => {
            closeNotification();

            if (clicked != null)
                clicked();
        };

        setTimeout(closeNotification, 3000);
    }

}
