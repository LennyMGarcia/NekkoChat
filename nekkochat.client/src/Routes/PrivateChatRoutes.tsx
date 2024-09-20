import { BrowserRouter, Route, Routes } from "react-router-dom";
import PrivateChats from "../Pages/PrivateChats/PrivateChats";
import Inbox from "../Pages/PrivateChats/Inbox";

import Chat from "../Pages/PrivateChats/Chat";

export default function PrivateChatRoutes() {
    return (
        <BrowserRouter>
            <Routes>
                <Route path="/" element={<Inbox />} />
                <Route path="/inbox" element={<Inbox />} />
                <Route path="/chats" element={<PrivateChats />} />
                <Route path="/chats/chat/:chat_id" element={<Chat />} />

            </Routes>
        </BrowserRouter>
    );
}
