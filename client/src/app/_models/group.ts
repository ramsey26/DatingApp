export interface Group {
    name: string;
    connections: Connections[];
}

interface Connections {
    ConnectionId: string;
    username: string;
}