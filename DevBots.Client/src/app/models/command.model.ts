export class Command {
    countsAsCommand: boolean;
    type: string;
    direction: number;
    position: {
        x: number,
        y: number,
    };
    error: string;
    say: string;
    console: string;
}