const fs = require('fs');

async function main() {
    const stdin = fs.readFileSync(0, 'utf-8'); // Read all from Stdin
    if (!stdin) return;

    try {
        const request = JSON.parse(stdin);
        const args = request.args || {};

        const response = {
            requestId: request.requestId,
            success: true,
            changed: true,
            data: {
                echo: args.message || "no message",
                runtime: "bun"
            }
        };

        process.stdout.write(JSON.stringify(response));
    } catch (e) {
        process.stdout.write(JSON.stringify({ success: false, error: e.toString() }));
    }
}

main();
