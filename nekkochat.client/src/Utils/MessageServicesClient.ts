import axios from 'axios';
import ServerLinks from "../Constants/ServerLinks";
import MessageSchema from "../Schemas/MessageSchema";
export default class MessageServicesClient {

    public static async sendMessageToUser(chat_id: number, sender_id: number, receiver_id: number, msj: string) {

        let url = ServerLinks.getSendMessageUrl(new MessageSchema(chat_id, sender_id, receiver_id, msj, sender_id.toString(), "user", new Date("YYYY-MM-DD HH:mm:ss").toJSON()));
        const value = msj;
        const result = await axios.put(url,
            value, {
            headers: {
                'Content-Type': 'application/json'
            }
        });
        return result;
    }

    public static async getAllUsersChats(user_id: string) {

        let url = ServerLinks.getAllUsersChatUrl(user_id);

        const result = await axios.get(url).then((res) => {
            return res.data;
        }).catch(function (error) {
            // handle error
            console.log(error);
        }).finally(async function () {
            // always executed
            console.log('funcciono?');
        });
       
        return result;
    }

    public static async getChatFromUser(chat_id: string) {
        let url = ServerLinks.getOneUsersChatUrl(chat_id);

        const result = await axios.get(url).then((res) => {
            return res.data;
        }).catch(function (error) {
            // handle error
            console.log(error);
        }).finally(async function () {
            // always executed
            console.log('funcciono?');
        });

        console.log(result);

      
        return result;
    }
};