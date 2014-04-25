
class Visibility {

    private static isHidden: boolean;

    static changed = new Signal();

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

    private static onFocus() {
        Visibility.isHidden = false;
        Visibility.changed.dispatch();
    }

    private static onBlur() {
        Visibility.isHidden = true;
        Visibility.changed.dispatch();
    }

}
