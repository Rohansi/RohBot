
class CommandReader {
    private value: string;
    private offset: number;

    constructor(value: string) {
        this.value = value;
        this.offset = 0;
    }

    peek() {
        if (this.offset >= this.value.length)
            return null;

        return this.value[this.offset];
    }

    read() {
        var value = this.peek();

        if (value !== null)
            this.offset++;

        return value;
    }

    skipWhitespace() {
        while (this.peek() === " ") {
            this.read();
        }
    }

    readRemaining() {
        this.skipWhitespace();

        var word = "";

        var ch: string;
        while ((ch = this.read()) !== null) {
            word += ch;
        }

        return word;
    }

    readWord() {
        this.skipWhitespace();

        var word = "";

        if (this.peek() === '"') {
            this.read(); // skip open

            while (this.peek() !== '"') {
                if (this.peek() === null)
                    break;

                if (this.peek() === "\\") {
                    this.read(); // skip \

                    var ch = this.read();
                    switch (ch) {
                        case null:
                            break; // eof, do nothing
                        case "\\":
                            word += "\\";
                            break;
                        case '"':
                            word += '"';
                            break;
                        default:
                            word += ch;
                            break;
                    }
                }
                else {
                    word += this.read();
                }
            }

            this.read(); // skip close
        }
        else {
            while (this.peek() !== " ") {
                if (this.peek() === null)
                    break;

                word += this.read();
            }
        }

        if (word.length === 0)
            word = null;

        return word;
    }
}

interface CommandHandler {
    name: string;
    signature: string;
    handler: (chat: Chat, args: string[]) => void;
}

class CommandDispatcher {
    private handlers: { [key: string]: CommandHandler };

    constructor() {
        this.handlers = {};
    }

    register(name: string, signature: string, handler: (chat: Chat, args: string[]) => void) {
        name = name.toLocaleLowerCase();

        if (this.handlers.hasOwnProperty(name))
            return false;

        this.handlers[name] = {
            name,
            signature,
            handler
        };

        return true;
    }

    dispatch(chat: Chat, command: string, commandHeader: string = "/") {
        if (command.substr(0, commandHeader.length) !== commandHeader)
            return false;

        command = command.substr(commandHeader.length);

        var reader = new CommandReader(command);

        var name = reader.readWord();
        if (name == null)
            return false;

        name = name.toLocaleLowerCase();

        if (!this.handlers.hasOwnProperty(name))
            return false;

        var commandHandler = this.handlers[name];
        var signature = commandHandler.signature;

        var args = [];
        for (var i = 0; i < signature.length; i++) {
            if (signature[i] === "-") {
                var arg = reader.readWord();
                if (arg == null)
                    break;

                args.push(arg);
            } else if (signature[i] === "]") {
                args.push(reader.readRemaining());
                break;
            }
        }

        try {
            commandHandler.handler(chat, args);
        } catch (e) {
            console.log("exception in command handler", e);
        }

        return true;
    }
}
