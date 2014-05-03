
class Visibility {

    private static isHidden: boolean;

    static changed: Event0 = new TypedEvent();

    static visible() {
        return !Visibility.isHidden;
    }

    static hidden() {
        return Visibility.isHidden;
    }

    private static _ctor = (() => {
        Visibility.isHidden = false;

        if (document.addEventListener) {
            window.addEventListener('focus', Visibility.onFocus, true);
            window.addEventListener('blur', Visibility.onBlur, true);
        } else {
            document.attachEvent('onfocusin', Visibility.onFocus);
            document.attachEvent('onfocusout', Visibility.onBlur);
        }
    })();

    private static onFocus(e: any) {
        var target = e.target || e.srcTarget;
        if (target != window)
            return;

        Visibility.isHidden = false;
        Visibility.changed.trigger();
    }

    private static onBlur(e: any) {
        var target = e.target || e.srcTarget;
        if (target != window)
            return;

        Visibility.isHidden = true;
        Visibility.changed.trigger();
    }

}
