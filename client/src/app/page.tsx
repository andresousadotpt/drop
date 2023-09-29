"use client";
import { io } from "socket.io-client";
import { useEffect, useRef } from "react";

declare global {
    interface Window {
        __webSocketClient: WebSocket;
    }
}

export default function Home() {
    const client = useRef<WebSocket | null>(null);

    useEffect(() => {
        if (!(window.__webSocketClient instanceof WebSocket)) {
            window.__webSocketClient = new WebSocket("ws://localhost:8006");
        } else if (
            window.__webSocketClient.readyState === WebSocket.CLOSED ||
            window.__webSocketClient.readyState === WebSocket.CLOSING
        ) {
            window.__webSocketClient = new WebSocket("ws://localhost:8006");
        }

        client.current = window.__webSocketClient;

        const message = (event: MessageEvent<any>) => {
            if (event.data == "ping") {
                console.log("veio ping");
                client.current?.send(`pong`);
            }
            console.log(event.data);
        };

        client.current.onopen = () => {
            client.current?.addEventListener("message", message);
            setInterval(() => {
                if (client.current?.readyState === WebSocket.OPEN) {
                    console.log("SENDING A MESSAGE", client.current.readyState);
                    client.current?.send(`ping`);
                }
            }, 10000);
        };

        // client.current.onopen = () => {
        //     console.log("ON OPEN CALLED ONLY ONCE")
        //     client.current?.addEventListener("message", message)

        //     setTimeout(() => {
        //       if (client.current?.readyState === WebSocket.OPEN) {
        //         console.log("SENDING A MESSAGE", client.current.readyState)
        //         client.current?.send(`Hello from client!`)
        //       }
        //     }, 5000)
        //   }

        return () => {
            console.log("CLEAN UP LISTENER!");
            client.current?.removeEventListener("message", message);
            if (client.current?.readyState === WebSocket.OPEN) {
                console.log("closing socket!");
                client.current?.close();
            }
        };
    }, []);

    return (
        <>
            <div id="output"></div>
        </>
    );
}
