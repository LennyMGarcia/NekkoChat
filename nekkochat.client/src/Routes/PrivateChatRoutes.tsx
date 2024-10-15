import { BrowserRouter, Route, Routes } from "react-router-dom";
import Inbox from "../Pages/PrivateChats/Inbox";
import Chat from "../Pages/PrivateChats/Chat";
import Login from "../Pages/Login/Login";
import { VideoCall } from "../Pages/VideoCall/VideoCall";

export default function PrivateChatRoutes() {
    return (
        <BrowserRouter>
            <Routes>
                <Route path="/" element={<Login />} />
                <Route path="/inbox" element={<Inbox />} />
                <Route path="/chats" element={<Inbox />} />
                <Route path="/chats/chat/:chat_id" element={<Chat />} />
                <Route path="/chats/videocall/:videoId" element={<VideoCall />} />
            </Routes>
        </BrowserRouter>
    );
}
