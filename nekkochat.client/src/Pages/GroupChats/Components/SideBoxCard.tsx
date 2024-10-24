import "../Inbox.css";

import { useState } from "react";

import { Conversation, Avatar } from '@chatscope/chat-ui-kit-react';

import avatar from "../../../assets/avatar.png";

import useGetReceiver from "../../../Hooks/useGetReceiver";

import MessageServicesClient from "../../../Utils/MessageServicesClient";
import { iChatSchema, iparticipants, iSideBoxCardProps } from "../../../Constants/Types/CommonTypes";
import FirstLetterUpperCase from "../../../Utils/FirstLetterUpperCase";

export default function SideBoxCard({
    chat,
    user,
    setCurrentConversation }: iSideBoxCardProps) {

    const [participants] = useState<iparticipants[]>([...chat.participants]);
    const [groupname] = useState<string>(chat.groupname || "Unknown");
    const [messages] = useState<iChatSchema[]>([...chat.messages]);
    const { unreadMsj } = useGetReceiver(user, messages);

    return (
        <Conversation
            key={chat._id}
            name={FirstLetterUpperCase(groupname) }
            lastSenderName={messages[messages.length - 1].username}
            info={messages[messages.length - 1].content}
            unreadCnt={unreadMsj}
            onClick={async () => {
                setCurrentConversation(chat._id);
                await MessageServicesClient.sendReadMessageGroup(parseInt(chat._id), user, groupname);
            }}>
            <Avatar
                src={avatar}
                name={participants[participants.length - 1].name}
            />
        </Conversation>
    );
}
