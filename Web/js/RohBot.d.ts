
interface Packet {
    Type: string;
}

interface AuthPacket extends Packet {
    Method: string;
    Username: string;
    Password: string;
    Tokens: string;
}

interface AuthResponsePacket extends Packet {
    Name: string;
    Tokens: string;
    Success: boolean;
}

interface ChatPacket extends Packet {
    Method: string;
    Name: string;
    ShortName: string;
}

interface ChatHistoryPacket extends Packet {
    ShortName: string;
    Requested: boolean;
    Lines: HistoryLine[];
    OldestLine: number;
}

interface ChatHistoryRequestPacket extends Packet {
    Target: string;
    AfterDate: number;
}

interface MessagePacket extends Packet {
    Line: HistoryLine;
}

interface PingPacket extends Packet { }

interface SendMessagePacket extends Packet {
    Target: string;
    Content: string;
}

interface SysMessagePacket extends Packet {
    Date: number;
    Content: string;
}

interface UserListUser {
    Name: string;
    UserId: string;
    Rank: string;
    Avatar: string;
    Status: string;
    Playing: string;
    Web: boolean;
    Style: string;
}

interface UserListPacket extends Packet {
    ShortName: string;
    Users: UserListUser[];
}

interface HistoryLine {
    Type: string;
    Date: number;
    Chat: string;
    Content: string;
}

interface ChatLine extends HistoryLine {
    UserType: string;
    Sender: string;
    SenderId: string;
    SenderStyle: string;
    InGame: boolean;
}

interface StateLine extends HistoryLine {
    State: string;
    For: string;
    ForId: string;
    ForType: string;
    ForStyle: string;
    By: string;
    ById: string;
    ByType: string;
    ByStyle: string;
}

// Doesn't actually exist, used internally by this client.
interface StatusLine extends HistoryLine {
    State: string;
}
