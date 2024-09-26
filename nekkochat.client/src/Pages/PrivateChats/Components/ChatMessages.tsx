import "../PrivateChats.css";
import ChatSchema from "../../../Schemas/ChatSchema";
import {  ChatContainer, MessageList, Message, MessageInput, Avatar, ConversationHeader, VoiceCallButton, VideoCallButton, EllipsisButton, TypingIndicator, MessageSeparator } from '@chatscope/chat-ui-kit-react';
import avatar from "../../../assets/avatar.png";
import { useEffect, useState } from "react";
import MessageServicesClient from "../../../Utils/MessageServicesClient";
import useGetReceiver from "../../../Hooks/useGetReceiver";

/*interface incomingData {
    _id: string,
    messages: object[],
    participants: object[]
}*/

export default function ChatMessages({ messages, user, connected, chat, sender, receiver }: any) {

    const { receiverName } = useGetReceiver(user, messages);

    return (
        <ChatContainer className="flexibleContainer">

            {/*Chat Header*/}

            <ConversationHeader>
                <ConversationHeader.Back />
                <Avatar src={avatar} name="Zoe" />
                <ConversationHeader.Content userName={receiverName.toLocaleUpperCase()} info="Active 10 mins ago" />
                <ConversationHeader.Actions>
                    <VoiceCallButton />
                    <VideoCallButton />
                    <EllipsisButton orientation="vertical" />
                </ConversationHeader.Actions>
            </ConversationHeader>

            {/*Chat Component*/}

            <MessageList typingIndicator={<TypingIndicator content="Zoe is typing" />}>
                <MessageSeparator content="Saturday, 30 November 2019" />
                {messages.map((el: ChatSchema, idx: number) => {
                    return (
                        <Message key={ idx} model={{
                                message: `${el.content}`,
                                sentTime: `${el.created_at}`,
                                sender: `${el.username}`,
                                direction:  `${ el.user_id === user ? "outgoing": "incoming" }` ,
                                position: "single"
                            }}>
                            <Avatar src={avatar} name={el.username} />
                            </Message>
                    );
                })}
            </MessageList>

            {/*Box to Send Message*/}

            <MessageInput
                placeholder="Type message here"
                disabled={!connected}
                sendOnReturnDisabled={!connected}
                sendDisabled={!connected}
                onSend={async (e) => { await MessageServicesClient.sendMessageToUser(chat, sender, receiver, e) }} /> 
        </ChatContainer>
    );
}

/**export default function ChatMessages({ messages, current }: any) {
    return (
        <div className="messageContainer">
            {messages.map((el: ChatSchema, idx: number) => {
                return (
                    <div key={idx} className="message">
                        <div>{el.username}: {el.content}</div>
                        <div> -- {el.created_at} </div>
                    </div>
                );
            })}
        </div>
    );

} 

<Message model={{
                    message: "Hello my friend",
                    sentTime: "15 mins ago",
                    sender: "Patrik",
                    direction: "outgoing",
                    position: "single"
                }} avatarSpacer />
                <Message model={{
                    message: "Hello my friend",
                    sentTime: "15 mins ago",
                    sender: "Zoe",
                    direction: "incoming",
                    position: "first"
                }} avatarSpacer />
                <Message model={{
                    message: "Hello my friend",
                    sentTime: "15 mins ago",
                    sender: "Zoe",
                    direction: "incoming",
                    position: "normal"
                }} avatarSpacer />
                <Message model={{
                    message: "Hello my friend",
                    sentTime: "15 mins ago",
                    sender: "Zoe",
                    direction: "incoming",
                    position: "normal"
                }} avatarSpacer />
                <Message model={{
                    message: "Hello my friend",
                    sentTime: "15 mins ago",
                    sender: "Zoe",
                    direction: "incoming",
                    position: "last"
                }}>
                    <Avatar src={avatar} name="Zoe" />
                </Message>
                <Message model={{
                    message: "Hello my friend",
                    sentTime: "15 mins ago",
                    sender: "Patrik",
                    direction: "outgoing",
                    position: "first"
                }} />
                <Message model={{
                    message: "Hello my friend",
                    sentTime: "15 mins ago",
                    sender: "Patrik",
                    direction: "outgoing",
                    position: "normal"
                }} />
                <Message model={{
                    message: "Hello my friend",
                    sentTime: "15 mins ago",
                    sender: "Patrik",
                    direction: "outgoing",
                    position: "normal"
                }} />
                <Message model={{
                    message: "Hello my friend",
                    sentTime: "15 mins ago",
                    sender: "Patrik",
                    direction: "outgoing",
                    position: "last"
                }} />

                <Message model={{
                    message: "Hello my friend",
                    sentTime: "15 mins ago",
                    sender: "Zoe",
                    direction: "incoming",
                    position: "first"
                }} avatarSpacer />
                <Message model={{
                    message: "Hello my friend",
                    sentTime: "15 mins ago",
                    sender: "Zoe",
                    direction: "incoming",
                    position: "last"
                }}>
                    <Avatar src={avatar} name="Zoe" />
                </Message>*/