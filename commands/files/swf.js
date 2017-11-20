const commando = require('discord.js-commando');

class SendSwfCommand extends commando.Command{
    constructor(client){
        super(client, {
            name: 'swf',
            group: 'files',
            memberName: 'swf',
            description: 'Send a swf'
        });
    }

    async run(message, args){
        message.say("Sent! ; )");
    }
}

module.exports=SendSwfCommand;